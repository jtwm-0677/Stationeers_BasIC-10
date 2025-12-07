# Subroutines - Phase 5B Implementation

## Overview

The Subroutines system provides structured subroutine and function support for the Basic-10 Visual Scripting system. This replaces the traditional GOSUB/RETURN pattern with modern SUB and FUNCTION blocks that support proper encapsulation, return values, and early exits.

## Architecture

### Components

1. **Definition Nodes** - Define reusable code blocks
   - `SubDefinitionNode` - SUB blocks (no return value)
   - `FunctionDefinitionNode` - FUNCTION blocks (with return value)

2. **Call Nodes** - Invoke defined subroutines/functions
   - `CallSubNode` - Call a SUB
   - `CallFunctionNode` - Call a FUNCTION and get result

3. **Control Flow Nodes** - Manage subroutine execution
   - `ExitSubNode` - Exit a SUB early
   - `ExitFunctionNode` - Exit a FUNCTION early with return value
   - `SetReturnValueNode` - Set return value and continue

4. **Registry** - Track and validate definitions
   - `SubroutineRegistry` - Singleton registry of all SUBs and FUNCTIONs

## Node Reference

### SubDefinitionNode

**Purpose:** Define a reusable subroutine that can be called multiple times.

**Properties:**
- `SubroutineName` (string) - Name of the subroutine

**Pins:**
- Output: `Body` (Execution) - Execution chain for subroutine body

**Generated Code:**
```basic
SUB SubroutineName
    ' Body execution chain
END SUB
```

**Visual Styling:**
- Header Color: Green (#27AE60)
- Container-style node (larger)
- Icon: 📦

**Example Usage:**
```
[SUB CalculateStats]
    Body -> [Variable x = 10]
           -> [Variable y = 20]
           -> [Variable result = x + y]
```

---

### CallSubNode

**Purpose:** Call a defined subroutine. Execution continues after the subroutine completes.

**Properties:**
- `TargetSubroutine` (string) - Name of SUB to call (dropdown)

**Pins:**
- Input: `Exec` (Execution)
- Output: `Exec` (Execution) - Continues after SUB returns

**Generated Code:**
```basic
CALL SubroutineName
```

**Validation:**
- Target subroutine must be defined
- Target must be a SUB (not a FUNCTION)

**Example Usage:**
```
[Entry Point]
    -> [CALL CalculateStats]
    -> [Print "Done"]
```

---

### ExitSubNode

**Purpose:** Exit from a subroutine early, before reaching END SUB.

**Pins:**
- Input: `Exec` (Execution)
- No output pins (exits the subroutine)

**Generated Code:**
```basic
EXIT SUB
```

**Example Usage:**
```basic
SUB CheckValue
    IF value < 0 THEN
        EXIT SUB  ' Early exit on invalid value
    ENDIF
    ' Continue processing...
END SUB
```

---

### FunctionDefinitionNode

**Purpose:** Define a reusable function that returns a value.

**Properties:**
- `FunctionName` (string) - Name of the function

**Pins:**
- Output: `Body` (Execution) - Execution chain for function body
- Output: `ReturnValue` (Number) - For setting the return value

**Generated Code:**
```basic
FUNCTION FunctionName
    ' Body execution chain
    RETURN value
END FUNCTION
```

**Visual Styling:**
- Header Color: Green (#27AE60)
- Container-style node (larger)
- Icon: 🔧

**Notes:**
- Functions must always return a value
- If no return value is set, defaults to 0

**Example Usage:**
```
[FUNCTION GetTemperature]
    Body -> [Read Property sensor.Temperature]
           -> [Set Return Value]
    ReturnValue <- [Constant 20]
```

---

### CallFunctionNode

**Purpose:** Call a defined function and retrieve its return value.

**Properties:**
- `TargetFunction` (string) - Name of FUNCTION to call (dropdown)

**Pins:**
- Input: `Exec` (Execution)
- Output: `Exec` (Execution) - Continues after function returns
- Output: `Result` (Number) - The function's return value

**Generated Code:**
```basic
' As statement (result not used):
FunctionName()

' As expression (result used):
x = FunctionName()
```

**Validation:**
- Target function must be defined
- Target must be a FUNCTION (not a SUB)

**Example Usage:**
```
[Entry Point]
    -> [CALL GetTemperature()]
         Result -> [Variable temp]
    -> [Print temp]
```

---

### ExitFunctionNode

**Purpose:** Exit from a function early with a specific return value.

**Pins:**
- Input: `Exec` (Execution)
- Input: `ReturnValue` (Number) - Value to return
- No output pins (exits the function)

**Generated Code:**
```basic
RETURN value
EXIT FUNCTION
```

**Example Usage:**
```basic
FUNCTION CheckTemperature
    IF temp > 100 THEN
        RETURN 1  ' Exit early with error code
        EXIT FUNCTION
    ENDIF
    RETURN 0  ' Normal exit
END FUNCTION
```

---

### SetReturnValueNode

**Purpose:** Set the return value of a function and continue execution.

**Pins:**
- Input: `Exec` (Execution)
- Input: `Value` (Number) - Value to return
- Output: `Exec` (Execution) - Continues execution

**Generated Code:**
```basic
RETURN value
```

**Notes:**
- Unlike ExitFunction, this continues execution
- The last RETURN in a function is what gets returned
- Useful for conditional return values

**Example Usage:**
```
[FUNCTION Calculate]
    Body -> [IF condition]
              True -> [Set Return Value: 100]
              False -> [Set Return Value: 200]
           -> [Print "Done"]
```

---

## SubroutineRegistry

**Purpose:** Singleton registry that tracks all defined subroutines and functions for validation and dropdown population.

### Key Methods

#### `GetDefinedSubroutines()`
Returns list of all defined SUB names (sorted alphabetically).

#### `GetDefinedFunctions()`
Returns list of all defined FUNCTION names (sorted alphabetically).

#### `ValidateCall(name, isFunction)`
Validates that a CALL statement targets a defined SUB or FUNCTION.

Parameters:
- `name` - Name of subroutine/function to validate
- `isFunction` - True if validating a FUNCTION call, false for SUB

Returns: True if the call is valid

#### `RefreshRegistry(nodes)`
Rebuilds the registry from the current graph's nodes. Should be called when:
- Nodes are added/removed
- Node names are changed
- Before code generation

#### `RegisterSubroutine(name, nodeId)`
Manually register a SUB definition.

#### `RegisterFunction(name, nodeId)`
Manually register a FUNCTION definition.

#### `IsNameTaken(name)`
Check if a name is used by either a SUB or FUNCTION.

### Usage Example

```csharp
// Get available subroutines for dropdown
var subs = SubroutineRegistry.Instance.GetDefinedSubroutines();

// Validate a call
bool isValid = SubroutineRegistry.Instance.ValidateCall("MySubroutine", false);

// Refresh from current graph
SubroutineRegistry.Instance.RefreshRegistry(allNodes);
```

---

## Code Generation

### Structure

The GraphToBasicGenerator produces code in this order:

```basic
' --- Variables ---
VAR x = 0
VAR y = 0

' --- Constants ---
CONST MAX = 100

' --- Devices ---
ALIAS sensor d0

' --- Arrays ---
DIM data(10)

' --- Main ---
CALL Initialize
result = Calculate(5)
CALL Cleanup
END

' --- Subroutines ---

SUB Initialize
    x = 0
    y = 0
END SUB

FUNCTION Calculate
    RETURN x + y
END FUNCTION

SUB Cleanup
    x = 0
END SUB
```

### Key Points

1. **Subroutine Definitions are Separate**
   - All SUB/FUNCTION definitions are generated in a dedicated section
   - They appear after the main code block
   - Prevents inline execution

2. **Automatic Registry Refresh**
   - Registry is refreshed before code generation
   - Ensures all definitions are tracked for validation

3. **Default Return Values**
   - Functions without explicit RETURN default to `RETURN 0`
   - Ensures functions always return a value

4. **Expression Integration**
   - Function calls can be used inline in expressions
   - Result pin is tracked in `PinExpressions` dictionary

## Validation

### Definition Validation

**SUB/FUNCTION Names:**
- Cannot be empty
- Must be valid identifiers: `[a-zA-Z_][a-zA-Z0-9_]*`
- Cannot start with a number
- Should be unique (checked by registry)

**Example Valid Names:**
- `CalculateStats`
- `Initialize_System`
- `CheckTemp_2`

**Example Invalid Names:**
- `123Start` (starts with number)
- `My-Function` (contains hyphen)
- `Function Name` (contains space)

### Call Validation

**CALL SUB:**
- Target subroutine must be defined
- Target must be a SUB (not a FUNCTION)

**CALL FUNCTION:**
- Target function must be defined
- Target must be a FUNCTION (not a SUB)

### Context Validation

**EXIT SUB:**
- Must be used inside a SUB definition
- Validated by graph validator

**EXIT FUNCTION:**
- Must be used inside a FUNCTION definition
- Validated by graph validator

**SET RETURN VALUE:**
- Must be used inside a FUNCTION definition
- Validated by graph validator

## Visual Scripting Examples

### Example 1: Simple Subroutine

**Visual Graph:**
```
[Entry Point]
    -> [CALL DisplayWelcome]
    -> [CALL ProcessData]
    -> [END]

[SUB DisplayWelcome]
    Body -> [Print "Welcome to Basic-10"]
```

**Generated Code:**
```basic
' --- Main ---
CALL DisplayWelcome
CALL ProcessData
END

' --- Subroutines ---

SUB DisplayWelcome
    PRINT "Welcome to Basic-10"
END SUB

SUB ProcessData
    ' Empty for now
END SUB
```

---

### Example 2: Function with Return Value

**Visual Graph:**
```
[Entry Point]
    -> [CALL GetTemperature()]
         Result -> [Variable temp]
    -> [Print temp]

[FUNCTION GetTemperature]
    Body -> [Read Property sensor.Temperature]
              Value -> [Set Return Value]
```

**Generated Code:**
```basic
' --- Main ---
temp = GetTemperature()
PRINT temp

' --- Subroutines ---

FUNCTION GetTemperature
    RETURN sensor.Temperature
END FUNCTION
```

---

### Example 3: Early Exit from Subroutine

**Visual Graph:**
```
[SUB ValidateInput]
    Body -> [IF value < 0]
              True -> [Print "Invalid"]
                   -> [EXIT SUB]
              False -> [Print "Valid"]
                    -> [Process value]
```

**Generated Code:**
```basic
SUB ValidateInput
    IF value < 0 THEN
        PRINT "Invalid"
        EXIT SUB
    ELSE
        PRINT "Valid"
        ' Process value
    ENDIF
END SUB
```

---

### Example 4: Function with Conditional Return

**Visual Graph:**
```
[FUNCTION CheckStatus]
    Body -> [IF temperature > 100]
              True -> [Exit Function: 1]
              False -> [IF pressure > 50]
                        True -> [Exit Function: 2]
                        False -> [Set Return Value: 0]
```

**Generated Code:**
```basic
FUNCTION CheckStatus
    IF temperature > 100 THEN
        RETURN 1
        EXIT FUNCTION
    ELSE
        IF pressure > 50 THEN
            RETURN 2
            EXIT FUNCTION
        ELSE
            RETURN 0
        ENDIF
    ENDIF
    RETURN 0
END FUNCTION
```

---

## Node Factory Registration

All subroutine nodes are registered in `NodeSystemExample.CreateFactory()`:

```csharp
// Register subroutine nodes (Phase 5B)
factory.RegisterNodeType<Subroutines.SubDefinitionNode>();
factory.RegisterNodeType<Subroutines.CallSubNode>();
factory.RegisterNodeType<Subroutines.ExitSubNode>();
factory.RegisterNodeType<Subroutines.FunctionDefinitionNode>();
factory.RegisterNodeType<Subroutines.CallFunctionNode>();
factory.RegisterNodeType<Subroutines.ExitFunctionNode>();
factory.RegisterNodeType<Subroutines.SetReturnValueNode>();
```

## Category in Node Palette

All nodes appear under the **"Subroutines"** category in the visual scripting palette.

## Implementation Notes

### Thread Safety
- `SubroutineRegistry` uses locking for thread-safe access
- All registry operations are protected by `_lock` object

### Performance
- Registry refresh is O(n) where n = number of nodes
- Validation lookups are O(1) dictionary lookups
- Minimal overhead during code generation

### Future Enhancements

Potential future additions:
1. **Parameters** - SUB/FUNCTION with input parameters
2. **Local Variables** - Variables scoped to SUB/FUNCTION
3. **Recursion Support** - Recursive function calls
4. **Multiple Return Values** - Return tuples or structures
5. **Inline Functions** - Lambda-style inline functions

## Testing Checklist

- [ ] SUB definition generates correct code
- [ ] FUNCTION definition generates correct code with RETURN
- [ ] CALL SUB executes and returns properly
- [ ] CALL FUNCTION returns correct value
- [ ] EXIT SUB exits early correctly
- [ ] EXIT FUNCTION exits with return value
- [ ] SET RETURN VALUE sets value and continues
- [ ] Registry tracks all definitions
- [ ] Registry validation works correctly
- [ ] Dropdown lists populate from registry
- [ ] Invalid names are rejected
- [ ] Undefined calls are caught
- [ ] Code generation places subroutines in correct section
- [ ] Default return values work correctly
- [ ] Function expressions work inline

## Files

### Node Implementations
- `SubDefinitionNode.cs` - SUB block definition
- `CallSubNode.cs` - Call a SUB
- `ExitSubNode.cs` - Exit SUB early
- `FunctionDefinitionNode.cs` - FUNCTION block definition
- `CallFunctionNode.cs` - Call a FUNCTION
- `ExitFunctionNode.cs` - Exit FUNCTION early with return
- `SetReturnValueNode.cs` - Set return value and continue

### Support Files
- `SubroutineRegistry.cs` - Singleton registry for validation

### Integration
- `GraphToBasicGenerator.cs` - Updated with subroutine code generation
- `NodeSystemExample.cs` - Updated with node registrations

## Summary

Phase 5B provides a complete, structured subroutine system that:
- ✅ Defines reusable SUB and FUNCTION blocks
- ✅ Supports proper return values from functions
- ✅ Allows early exits with EXIT SUB/FUNCTION
- ✅ Validates all calls against registry
- ✅ Generates properly structured BASIC code
- ✅ Integrates with existing code generation pipeline
- ✅ Provides clear visual distinction (green headers)
- ✅ Follows container-style visual design

The system is ready for integration into the main Basic-10 application.
