# Phase 3A: Quick Reference Guide

## Node Quick Reference

### Variable Nodes

| Node | Icon | Inputs | Outputs | Generates | Config |
|------|------|--------|---------|-----------|--------|
| **ConstantNode** | 🔢 | - | Value (Number) | `42` | Value (double) |
| **ConstNode** | 🔒 | - | - | `CONST PI = 3.14` | Name, Value |
| **DefineNode** | 📝 | - | - | `DEFINE MAX 100` | Name, Value |
| **ArrayNode** | 📊 | - | Array (Number) | `DIM arr(10)` | Name, Size |
| **ArrayAccessNode** | 🔍 | Index | Value | `arr(5)` | Name |
| **ArrayAssignNode** | 📥 | Exec, Index, Value | Exec | `arr(5) = val` | Name |

### Math Operation Nodes

| Node | Icon | Inputs | Outputs | Generates |
|------|------|--------|---------|-----------|
| **AddNode** | ➕ | A, B | Result | `a + b` |
| **SubtractNode** | ➖ | A, B | Result | `a - b` |
| **MultiplyNode** | ✖️ | A, B | Result | `a * b` |
| **DivideNode** | ➗ | A, B | Result | `a / b` |
| **ModuloNode** | 📐 | A, B | Result | `a MOD b` |
| **PowerNode** | 🔺 | Base, Exponent | Result | `base ^ exp` |

### Advanced Math Nodes

| Node | Icon | Inputs | Outputs | Generates | Config |
|------|------|--------|---------|-----------|--------|
| **NegateNode** | ➖ | Value | Result | `-value` | - |
| **MathFunctionNode** | 🧮 | X (or none) | Result | `ABS(x)`, `RND()` | Function type |
| **MinMaxNode** | ⬌ | A, B | Result | `MIN(a,b)`, `MAX(a,b)` | MIN/MAX |
| **TrigNode** | 📐 | Angle | Result | `SIN(x)`, `COS(x)`, etc. | Function |
| **Atan2Node** | 📐 | Y, X | Result | `ATAN2(y,x)` | - |
| **ExpLogNode** | 📈 | X | Result | `EXP(x)`, `LOG(x)` | EXP/LOG |

### Compound Assignment Nodes

| Node | Icon | Inputs | Outputs | Generates | Config |
|------|------|--------|---------|-----------|--------|
| **CompoundAssignNode** | ⚡ | Exec, Value | Exec | `x += val` | Operator, VarName |
| **IncrementNode** | 🔼 | Exec | Exec, Value | `++x`, `x++` | Type, Position, VarName |

### Comparison Nodes

| Node | Icon | Inputs | Outputs | Generates | Config |
|------|------|--------|---------|-----------|--------|
| **CompareNode** | ⚖️ | A, B (Number) | Result (Bool) | `a = b`, `a < b` | Operator |

### Logical Nodes

| Node | Icon | Inputs | Outputs | Generates |
|------|------|--------|---------|-----------|
| **AndNode** | ∧ | A, B (Bool) | Result (Bool) | `a AND b` |
| **OrNode** | ∨ | A, B (Bool) | Result (Bool) | `a OR b` |
| **NotNode** | ¬ | Value (Bool) | Result (Bool) | `NOT val` |

### Bitwise Nodes

| Node | Icon | Inputs | Outputs | Generates | Config |
|------|------|--------|---------|-----------|--------|
| **BitwiseNode** | 🔣 | A, B | Result | `BAND(a,b)`, `BOR(a,b)` | Operation |
| **BitwiseNotNode** | 🔀 | Value | Result | `BNOT(val)` | - |
| **ShiftNode** | ⇄ | Value, Bits | Result | `SHL(val,bits)`, `SHR(val,bits)` | Direction |

## Pin Types

| Type | Color | Usage |
|------|-------|-------|
| **Execution** | White | Control flow |
| **Number** | Blue | Numeric values |
| **Boolean** | Green/Red | True/false values |
| **Device** | Orange | Device references |
| **String** | Purple | Text values |

## Usage Examples

### Simple Math Expression
```
ConstantNode(5) → AddNode → Result
ConstantNode(3) ┘
// Generates: 5 + 3
```

### Variable Declaration and Use
```
ConstNode(PI=3.14159)
VariableNode(radius=10)
→ MultiplyNode → MultiplyNode → ConstantNode(2) → Result
                ↑ radius
// Generates: CONST PI = 3.14159
//           VAR radius = 10
//           2 * PI * radius
```

### Array Operations
```
ArrayNode(temps, 10) → ArrayAssignNode
                       ↑ Index: 5
                       ↑ Value: 25.5
// Generates: DIM temps(10)
//           temps(5) = 25.5
```

### Conditional Logic
```
VariableNode(temp) → CompareNode(>) → AndNode → Result
ConstantNode(30)    ┘                 ↑
VariableNode(active) → CompareNode(=) ┘
ConstantNode(1)       ┘
// Generates: temp > 30 AND active = 1
```

### Math Functions
```
VariableNode(x) → MathFunctionNode(SQRT) → Result
// Generates: SQRT(x)

VariableNode(angle) → TrigNode(SIN) → Result
// Generates: SIN(angle)
```

### Compound Operations
```
VariableNode(counter) → CompoundAssignNode(+=) → Next
                        ↑ Value: ConstantNode(1)
// Generates: counter += 1

VariableNode(index) → IncrementNode(++) → Next
// Generates: ++index
```

### Bitwise Operations
```
VariableNode(flags) → BitwiseNode(AND) → Result
ConstantNode(0xFF)  ┘
// Generates: BAND(flags, 255)

VariableNode(bits) → ShiftNode(LEFT) → Result
ConstantNode(2)    ┘
// Generates: SHL(bits, 2)
```

## Configuration Options

### MathFunctionNode
- ABS (Absolute value)
- SQRT (Square root)
- CEIL (Ceiling)
- FLOOR (Floor)
- ROUND (Round)
- TRUNC (Truncate)
- SGN (Sign)
- RND (Random)

### TrigNode
- SIN (Sine)
- COS (Cosine)
- TAN (Tangent)
- ASIN (Arc sine)
- ACOS (Arc cosine)
- ATAN (Arc tangent)

### CompareNode
- = (Equal)
- <> (Not equal)
- < (Less than)
- > (Greater than)
- <= (Less than or equal)
- >= (Greater than or equal)

### CompoundAssignNode
- += (Add assign)
- -= (Subtract assign)
- *= (Multiply assign)
- /= (Divide assign)

### IncrementNode
- Type: ++ (Increment), -- (Decrement)
- Position: Prefix, Postfix

### BitwiseNode
- AND (&) - BAND
- OR (|) - BOR
- XOR (^) - BXOR

### ShiftNode
- Left (<<) - SHL
- Right (>>) - SHR

## Node Categories

Use these categories when searching in the node palette:

- **Variables**: ConstantNode, ConstNode, DefineNode, ArrayNode, ArrayAccessNode, ArrayAssignNode, CompoundAssignNode, IncrementNode
- **Math**: All math, trig, and bitwise nodes
- **Logic**: CompareNode, AndNode, OrNode, NotNode
- **Comments**: CommentNode (from Phase 1)

## Validation Rules

### Variable Names
- Must start with a letter
- Can contain letters, numbers, and underscores
- Case sensitive
- No spaces or special characters

### Array Sizes
- Must be positive integers
- No upper limit validation (compiler handles)

### Constants and Defines
- Follow variable name rules
- Values must be valid numbers

## Code Generation Tips

1. **Expression Nodes**: Generate code fragments, not statements
2. **Statement Nodes**: Generate complete lines with semicolons (if needed)
3. **Declaration Nodes**: Generate at module level
4. **Value Connections**: Actual values come from connected nodes
5. **Placeholder Names**: Use descriptive placeholders (a, b, value, etc.)

## Common Patterns

### Initialize Variables
```
ConstNode → DefineNode → ArrayNode → VariableNode
```

### Calculate Expression
```
VariableNode → MathNode → MathNode → Result
               ↑           ↑
          ConstantNode  VariableNode
```

### Conditional Check
```
VariableNode → CompareNode → LogicalNode → Result
ConstantNode ┘              ↑
                            CompareNode
```

### Loop Counter
```
IncrementNode → CompareNode → Branch
    ↑              ↑
    counter    ConstantNode(max)
```

## Performance Notes

- Lightweight nodes (minimal memory)
- Fast initialization
- No heavy computations
- String-based code generation
- Pin position calculations cached

## Best Practices

1. **Name Variables Clearly**: Use descriptive names
2. **Group Related Nodes**: Keep related operations together
3. **Comment Complex Logic**: Use CommentNode
4. **Validate Early**: Check node validation before code gen
5. **Test Incrementally**: Test each node type separately
6. **Use Constants**: Define magic numbers as CONST
7. **Organize by Category**: Keep variables, math, logic separate

## Troubleshooting

### Node Won't Connect
- Check pin types match (Number → Number, etc.)
- Ensure input/output direction correct
- Verify node initialized properly

### Invalid Code Generated
- Check node validation first
- Verify all required inputs connected
- Ensure variable names valid
- Check operator/function names

### Node Not Found in Factory
- Verify node registered in NodeSystemExample
- Check node type string matches NodeType property
- Ensure node class is public and has parameterless constructor

## Next Steps

After mastering Phase 3A nodes:

1. **Phase 3B**: Control flow (IF, WHILE, FOR, GOTO)
2. **Phase 3C**: Device operations
3. **Phase 3D**: Visual canvas implementation
4. **Phase 3E**: Full code generation engine
5. **Phase 3F**: Testing and optimization

## Quick Node Count

- **Variable**: 6 nodes
- **Math Basic**: 6 nodes
- **Math Advanced**: 6 nodes
- **Compound**: 2 nodes
- **Logical**: 4 nodes
- **Bitwise**: 3 nodes
- **Total**: 27 nodes

---

**Quick Tip**: Use Ctrl+F to search this reference for specific node types or operations!
