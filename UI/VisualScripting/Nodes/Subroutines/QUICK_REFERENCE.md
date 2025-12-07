# Subroutines Quick Reference - Phase 5B

## Node Summary

| Node | Icon | Purpose | Inputs | Outputs |
|------|------|---------|--------|---------|
| **SubDefinition** | 📦 | Define a SUB block | None | Body (Exec) |
| **CallSub** | 📞 | Call a SUB | Exec | Exec |
| **ExitSub** | 🚪 | Exit SUB early | Exec | None |
| **FunctionDefinition** | 🔧 | Define a FUNCTION | None | Body (Exec), ReturnValue (Number) |
| **CallFunction** | 📱 | Call a FUNCTION | Exec | Exec, Result (Number) |
| **ExitFunction** | 🚪 | Exit FUNCTION early | Exec, ReturnValue (Number) | None |
| **SetReturnValue** | ↩️ | Set return and continue | Exec, Value (Number) | Exec |

## Code Generation Patterns

### SUB Definition
```
[SUB MySubroutine]
    Body -> [nodes...]

↓ Generates ↓

SUB MySubroutine
    ' nodes...
END SUB
```

### FUNCTION Definition
```
[FUNCTION MyFunction]
    Body -> [nodes...]
    ReturnValue <- [value]

↓ Generates ↓

FUNCTION MyFunction
    ' nodes...
    RETURN value
END FUNCTION
```

### Calling a SUB
```
[Entry Point]
    -> [CALL MySubroutine]
    -> [Next node]

↓ Generates ↓

CALL MySubroutine
' next node
```

### Calling a FUNCTION
```
[Entry Point]
    -> [CALL MyFunction()]
         Result -> [Variable x]

↓ Generates ↓

x = MyFunction()
```

### Early Exit from SUB
```
[SUB MySubroutine]
    Body -> [IF condition]
              True -> [EXIT SUB]
              False -> [Continue...]

↓ Generates ↓

SUB MySubroutine
    IF condition THEN
        EXIT SUB
    ELSE
        ' Continue...
    ENDIF
END SUB
```

### Early Exit from FUNCTION
```
[FUNCTION MyFunction]
    Body -> [IF error]
              True -> [EXIT FUNCTION: -1]
              False -> [Return normal value]

↓ Generates ↓

FUNCTION MyFunction
    IF error THEN
        RETURN -1
        EXIT FUNCTION
    ELSE
        ' Return normal value
    ENDIF
END FUNCTION
```

## SubroutineRegistry API

```csharp
// Get available SUBs for dropdown
var subs = SubroutineRegistry.Instance.GetDefinedSubroutines();

// Get available FUNCTIONs for dropdown
var funcs = SubroutineRegistry.Instance.GetDefinedFunctions();

// Validate a SUB call
bool valid = SubroutineRegistry.Instance.ValidateCall("MySub", false);

// Validate a FUNCTION call
bool valid = SubroutineRegistry.Instance.ValidateCall("MyFunc", true);

// Refresh from current graph (before code generation)
SubroutineRegistry.Instance.RefreshRegistry(nodes);

// Check if name is already taken
bool taken = SubroutineRegistry.Instance.IsNameTaken("MyName");
```

## Validation Rules

### Name Requirements
- ✅ Letters, numbers, underscore
- ✅ Cannot start with number
- ❌ No spaces, hyphens, special chars
- ✅ Examples: `Calculate`, `Init_System`, `CheckTemp2`
- ❌ Examples: `123Start`, `My-Func`, `My Function`

### Call Validation
- CALL SUB: Target must be a defined SUB
- CALL FUNCTION: Target must be a defined FUNCTION
- Cannot call undefined names

### Context Validation
- EXIT SUB: Must be inside a SUB
- EXIT FUNCTION: Must be inside a FUNCTION
- SET RETURN VALUE: Must be inside a FUNCTION

## Common Patterns

### Pattern 1: Initialization Subroutine
```
[Entry Point]
    -> [CALL Initialize]
    -> [Main logic...]

[SUB Initialize]
    Body -> [Set variables...]
         -> [Configure devices...]
```

### Pattern 2: Function for Calculations
```
[FUNCTION Average]
    Body -> [Add inputs]
         -> [Divide by count]
         -> [Set Return Value]
```

### Pattern 3: Error Checking with Early Exit
```
[SUB ProcessData]
    Body -> [IF invalid]
              True -> [Log error]
                   -> [EXIT SUB]
              False -> [Process data...]
```

### Pattern 4: Conditional Function Return
```
[FUNCTION GetStatus]
    Body -> [Read sensor]
         -> [IF temp > 100]
              True -> [Exit Function: 1]
              False -> [IF pressure > 50]
                        True -> [Exit Function: 2]
                        False -> [Set Return Value: 0]
```

## Code Structure

Generated code follows this order:

```basic
' --- Variables ---
' --- Constants ---
' --- Devices ---
' --- Arrays ---
' --- Main ---
END

' --- Subroutines ---
SUB ...
END SUB

FUNCTION ...
END FUNCTION
```

## Visual Styling

### Definition Nodes (SUB/FUNCTION)
- **Color**: Green (#27AE60)
- **Size**: Larger (250px wide)
- **Style**: Container appearance
- **Header**: Shows "SUB name" or "FUNCTION name"

### Call Nodes
- **Color**: Match target's color
- **Size**: Standard (200px wide)
- **Label**: Shows "CALL name" or "CALL name()"

### Exit Nodes
- **Color**: Standard
- **Size**: Compact (180px wide)
- **Icon**: 🚪

## Integration Points

### NodeFactory Registration
```csharp
// In NodeSystemExample.CreateFactory()
factory.RegisterNodeType<Subroutines.SubDefinitionNode>();
factory.RegisterNodeType<Subroutines.CallSubNode>();
factory.RegisterNodeType<Subroutines.ExitSubNode>();
factory.RegisterNodeType<Subroutines.FunctionDefinitionNode>();
factory.RegisterNodeType<Subroutines.CallFunctionNode>();
factory.RegisterNodeType<Subroutines.ExitFunctionNode>();
factory.RegisterNodeType<Subroutines.SetReturnValueNode>();
```

### Code Generator Integration
```csharp
// In GraphToBasicGenerator.Generate()
SubroutineRegistry.Instance.RefreshRegistry(_nodes);
GenerateSubroutinesSection();
```

## Files Created

### Nodes (7 files)
- `SubDefinitionNode.cs`
- `CallSubNode.cs`
- `ExitSubNode.cs`
- `FunctionDefinitionNode.cs`
- `CallFunctionNode.cs`
- `ExitFunctionNode.cs`
- `SetReturnValueNode.cs`

### Registry (1 file)
- `SubroutineRegistry.cs`

### Documentation (2 files)
- `README.md` (this file)
- `QUICK_REFERENCE.md`

### Updated Files
- `GraphToBasicGenerator.cs` - Added subroutine generation
- `NodeSystemExample.cs` - Added node registrations

## Testing Commands

```csharp
// Create factory with subroutine nodes
var factory = NodeSystemExample.CreateFactory();

// Create a SUB definition
var subDef = factory.CreateNode("SubDefinition") as SubDefinitionNode;
subDef.SubroutineName = "Initialize";

// Create a FUNCTION definition
var funcDef = factory.CreateNode("FunctionDefinition") as FunctionDefinitionNode;
funcDef.FunctionName = "Calculate";

// Create a CALL SUB
var callSub = factory.CreateNode("CallSub") as CallSubNode;
callSub.TargetSubroutine = "Initialize";

// Create a CALL FUNCTION
var callFunc = factory.CreateNode("CallFunction") as CallFunctionNode;
callFunc.TargetFunction = "Calculate";

// Validate
bool valid = subDef.Validate(out string error);
```

## Phase 5B Completion Status

✅ All 7 node types implemented
✅ SubroutineRegistry implemented
✅ GraphToBasicGenerator updated
✅ NodeFactory registrations added
✅ Comprehensive documentation created
✅ Quick reference guide created
✅ Code generation tested
✅ Validation implemented

**Status: Phase 5B Complete**
