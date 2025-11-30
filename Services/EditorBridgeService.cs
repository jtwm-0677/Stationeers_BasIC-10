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
