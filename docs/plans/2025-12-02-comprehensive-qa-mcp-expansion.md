# Comprehensive QA & MCP Expansion Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix all critical compiler bugs and add comprehensive MCP tooling for full QA automation of the Basic-10 compiler.

**Architecture:** Three-phase approach: (1) Fix critical compiler bugs, (2) Add MCP tools for simulator/debugging, (3) Add MCP tools for editor state/settings. Each MCP tool follows the pattern: MainWindow API method -> EditorBridgeService wrapper -> HttpApiServer endpoint -> MCP tool definition + handler.

**Tech Stack:** .NET 8.0, WPF, ASP.NET Core Minimal APIs, MCP (Model Context Protocol)

---

## Phase 1: Critical Bug Fixes

### Task 1.1: Fix 128 Line Limit Warning

**Files:**
- Modify: `src/CodeGen/MipsGenerator.cs`
- Modify: `src/CodeGen/CodeEmitter.cs`

**Step 1: Add line count validation to MipsGenerator**

In `MipsGenerator.cs`, after code generation completes, add validation:

```csharp
// At the end of the Generate() method, before returning:
if (_emitter.LineCount > 128)
{
    AddError(new CompilerError
    {
        Message = $"IC10 code exceeds 128 line limit ({_emitter.LineCount} lines). Code beyond line 128 will be ignored in-game.",
        Line = 0,
        Column = 0,
        Severity = ErrorSeverity.Error
    });
}
```

**Step 2: Verify by compiling a >128 line script**

Expected: Compilation error about line limit.

---

### Task 1.2: Add XOR Keyword Alias

**Files:**
- Modify: `src/Lexer/Lexer.cs` (line ~85)

**Step 1: Add XOR as alias for BXOR**

In the `_keywords` dictionary around line 85, add:

```csharp
["XOR"] = TokenType.BitXor,  // Alias for BXOR
```

**Step 2: Verify compilation**

```basic
var x = 5 XOR 3
```

Expected: Generates `xor rX 5 3`

---

### Task 1.3: Fix Slot Access Syntax (.Slot[n].Property)

**Files:**
- Modify: `src/Parser/Parser.cs` (around line 1730-1770)

**Step 1: Add .Slot[n] syntax handling in ParseDeviceAccess**

After detecting a device identifier followed by `.`, check if the property is "Slot":

```csharp
// After parsing device name and checking for '.'
if (propertyName.Equals("Slot", StringComparison.OrdinalIgnoreCase))
{
    // Expect [index]
    Expect(TokenType.LeftBracket, "Expected '[' after .Slot");
    var slotIndex = ParseExpression();
    Expect(TokenType.RightBracket, "Expected ']'");

    // Now expect .PropertyName
    Expect(TokenType.Dot, "Expected '.' after slot index");
    var slotPropToken = ExpectPropertyName("Expected slot property name");

    return new DeviceSlotReadExpression
    {
        Line = token.Line,
        Column = token.Column,
        DeviceName = deviceName,
        SlotIndex = slotIndex,
        PropertyName = slotPropToken.Value
    };
}
```

**Step 2: Verify compilation**

```basic
var hash = d0.Slot[0].OccupantHash
```

Expected: Generates `ls rX d0 0 OccupantHash`

---

### Task 1.4: Fix CASE ELSE in SELECT

**Files:**
- Modify: `src/Parser/Parser.cs` (ParseSelectStatement)

**Step 1: Find and fix CASE ELSE handling**

Search for `ParseSelectStatement` and ensure `CASE ELSE` is treated as the default case, not as `CASE` followed by `ELSE` keyword.

---

### Task 1.5: Add .Count Batch Mode

**Files:**
- Modify: `src/AST/AstNode.cs` (BatchMode enum)
- Modify: `src/Parser/Parser.cs` (ParseBatchRead)
- Modify: `src/CodeGen/MipsGenerator.cs` (GenerateBatchRead)

**Step 1: Add Count to BatchMode enum**

```csharp
public enum BatchMode
{
    Average = 0,
    Sum = 1,
    Minimum = 2,
    Maximum = 3,
    Count = 4  // Add this
}
```

**Step 2: Handle Count in parser**

**Step 3: Generate code (uses `lbn` instruction or alternative)**

---

## Phase 2: MCP Tools - Simulator Control

### Task 2.1: Add Simulator API to MainWindow

**Files:**
- Modify: `UI/MainWindow.xaml.cs`

**Add these public methods:**

```csharp
// ==================== SIMULATOR API ====================

private SimulatorWindow? _simulatorWindow;
private IC10Simulator? _activeSimulator;

public SimulatorStartResult ApiStartSimulator()
{
    try
    {
        var ic10Code = _lastIc10Output;
        if (string.IsNullOrEmpty(ic10Code))
        {
            // Compile first
            Compile();
            ic10Code = _lastIc10Output;
        }

        if (_simulatorWindow == null || !_simulatorWindow.IsLoaded)
        {
            _simulatorWindow = new SimulatorWindow(ic10Code);
            _simulatorWindow.Show();
        }

        _activeSimulator = _simulatorWindow.GetSimulator();
        _activeSimulator.LoadProgram(ic10Code);

        return new SimulatorStartResult { Success = true, Message = "Simulator started" };
    }
    catch (Exception ex)
    {
        return new SimulatorStartResult { Success = false, Error = ex.Message };
    }
}

public SimulatorStateResult ApiGetSimulatorState()
{
    if (_activeSimulator == null)
        return new SimulatorStateResult { Success = false, Error = "Simulator not running" };

    return new SimulatorStateResult
    {
        Success = true,
        ProgramCounter = _activeSimulator.ProgramCounter,
        IsRunning = _activeSimulator.IsRunning,
        IsPaused = _activeSimulator.IsPaused,
        IsHalted = _activeSimulator.IsHalted,
        InstructionCount = _activeSimulator.InstructionCount,
        Registers = _activeSimulator.Registers.ToArray(),
        StackPointer = _activeSimulator.StackPointer,
        ErrorMessage = _activeSimulator.ErrorMessage
    };
}

public bool ApiSimulatorStep(int count = 1)
{
    if (_activeSimulator == null) return false;
    for (int i = 0; i < count; i++)
    {
        if (!_activeSimulator.Step()) break;
    }
    return true;
}

public bool ApiSimulatorRun()
{
    if (_activeSimulator == null) return false;
    _activeSimulator.Run();
    return true;
}

public bool ApiSimulatorPause()
{
    if (_activeSimulator == null) return false;
    _activeSimulator.Pause();
    return true;
}

public bool ApiSimulatorReset()
{
    if (_activeSimulator == null) return false;
    _activeSimulator.Reset();
    return true;
}

public bool ApiSimulatorStop()
{
    if (_simulatorWindow != null)
    {
        _simulatorWindow.Close();
        _simulatorWindow = null;
    }
    _activeSimulator = null;
    return true;
}

public List<double> ApiGetSimulatorStack()
{
    if (_activeSimulator == null) return new List<double>();
    return _activeSimulator.Stack.Take(_activeSimulator.StackPointer).ToList();
}

public bool ApiSetSimulatorRegister(int regNum, double value)
{
    if (_activeSimulator == null || regNum < 0 || regNum >= 18) return false;
    _activeSimulator.Registers[regNum] = value;
    return true;
}
```

---

### Task 2.2: Add Simulator HTTP Endpoints

**Files:**
- Modify: `Services/HttpApiServer.cs`

**Add these endpoints:**

```csharp
// === Simulator ===

app.MapPost("/api/simulator/start", () =>
{
    var result = _bridge.StartSimulator();
    return Results.Ok(result);
});

app.MapGet("/api/simulator/state", () =>
{
    var result = _bridge.GetSimulatorState();
    return Results.Ok(result);
});

app.MapPost("/api/simulator/step", async (HttpContext ctx) =>
{
    var body = await JsonSerializer.DeserializeAsync<StepRequest>(ctx.Request.Body, jsonOptions);
    var success = _bridge.SimulatorStep(body?.Count ?? 1);
    return Results.Ok(new { success });
});

app.MapPost("/api/simulator/run", () =>
{
    var success = _bridge.SimulatorRun();
    return Results.Ok(new { success });
});

app.MapPost("/api/simulator/pause", () =>
{
    var success = _bridge.SimulatorPause();
    return Results.Ok(new { success });
});

app.MapPost("/api/simulator/reset", () =>
{
    var success = _bridge.SimulatorReset();
    return Results.Ok(new { success });
});

app.MapPost("/api/simulator/stop", () =>
{
    var success = _bridge.SimulatorStop();
    return Results.Ok(new { success });
});

app.MapGet("/api/simulator/stack", () =>
{
    var stack = _bridge.GetSimulatorStack();
    return Results.Ok(new { stack, count = stack.Count });
});

app.MapPost("/api/simulator/register", async (HttpContext ctx) =>
{
    var body = await JsonSerializer.DeserializeAsync<SetRegisterRequest>(ctx.Request.Body, jsonOptions);
    if (body == null) return Results.BadRequest(new { error = "Invalid request" });
    var success = _bridge.SetSimulatorRegister(body.Register, body.Value);
    return Results.Ok(new { success });
});
```

---

### Task 2.3: Add Simulator MCP Tools

**Files:**
- Modify: `Basic10.Mcp/McpServer.cs`
- Modify: `Basic10.Mcp/HttpBridge.cs`

**Add these tool definitions:**

```csharp
new
{
    name = "basic10_sim_start",
    description = "Start the IC10 simulator with the current compiled code",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_state",
    description = "Get the current simulator state: registers, PC, running status, etc.",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_step",
    description = "Execute one or more IC10 instructions",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            count = new { type = "integer", description = "Number of instructions to execute (default 1)" }
        },
        required = Array.Empty<string>()
    }
},
new
{
    name = "basic10_sim_run",
    description = "Start continuous execution of the simulator",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_pause",
    description = "Pause simulator execution",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_reset",
    description = "Reset the simulator to initial state",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_stop",
    description = "Stop and close the simulator",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_get_stack",
    description = "Get the current stack contents",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_sim_set_register",
    description = "Set a register value in the simulator",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            register = new { type = "integer", description = "Register number (0-15 for r0-r15, 16 for sp, 17 for ra)" },
            value = new { type = "number", description = "Value to set" }
        },
        required = new[] { "register", "value" }
    }
}
```

---

## Phase 3: MCP Tools - Simulated Devices

### Task 3.1: Add Simulated Device API

**Files:**
- Modify: `UI/MainWindow.xaml.cs`
- Modify: `Simulator/IC10Simulator.cs`

**Add methods to configure simulated devices:**

```csharp
public SimDeviceResult ApiAddSimulatedDevice(int pin, string deviceType, string? name = null)
{
    if (_activeSimulator == null)
        return new SimDeviceResult { Success = false, Error = "Simulator not running" };

    if (pin < 0 || pin >= 6)
        return new SimDeviceResult { Success = false, Error = "Invalid pin (0-5)" };

    var device = _activeSimulator.Devices[pin];
    device.TypeHash = DeviceDatabase.GetHashByName(deviceType);
    device.Name = name ?? deviceType;
    device.IsConnected = true;

    return new SimDeviceResult { Success = true, DevicePin = pin, DeviceType = deviceType };
}

public bool ApiSetSimDeviceProperty(int pin, string property, double value)
{
    if (_activeSimulator == null || pin < 0 || pin >= 6) return false;
    _activeSimulator.Devices[pin].SetProperty(property, value);
    return true;
}

public double? ApiGetSimDeviceProperty(int pin, string property)
{
    if (_activeSimulator == null || pin < 0 || pin >= 6) return null;
    return _activeSimulator.Devices[pin].GetProperty(property);
}

public List<SimDeviceInfo> ApiListSimulatedDevices()
{
    if (_activeSimulator == null) return new List<SimDeviceInfo>();

    return _activeSimulator.Devices.Select((d, i) => new SimDeviceInfo
    {
        Pin = i,
        Name = d.Name,
        TypeHash = d.TypeHash,
        IsConnected = d.IsConnected
    }).ToList();
}
```

---

### Task 3.2: Add Simulated Device MCP Tools

**Add tools:**

```csharp
new
{
    name = "basic10_sim_add_device",
    description = "Add a simulated device to a pin",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            pin = new { type = "integer", description = "Device pin (0-5 for d0-d5)" },
            deviceType = new { type = "string", description = "Device type name or hash" },
            name = new { type = "string", description = "Optional device name" }
        },
        required = new[] { "pin", "deviceType" }
    }
},
new
{
    name = "basic10_sim_set_device_property",
    description = "Set a property value on a simulated device",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            pin = new { type = "integer", description = "Device pin (0-5)" },
            property = new { type = "string", description = "Property name (e.g., Temperature, Pressure)" },
            value = new { type = "number", description = "Value to set" }
        },
        required = new[] { "pin", "property", "value" }
    }
},
new
{
    name = "basic10_sim_get_device_property",
    description = "Read a property value from a simulated device",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            pin = new { type = "integer", description = "Device pin (0-5)" },
            property = new { type = "string", description = "Property name" }
        },
        required = new[] { "pin", "property" }
    }
},
new
{
    name = "basic10_sim_list_devices",
    description = "List all simulated devices and their status",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
}
```

---

## Phase 4: MCP Tools - Debugging (Breakpoints & Watch)

### Task 4.1: Add Breakpoint API

**Files:**
- Modify: `UI/MainWindow.xaml.cs`

```csharp
public bool ApiSetBreakpoint(int line)
{
    if (_activeSimulator == null) return false;
    _activeSimulator.Breakpoints.Add(line);
    return true;
}

public bool ApiClearBreakpoint(int line)
{
    if (_activeSimulator == null) return false;
    return _activeSimulator.Breakpoints.Remove(line);
}

public List<int> ApiGetBreakpoints()
{
    if (_activeSimulator == null) return new List<int>();
    return _activeSimulator.Breakpoints.ToList();
}

public bool ApiClearAllBreakpoints()
{
    if (_activeSimulator == null) return false;
    _activeSimulator.Breakpoints.Clear();
    return true;
}
```

---

### Task 4.2: Add Watch API

**Files:**
- Modify: `UI/MainWindow.xaml.cs`

```csharp
private WatchWindow? _watchWindow;
private WatchManager? _watchManager;

public int ApiAddWatch(string expression)
{
    _watchManager ??= new WatchManager();
    return _watchManager.AddWatch(expression);
}

public bool ApiRemoveWatch(int watchId)
{
    return _watchManager?.RemoveWatch(watchId) ?? false;
}

public List<WatchInfo> ApiGetWatches()
{
    if (_watchManager == null) return new List<WatchInfo>();
    // Evaluate each watch against current simulator state
    return _watchManager.WatchItems.Select(w => new WatchInfo
    {
        Id = w.Id,
        Expression = w.Name,
        Value = EvaluateWatchExpression(w.Name),
        Type = w.Type.ToString()
    }).ToList();
}
```

---

### Task 4.3: Add Debugging MCP Tools

```csharp
new
{
    name = "basic10_debug_set_breakpoint",
    description = "Set a breakpoint on an IC10 line",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            line = new { type = "integer", description = "Line number (0-based)" }
        },
        required = new[] { "line" }
    }
},
new
{
    name = "basic10_debug_clear_breakpoint",
    description = "Clear a breakpoint from an IC10 line",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            line = new { type = "integer", description = "Line number" }
        },
        required = new[] { "line" }
    }
},
new
{
    name = "basic10_debug_get_breakpoints",
    description = "List all set breakpoints",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_debug_add_watch",
    description = "Add an expression to the watch list",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            expression = new { type = "string", description = "Expression to watch (e.g., r0, sp, variableName)" }
        },
        required = new[] { "expression" }
    }
},
new
{
    name = "basic10_debug_get_watches",
    description = "Get all watch expressions with their current values",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_debug_get_variables",
    description = "Get all declared BASIC variables with their mapped registers and current values",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
}
```

---

## Phase 5: MCP Tools - Editor State & Errors

### Task 5.1: Add Editor State API

**Files:**
- Modify: `UI/MainWindow.xaml.cs`

```csharp
public CursorPosition ApiGetCursorPosition()
{
    return new CursorPosition
    {
        Line = BasicEditor.TextArea.Caret.Line,
        Column = BasicEditor.TextArea.Caret.Column
    };
}

public bool ApiSetCursorPosition(int line, int column)
{
    SetCursorPosition(line, column);
    return true;
}

public SelectionInfo ApiGetSelection()
{
    var selection = BasicEditor.TextArea.Selection;
    if (selection.IsEmpty)
    {
        return new SelectionInfo { HasSelection = false };
    }

    return new SelectionInfo
    {
        HasSelection = true,
        StartLine = selection.StartPosition.Line,
        StartColumn = selection.StartPosition.Column,
        EndLine = selection.EndPosition.Line,
        EndColumn = selection.EndPosition.Column,
        SelectedText = BasicEditor.SelectedText
    };
}

public List<CompilerErrorInfo> ApiGetErrors()
{
    return _lastErrors.Select(e => new CompilerErrorInfo
    {
        Message = e.Message,
        Line = e.Line,
        Column = e.Column,
        Severity = e.Severity.ToString()
    }).ToList();
}

public List<CompilerErrorInfo> ApiGetWarnings()
{
    return _lastErrors.Where(e => e.Severity == ErrorSeverity.Warning)
        .Select(e => new CompilerErrorInfo
        {
            Message = e.Message,
            Line = e.Line,
            Column = e.Column,
            Severity = "Warning"
        }).ToList();
}
```

---

### Task 5.2: Add Editor State MCP Tools

```csharp
new
{
    name = "basic10_editor_get_cursor",
    description = "Get current cursor position in the BASIC editor",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_editor_set_cursor",
    description = "Set cursor position in the BASIC editor",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            line = new { type = "integer", description = "Line number (1-based)" },
            column = new { type = "integer", description = "Column number (1-based)" }
        },
        required = new[] { "line", "column" }
    }
},
new
{
    name = "basic10_editor_get_selection",
    description = "Get current text selection in the editor",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_editor_get_errors",
    description = "Get all compilation errors from the last compile",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_editor_get_warnings",
    description = "Get all compilation warnings from the last compile",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
}
```

---

## Phase 6: MCP Tools - Settings & Theme

### Task 6.1: Add Settings API

**Files:**
- Modify: `UI/MainWindow.xaml.cs`

```csharp
public Dictionary<string, object> ApiGetSettings()
{
    return new Dictionary<string, object>
    {
        ["theme"] = _settings.Theme,
        ["fontSize"] = _settings.FontSize,
        ["fontFamily"] = _settings.FontFamily,
        ["autoCompile"] = _settings.AutoCompile,
        ["autoSave"] = _settings.AutoSave,
        ["autoSaveInterval"] = _settings.AutoSaveIntervalSeconds,
        ["showLineNumbers"] = _settings.ShowLineNumbers,
        ["wordWrap"] = _settings.WordWrap,
        ["scriptsFolder"] = _settings.ScriptsFolder,
        ["httpApiEnabled"] = _settings.HttpApiEnabled,
        ["httpApiPort"] = _settings.HttpApiPort,
        ["retro_scanlines"] = _settings.RetroScanlines,
        ["retro_glow"] = _settings.RetroGlow,
        ["retro_blockCursor"] = _settings.RetroBlockCursor
    };
}

public bool ApiSetSetting(string key, object value)
{
    switch (key.ToLowerInvariant())
    {
        case "theme":
            _settings.Theme = value.ToString() ?? "Dark";
            ApplyTheme();
            break;
        case "fontsize":
            _settings.FontSize = Convert.ToInt32(value);
            BasicEditor.FontSize = _settings.FontSize;
            break;
        // ... handle other settings
        default:
            return false;
    }
    _settings.Save();
    return true;
}

public string ApiGetTheme()
{
    return _settings.Theme;
}

public bool ApiSetTheme(string themeName)
{
    _settings.Theme = themeName;
    ApplyTheme();
    _settings.Save();
    return true;
}
```

---

### Task 6.2: Add Settings MCP Tools

```csharp
new
{
    name = "basic10_get_settings",
    description = "Get all application settings",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_set_setting",
    description = "Change an application setting",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            key = new { type = "string", description = "Setting name (theme, fontSize, autoCompile, etc.)" },
            value = new { type = "string", description = "New value for the setting" }
        },
        required = new[] { "key", "value" }
    }
},
new
{
    name = "basic10_get_theme",
    description = "Get current theme name",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_set_theme",
    description = "Switch application theme",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            theme = new { type = "string", description = "Theme name (Light, Dark)" }
        },
        required = new[] { "theme" }
    }
}
```

---

## Phase 7: MCP Tools - Code Analysis & Testing

### Task 7.1: Add Code Analysis API

```csharp
public CodeAnalysisResult ApiAnalyzeCode()
{
    return new CodeAnalysisResult
    {
        SourceLineCount = BasicEditor.LineCount,
        CompiledLineCount = _emitter?.LineCount ?? 0,
        VariableCount = _symbolTable?.Variables.Count ?? 0,
        ConstantCount = _symbolTable?.Constants.Count ?? 0,
        LabelCount = _symbolTable?.Labels.Count ?? 0,
        AliasCount = _symbolTable?.Aliases.Count ?? 0,
        FunctionCount = _symbolTable?.Functions.Count ?? 0,
        ArrayCount = _symbolTable?.Arrays.Count ?? 0,
        MaxLoopDepth = CalculateMaxLoopDepth(),
        HasErrors = _lastErrors.Any(e => e.Severity == ErrorSeverity.Error),
        ErrorCount = _lastErrors.Count(e => e.Severity == ErrorSeverity.Error),
        WarningCount = _lastErrors.Count(e => e.Severity == ErrorSeverity.Warning)
    };
}

public string ApiGetIc10Output()
{
    return _lastIc10Output ?? "";
}

public CompareResult ApiCompareOutput(string expectedIc10)
{
    var actual = _lastIc10Output ?? "";
    var actualLines = actual.Split('\n').Select(l => l.Trim()).ToArray();
    var expectedLines = expectedIc10.Split('\n').Select(l => l.Trim()).ToArray();

    var differences = new List<string>();
    var maxLines = Math.Max(actualLines.Length, expectedLines.Length);

    for (int i = 0; i < maxLines; i++)
    {
        var actualLine = i < actualLines.Length ? actualLines[i] : "";
        var expectedLine = i < expectedLines.Length ? expectedLines[i] : "";

        if (actualLine != expectedLine)
        {
            differences.Add($"Line {i + 1}: Expected '{expectedLine}' but got '{actualLine}'");
        }
    }

    return new CompareResult
    {
        Match = differences.Count == 0,
        Differences = differences
    };
}
```

---

### Task 7.2: Add Code Analysis MCP Tools

```csharp
new
{
    name = "basic10_analyze_code",
    description = "Analyze current code and return metrics (line counts, symbol counts, etc.)",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_get_ic10",
    description = "Get the last compiled IC10 output",
    inputSchema = new { type = "object", properties = new { }, required = Array.Empty<string>() }
},
new
{
    name = "basic10_compare_output",
    description = "Compare compiled IC10 against expected output",
    inputSchema = new
    {
        type = "object",
        properties = new
        {
            expected = new { type = "string", description = "Expected IC10 code to compare against" }
        },
        required = new[] { "expected" }
    }
}
```

---

## Summary of All New MCP Tools

### Simulator Control (Phase 2-3)
1. `basic10_sim_start` - Start simulator
2. `basic10_sim_state` - Get simulator state (registers, PC, etc.)
3. `basic10_sim_step` - Execute instructions
4. `basic10_sim_run` - Continuous execution
5. `basic10_sim_pause` - Pause execution
6. `basic10_sim_reset` - Reset state
7. `basic10_sim_stop` - Stop and close
8. `basic10_sim_get_stack` - Get stack contents
9. `basic10_sim_set_register` - Set register value
10. `basic10_sim_add_device` - Add simulated device
11. `basic10_sim_set_device_property` - Set device property
12. `basic10_sim_get_device_property` - Get device property
13. `basic10_sim_list_devices` - List devices

### Debugging (Phase 4)
14. `basic10_debug_set_breakpoint` - Set breakpoint
15. `basic10_debug_clear_breakpoint` - Clear breakpoint
16. `basic10_debug_get_breakpoints` - List breakpoints
17. `basic10_debug_add_watch` - Add watch expression
18. `basic10_debug_get_watches` - Get watch values
19. `basic10_debug_get_variables` - Get BASIC variables

### Editor State (Phase 5)
20. `basic10_editor_get_cursor` - Get cursor position
21. `basic10_editor_set_cursor` - Set cursor position
22. `basic10_editor_get_selection` - Get selection info
23. `basic10_editor_get_errors` - Get compilation errors
24. `basic10_editor_get_warnings` - Get warnings

### Settings (Phase 6)
25. `basic10_get_settings` - Get all settings
26. `basic10_set_setting` - Change a setting
27. `basic10_get_theme` - Get current theme
28. `basic10_set_theme` - Change theme

### Code Analysis (Phase 7)
29. `basic10_analyze_code` - Get code metrics
30. `basic10_get_ic10` - Get compiled IC10
31. `basic10_compare_output` - Compare against expected

---

## Implementation Order

**Day 1: Bug Fixes**
- Task 1.1: 128 line limit (30 min)
- Task 1.2: XOR keyword (5 min)
- Task 1.3: .Slot[n] syntax (1 hour)
- Task 1.4: CASE ELSE (30 min)
- Task 1.5: .Count batch mode (30 min)

**Day 2: Simulator MCP**
- Task 2.1-2.3: Simulator control (2-3 hours)
- Task 3.1-3.2: Simulated devices (1-2 hours)

**Day 3: Debugging MCP**
- Task 4.1-4.3: Breakpoints & Watch (2 hours)
- Task 5.1-5.2: Editor state (1 hour)

**Day 4: Settings & Analysis MCP**
- Task 6.1-6.2: Settings (1 hour)
- Task 7.1-7.2: Code analysis (1 hour)
- Integration testing

---

**Plan complete and saved to `docs/plans/2025-12-02-comprehensive-qa-mcp-expansion.md`.**

**Two execution options:**

**1. Subagent-Driven (this session)** - I dispatch fresh subagent per task, review between tasks, fast iteration

**2. Parallel Session (separate)** - Open new session with executing-plans, batch execution with checkpoints

**Which approach?**
