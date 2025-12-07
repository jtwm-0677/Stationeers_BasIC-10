# Flow Control Nodes - Quick Reference Card

## Entry/Exit
| Node | Inputs | Outputs | Generated Code |
|------|--------|---------|----------------|
| **EntryPoint** | - | Exec | `' --- Program Start ---` |
| **End** | Exec | - | `END` |

## Conditionals
| Node | Inputs | Outputs | Generated Code |
|------|--------|---------|----------------|
| **If** | Exec, Condition (Bool) | True, False, Done | `IF ... THEN ... ELSE ... ENDIF` |
| **SelectCase** | Exec, Value (Num) | Case 0..N, Default, Done | `SELECT CASE ... END SELECT` |

## Loops
| Node | Inputs | Outputs | Generated Code |
|------|--------|---------|----------------|
| **While** | Exec, Condition (Bool) | LoopBody, Done | `WHILE ... WEND` |
| **For** | Exec, Start (Num), End (Num) | LoopBody, Done, Index | `FOR i = ... TO ... STEP ... NEXT i` |
| **DoUntil** | Exec, Condition (Bool) | LoopBody, Done | `DO ... LOOP UNTIL ...` |

## Loop Control
| Node | Inputs | Outputs | Generated Code |
|------|--------|---------|----------------|
| **Break** | Exec | - | `BREAK` |
| **Continue** | Exec | - | `CONTINUE` |

## Labels & Jumps
| Node | Inputs | Outputs | Generated Code |
|------|--------|---------|----------------|
| **Label** | Exec (opt) | Exec | `labelName:` |
| **Goto** | Exec | - | `GOTO labelName` |
| **Gosub** | Exec | Exec | `GOSUB labelName` |
| **Return** | Exec | - | `RETURN` |

## Execution Control
| Node | Inputs | Outputs | Generated Code |
|------|--------|---------|----------------|
| **Yield** | Exec | Exec | `YIELD` |
| **Sleep** | Exec, Duration (Num) | Exec | `SLEEP duration` |

## Properties

### Loop Nodes (While, For, DoUntil)
- **AutoYield** (bool, default: true) - Automatically insert YIELD at end of loop

### ForNode
- **VariableName** (string, default: "i") - Loop counter variable
- **Step** (double, default: 1.0) - Loop increment

### LabelNode
- **LabelName** (string) - Name of the label

### GotoNode / GosubNode
- **TargetLabel** (string) - Target label to jump to

### SelectCaseNode
- **CaseValues** (List&lt;int&gt;) - List of case values

## Pin Colors
- **Execution (white)**: Control flow
- **Number (blue)**: Numeric values
- **Boolean (green)**: True/False conditions

## Common Patterns

### Simple Loop
```
[While] -> [LoopBody] -> [Action] -> [Yield]
  |
 [Done] -> [Next]
```

### Conditional Break
```
[While] -> [If] -> True -> [Break]
  |         |
  |        False -> [Continue]
  |
 [LoopBody]
```

### Nested Loops
```
[For (outer)] -> [For (inner)] -> [Action]
     |                |
    [Done]           [Done]
```

### Subroutine Call
```
[Gosub] -> [Exec]
   |
   v
[Label] -> [Subroutine Code] -> [Return]
```

## Tips

1. **Always use YIELD in loops** to prevent Stationeers lockup
2. **Done pins** fire after all branches complete
3. **Break/Continue** only work inside loops
4. **Labels** must be unique
5. **SelectCase** supports dynamic case values
6. **Entry points** should be unique per graph

## Code Generation Notes

- Uses `Indent()`/`Unindent()` for proper nesting
- Follows execution chains via white wires
- Branches are generated recursively
- Auto-YIELD inserted before loop end markers (WEND, NEXT, LOOP)
