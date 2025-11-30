# MipsGenerator Rewrite + UI Menu Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Rewrite the IC10 code generator to produce efficient, minimal output that stays under the 128-line limit (like the proven compiler), and fix light theme menu text visibility.

**Architecture:** Replace the current register-per-expression approach with a smart allocator that reuses registers, uses inline literals, leverages stack for spilling, and generates optimal IC10 instructions. The proven compiler achieves ~110 lines for a 190-line output from our compiler.

**Tech Stack:** C# .NET 8.0, WPF, AvalonEdit

---

## Part 1: UI Fix (Quick Win)

### Task 1: Fix Light Theme Menu Dropdown Text Color

**Files:**
- Modify: `UI/Themes/LightTheme.xaml:149-161`

**Problem:** Menu dropdown items have light-colored text on white background. The current MenuItem style sets `Foreground="#1E1E1E"` but WPF menu dropdowns use a popup that may not inherit these styles properly.

**Step 1: Add complete MenuItem control template with explicit popup styling**

Replace lines 149-161 in `LightTheme.xaml`:

```xml
<!-- MenuItem Style with explicit popup background -->
<Style TargetType="MenuItem">
    <Setter Property="Foreground" Value="#1E1E1E"/>
    <Setter Property="Background" Value="Transparent"/>
    <Style.Triggers>
        <Trigger Property="IsHighlighted" Value="True">
            <Setter Property="Background" Value="#D4D4D4"/>
            <Setter Property="Foreground" Value="#1E1E1E"/>
        </Trigger>
        <Trigger Property="IsEnabled" Value="False">
            <Setter Property="Foreground" Value="#999999"/>
        </Trigger>
    </Style.Triggers>
</Style>

<!-- Override popup/submenu background for menus -->
<Style TargetType="{x:Type ContextMenu}">
    <Setter Property="Background" Value="#F3F3F3"/>
    <Setter Property="Foreground" Value="#1E1E1E"/>
</Style>

<!-- Separator style -->
<Style TargetType="Separator">
    <Setter Property="Background" Value="#D4D4D4"/>
    <Setter Property="Margin" Value="0,4,0,4"/>
</Style>
```

**Step 2: Build and test**

Run: `dotnet build`
Expected: Build succeeds

**Step 3: Verify visually**

Run the app, click File/Edit/View menus and verify text is dark (#1E1E1E) on light background.

**Step 4: Commit**

```bash
git add UI/Themes/LightTheme.xaml
git commit -m "fix: menu dropdown text visibility in light theme"
```

---

## Part 2: MipsGenerator Rewrite

### Task 2: Create New Register Allocator

**Files:**
- Create: `src/CodeGen/RegisterAllocator.cs`

**Step 1: Create the RegisterAllocator class**

```csharp
namespace BasicToMips.CodeGen;

/// <summary>
/// Smart register allocator that:
/// - Tracks which registers hold which variables
/// - Reuses registers when values are no longer needed
/// - Spills to stack (db) when registers run out
/// - Returns raw values (literals/defines) when possible instead of allocating
/// </summary>
public class RegisterAllocator
{
    private const int MaxRegisters = 16; // r0-r15
    private const int PreferredMax = 12; // Prefer r0-r11, reserve r12-r15 for temps

    // Maps variable name to register number
    private readonly Dictionary<string, int> _variableRegisters = new();

    // Maps register number to what it currently holds (null = free)
    private readonly string?[] _registerContents = new string?[MaxRegisters];

    // Stack slots used for spilled variables
    private readonly Dictionary<string, int> _spilledVariables = new();
    private int _nextStackSlot = 0;

    // Defined constants (can use directly without register)
    private readonly HashSet<string> _defines = new();

    // Track temporary allocations that can be freed after use
    private readonly HashSet<int> _tempRegisters = new();

    public void AddDefine(string name)
    {
        _defines.Add(name);
    }

    public bool IsDefine(string name) => _defines.Contains(name);

    /// <summary>
    /// Get or allocate a register for a variable.
    /// Returns the register name (e.g., "r0").
    /// </summary>
    public string GetVariableRegister(string variableName, List<string> emitBuffer)
    {
        // Already in a register?
        if (_variableRegisters.TryGetValue(variableName, out int regNum))
        {
            return $"r{regNum}";
        }

        // Was spilled to stack? Reload it
        if (_spilledVariables.TryGetValue(variableName, out int stackSlot))
        {
            int reg = AllocateRegister(variableName, emitBuffer);
            emitBuffer.Add($"get r{reg} db {stackSlot}");
            _spilledVariables.Remove(variableName);
            return $"r{reg}";
        }

        // New variable - allocate register
        int newReg = AllocateRegister(variableName, emitBuffer);
        return $"r{newReg}";
    }

    /// <summary>
    /// Allocate a temporary register for intermediate calculations.
    /// Caller must call FreeTemp() when done.
    /// </summary>
    public string AllocateTemp(List<string> emitBuffer)
    {
        // First try to find a completely free register
        for (int i = PreferredMax; i < MaxRegisters; i++)
        {
            if (_registerContents[i] == null && !_tempRegisters.Contains(i))
            {
                _tempRegisters.Add(i);
                return $"r{i}";
            }
        }

        // Try lower registers
        for (int i = 0; i < PreferredMax; i++)
        {
            if (_registerContents[i] == null && !_tempRegisters.Contains(i))
            {
                _tempRegisters.Add(i);
                return $"r{i}";
            }
        }

        // All registers in use - spill the least recently used variable
        // For simplicity, spill the first variable we find
        for (int i = 0; i < PreferredMax; i++)
        {
            if (_registerContents[i] != null && !_tempRegisters.Contains(i))
            {
                SpillRegister(i, emitBuffer);
                _tempRegisters.Add(i);
                return $"r{i}";
            }
        }

        // Fallback to r15
        _tempRegisters.Add(15);
        return "r15";
    }

    /// <summary>
    /// Free a temporary register for reuse.
    /// </summary>
    public void FreeTemp(string reg)
    {
        if (reg.StartsWith("r") && int.TryParse(reg[1..], out int num))
        {
            _tempRegisters.Remove(num);
        }
    }

    /// <summary>
    /// Check if a value can be used directly as an operand (literal or define).
    /// </summary>
    public bool CanUseDirectly(string value)
    {
        // Numeric literals
        if (double.TryParse(value, out _)) return true;
        // Defined constants
        if (_defines.Contains(value)) return true;
        return false;
    }

    private int AllocateRegister(string variableName, List<string> emitBuffer)
    {
        // Find a free register
        for (int i = 0; i < PreferredMax; i++)
        {
            if (_registerContents[i] == null && !_tempRegisters.Contains(i))
            {
                _registerContents[i] = variableName;
                _variableRegisters[variableName] = i;
                return i;
            }
        }

        // No free registers - spill one
        for (int i = 0; i < PreferredMax; i++)
        {
            if (!_tempRegisters.Contains(i))
            {
                SpillRegister(i, emitBuffer);
                _registerContents[i] = variableName;
                _variableRegisters[variableName] = i;
                return i;
            }
        }

        throw new InvalidOperationException("No registers available for allocation");
    }

    private void SpillRegister(int regNum, List<string> emitBuffer)
    {
        var variableName = _registerContents[regNum];
        if (variableName == null) return;

        int stackSlot = _nextStackSlot++;
        emitBuffer.Add($"put db {stackSlot} r{regNum}");
        _spilledVariables[variableName] = stackSlot;
        _variableRegisters.Remove(variableName);
        _registerContents[regNum] = null;
    }

    /// <summary>
    /// Reset allocator state (for new compilation).
    /// </summary>
    public void Reset()
    {
        _variableRegisters.Clear();
        Array.Fill(_registerContents, null);
        _spilledVariables.Clear();
        _nextStackSlot = 0;
        _defines.Clear();
        _tempRegisters.Clear();
    }
}
```

**Step 2: Build and verify**

Run: `dotnet build`
Expected: Build succeeds with no errors in new file

**Step 3: Commit**

```bash
git add src/CodeGen/RegisterAllocator.cs
git commit -m "feat: add smart register allocator with spilling support"
```

---

### Task 3: Create Optimized Code Emitter

**Files:**
- Create: `src/CodeGen/CodeEmitter.cs`

**Step 1: Create the CodeEmitter class**

This class handles efficient instruction emission, avoiding redundant moves and using optimal instruction patterns.

```csharp
using System.Text;

namespace BasicToMips.CodeGen;

/// <summary>
/// Efficient IC10 code emitter that:
/// - Avoids redundant move instructions
/// - Uses immediate values directly when possible
/// - Combines operations where IC10 supports it
/// - Tracks current values in registers to avoid reloads
/// </summary>
public class CodeEmitter
{
    private readonly StringBuilder _output = new();
    private readonly List<string> _lines = new();
    private readonly RegisterAllocator _registers;

    // Track what value each register currently holds (for optimization)
    private readonly Dictionary<string, string> _registerValues = new();

    public CodeEmitter(RegisterAllocator registers)
    {
        _registers = registers;
    }

    public int LineCount => _lines.Count;

    /// <summary>
    /// Emit a raw instruction line.
    /// </summary>
    public void Emit(string instruction)
    {
        _lines.Add(instruction);
    }

    /// <summary>
    /// Emit a label.
    /// </summary>
    public void EmitLabel(string label)
    {
        _lines.Add($"{label}:");
    }

    /// <summary>
    /// Emit a comment.
    /// </summary>
    public void EmitComment(string comment)
    {
        _lines.Add($"# {comment}");
    }

    /// <summary>
    /// Emit a define statement.
    /// </summary>
    public void EmitDefine(string name, string value)
    {
        _lines.Add($"define {name} {value}");
        _registers.AddDefine(name);
    }

    /// <summary>
    /// Emit a move only if necessary (dest doesn't already contain value).
    /// </summary>
    public void EmitMove(string dest, string source)
    {
        if (dest == source) return;

        // Check if dest already holds this value
        if (_registerValues.TryGetValue(dest, out var currentValue) && currentValue == source)
        {
            return; // Already has the value
        }

        _lines.Add($"move {dest} {source}");
        _registerValues[dest] = source;
    }

    /// <summary>
    /// Emit a binary operation, using immediate values when possible.
    /// Returns the destination register.
    /// </summary>
    public string EmitBinaryOp(string op, string left, string right, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        // Emit any spill instructions first
        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        _lines.Add($"{op} {dest} {left} {right}");
        _registerValues.Remove(dest); // Value is now computed, not a simple copy

        return dest;
    }

    /// <summary>
    /// Emit a unary operation.
    /// </summary>
    public string EmitUnaryOp(string op, string operand, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        _lines.Add($"{op} {dest} {operand}");
        _registerValues.Remove(dest);

        return dest;
    }

    /// <summary>
    /// Emit a device read instruction.
    /// </summary>
    public string EmitDeviceRead(string device, string property, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        _lines.Add($"l {dest} {device} {property}");
        _registerValues.Remove(dest);

        return dest;
    }

    /// <summary>
    /// Emit a batch device read instruction (lb or lbn).
    /// </summary>
    public string EmitBatchRead(string hash, string? nameHash, string property, int mode, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        if (nameHash != null)
        {
            _lines.Add($"lbn {dest} {hash} {nameHash} {property} {mode}");
        }
        else
        {
            _lines.Add($"lb {dest} {hash} {property} {mode}");
        }
        _registerValues.Remove(dest);

        return dest;
    }

    /// <summary>
    /// Emit a device write instruction.
    /// </summary>
    public void EmitDeviceWrite(string device, string property, string value)
    {
        _lines.Add($"s {device} {property} {value}");
    }

    /// <summary>
    /// Emit a batch device write instruction (sb or sbn).
    /// </summary>
    public void EmitBatchWrite(string hash, string? nameHash, string property, string value)
    {
        if (nameHash != null)
        {
            _lines.Add($"sbn {hash} {nameHash} {property} {value}");
        }
        else
        {
            _lines.Add($"sb {hash} {property} {value}");
        }
    }

    /// <summary>
    /// Emit a conditional branch.
    /// </summary>
    public void EmitBranch(string condition, string left, string right, string label)
    {
        _lines.Add($"{condition} {left} {right} {label}");
    }

    /// <summary>
    /// Emit a conditional branch against zero.
    /// </summary>
    public void EmitBranchZero(string condition, string operand, string label)
    {
        _lines.Add($"{condition} {operand} {label}");
    }

    /// <summary>
    /// Emit an unconditional jump.
    /// </summary>
    public void EmitJump(string label)
    {
        _lines.Add($"j {label}");
    }

    /// <summary>
    /// Clear tracked register values (e.g., after a label where control flow merges).
    /// </summary>
    public void InvalidateRegisterTracking()
    {
        _registerValues.Clear();
    }

    /// <summary>
    /// Get the final output as a string.
    /// </summary>
    public string GetOutput()
    {
        return string.Join("\n", _lines);
    }

    /// <summary>
    /// Reset for new compilation.
    /// </summary>
    public void Reset()
    {
        _lines.Clear();
        _registerValues.Clear();
    }
}
```

**Step 2: Build and verify**

Run: `dotnet build`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/CodeGen/CodeEmitter.cs
git commit -m "feat: add optimized code emitter with redundancy elimination"
```

---

### Task 4: Rewrite MipsGenerator Expression Evaluation

**Files:**
- Modify: `src/CodeGen/MipsGenerator.cs`

**Problem:** Current expression evaluation allocates a new register for every sub-expression and emits unnecessary move instructions.

**Step 1: Add new fields and update constructor**

At the top of the MipsGenerator class (after line 18), add:

```csharp
private readonly RegisterAllocator _regAlloc = new();
private readonly CodeEmitter _emitter;
private bool _useOptimizedCodegen = true;

public MipsGenerator()
{
    _emitter = new CodeEmitter(_regAlloc);
}
```

**Step 2: Rewrite GenerateExpression to return operand strings**

The key insight is that expressions should return the **operand** (which could be a register, a literal, or a define name) rather than always allocating a new register.

Replace the `GenerateExpression` method (lines 813-872) with:

```csharp
/// <summary>
/// Generate code for an expression and return the operand string.
/// The operand could be:
/// - A register (e.g., "r0")
/// - A literal (e.g., "42", "3.14")
/// - A defined constant name (e.g., "TARGET")
/// This avoids unnecessary move instructions.
/// </summary>
private string GenerateExpression(ExpressionNode expr)
{
    switch (expr)
    {
        case NumberLiteral num:
            // Return the literal directly - no register needed!
            return FormatNumber(num.Value);

        case BooleanLiteral boolean:
            return boolean.Value ? "1" : "0";

        case StringLiteral str:
            // Strings become hashes
            return CalculateHash(str.Value).ToString();

        case HashExpression hashExpr:
            return CalculateHash(hashExpr.StringValue).ToString();

        case VariableExpression varExpr:
            return GenerateVariableExpression(varExpr);

        case BinaryExpression bin:
            return GenerateBinaryExpression(bin);

        case UnaryExpression unary:
            return GenerateUnaryExpression(unary);

        case TernaryExpression ternary:
            return GenerateTernaryExpression(ternary);

        case FunctionCallExpression func:
            return GenerateFunctionCall(func);

        case DeviceReadExpression deviceRead:
            return GenerateDeviceRead(deviceRead);

        case DeviceSlotReadExpression slotRead:
            return GenerateDeviceSlotRead(slotRead);

        case BatchReadExpression batchRead:
            return GenerateBatchRead(batchRead);

        case ReagentReadExpression reagentRead:
            return GenerateReagentRead(reagentRead);

        default:
            EmitComment("Unknown expression type");
            return "0";
    }
}
```

**Step 3: Rewrite GenerateVariableExpression**

Replace the method (lines 875-898):

```csharp
private string GenerateVariableExpression(VariableExpression varExpr)
{
    // Defined constants can be used directly
    if (_defines.ContainsKey(varExpr.Name))
    {
        return varExpr.Name;
    }

    // Regular variable - get its register
    var emitBuffer = new List<string>();
    var varReg = _regAlloc.GetVariableRegister(varExpr.Name, emitBuffer);

    foreach (var line in emitBuffer)
    {
        Emit(line);
    }

    if (varExpr.ArrayIndices != null && varExpr.ArrayIndices.Count > 0)
    {
        var indexOp = GenerateExpression(varExpr.ArrayIndices[0]);
        var resultReg = AllocateRegister();
        Emit($"add r{TempRegister} {varReg} {indexOp}");
        Emit($"get {resultReg} db r{TempRegister}");
        if (indexOp.StartsWith("r")) FreeRegister(indexOp);
        return resultReg;
    }

    return varReg;
}
```

**Step 4: Rewrite GenerateBinaryExpression for efficiency**

Replace the method (lines 900-979):

```csharp
private string GenerateBinaryExpression(BinaryExpression bin)
{
    var leftOp = GenerateExpression(bin.Left);
    var rightOp = GenerateExpression(bin.Right);
    var resultReg = AllocateRegister();

    var op = bin.Operator switch
    {
        BinaryOperator.Add => "add",
        BinaryOperator.Subtract => "sub",
        BinaryOperator.Multiply => "mul",
        BinaryOperator.Divide => "div",
        BinaryOperator.Modulo => "mod",
        BinaryOperator.Equal => "seq",
        BinaryOperator.NotEqual => "sne",
        BinaryOperator.LessThan => "slt",
        BinaryOperator.GreaterThan => "sgt",
        BinaryOperator.LessEqual => "sle",
        BinaryOperator.GreaterEqual => "sge",
        BinaryOperator.And => "and",
        BinaryOperator.Or => "or",
        BinaryOperator.BitAnd => "and",
        BinaryOperator.BitOr => "or",
        BinaryOperator.BitXor => "xor",
        BinaryOperator.ShiftLeft => "sll",
        BinaryOperator.ShiftRight => "srl",
        BinaryOperator.ShiftRightArith => "sra",
        BinaryOperator.Power => null, // Special handling
        BinaryOperator.ApproxEqual => null, // Special handling
        _ => throw new NotSupportedException($"Operator {bin.Operator} not supported")
    };

    if (op != null)
    {
        Emit($"{op} {resultReg} {leftOp} {rightOp}");
    }
    else if (bin.Operator == BinaryOperator.Power)
    {
        // a^b = exp(b * log(a))
        Emit($"log r{TempRegister} {leftOp}");
        Emit($"mul r{TempRegister} r{TempRegister} {rightOp}");
        Emit($"exp {resultReg} r{TempRegister}");
    }
    else if (bin.Operator == BinaryOperator.ApproxEqual)
    {
        Emit($"sap {resultReg} {leftOp} {rightOp} 0.0001");
    }

    // Free temporary registers if they were allocated
    if (leftOp.StartsWith("r") && !_variables.ContainsValue(int.Parse(leftOp[1..])))
        FreeRegister(leftOp);
    if (rightOp.StartsWith("r") && !_variables.ContainsValue(int.Parse(rightOp[1..])))
        FreeRegister(rightOp);

    return resultReg;
}
```

**Step 5: Build and verify**

Run: `dotnet build`
Expected: Build succeeds

**Step 6: Commit**

```bash
git add src/CodeGen/MipsGenerator.cs
git commit -m "refactor: optimize expression evaluation to reduce redundant moves"
```

---

### Task 5: Fix Device Hash Generation

**Files:**
- Modify: `src/CodeGen/MipsGenerator.cs`

**Problem:** Device type hashes (e.g., `SensorChamber_HASH`) are referenced but never defined. The proven compiler uses inline numeric hashes.

**Step 1: Update GenerateAlias to emit device type hash defines**

In the `GenerateAlias` method, around line 595-626, update the DeviceNamed case to emit the device type hash using the DeviceDatabase:

```csharp
case DeviceReferenceType.DeviceNamed:
    // IC.Device[hash].Name["name"] - named device reference
    if (devRef.DeviceHash is StringLiteral strHash2)
    {
        // Look up the device type hash from database
        var typeHash = BasicToMips.Data.DeviceDatabase.GetDeviceHash(strHash2.Value);
        Emit($"define {alias.AliasName}_HASH {typeHash}");
    }
    else if (devRef.DeviceHash is NumberLiteral numHash2)
    {
        Emit($"define {alias.AliasName}_HASH {FormatNumber(numHash2.Value)}");
    }
    // Store name hash for lbn/sbn operations
    var nameHash = CalculateHash(devRef.DeviceName!);
    Emit($"define {alias.AliasName}_NAME {nameHash}");
    break;
```

Similarly update the `Device` case.

**Step 2: Build and verify**

Run: `dotnet build`
Expected: Build succeeds

**Step 3: Test with sample code**

Compile:
```basic
ALIAS Sensor = IC.Device[StructureGasSensor].Name["Test Sensor"]
var pressure = Sensor.Pressure
```

Expected output should include:
```
define Sensor_HASH -1252983604
define Sensor_NAME <calculated-hash>
```

**Step 4: Commit**

```bash
git add src/CodeGen/MipsGenerator.cs
git commit -m "fix: generate device type hash defines from DeviceDatabase"
```

---

### Task 6: Fix Batch Mode Handling (.Average, .Sum, etc.)

**Files:**
- Modify: `src/CodeGen/MipsGenerator.cs`

**Problem:** `.Average` is being treated as a function call with `jal Average`, but it should be the batch mode parameter (0=Average, 1=Sum, 2=Min, 3=Max) in `lbn` instructions.

**Step 1: Update GenerateDeviceRead to handle batch modes inline**

The device read expression should check if it has a batch mode property and include it in the instruction. Update `GenerateDeviceRead` (around line 1020-1070):

```csharp
private string GenerateDeviceRead(DeviceReadExpression read)
{
    var resultReg = AllocateRegister();

    // Check for advanced device reference (batch/named)
    if (_deviceReferences.TryGetValue(read.DeviceName, out var devRef))
    {
        // Determine batch mode from property suffix
        int batchMode = 0; // Default: Average
        var property = read.PropertyName;

        if (property.EndsWith(".Average", StringComparison.OrdinalIgnoreCase))
        {
            property = property[..^8]; // Remove .Average
            batchMode = 0;
        }
        else if (property.EndsWith(".Sum", StringComparison.OrdinalIgnoreCase))
        {
            property = property[..^4];
            batchMode = 1;
        }
        else if (property.EndsWith(".Minimum", StringComparison.OrdinalIgnoreCase) ||
                 property.EndsWith(".Min", StringComparison.OrdinalIgnoreCase))
        {
            property = property.EndsWith(".Min") ? property[..^4] : property[..^8];
            batchMode = 2;
        }
        else if (property.EndsWith(".Maximum", StringComparison.OrdinalIgnoreCase) ||
                 property.EndsWith(".Max", StringComparison.OrdinalIgnoreCase))
        {
            property = property.EndsWith(".Max") ? property[..^4] : property[..^8];
            batchMode = 3;
        }

        switch (devRef.Type)
        {
            case DeviceReferenceType.Device:
                Emit($"lb {resultReg} {read.DeviceName}_HASH {property} {batchMode}");
                return resultReg;

            case DeviceReferenceType.DeviceNamed:
                Emit($"lbn {resultReg} {read.DeviceName}_HASH {read.DeviceName}_NAME {property} {batchMode}");
                return resultReg;

            // ... other cases remain the same
        }
    }

    // Standard device read
    var deviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);
    Emit($"l {resultReg} {deviceSpec} {read.PropertyName}");
    return resultReg;
}
```

**Step 2: Update Parser to not create function calls for batch modes**

This may require parser changes to recognize `.Average`, `.Sum`, etc. as batch mode specifiers rather than method calls.

**Step 3: Build and test**

Run: `dotnet build`
Test with: `var pressure = Sensor.Pressure.Average`
Expected: `lbn r0 Sensor_HASH Sensor_NAME Pressure 0` (not `jal Average`)

**Step 4: Commit**

```bash
git add src/CodeGen/MipsGenerator.cs src/Parser/Parser.cs
git commit -m "fix: handle batch modes (.Average, .Sum, etc.) as instruction parameters"
```

---

### Task 7: Optimize IF Statement Code Generation

**Files:**
- Modify: `src/CodeGen/MipsGenerator.cs`

**Problem:** IF statements generate comparison to register, then branch on result. Should use direct comparison branches like `bge`, `blt`, `beq`, `bne`.

**Step 1: Add comparison branch optimization in GenerateIf**

Replace `GenerateIf` (lines 249-282) with:

```csharp
private void GenerateIf(IfStatement ifStmt)
{
    var elseLabel = NewLabel("else");
    var endLabel = NewLabel("endif");

    // Try to generate optimized conditional branch
    if (TryGenerateOptimizedBranch(ifStmt.Condition,
        ifStmt.ElseBranch.Count > 0 ? elseLabel : endLabel,
        negate: true))
    {
        // Optimized branch was generated
    }
    else
    {
        // Fallback: evaluate condition to register and branch
        var condReg = GenerateExpression(ifStmt.Condition);
        if (ifStmt.ElseBranch.Count > 0)
        {
            Emit($"beqz {condReg} {elseLabel}");
        }
        else
        {
            Emit($"beqz {condReg} {endLabel}");
        }
        if (condReg.StartsWith("r")) FreeRegister(condReg);
    }

    foreach (var stmt in ifStmt.ThenBranch)
    {
        GenerateStatement(stmt);
    }

    if (ifStmt.ElseBranch.Count > 0)
    {
        Emit($"j {endLabel}");
        EmitLabel(elseLabel);

        foreach (var stmt in ifStmt.ElseBranch)
        {
            GenerateStatement(stmt);
        }
    }

    EmitLabel(endLabel);
}

/// <summary>
/// Try to generate an optimized branch instruction for a comparison.
/// Returns true if successful, false if fallback is needed.
/// </summary>
private bool TryGenerateOptimizedBranch(ExpressionNode condition, string targetLabel, bool negate)
{
    if (condition is BinaryExpression bin)
    {
        var leftOp = GenerateExpression(bin.Left);
        var rightOp = GenerateExpression(bin.Right);

        // Map operator to branch instruction
        var (branchOp, negatedOp) = bin.Operator switch
        {
            BinaryOperator.Equal => ("beq", "bne"),
            BinaryOperator.NotEqual => ("bne", "beq"),
            BinaryOperator.LessThan => ("blt", "bge"),
            BinaryOperator.GreaterThan => ("bgt", "ble"),
            BinaryOperator.LessEqual => ("ble", "bgt"),
            BinaryOperator.GreaterEqual => ("bge", "blt"),
            _ => (null, null)
        };

        if (branchOp != null)
        {
            var op = negate ? negatedOp : branchOp;
            Emit($"{op} {leftOp} {rightOp} {targetLabel}");

            if (leftOp.StartsWith("r") && !_variables.ContainsValue(int.Parse(leftOp[1..])))
                FreeRegister(leftOp);
            if (rightOp.StartsWith("r") && !_variables.ContainsValue(int.Parse(rightOp[1..])))
                FreeRegister(rightOp);

            return true;
        }

        // Handle OR by chaining branches
        if (bin.Operator == BinaryOperator.Or && !negate)
        {
            // For OR with negate=false (branch if true):
            // Branch to target if left is true OR right is true
            var skipLabel = NewLabel("or_skip");
            TryGenerateOptimizedBranch(bin.Left, targetLabel, negate: false);
            TryGenerateOptimizedBranch(bin.Right, targetLabel, negate: false);
            return true;
        }
    }

    return false;
}
```

**Step 2: Build and test**

Run: `dotnet build`
Test with: `IF pressure > 100 THEN`
Expected: `bgt r0 100 label` (single instruction, not `sgt` + `beqz`)

**Step 3: Commit**

```bash
git add src/CodeGen/MipsGenerator.cs
git commit -m "optimize: use direct comparison branches in IF statements"
```

---

### Task 8: Final Integration and Testing

**Files:**
- Modify: `src/CodeGen/MipsGenerator.cs` (cleanup)
- Test with full example code

**Step 1: Remove unused register allocation code**

Clean up the old `_nextRegister` field and related dead code.

**Step 2: Build release**

Run: `dotnet publish -c Release -o ./publish`

**Step 3: Test with the full airlock display example**

Compile the full BASIC code from the user's example and verify:
- Output is under 128 lines
- All device hashes are defined
- No `jal Average` calls
- Comparisons use direct branches where possible

**Step 4: Create release zip**

```powershell
Compress-Archive -Path './publish/*' -DestinationPath './BasicToMips_v1.0.6.zip' -Force
```

**Step 5: Commit**

```bash
git add .
git commit -m "release: v1.0.6 - optimized code generation"
```

---

## Summary of Expected Improvements

| Metric | Before | After |
|--------|--------|-------|
| Line count (airlock example) | ~190 | <128 |
| Redundant move instructions | Many | Minimal |
| Device hash generation | Missing | Correct |
| Batch mode handling | Wrong (jal) | Correct (inline) |
| Branch optimization | None | Direct comparison |
| Menu text visibility | Light on light | Dark on light |

---

## Testing Checklist

- [ ] Light theme menu text is readable (dark text on light background)
- [ ] Device type hashes are generated with correct values
- [ ] Named device references use `lbn`/`sbn` with both hashes
- [ ] `.Average`/`.Sum`/`.Min`/`.Max` become batch mode parameters (0/1/2/3)
- [ ] IF statements use direct branch instructions where possible
- [ ] Output line count is significantly reduced
- [ ] All original functionality still works (ALIAS, VAR, IF, FOR, WHILE, etc.)
- [ ] Decompiler still works with the new output format
