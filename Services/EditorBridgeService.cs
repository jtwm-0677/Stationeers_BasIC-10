using System.Windows.Threading;
using BasicToMips.UI;
using BasicToMips.UI.Services;
using BasicToMips.Shared;

namespace BasicToMips.Services;

/// <summary>
/// Bridge service that exposes editor operations to the HTTP API.
/// All UI operations are marshaled to the UI thread via Dispatcher.
/// </summary>
public class EditorBridgeService
{
    private readonly MainWindow _mainWindow;
    private readonly Dispatcher _dispatcher;
    private readonly CompilerService _compiler;

    // Last compilation result for caching
    private CompilationResult? _lastCompilationResult;

    public EditorBridgeService(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _dispatcher = mainWindow.Dispatcher;
        _compiler = new CompilerService();
    }

    /// <summary>
    /// Get the current BASIC source code from the editor.
    /// </summary>
    public string GetCode()
    {
        return _dispatcher.Invoke(() => _mainWindow.GetEditorCode());
    }

    /// <summary>
    /// Set the BASIC source code in the editor.
    /// </summary>
    public void SetCode(string code)
    {
        _dispatcher.Invoke(() => _mainWindow.SetEditorCode(code));
    }

    /// <summary>
    /// Insert code at the current cursor position or at a specific line.
    /// </summary>
    public void InsertCode(string code, int? atLine = null)
    {
        _dispatcher.Invoke(() => _mainWindow.InsertCode(code, atLine));
    }

    /// <summary>
    /// Compile the current code and return the result.
    /// </summary>
    public CompileResponse Compile()
    {
        var basicCode = GetCode();
        var result = _compiler.Compile(basicCode);
        _lastCompilationResult = result;

        var response = new CompileResponse
        {
            Success = result.Success,
            Ic10Output = result.Output ?? "",
            LineCount = result.LineCount,
            Errors = new List<CompileError>()
        };

        if (!result.Success && result.ErrorMessage != null)
        {
            response.Errors.Add(new CompileError
            {
                Message = result.ErrorMessage,
                Line = result.ErrorLine ?? 0,
                Column = 0
            });
        }

        // Add warnings as errors with warning severity
        foreach (var warning in result.Warnings)
        {
            response.Errors.Add(new CompileError
            {
                Message = warning.Message,
                Line = warning.Line,
                Column = 0,
                Severity = "warning"
            });
        }

        // Update the IC10 output panel in the UI
        if (result.Success)
        {
            _dispatcher.Invoke(() => _mainWindow.SetIc10Output(result.Output ?? ""));
        }

        return response;
    }

    /// <summary>
    /// Get the current IC10 output from the output panel.
    /// </summary>
    public string GetIc10Output()
    {
        return _dispatcher.Invoke(() => _mainWindow.GetIc10Output());
    }

    /// <summary>
    /// Get the current symbol table from the last compilation.
    /// </summary>
    public SymbolTableResponse GetSymbols()
    {
        var basicCode = GetCode();
        var result = _compiler.Compile(basicCode);
        _lastCompilationResult = result;

        var response = new SymbolTableResponse
        {
            Variables = new List<string>(),
            Labels = new List<string>(),
            Aliases = new Dictionary<string, string>(),
            Constants = new Dictionary<string, double>(),
            Functions = new Dictionary<string, int>()
        };

        if (result.Metadata != null)
        {
            response.Variables = result.Metadata.Variables.ToList();
            response.Labels = result.Metadata.Labels.ToList();
            response.Aliases = result.Metadata.DeviceTypes;
            response.Constants = result.Metadata.Constants;
            response.Functions = result.Metadata.Functions;
        }

        return response;
    }

    /// <summary>
    /// Get compilation errors from the last compilation.
    /// </summary>
    public List<CompileError> GetErrors()
    {
        var response = Compile();
        return response.Errors;
    }

    /// <summary>
    /// Get editor cursor position.
    /// </summary>
    public CursorPosition GetCursorPosition()
    {
        return _dispatcher.Invoke(() => _mainWindow.GetCursorPosition());
    }

    /// <summary>
    /// Set editor cursor position.
    /// </summary>
    public void SetCursorPosition(int line, int column)
    {
        _dispatcher.Invoke(() => _mainWindow.SetCursorPosition(line, column));
    }

    // ==================== TAB MANAGEMENT API ====================

    /// <summary>
    /// Create a new tab.
    /// </summary>
    public int CreateNewTab(string? name = null)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiCreateNewTab(name));
    }

    /// <summary>
    /// Get list of all open tabs.
    /// </summary>
    public List<BasicToMips.UI.TabInfo> GetTabs()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetTabs());
    }

    /// <summary>
    /// Switch to a specific tab by index.
    /// </summary>
    public bool SwitchTab(int tabIndex)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSwitchTab(tabIndex));
    }

    /// <summary>
    /// Switch to a specific tab by name.
    /// </summary>
    public bool SwitchTabByName(string name)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSwitchTabByName(name));
    }

    /// <summary>
    /// Close a specific tab by index.
    /// </summary>
    /// <param name="tabIndex">Index of the tab to close</param>
    /// <param name="force">If true, close without prompting to save unsaved changes</param>
    public bool CloseTab(int tabIndex, bool force = false)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiCloseTab(tabIndex, force));
    }

    // ==================== SCRIPT SAVE/LOAD API ====================

    /// <summary>
    /// Save the current script to a folder by name.
    /// </summary>
    public BasicToMips.UI.SaveScriptResult SaveScript(string scriptName)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSaveScript(scriptName));
    }

    /// <summary>
    /// Load a script from a folder by name.
    /// </summary>
    public BasicToMips.UI.LoadScriptResult LoadScript(string scriptName, bool newTab = false)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiLoadScript(scriptName, newTab));
    }

    /// <summary>
    /// List all available scripts in the scripts folder.
    /// </summary>
    public List<BasicToMips.UI.ScriptInfo> ListScripts()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiListScripts());
    }

    // ==================== SIMULATOR API ====================

    /// <summary>
    /// Start/initialize the simulator with IC10 code.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorStart(string? ic10Code = null)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorStart(ic10Code));
    }

    /// <summary>
    /// Stop the simulator.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorStop()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorStop());
    }

    /// <summary>
    /// Reset the simulator.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorReset()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorReset());
    }

    /// <summary>
    /// Step one instruction.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorStep()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorStep());
    }

    /// <summary>
    /// Run until breakpoint or halt.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorRun(int maxInstructions = 10000)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorRun(maxInstructions));
    }

    /// <summary>
    /// Get current simulator state.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorGetState()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorGetState());
    }

    /// <summary>
    /// Set a register value.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorSetRegister(string register, double value)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorSetRegister(register, value));
    }

    /// <summary>
    /// Add a breakpoint.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorAddBreakpoint(int line)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorAddBreakpoint(line));
    }

    /// <summary>
    /// Remove a breakpoint.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorRemoveBreakpoint(int line)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorRemoveBreakpoint(line));
    }

    /// <summary>
    /// Clear all breakpoints.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorClearBreakpoints()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorClearBreakpoints());
    }

    /// <summary>
    /// Set a device property.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorSetDeviceProperty(int deviceIndex, string property, double value)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorSetDeviceProperty(deviceIndex, property, value));
    }

    /// <summary>
    /// Get a device property.
    /// </summary>
    public double SimulatorGetDeviceProperty(int deviceIndex, string property)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorGetDeviceProperty(deviceIndex, property));
    }

    /// <summary>
    /// Set a device slot property.
    /// </summary>
    public BasicToMips.UI.SimulatorStateResult SimulatorSetDeviceSlotProperty(int deviceIndex, int slot, string property, double value)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiSimulatorSetDeviceSlotProperty(deviceIndex, slot, property, value));
    }

    // ==================== DEBUGGING API ====================

    /// <summary>
    /// Add a watch expression.
    /// </summary>
    public BasicToMips.UI.WatchInfo AddWatch(string expression)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiAddWatch(expression));
    }

    /// <summary>
    /// Remove a watch by name.
    /// </summary>
    public bool RemoveWatch(string name)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiRemoveWatch(name));
    }

    /// <summary>
    /// Get all watch values.
    /// </summary>
    public List<BasicToMips.UI.WatchInfo> GetWatches()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetWatches());
    }

    /// <summary>
    /// Clear all watches.
    /// </summary>
    public void ClearWatches()
    {
        _dispatcher.Invoke(() => _mainWindow.ApiClearWatches());
    }

    /// <summary>
    /// Add a BASIC editor breakpoint.
    /// </summary>
    public List<int> AddEditorBreakpoint(int line)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiAddBreakpoint(line));
    }

    /// <summary>
    /// Remove a BASIC editor breakpoint.
    /// </summary>
    public List<int> RemoveEditorBreakpoint(int line)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiRemoveBreakpoint(line));
    }

    /// <summary>
    /// Toggle a BASIC editor breakpoint.
    /// </summary>
    public BasicToMips.UI.BreakpointToggleResult ToggleEditorBreakpoint(int line)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiToggleBreakpoint(line));
    }

    /// <summary>
    /// Get all BASIC editor breakpoints.
    /// </summary>
    public List<int> GetEditorBreakpoints()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetBreakpoints());
    }

    /// <summary>
    /// Clear all BASIC editor breakpoints.
    /// </summary>
    public void ClearEditorBreakpoints()
    {
        _dispatcher.Invoke(() => _mainWindow.ApiClearBreakpoints());
    }

    /// <summary>
    /// Get the source map.
    /// </summary>
    public BasicToMips.UI.SourceMapInfo GetSourceMap()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetSourceMap());
    }

    /// <summary>
    /// Get compilation errors and warnings.
    /// </summary>
    public List<BasicToMips.UI.ErrorInfo> GetCompilationErrors()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetErrors());
    }

    /// <summary>
    /// Navigate to a specific line.
    /// </summary>
    public void GoToLine(int line)
    {
        _dispatcher.Invoke(() => _mainWindow.ApiGoToLine(line));
    }

    // ==================== SETTINGS API ====================

    /// <summary>
    /// Get all current settings.
    /// </summary>
    public BasicToMips.UI.SettingsSnapshot GetSettings()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetSettings());
    }

    /// <summary>
    /// Update a specific setting.
    /// </summary>
    public BasicToMips.UI.SettingsUpdateResult UpdateSetting(string name, object value)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiUpdateSetting(name, value));
    }

    // ==================== CODE ANALYSIS API ====================

    /// <summary>
    /// Find all references to a symbol.
    /// </summary>
    public List<BasicToMips.UI.SymbolReference> FindReferences(string symbolName)
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiFindReferences(symbolName));
    }

    /// <summary>
    /// Get code metrics.
    /// </summary>
    public BasicToMips.UI.CodeMetrics GetCodeMetrics()
    {
        return _dispatcher.Invoke(() => _mainWindow.ApiGetCodeMetrics());
    }
}

/// <summary>
/// Response from compilation operation.
/// </summary>
public class CompileResponse
{
    public bool Success { get; set; }
    public string Ic10Output { get; set; } = "";
    public int LineCount { get; set; }
    public List<CompileError> Errors { get; set; } = new();
}

/// <summary>
/// A compilation error or warning.
/// </summary>
public class CompileError
{
    public string Message { get; set; } = "";
    public int Line { get; set; }
    public int Column { get; set; }
    public string Severity { get; set; } = "error";
}

/// <summary>
/// Symbol table response containing all defined symbols.
/// </summary>
public class SymbolTableResponse
{
    public List<string> Variables { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public Dictionary<string, string> Aliases { get; set; } = new();
    public Dictionary<string, double> Constants { get; set; } = new();
    public Dictionary<string, int> Functions { get; set; } = new();
}

/// <summary>
/// Cursor position in the editor.
/// </summary>
public class CursorPosition
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Offset { get; set; }
}
