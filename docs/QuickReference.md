# BASIC-IC10 Quick Reference Card

## Program Structure
```basic
' BASIC comment
# IC10 comment (also valid)
ALIAS device d0
DEFINE CONST 123
VAR variable = 0

main:
    ' Code here
    YIELD
    GOTO main
END
```

## Variables & Constants
| Syntax | Description |
|--------|-------------|
| `VAR x = 0` | Declare variable |
| `LET x = 5` | Assign value |
| `DEFINE PI 3.14` | Define constant |
| `DIM arr(10)` | Declare array |

## Operators
| Type | Operators |
|------|-----------|
| Math | `+ - * / % ^` |
| Compare | `= <> < > <= >=` |
| Logic | `AND OR NOT XOR` |
| Bitwise | `BAND BOR BXOR BNOT SHL SHR` |

## Control Flow
```basic
IF cond THEN
    ' code
ELSEIF cond2 THEN
    ' code
ELSE
    ' code
ENDIF

FOR i = 1 TO 10 STEP 1
    ' code
NEXT i

WHILE condition
    ' code
WEND

GOTO label
GOSUB label
RETURN
```

## Device Access
```basic
' Read property
x = device.Property

' Write property
device.Property = value

' Slot access
x = device[slot].Property
```

## Common Properties
| Property | R/W | Description |
|----------|-----|-------------|
| On | RW | Power state |
| Setting | RW | Target value |
| Mode | RW | Operating mode |
| Open | RW | Open/Close |
| Lock | RW | Lock state |
| Temperature | R | Temp (Kelvin) |
| Pressure | R | Press (kPa) |
| Charge | R | Battery (0-1) |
| RatioOxygen | R | O2 ratio |

## Math Functions
| Function | Description |
|----------|-------------|
| `ABS(x)` | Absolute value |
| `SQRT(x)` | Square root |
| `MIN(a,b)` | Minimum |
| `MAX(a,b)` | Maximum |
| `FLOOR(x)` | Round down |
| `CEIL(x)` | Round up |
| `ROUND(x)` | Round nearest |
| `SIN(x)` | Sine |
| `COS(x)` | Cosine |
| `TAN(x)` | Tangent |
| `LOG(x)` | Natural log |
| `EXP(x)` | e^x |
| `RAND()` | Random 0-1 |

## Batch Operations
```basic
' Read from all devices of type
x = BATCHREAD(hash, prop, mode)
' Modes: 0=Avg, 1=Sum, 2=Min, 3=Max

' Write to all devices of type
BATCHWRITE(hash, prop, value)
```

## Stack Operations
```basic
PUSH value      ' Push onto stack
POP variable    ' Pop from stack
PEEK variable   ' Read top without pop
```

## Flow Control
| Statement | Description |
|-----------|-------------|
| `YIELD` | Wait one tick |
| `SLEEP n` | Wait n seconds |
| `HCF` | Halt execution |

## Keyboard Shortcuts
| Key | Action |
|-----|--------|
| F5 | Compile |
| F9 | Simulator |
| Ctrl+S | Save |
| Ctrl+F | Find |
| Ctrl+Space | Autocomplete |

## Bidirectional Editing
- IC10 panel is now **editable**
- **"To BASIC"** button decompiles IC10
- Both `'` and `#` comments work in BASIC editor
- Auto-detects BASIC vs IC10 code

## Temperature Conversions
```basic
' Kelvin to Celsius
celsius = kelvin - 273.15

' Celsius to Kelvin
kelvin = celsius + 273.15
```

## Common Device Hashes
| Device | Hash |
|--------|------|
| Solar Panel | -539224550 |
| Gas Sensor | 1255689925 |
| Wall Heater | -1253014094 |
| Wall Cooler | 1621028804 |
| Volume Pump | -321403609 |
| Battery | -1388288459 |

## Tips
- Max 128 lines in IC10
- Use YIELD in main loop
- Batch ops for multiple devices
- Use hysteresis for control
- Test in simulator first

---
*BASIC-IC10 Compiler v1.0*
