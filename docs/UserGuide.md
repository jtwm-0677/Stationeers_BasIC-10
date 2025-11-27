# BASIC to IC10 Compiler - User Guide

## Table of Contents
1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Interface Overview](#interface-overview)
4. [Writing Your First Program](#writing-your-first-program)
5. [Compiling and Deploying](#compiling-and-deploying)
6. [Using the Simulator](#using-the-simulator)
7. [Device Hash Database](#device-hash-database)
8. [Settings and Customization](#settings-and-customization)
9. [Keyboard Shortcuts](#keyboard-shortcuts)
10. [Troubleshooting](#troubleshooting)

---

## Introduction

The BASIC to IC10 Compiler is a professional development environment for creating programs for Stationeers IC10 chips. It allows you to write code in a high-level BASIC language and automatically compiles it to optimized IC10 MIPS assembly.

### Why Use BASIC?

- **Easier to Read**: BASIC syntax is more intuitive than raw IC10 assembly
- **Faster Development**: Write complex logic in fewer lines of code
- **Automatic Optimization**: The compiler generates efficient IC10 code
- **Error Detection**: Real-time syntax checking catches mistakes early
- **Auto-complete**: IntelliSense-style suggestions speed up coding

### Key Features

- Modern dark/light themed IDE
- Real-time syntax highlighting
- Auto-complete for keywords, functions, and device properties
- Step-through IC10 simulator with debugging
- Device hash database with search
- Direct deployment to Stationeers
- 128-line limit monitoring

---

## Getting Started

### System Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- Stationeers (for deployment)

### Installation

1. Extract the ZIP file to your preferred location
2. Run `BasicToMips.exe`
3. On first launch, configure your Stationeers directory (Tools > Set Stationeers Directory)

### First Launch

When you first open the compiler, you'll see:
- The welcome example code in the BASIC editor (top panel)
- The compiled IC10 output area (bottom panel)
- Documentation panel on the right
- Status bar showing line counts and Stationeers path

---

## Interface Overview

### Main Window Layout

```
+------------------------------------------+
|  Menu Bar                                |
+------------------------------------------+
|  Toolbar (New, Open, Save, Compile...)   |
+------------------------------------------+
|                          |               |
|   BASIC Source Editor    |  Documentation|
|   (Write code here)      |  Panel        |
|                          |               |
+--------------------------|               |
|                          |               |
|   IC10 MIPS Output       |               |
|   (Compiled result)      |               |
|                          |               |
+------------------------------------------+
|  Action Bar (Load Example, Optimize...)  |
+------------------------------------------+
|  Status Bar (Line/Col, Stationeers Path) |
+------------------------------------------+
```

### Menu Structure

- **File**: New, Open, Save, Export IC10, Recent Files, Exit
- **Edit**: Undo, Redo, Cut, Copy, Paste, Find, Replace, Snippets
- **Build**: Compile, Run Simulator, Optimization Level, Auto-compile
- **View**: Documentation Panel, Line Numbers, Word Wrap, Zoom
- **Tools**: Device Hash Database, Settings, Stationeers Directory
- **Help**: Documentation, Quick Start, Language Reference, Examples, About

### Toolbar Icons

| Icon | Action | Shortcut |
|------|--------|----------|
| New | Create new file | Ctrl+N |
| Open | Open file | Ctrl+O |
| Save | Save file | Ctrl+S |
| Undo | Undo last action | Ctrl+Z |
| Redo | Redo action | Ctrl+Y |
| Find | Find/Replace dialog | Ctrl+F |
| Compile | Compile BASIC to IC10 | F5 |
| Copy IC10 | Copy output to clipboard | - |

---

## Writing Your First Program

### Hello World (Blink a Light)

```basic
' My first IC10 program - Blink a light
ALIAS light d0

main:
    light.On = 1
    SLEEP 1
    light.On = 0
    SLEEP 1
    GOTO main
END
```

### Understanding the Code

1. **Comments** start with `'` or `REM`
2. **ALIAS** assigns a friendly name to a device port (d0-d5)
3. **Labels** end with `:` and mark jump targets
4. **Device properties** are accessed with `device.Property`
5. **SLEEP** pauses execution (in seconds)
6. **GOTO** jumps to a label
7. **END** marks the end of the program

### Step-by-Step: Temperature Controller

```basic
' Temperature Controller
' Controls a heater to maintain target temperature

' Device Aliases
ALIAS sensor d0      ' Gas Sensor
ALIAS heater d1      ' Wall Heater
ALIAS display d2     ' LED Display (optional)

' Constants
DEFINE TARGET_TEMP 293.15   ' 20°C in Kelvin
DEFINE TOLERANCE 2          ' ±2 degrees

' Variables
VAR currentTemp = 0
VAR heaterState = 0

main:
    ' Read current temperature
    currentTemp = sensor.Temperature

    ' Display current temp (optional)
    IF display THEN
        display.Setting = currentTemp - 273.15
    ENDIF

    ' Control logic with hysteresis
    IF currentTemp < TARGET_TEMP - TOLERANCE THEN
        heaterState = 1
    ELSEIF currentTemp > TARGET_TEMP + TOLERANCE THEN
        heaterState = 0
    ENDIF

    ' Apply heater state
    heater.On = heaterState

    YIELD
    GOTO main
END
```

---

## Compiling and Deploying

### Compiling Your Code

1. **Manual Compile**: Press F5 or click the Compile button
2. **Auto-compile**: Enable "Auto-compile on Save" in Build menu

### Understanding the Output

The IC10 panel shows:
- Generated MIPS assembly code
- Line count with 128-line limit indicator
- Warning badge (yellow) when approaching limit
- Error badge (red) when exceeding limit

### Optimization Levels

| Level | Description |
|-------|-------------|
| None | Fastest compile, larger output |
| Basic | Recommended balance of speed and size |
| Aggressive | Smallest output, may increase compile time |

### Deploying to Stationeers

1. **Save & Deploy** button: Saves both .bas file and .ic10 to Stationeers scripts folder
2. **Export IC10**: Manually save IC10 file anywhere
3. **Copy to Clipboard**: Copy IC10 code for pasting in-game

### In-Game Usage

1. Open the IC Housing in Stationeers
2. Click "Import" in the IC editor
3. Select your .ic10 file from the scripts folder
4. The code is now loaded into the chip

---

## Using the Simulator

The IC10 Simulator allows you to test your code without running Stationeers.

### Opening the Simulator

- Build menu > Run Simulator (F9)
- Code must compile successfully first

### Simulator Interface

```
+------------------------------------------+
|  Run | Pause | Stop | Step | Reset       |
+------------------------------------------+
|                    |  Registers          |
|   IC10 Code        |  r0-r15, sp, ra     |
|   (highlighted     +---------------------+
|    current line)   |  Devices            |
|                    |  d0-d5 properties   |
|                    +---------------------+
|                    |  Stack              |
|                    |  (push/pop values)  |
+------------------------------------------+
|  Status: Running | PC: 5 | Instructions: 42
+------------------------------------------+
```

### Controls

| Button | Action | Shortcut |
|--------|--------|----------|
| Run | Execute continuously | F5 |
| Pause | Pause execution | - |
| Stop | Halt execution | F6 |
| Step | Execute one instruction | F10 |
| Reset | Restart from beginning | - |

### Modifying Values

- Click on register values to edit them
- Modify device properties (On, Setting, Temperature)
- Changes take effect immediately

### Speed Control

Use the speed slider to control execution rate:
- Left: Slower (easier to follow)
- Right: Faster (quick testing)

---

## Device Hash Database

The Device Hash Database (Tools > Device Hash Database) provides:

### Devices Tab
Browse all Stationeers devices with their:
- Display name
- Prefab name (for batch operations)
- Category
- Hash value (for HASH function)
- Description

### Logic Types Tab
All device properties you can read/write:
- Property name (Temperature, Pressure, On, etc.)
- Hash value
- Description

### Slot Logic Types Tab
Properties for device slots:
- Occupied, OccupantHash, Quantity, etc.

### Hash Calculator Tab
Calculate CRC32 hashes for any string:
- Enter text, get hash instantly
- Copy hash to clipboard

### Using Hashes

```basic
' Using hash values for batch operations
DEFINE SOLAR_HASH -539224550   ' ItemStructureSolarPanel

VAR totalPower = 0
totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, Sum)
```

---

## Settings and Customization

### Accessing Settings

Tools > Settings or click the gear icon

### Appearance

- **Theme**: Dark (default) or Light
- Theme changes apply immediately

### General Settings

- **Auto-compile on save**: Automatically compile when saving
- **Show documentation panel**: Toggle the right-side panel
- **Word wrap**: Enable/disable word wrapping in editors

### Editor Settings

- **Font Size**: 8-32 points
- **Optimization Level**: None, Basic, or Aggressive

### Stationeers Integration

- **Game Directory**: Path to Stationeers data folder
- Default: `%LocalAppData%Low\Rocketwerkz\rocketstation`
- Scripts are saved to the `scripts` subfolder

---

## Keyboard Shortcuts

### File Operations
| Shortcut | Action |
|----------|--------|
| Ctrl+N | New file |
| Ctrl+O | Open file |
| Ctrl+S | Save file |
| Ctrl+Shift+S | Save As |

### Editing
| Shortcut | Action |
|----------|--------|
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+X | Cut |
| Ctrl+C | Copy |
| Ctrl+V | Paste |
| Ctrl+F | Find |
| Ctrl+H | Replace |
| Ctrl+Space | Trigger auto-complete |

### Building
| Shortcut | Action |
|----------|--------|
| F5 | Compile |
| F6 | Compile and Copy |
| F9 | Run Simulator |

### View
| Shortcut | Action |
|----------|--------|
| Ctrl++ | Zoom in |
| Ctrl+- | Zoom out |
| F1 | Show documentation |

---

## Troubleshooting

### Common Errors

#### "Undefined label: xxx"
The GOTO or GOSUB references a label that doesn't exist.
```basic
' Wrong
GOTO mian    ' Typo!

' Correct
GOTO main
```

#### "Missing ENDIF"
Every IF needs a matching ENDIF (unless single-line IF).
```basic
' Wrong
IF x > 0 THEN
    y = 1
' Missing ENDIF!

' Correct
IF x > 0 THEN
    y = 1
ENDIF
```

#### "Line limit exceeded (128)"
IC10 chips have a 128-line limit. Solutions:
1. Enable Aggressive optimization
2. Combine operations
3. Remove comments (they don't compile anyway)
4. Use GOSUB for repeated code

#### "Stack overflow"
Too many PUSH operations or recursive GOSUB calls.
- Maximum stack depth is 512
- Ensure GOSUB has matching RETURN
- Check for infinite recursion

### Performance Tips

1. **Use YIELD instead of SLEEP 0**: More efficient
2. **Cache device reads**: Store in variables if used multiple times
3. **Use batch operations**: For multiple similar devices
4. **Avoid unnecessary calculations**: Pre-compute constants

### Getting Help

- Press F1 for built-in documentation
- Documentation panel has Quick Reference and Examples
- Check the Language Reference for syntax details

---

## Appendix: IC10 Line Limit

Stationeers IC10 chips have a hard limit of 128 lines. The compiler shows:
- Current line count in the IC10 panel header
- Yellow warning badge at 100-128 lines
- Red error badge above 128 lines

### Reducing Line Count

1. **Remove blank lines**: They don't count but are stripped anyway
2. **Combine operations**: `x = y + z * 2` vs separate lines
3. **Use loops**: Instead of repeated code
4. **Aggressive optimization**: Removes redundant operations
5. **Shorter variable names**: Compile to same result, but cleaner source

The compiler automatically strips comments and optimizes code, but complex programs may still exceed the limit. Consider splitting functionality across multiple IC10 chips if needed.
