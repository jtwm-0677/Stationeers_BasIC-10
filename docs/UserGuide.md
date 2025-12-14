# Basic-10 User Guide

**Stationeers BASIC to IC10 Compiler**
*By Dog Tired Studios*
**Version 3.0**

---

## Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Getting Started](#getting-started)
4. [Main Interface Overview](#main-interface-overview)
5. [Menu Reference](#menu-reference)
6. [Keyboard Shortcuts](#keyboard-shortcuts)
7. [Editor Features](#editor-features)
8. [BASIC Language Reference](#basic-language-reference)
9. [Device Operations](#device-operations)
10. [Built-in Functions](#built-in-functions)
11. [Advanced Features](#advanced-features)
12. [Simulator](#simulator)
13. [Troubleshooting](#troubleshooting)
14. [Appendix](#appendix)

---

## Introduction

Basic-10 is a comprehensive development environment for writing Stationeers IC10 automation code using a friendly BASIC-like syntax. Instead of writing complex IC10 MIPS assembly directly, you can write in an intuitive high-level language that compiles to optimized IC10 code.

### Key Features

- **Modern IDE** with syntax highlighting, autocomplete, and error checking
- **BASIC-style programming** that's easier to read and write than raw IC10
- **Bidirectional editing** - Edit BASIC or IC10 code, decompile IC10 back to BASIC
- **Integrated simulator** for testing code before deploying
- **Device Hash Database** with all Stationeers devices and properties
- **Extensible** with custom device definitions via JSON files
- **Colorblind-friendly** syntax highlighting presets
- **Retro aesthetic options** including Apple ][ and TRS-80 fonts

### Why Use Basic-10?

| Raw IC10 | Basic-10 BASIC |
|----------|----------------|
| Cryptic assembly instructions | Readable English-like syntax |
| Manual register management | Automatic variable allocation |
| Limited comments (128 line limit) | Unlimited comments (stripped on compile) |
| No structured loops | FOR, WHILE, DO loops |
| Manual jump calculations | Automatic label resolution |

---

## Installation

### System Requirements

- Windows 10 or later (64-bit)
- .NET 8.0 Runtime (included in self-contained package)
- 100 MB disk space
- 1280x720 minimum screen resolution

### Installation Steps

1. **Download** the latest release from GitHub
2. **Extract** the zip file to your desired location
3. **Run** `Basic_10.exe` to launch the application
4. **Optional**: Set your Stationeers installation directory via **Tools > Set Stationeers Directory**

### First-Time Setup

On first launch, Basic-10 will:
- Create a settings file in your user profile
- Display the "Getting Started" documentation panel
- Set default optimization and output options

---

## Getting Started

### Your First Program

1. Launch Basic-10
2. The editor opens with a blank file
3. Type the following simple program:

```basic
# My first Basic-10 program
ALIAS sensor d0
ALIAS light d1

VAR temp = 0

main:
    temp = sensor.Temperature

    IF temp > 300 THEN
        light.On = 1
    ELSE
        light.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

4. Press **F5** to compile
5. The IC10 output appears in the bottom panel
6. Click **Copy IC10** or press **F6** to copy to clipboard
7. Paste into an IC10 Housing in Stationeers

### Understanding the Workflow

```
[Write BASIC Code] --> [Compile (F5)] --> [IC10 Output] --> [Copy to Game]
        ^                                       |
        |                                       v
        +----------- [Decompile] <---------[Edit IC10]
```

---

## Main Interface Overview

### Layout

```
+------------------------------------------------------------------+
|  Menu Bar                                                  Banner |
+------------------------------------------------------------------+
|  Toolbar (New, Open, Save, Undo, Redo, Find, Compile, Copy)      |
+--------+------------------------------------------+---------------+
|        |                                          |               |
| Symbols|         BASIC Source Editor              | Documentation |
| Panel  |                                          |    Panel      |
|        |                                          |               |
|Variables------------------------------------------|  Start        |
|Constants|                                         |  Syntax       |
| Labels |  (Line numbers, syntax highlighting)    |  Funcs        |
| Aliases|                                          |  Devices      |
|        +------------------------------------------+  IC10         |
|        |                                          |  Tips         |
|        |         IC10 MIPS Output                 |  Examples     |
|        |                                          |  Wiki         |
|        |  (Editable, with decompile option)       |               |
+--------+------------------------------------------+---------------+
|  Error/Warning Panel                     Action Buttons           |
+------------------------------------------------------------------+
|  Status Bar: Ready | File Path | Ln/Col | IC10: 0/128            |
+------------------------------------------------------------------+
```

### Panel Descriptions

| Panel | Purpose |
|-------|---------|
| **Symbols Panel** | Lists all variables, constants, labels, and aliases in your code |
| **BASIC Source Editor** | Main code editor with syntax highlighting |
| **IC10 MIPS Output** | Compiled assembly code (editable!) |
| **Documentation Panel** | Built-in reference with tabs for syntax, functions, devices |
| **Error Panel** | Shows compilation errors and warnings |
| **Status Bar** | Current file, cursor position, line count |

---

## Menu Reference

### File Menu

| Menu Item | Shortcut | Description |
|-----------|----------|-------------|
| **New** | Ctrl+N | Create a new blank file |
| **Open...** | Ctrl+O | Open an existing .bas file |
| **Save** | Ctrl+S | Save current file |
| **Save As...** | Ctrl+Shift+S | Save with a new filename |
| **Export IC10...** | - | Export compiled IC10 to .ic10 file |
| **Recent Files** | - | Quick access to recently opened files |
| **Import IC10...** | - | Import IC10 file and decompile to BASIC |
| **Import IC10 from Clipboard** | - | Paste IC10 from clipboard and decompile |
| **Exit** | Alt+F4 | Close the application |

### Edit Menu

| Menu Item | Shortcut | Description |
|-----------|----------|-------------|
| **Undo** | Ctrl+Z | Undo last action |
| **Redo** | Ctrl+Y | Redo undone action |
| **Cut** | Ctrl+X | Cut selected text |
| **Copy** | Ctrl+C | Copy selected text |
| **Paste** | Ctrl+V | Paste from clipboard |
| **Find...** | Ctrl+F | Open find dialog |
| **Replace...** | Ctrl+H | Open find and replace dialog |
| **Insert Snippet** | - | Insert code templates |
| **Format Document** | Ctrl+Shift+F | Auto-format code indentation |

### Build Menu

| Menu Item | Shortcut | Description |
|-----------|----------|-------------|
| **Compile** | F5 | Compile BASIC to IC10 |
| **Compile and Copy IC10** | F6 | Compile and copy result to clipboard |
| **Run Simulator...** | F9 | Open the IC10 simulator window |
| **Optimization Level** | - | Set optimization (None/Basic/Aggressive) |
| **Output Mode** | - | Set output format (Readable/Compact/Debug) |
| **Auto-compile on Save** | - | Toggle automatic compilation on save |
| **Include Source Line Numbers** | - | Add BASIC line numbers as IC10 comments |

#### Optimization Levels

| Level | Description |
|-------|-------------|
| **None** | Fastest compilation, no optimization |
| **Basic** | Recommended - removes dead code, optimizes simple patterns |
| **Aggressive** | Smallest output - maximum optimization, may change code structure |

#### Output Modes

| Mode | Description |
|------|-------------|
| **Readable** | Preserves comments and formatting (default) |
| **Compact** | Strips comments, minimizes whitespace |
| **Debug** | Includes source line numbers on each instruction |

### View Menu

| Menu Item | Shortcut | Description |
|-----------|----------|-------------|
| **Show Symbols Panel** | F4 | Toggle left symbols panel |
| **Show Documentation Panel** | - | Toggle right documentation panel |
| **Show Snippets Panel** | F6 | Toggle floating snippets popup |
| **Show Simulator Window** | F7 | Open/close simulator window |
| **Show Watch Window** | F8 | Open variable watch window |
| **Show Variable Inspector** | F9 | Open variable inspector |
| **Show Line Numbers** | - | Toggle line numbers in editor |
| **Word Wrap** | - | Toggle word wrapping |
| **Split View** | - | Change editor/output layout |
| **Retro Effects** | - | Visual effects submenu |
| **Zoom In/Out/Reset** | Ctrl++/- | Adjust editor zoom |

#### Split View Options

- **Horizontal Split** (Top/Bottom) - BASIC above, IC10 below
- **Vertical Split** (Side by Side) - BASIC left, IC10 right
- **Editor Only** - Hide IC10 output panel

#### Retro Effects

| Effect | Description |
|--------|-------------|
| **Block Cursor** | Classic blinking block cursor |
| **Current Line Highlight** | Subtle highlight on cursor line |
| **Scanline Overlay** | Faint CRT-style horizontal lines |
| **Screen Glow** | Phosphor glow effect around text |
| **Retro Font** | Enable retro font style |
| **Font Style** | Choose: Default (Consolas), Apple ][, TRS-80 |
| **Startup Beep** | Classic "READY" beep on launch |

### Tools Menu

| Menu Item | Shortcut | Description |
|-----------|----------|-------------|
| **Device Hash Database...** | - | Browse all device types and hashes |
| **Settings...** | - | Open application settings |
| **Syntax Colors...** | - | Customize syntax highlighting colors |
| **Set Stationeers Directory...** | - | Set game installation path |
| **Open Stationeers Scripts Folder** | - | Open scripts folder in Explorer |
| **Debug Console...** | F12 | Open debug console (developer tool) |

### Help Menu

| Menu Item | Shortcut | Description |
|-----------|----------|-------------|
| **Documentation** | F1 | Open documentation panel |
| **Quick Start Guide** | - | Display getting started guide |
| **Language Reference** | - | Display language reference |
| **Examples** | - | Load example programs |
| **Check for Updates...** | - | Check for new versions |
| **About** | - | Version and credits information |

---

## Keyboard Shortcuts

### File Operations
| Shortcut | Action |
|----------|--------|
| Ctrl+N | New File |
| Ctrl+O | Open File |
| Ctrl+S | Save File |
| Ctrl+Shift+S | Save As |

### Editing
| Shortcut | Action |
|----------|--------|
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+X | Cut |
| Ctrl+C | Copy |
| Ctrl+V | Paste |
| Ctrl+A | Select All |
| Ctrl+F | Find |
| Ctrl+H | Replace |
| Ctrl+Shift+F | Format Document |

### Build & Debug
| Shortcut | Action |
|----------|--------|
| F5 | Compile |
| F6 | Compile and Copy to Clipboard |
| F7 | Toggle Simulator |
| F8 | Toggle Watch Window |
| F9 | Toggle Variable Inspector / Breakpoint |

### View & Navigation
| Shortcut | Action |
|----------|--------|
| F1 | Help/Documentation |
| F4 | Toggle Symbols Panel |
| Ctrl++ | Zoom In |
| Ctrl+- | Zoom Out |
| Ctrl+Space | Trigger Autocomplete |

---

## Editor Features

### Syntax Highlighting

Basic-10 provides comprehensive syntax highlighting for:

| Element | Example | Default Color |
|---------|---------|---------------|
| Keywords | `IF`, `THEN`, `WHILE`, `FOR` | Blue |
| Declarations | `VAR`, `LET`, `ALIAS`, `DEFINE` | Teal |
| Device References | `d0`, `d1`, `db` | Light Blue |
| Properties | `.Temperature`, `.On`, `.Setting` | Light Blue |
| Functions | `ABS`, `SIN`, `MAX`, `SQRT` | Yellow |
| Labels | `main:`, `loop:` | Purple |
| Strings | `"Hello World"` | Orange |
| Numbers | `123`, `3.14159` | Light Green |
| Comments | `# comment` (recommended), `' comment` | Green |
| Booleans | `TRUE`, `FALSE` | Blue |
| Operators | `+`, `-`, `*`, `/`, `=` | Gray |
| Brackets | `[`, `]` | Gold |

### Colorblind Presets

Access via **Tools > Syntax Colors...** and select from presets:

| Preset | Description |
|--------|-------------|
| **Default** | Standard VS Code-inspired colors |
| **Protanopia** | Red-blind friendly (blue/yellow focus) |
| **Deuteranopia** | Green-blind friendly (blue/orange focus) |
| **Tritanopia** | Blue-blind friendly (red/green focus) |
| **High Contrast** | Maximum distinction with bold primary colors |
| **Monochrome** | Grayscale with brightness variation |
| **Custom** | Create your own color scheme |

### Autocomplete

Press **Ctrl+Space** to trigger autocomplete suggestions for:
- Keywords and language constructs
- Device properties
- Built-in functions
- Defined variables, constants, and aliases

### Error Highlighting

Real-time error detection highlights:
- Syntax errors (red underline)
- Undefined variables (yellow warning)
- Type mismatches
- Device property errors

### Code Folding

Collapse sections of code:
- IF...ENDIF blocks
- FOR...NEXT loops
- WHILE...WEND loops
- SUB...END SUB blocks

### Line Numbers

Line numbers are shown by default. Toggle via **View > Show Line Numbers**.

The status bar shows:
- Current line and column position
- Total lines in BASIC source
- IC10 output lines (out of 128 maximum)

---

## BASIC Language Reference

### Comments

```basic
# Comment (recommended - works in BASIC and IC10)
' Single line comment (traditional BASIC style)
REM This is also a comment (traditional BASIC style)
x = 5  # Inline comment
```

**Note**: All comment styles are stripped during compilation and don't count toward the IC10 128-line limit. Using `#` comments is recommended because they work in both BASIC source and IC10 output.

### Variables and Constants

```basic
' Variable declaration
VAR temperature = 0
VAR pressure = 101.325
VAR isActive = 1

' Assignment
LET x = 10
x = x + 1

' Constants (cannot be changed)
DEFINE MAX_TEMP 373.15
DEFINE PI 3.14159
DEFINE SOLAR_HASH -539224550
```

### Data Types

All values are floating-point numbers:

```basic
x = 42              ' Integer
y = 3.14159         ' Decimal
z = -273.15         ' Negative
w = 1.5e6           ' Scientific notation

' Boolean values: 0 = false, non-zero = true
isOn = 1            ' true
isOff = 0           ' false
```

### Operators

#### Arithmetic
| Operator | Description | Example |
|----------|-------------|---------|
| `+` | Addition | `x + y` |
| `-` | Subtraction | `x - y` |
| `*` | Multiplication | `x * y` |
| `/` | Division | `x / y` |
| `%` or `MOD` | Modulo | `x % y` |
| `^` | Power | `x ^ 2` |

#### Comparison
| Operator | Description | Example |
|----------|-------------|---------|
| `=` or `==` | Equal | `x = 5` |
| `<>` or `!=` | Not equal | `x <> 0` |
| `<` | Less than | `x < 10` |
| `>` | Greater than | `x > 0` |
| `<=` | Less or equal | `x <= 100` |
| `>=` | Greater or equal | `x >= 0` |

#### Logical
| Operator | Description | Example |
|----------|-------------|---------|
| `AND` | Logical AND | `a AND b` |
| `OR` | Logical OR | `a OR b` |
| `NOT` | Logical NOT | `NOT a` |
| `XOR` | Exclusive OR | `a XOR b` |

#### Bitwise
| Operator | Description | Example |
|----------|-------------|---------|
| `BAND` or `&` | Bitwise AND | `a BAND b` |
| `BOR` or `\|` | Bitwise OR | `a BOR b` |
| `BXOR` | Bitwise XOR | `a BXOR b` |
| `SHL` or `<<` | Shift left | `a SHL 2` |
| `SHR` or `>>` | Shift right | `a SHR 2` |

### Control Flow

#### IF...THEN...ELSE...ENDIF

```basic
' Single line
IF temp > 100 THEN heater.On = 0

' Multi-line
IF temp > 300 THEN
    heater.On = 0
    cooler.On = 1
ELSEIF temp < 280 THEN
    heater.On = 1
    cooler.On = 0
ELSE
    heater.On = 0
    cooler.On = 0
ENDIF
```

#### FOR...NEXT Loop

```basic
' Count from 1 to 10
FOR i = 1 TO 10
    PRINT i
NEXT i

' With STEP
FOR i = 0 TO 100 STEP 10
    ' i = 0, 10, 20, ... 100
NEXT i

' Count down
FOR i = 10 TO 1 STEP -1
    ' i = 10, 9, 8, ... 1
NEXT i
```

#### WHILE...WEND Loop

```basic
WHILE sensor.Temperature > 300
    heater.On = 0
    YIELD
WEND
heater.On = 1
```

#### DO...LOOP

```basic
' Check condition at end
DO
    temp = sensor.Temperature
    YIELD
LOOP UNTIL temp < 300
```

#### GOTO and Labels

```basic
main:
    ' Processing code
    YIELD
    GOTO main
```

#### GOSUB...RETURN

```basic
main:
    GOSUB ReadSensors
    GOSUB ProcessData
    YIELD
    GOTO main

ReadSensors:
    temp = sensor.Temperature
    pressure = sensor.Pressure
    RETURN

ProcessData:
    ' Processing logic
    RETURN
```

### Program Control

| Statement | Description |
|-----------|-------------|
| `YIELD` | Pause for one game tick (required in loops!) |
| `SLEEP n` | Pause for n seconds |
| `END` | End program execution |
| `HCF` | Halt and Catch Fire (stops IC10 permanently) |
| `BREAK` | Exit current loop early |
| `CONTINUE` | Skip to next loop iteration |

---

## Device Operations

### Device Aliases

Assign friendly names to device ports (d0-d5):

```basic
ALIAS sensor d0          ' Gas Sensor on port 0
ALIAS heater d1          ' Wall Heater on port 1
ALIAS display d2         ' LED Display on port 2
ALIAS pump d3            ' Volume Pump on port 3
```

### Reading Device Properties

```basic
temp = sensor.Temperature       ' Read temperature
pressure = sensor.Pressure      ' Read pressure
isOn = heater.On               ' Check if on
charge = battery.Charge        ' Battery charge (0-1)
```

### Writing Device Properties

```basic
heater.On = 1                  ' Turn on
heater.Setting = 500           ' Set power level
pump.Setting = 100             ' Set target pressure
display.Setting = temp         ' Display a value
door.Open = 1                  ' Open door
device.Lock = 1                ' Lock device
```

### Reading Device Slots

```basic
occupied = storage[0].Occupied      ' Is slot occupied?
itemHash = storage[0].OccupantHash  ' Item type in slot
quantity = storage[0].Quantity      ' Item count
```

### Common Properties Reference

#### Universal Properties
| Property | R/W | Description |
|----------|-----|-------------|
| On | R/W | Power state (0 or 1) |
| Setting | R/W | Target/setting value |
| Mode | R/W | Operating mode |
| Lock | R/W | Lock state |
| Open | R/W | Open/closed state |
| Error | R | Error state |
| Power | R | Has power |

#### Atmospheric Properties
| Property | R/W | Description |
|----------|-----|-------------|
| Temperature | R | Temperature (Kelvin) |
| Pressure | R | Pressure (kPa) |
| TotalMoles | R | Total gas moles |
| RatioOxygen | R | O2 ratio (0-1) |
| RatioCarbonDioxide | R | CO2 ratio (0-1) |
| RatioNitrogen | R | N2 ratio (0-1) |
| RatioVolatiles | R | H2 ratio (0-1) |
| RatioPollutant | R | Pollutant ratio (0-1) |

#### Power Properties
| Property | R/W | Description |
|----------|-----|-------------|
| Charge | R | Battery charge (0-1) |
| PowerRequired | R | Power demand (W) |
| PowerActual | R | Current draw (W) |
| PowerGeneration | R | Power output (W) |

#### Solar Panel Properties
| Property | R/W | Description |
|----------|-----|-------------|
| Horizontal | R/W | Horizontal angle |
| Vertical | R/W | Vertical angle |
| SolarAngle | R | Current sun angle |

### Named Device References (Bypass 6-Pin Limit!)

The most powerful feature for complex automation. Reference devices by their labeler-assigned names:

```basic
' Reference devices by their custom names (set with labeler in-game)
ALIAS bedroomLight = IC.Device["StructureWallLight"].Name["Bedroom Light"]
ALIAS kitchenSensor = IC.Device["StructureGasSensor"].Name["Kitchen Sensor"]
ALIAS mainPump = IC.Device["StructurePumpVolume"].Name["Main Pump"]

' Use them like any device
bedroomLight.On = 1
temp = kitchenSensor.Temperature
mainPump.Setting = 500
```

**Important**: Use `Structure*` prefix for placed structures (not `ItemStructure*`).

### Batch Operations

Control multiple devices of the same type:

```basic
' Write to ALL devices of a type
DEFINE SOLAR_HASH -539224550
BATCHWRITE(SOLAR_HASH, Horizontal, solarAngle)

' Read from all devices (with aggregation mode)
' Mode: 0=Average, 1=Sum, 2=Min, 3=Max
avgTemp = BATCHREAD(GAS_SENSOR_HASH, Temperature, 0)
totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)
```

---

## Built-in Functions

### Mathematical Functions

| Function | Description | Example |
|----------|-------------|---------|
| `ABS(x)` | Absolute value | `ABS(-5)` = 5 |
| `SQRT(x)` | Square root | `SQRT(16)` = 4 |
| `FLOOR(x)` | Round down | `FLOOR(3.7)` = 3 |
| `CEIL(x)` | Round up | `CEIL(3.2)` = 4 |
| `ROUND(x)` | Round nearest | `ROUND(3.5)` = 4 |
| `TRUNC(x)` | Truncate decimal | `TRUNC(3.9)` = 3 |
| `MIN(a,b)` | Minimum value | `MIN(3, 7)` = 3 |
| `MAX(a,b)` | Maximum value | `MAX(3, 7)` = 7 |
| `LOG(x)` | Natural logarithm | `LOG(2.718)` = 1 |
| `LOG10(x)` | Base-10 logarithm | `LOG10(100)` = 2 |
| `EXP(x)` | e^x | `EXP(1)` = 2.718 |
| `POW(a,b)` | a raised to b | `POW(2, 3)` = 8 |
| `SGN(x)` | Sign (-1, 0, 1) | `SGN(-5)` = -1 |
| `RAND()` | Random 0-1 | `RAND()` = 0.xxx |

### Trigonometric Functions

| Function | Description |
|----------|-------------|
| `SIN(x)` | Sine (radians) |
| `COS(x)` | Cosine (radians) |
| `TAN(x)` | Tangent (radians) |
| `ASIN(x)` | Arc sine |
| `ACOS(x)` | Arc cosine |
| `ATAN(x)` | Arc tangent |
| `ATAN2(y,x)` | Two-argument arctangent |

### Comparison Functions

| Function | Description |
|----------|-------------|
| `SEQZ(x)` | 1 if x = 0 |
| `SNEZ(x)` | 1 if x != 0 |
| `SLTZ(x)` | 1 if x < 0 |
| `SGTZ(x)` | 1 if x > 0 |
| `SLEZ(x)` | 1 if x <= 0 |
| `SGEZ(x)` | 1 if x >= 0 |
| `ISNAN(x)` | 1 if x is NaN |

### Utility Functions

| Function | Description |
|----------|-------------|
| `SELECT(c,t,f)` | If c then t else f |
| `CLAMP(x,a,b)` | Clamp x to range [a,b] |
| `LERP(a,b,t)` | Linear interpolation |
| `INRANGE(x,a,b)` | 1 if a <= x <= b |
| `HASH("str")` | CRC-32 hash of string |
| `SDSE(hash)` | 1 if device exists |
| `SDNS(hash)` | 1 if device missing |
| `IIF(c,t,f)` | Inline IF (same as SELECT) |

---

## Advanced Features

### IC10 Decompiler

Basic-10 includes a decompiler that converts IC10 assembly back to BASIC:

1. **Import IC10**: File > Import IC10... or File > Import IC10 from Clipboard
2. **Decompile**: Click "To BASIC" button above the IC10 panel
3. **Edit**: Modify the decompiled BASIC code
4. **Recompile**: Press F5 to generate new IC10

The decompiler recognizes:
- Variable declarations from `alias` and `define`
- Control flow patterns (if/then, loops)
- Common code patterns

### Bidirectional Editing

The IC10 output panel is **editable**:
- Make quick fixes directly to IC10
- The "To BASIC" button decompiles your changes
- Both panels sync through compile/decompile cycle

### Stack Operations

IC10 provides a 512-value stack:

```basic
PUSH value          ' Push onto stack
POP variable        ' Pop from stack
PEEK variable       ' Read top without removing
```

### Extensible Device Database

Add custom devices via JSON files in:
- `CustomDevices.json` (same folder as executable)
- `CustomDevices/` subfolder
- `Documents/BASIC-IC10/CustomDevices/`

JSON format:
```json
{
  "devices": [
    {
      "prefabName": "StructureMyDevice",
      "category": "Custom",
      "displayName": "My Device",
      "description": "Description here"
    }
  ]
}
```

Reload via **Tools > Reload Custom Devices** (or restart application).

---

## Simulator

Access via **Build > Run Simulator** or press **F7**.

### Simulator Features

- **Step-by-step execution** - Execute one instruction at a time
- **Breakpoints** - Pause at specific lines (F9)
- **Variable watch** - Monitor variable values in real-time
- **Device simulation** - Mock device inputs/outputs
- **Register view** - See all IC10 registers (r0-r15)

### Using the Simulator

1. Compile your code (F5)
2. Open simulator (F7)
3. Configure virtual devices in the device panel
4. Click "Run" or "Step" to execute
5. Watch variables in the Watch panel (F8)

### Simulator Tips

- Set initial sensor values before running
- Use breakpoints to debug specific sections
- Step through loops to verify logic
- Check register values to debug complex expressions

---

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "Undefined variable" | Using variable before declaration | Add `VAR name = 0` |
| "Expected THEN" | IF statement syntax | Check `IF condition THEN` |
| "Line limit exceeded" | Over 128 IC10 lines | Optimize code or split programs |
| "Unknown device property" | Typo in property name | Check property spelling |
| "Label not found" | GOTO to undefined label | Define the label with `name:` |

### Performance Tips

1. **Use YIELD in loops** - Required for device values to update
2. **Cache device reads** - Read once, use variable multiple times
3. **Use batch operations** - More efficient than individual device access
4. **Optimize calculations** - Pre-calculate constants

### IC10 Line Limit

IC10 has a **128-line limit**. To stay under:
- Use optimization (Build > Optimization Level > Aggressive)
- Use compact output mode
- Combine related operations
- Use batch operations instead of multiple single-device accesses
- Remove unnecessary variables

### Debugging Tips

1. **Use Debug output mode** - See BASIC line numbers in IC10
2. **Add temporary PRINTs** - Display values to console
3. **Use simulator** - Step through code execution
4. **Check status bar** - Monitor IC10 line count

---

## Appendix

### Common Device Hashes

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

### LED Color Codes

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

### Temperature Conversions

```basic
' Kelvin to Celsius
celsius = kelvin - 273.15

' Celsius to Kelvin
kelvin = celsius + 273.15

' Fahrenheit to Celsius
celsius = (fahrenheit - 32) * 5 / 9
```

### Filtration Mode Values

| Mode | Gas |
|------|-----|
| 0 | Oxygen (O2) |
| 1 | Nitrogen (N2) |
| 2 | Carbon Dioxide (CO2) |
| 3 | Volatiles (H2) |
| 4 | Water (H2O) |
| 5 | Pollutant |
| 6 | Nitrous Oxide (N2O) |

### Pump/Vent Mode Values

| Mode | Direction |
|------|-----------|
| 0 | Outward |
| 1 | Inward |

### Slot Class Values (for Sorters)

| Class | Value | Items |
|-------|-------|-------|
| None | 0 | Empty |
| Helmet | 1 | Helmets |
| Suit | 2 | Suits |
| Back | 3 | Jetpacks, tanks |
| GasFilter | 4 | Filters |
| GasCanister | 5 | Canisters |
| Ore | 9 | Raw ores |
| Plant | 10 | Seeds, plants |
| Battery | 13 | Batteries |
| Ingot | 14 | Metal ingots |
| Tool | 21 | Tools |
| Food | 23 | Food items |

---

## Complete Code Examples

### Temperature Controller with Hysteresis

```basic
' ============================================
' Smart Temperature Controller
' Controls heating/cooling to maintain temp
' ============================================

' Devices
ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2
ALIAS display d3

' Constants
DEFINE TARGET 293.15    ' 20C in Kelvin
DEFINE TOLERANCE 2      ' +/-2 degrees

' Variables
VAR currentTemp = 0
VAR mode = 0            ' 0=off, 1=heat, 2=cool

' Main program
main:
    currentTemp = sensor.Temperature

    ' Control logic with hysteresis
    IF currentTemp < TARGET - TOLERANCE THEN
        mode = 1    ' Need heating
    ELSEIF currentTemp > TARGET + TOLERANCE THEN
        mode = 2    ' Need cooling
    ELSEIF currentTemp >= TARGET - 1 AND currentTemp <= TARGET + 1 THEN
        mode = 0    ' At target, turn off
    ENDIF

    ' Apply control
    IF mode = 1 THEN
        heater.On = 1
        cooler.On = 0
    ELSEIF mode = 2 THEN
        heater.On = 0
        cooler.On = 1
    ELSE
        heater.On = 0
        cooler.On = 0
    ENDIF

    ' Update display (Celsius)
    display.Setting = currentTemp - 273.15

    YIELD
    GOTO main
END
```

### Solar Panel Sun Tracker

```basic
' Solar Panel Sun Tracker
ALIAS panel d0

VAR horizontal = 0
VAR solarAngle = 0

main:
    solarAngle = panel.SolarAngle

    ' Calculate optimal angle
    horizontal = solarAngle

    ' Apply to panel
    panel.Horizontal = horizontal
    panel.Vertical = 60    ' Fixed tilt

    YIELD
    GOTO main
END
```

### Airlock Controller

```basic
' Airlock Door Controller
ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3

DEFINE VACUUM_THRESHOLD 1
DEFINE PRESSURIZE_THRESHOLD 90

VAR pressure = 0
VAR state = 0    ' 0=idle, 1=depressurize, 2=pressurize

main:
    pressure = sensor.Pressure

    IF state = 1 THEN
        ' Depressurizing
        innerDoor.Open = 0
        innerDoor.Lock = 1
        pump.On = 1
        pump.Mode = 0    ' Outward

        IF pressure < VACUUM_THRESHOLD THEN
            outerDoor.Lock = 0
            state = 0
        ENDIF
    ELSEIF state = 2 THEN
        ' Pressurizing
        outerDoor.Open = 0
        outerDoor.Lock = 1
        pump.On = 1
        pump.Mode = 1    ' Inward

        IF pressure > PRESSURIZE_THRESHOLD THEN
            innerDoor.Lock = 0
            state = 0
        ENDIF
    ELSE
        pump.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

### Multi-Room Temperature Control (Named Devices)

```basic
' Multi-Room Temperature Control
' Using named device references to bypass 6-pin limit

' Room 1 devices
ALIAS room1_sensor = IC.Device["StructureGasSensor"].Name["Room 1 Sensor"]
ALIAS room1_heater = IC.Device["StructureWallHeater"].Name["Room 1 Heater"]

' Room 2 devices
ALIAS room2_sensor = IC.Device["StructureGasSensor"].Name["Room 2 Sensor"]
ALIAS room2_heater = IC.Device["StructureWallHeater"].Name["Room 2 Heater"]

' Room 3 devices
ALIAS room3_sensor = IC.Device["StructureGasSensor"].Name["Room 3 Sensor"]
ALIAS room3_heater = IC.Device["StructureWallHeater"].Name["Room 3 Heater"]

DEFINE TARGET 293    ' 20C

main:
    ' Control Room 1
    IF room1_sensor.Temperature < TARGET THEN
        room1_heater.On = 1
    ELSE
        room1_heater.On = 0
    ENDIF

    ' Control Room 2
    IF room2_sensor.Temperature < TARGET THEN
        room2_heater.On = 1
    ELSE
        room2_heater.On = 0
    ENDIF

    ' Control Room 3
    IF room3_sensor.Temperature < TARGET THEN
        room3_heater.On = 1
    ELSE
        room3_heater.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

---

## Version History

- **v3.0.x** - Extended Script Mode (512 lines), living hash dictionary, decompiler improvements
- **v2.2.x** - HTTP API server, MCP integration, visual programming (experimental)
- **v1.6.3** - Custom fonts (Apple ][, TRS-80), bracket highlighting, syntax colors fix
- **v1.6.0** - Retro effects, colorblind presets
- **v1.5.0** - Named device references, extensible device database
- **v1.4.0** - IC10 decompiler, bidirectional editing
- **v1.3.0** - Simulator, watch window
- **v1.2.0** - Syntax highlighting, autocomplete
- **v1.1.0** - Batch operations, stack support
- **v1.0.0** - Initial release

---

## Support & Resources

- **GitHub**: Report issues and feature requests
- **Discord**: Join the Stationeers community
- **Wiki**: In-app wiki tab links to Stationeers Wiki

---

*Basic-10 - Stationeers BASIC to IC10 Compiler*
*By Dog Tired Studios*
*Version 3.0*
