# Flow Control Nodes - Phase 5A

This directory contains the flow control nodes for Basic-10's visual scripting system.

## Overview

Flow control nodes use **Execution pins** (white color, `DataType.Execution`) to control program flow. These nodes enable loops, conditional branching, jumps, and other control structures in the visual scripting graph.

## Node List

### Entry and Exit
1. **EntryPointNode** - Program start marker (only one per graph)
2. **EndNode** - Terminates program execution

### Conditional Branching
3. **IfNode** - IF/THEN/ELSE branching with True/False/Done pins
4. **SelectCaseNode** - Multi-way switch/case statement with dynamic case pins

### Loops
5. **WhileNode** - WHILE loop (condition checked before each iteration)
6. **ForNode** - FOR/NEXT loop with start, end, step, and index output
7. **DoUntilNode** - DO/LOOP UNTIL (condition checked at end)

### Loop Control
8. **BreakNode** - Exit loop early
9. **ContinueNode** - Skip to next iteration

### Labels and Jumps
10. **LabelNode** - Define jump target
11. **GotoNode** - Unconditional jump to label
12. **GosubNode** - Call subroutine at label
13. **ReturnNode** - Return from subroutine

### Execution Control
14. **YieldNode** - Yield execution to prevent lockup
15. **SleepNode** - Pause execution for duration

## Key Features

### Auto-YIELD
Loop nodes (While, For, DoUntil) have an `AutoYield` property (default: true) that automatically inserts a YIELD statement at the end of each loop iteration. This prevents infinite loop lockups in Stationeers.

### Execution Pin Flow
- **Input Execution Pin (Exec)**: Triggers node execution
- **Output Execution Pin (Exec)**: Continues to next node
- **Branch Pins**: Multiple outputs for conditional flow (True/False, Case 0/Case 1/etc.)
- **Done Pin**: Fires after all branches complete (for loops and conditionals)

### Code Generation
Flow control nodes are handled by `GraphToBasicGenerator.cs` which:
- Follows execution wire chains
- Generates proper nesting and indentation
- Handles branch merging via Done pins
- Supports recursive chain building for nested structures

## Visual Styling

All flow control nodes use:
- **Header Color**: Purple (#9B59B6)
- **Execution Pins**: White circles
- **Larger Node Size**: To clearly show branching structure

## Node Details

### EntryPointNode
```csharp
Outputs: Exec (Execution)
Generates: ' --- Program Start ---
```

### IfNode
```csharp
Inputs: Exec (Execution), Condition (Boolean)
Outputs: True (Execution), False (Execution), Done (Execution)
Generates:
  IF condition THEN
      ' True branch
  ELSE
      ' False branch
  ENDIF
```

### WhileNode
```csharp
Inputs: Exec (Execution), Condition (Boolean)
Outputs: LoopBody (Execution), Done (Execution)
Properties: AutoYield (bool, default: true)
Generates:
  WHILE condition
      ' Loop body
      YIELD  ' if AutoYield enabled
  WEND
```

### ForNode
```csharp
Inputs: Exec (Execution), Start (Number), End (Number)
Outputs: LoopBody (Execution), Done (Execution), Index (Number)
Properties: VariableName (string), Step (double), AutoYield (bool)
Generates:
  FOR i = start TO end STEP step
      ' Loop body
      YIELD  ' if AutoYield enabled
  NEXT i
```

### DoUntilNode
```csharp
Inputs: Exec (Execution), Condition (Boolean)
Outputs: LoopBody (Execution), Done (Execution)
Properties: AutoYield (bool, default: true)
Generates:
  DO
      ' Loop body
      YIELD  ' if AutoYield enabled
  LOOP UNTIL condition
```

### BreakNode
```csharp
Inputs: Exec (Execution)
Outputs: None (flow terminates)
Generates: BREAK
```

### ContinueNode
```csharp
Inputs: Exec (Execution)
Outputs: None (flow terminates)
Generates: CONTINUE
```

### LabelNode
```csharp
Inputs: Exec (Execution) - optional
Outputs: Exec (Execution)
Properties: LabelName (string)
Generates: labelName:
```

### GotoNode
```csharp
Inputs: Exec (Execution)
Outputs: None (flow jumps)
Properties: TargetLabel (string)
Generates: GOTO targetLabel
```

### GosubNode
```csharp
Inputs: Exec (Execution)
Outputs: Exec (Execution) - continues after RETURN
Properties: TargetLabel (string)
Generates: GOSUB targetLabel
```

### ReturnNode
```csharp
Inputs: Exec (Execution)
Outputs: None (flow returns)
Generates: RETURN
```

### SelectCaseNode
```csharp
Inputs: Exec (Execution), Value (Number)
Outputs: Case 0..N (Execution), Default (Execution), Done (Execution)
Properties: CaseValues (List<int>)
Generates:
  SELECT CASE value
      CASE 0
          ' case 0 chain
      CASE 1
          ' case 1 chain
      DEFAULT
          ' default chain
  END SELECT
```

### YieldNode
```csharp
Inputs: Exec (Execution)
Outputs: Exec (Execution)
Generates: YIELD
```

### SleepNode
```csharp
Inputs: Exec (Execution), Duration (Number)
Outputs: Exec (Execution)
Generates: SLEEP duration
```

### EndNode
```csharp
Inputs: Exec (Execution)
Outputs: None (program ends)
Generates: END
```

## Usage Example

```
[EntryPoint] -> [While] -> [If] -> [YieldNode] -> [End]
                  ^  |       |
                  |  |       +-> True: [SetVariable]
                  |  |       +-> False: [Break]
                  |  |
                  |  +-> Done: [End]
                  |
                  +-> LoopBody: [If]
```

This creates:
```basic
' --- Program Start ---
WHILE condition
    IF subCondition THEN
        LET myVar = value
    ELSE
        BREAK
    ENDIF
    YIELD
WEND
END
```

## Integration

All flow control nodes are registered in `NodeSystemExample.CreateFactory()`:
```csharp
factory.RegisterNodeType<FlowControl.EntryPointNode>();
factory.RegisterNodeType<FlowControl.IfNode>();
// ... etc
```

Code generation is handled in `GraphToBasicGenerator.cs` with dedicated methods for each node type.

## Future Enhancements

- Visual debugging: highlight current execution node
- Breakpoint support in nodes
- Step-through execution
- Flow visualization overlays
- Loop iteration counters
- Execution profiling
