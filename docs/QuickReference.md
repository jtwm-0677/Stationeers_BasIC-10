# BASIC-IC10 Quick Reference Card

## Program Structure
```basic
# Comment (recommended - works in BASIC and IC10)
' BASIC comment (traditional style)
ALIAS device d0
DEFINE CONST 123
VAR variable = 0

main:
    # Code here
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
    # code
ELSEIF cond2 THEN
    # code
ELSE
    # code
ENDIF

FOR i = 1 TO 10 STEP 1
    # code
    BREAK       # Exit loop early
    CONTINUE    # Skip to next iteration
NEXT i

WHILE condition
    # code
WEND

DO
    # code
LOOP UNTIL condition

SELECT CASE x
CASE 1:
    # code for x = 1
CASE 2, 3:
    # code for x = 2 or 3
DEFAULT:
    # code for other values
END SELECT

GOTO label
GOSUB label
RETURN
```

## Device Access
```basic
# Read property
x = device.Property

# Write property
device.Property = value

# Slot access
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
| `TRUNC(x)` | Truncate to integer |
| `SIN(x)` | Sine |
| `COS(x)` | Cosine |
| `TAN(x)` | Tangent |
| `ASIN(x)` | Arcsine |
| `ACOS(x)` | Arccosine |
| `ATAN(x)` | Arctangent |
| `ATAN2(y,x)` | Two-arg arctangent |
| `LOG(x)` | Natural log |
| `LOG10(x)` | Base-10 log |
| `EXP(x)` | e^x |
| `POW(a,b)` | a raised to b |
| `RAND()` | Random 0-1 |
| `SGN(x)` | Sign (-1, 0, 1) |

## Comparison Functions
| Function | Description |
|----------|-------------|
| `SEQZ(x)` | 1 if x = 0 |
| `SNEZ(x)` | 1 if x ≠ 0 |
| `SLTZ(x)` | 1 if x < 0 |
| `SGTZ(x)` | 1 if x > 0 |
| `SLEZ(x)` | 1 if x ≤ 0 |
| `SGEZ(x)` | 1 if x ≥ 0 |
| `ISNAN(x)` | 1 if NaN |
| `APPROX(a,b,e)` | 1 if \|a-b\| < e |

## Utility Functions
| Function | Description |
|----------|-------------|
| `SELECT(c,t,f)` | If c then t else f |
| `CLAMP(x,a,b)` | Clamp x to [a,b] |
| `LERP(a,b,t)` | Linear interpolation |
| `INRANGE(x,a,b)` | 1 if a ≤ x ≤ b |
| `HASH("str")` | CRC-32 of string |
| `SDSE(hash)` | 1 if device exists |
| `SDNS(hash)` | 1 if device missing |

## Batch Operations
```basic
# Read from all devices of type
x = BATCHREAD(hash, prop, mode)
# Modes: 0=Avg, 1=Sum, 2=Min, 3=Max

# Write to all devices of type
BATCHWRITE(hash, prop, value)
```

## Stack Operations
```basic
PUSH value      # Push onto stack
POP variable    # Pop from stack
PEEK variable   # Read top without pop
```

## Flow Control
| Statement | Description |
|-----------|-------------|
| `YIELD` | Wait one tick |
| `SLEEP n` | Wait n seconds |
| `END` | Halt execution |

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
- `#` comments recommended (works in BASIC and IC10)
- `'` and `REM` comments also supported (traditional BASIC)
- Auto-detects BASIC vs IC10 code

## Temperature Conversions
```basic
# Kelvin to Celsius
celsius = kelvin - 273.15

# Celsius to Kelvin
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
| Battery (Large) | -1388288459 |
| Battery (Small) | -1833420725 |
| Arc Furnace | -132347264 |
| Electrolyzer | 1969822547 |
| Logic Memory | -795663717 |
| Logic Switch | -1257439961 |
| Dial | 1221026008 |
| Console (LED) | -815193061 |
| Wall Light | -1840194338 |
| Active Vent | -1129453144 |
| Passive Vent | -1663152985 |
| Filtration | -348054045 |
| Air Conditioner | 1621028804 |
| Transformer | 1790053042 |
| Stirling Engine | 498451658 |

## LED Color Codes
| Color | Value | Hex |
|-------|-------|-----|
| Red | 16711680 | #FF0000 |
| Green | 65280 | #00FF00 |
| Blue | 255 | #0000FF |
| Yellow | 16776960 | #FFFF00 |
| Orange | 16744448 | #FFA500 |
| Purple | 8388736 | #800080 |
| Cyan | 65535 | #00FFFF |
| White | 16777215 | #FFFFFF |
| Black | 0 | #000000 |
| Pink | 16761035 | #FFC0CB |

## Mode Values
| Device | Mode | Description |
|--------|------|-------------|
| Pump/Vent | 0 | Outward |
| Pump/Vent | 1 | Inward |
| Filtration | 0 | O2 |
| Filtration | 1 | N2 |
| Filtration | 2 | CO2 |
| Filtration | 3 | H2 (Volatiles) |
| Filtration | 4 | H2O |
| Filtration | 5 | Pollutant |
| Filtration | 6 | N2O |
| Sorter | 0-N | Item class |

## Slot Class Values (Sorters)
| Class | Value | Items |
|-------|-------|-------|
| None | 0 | Empty |
| Helmet | 1 | Helmets |
| Suit | 2 | Suits |
| Back | 3 | Jetpacks, tanks |
| GasFilter | 4 | Filters |
| GasCanister | 5 | Canisters |
| Motherboard | 6 | Circuits |
| Circuitboard | 7 | Logic boards |
| DataDisk | 8 | Disks, cartridges |
| Ore | 9 | Raw ores |
| Plant | 10 | Seeds, plants |
| Uniform | 11 | Clothes |
| Entity | 12 | Eggs, creatures |
| Battery | 13 | Batteries |
| Ingot | 14 | Metal ingots |
| Torpedo | 15 | Missiles |
| Cartridge | 16 | Tool cartridges |
| AccessCard | 17 | Access cards |
| Magazine | 18 | Ammo |
| Tool | 21 | Tools |
| Appliance | 22 | Kitchen items |
| Food | 23 | Food items |
| DrillHead | 25 | Drill heads |
| ScanningHead | 26 | Scanner heads |
| Blocked | 27 | Unusable slot |

## Tips
- Max 128 lines in IC10
- Use YIELD in main loop
- Batch ops for multiple devices
- Use hysteresis for control
- Test in simulator first

---
*Stationeers Basic-10 v3.0*
