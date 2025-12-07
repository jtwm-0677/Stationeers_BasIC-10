# Basic-10 Continuation Document

**Date:** 2025-12-05
**Current Version:** 3.0.20
**Project Path:** `C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired`

---

## Project Overview

**Basic-10** is a BASIC to IC10 (MIPS) compiler for the game **Stationeers**. It provides:
- A WPF-based IDE with syntax highlighting
- Visual Scripting mode (node-based programming)
- IC10 Simulator with Loop mode
- MCP integration for AI pair programming

**Tech Stack:**
- .NET 8.0, WPF
- AvalonEdit (code editor)
- WebView2 (documentation)
- ASP.NET Core (HTTP API for MCP)

---

## BASIC-10 Syntax Reference

### Device Declaration

```basic
' Pin-based device (connected to IC Housing pins d0-d5)
ALIAS myDevice = d0

' Named device (by type and labeler name)
ALIAS sensor = IC.Device[StructureGasSensor].Name["Room Sensor"]
ALIAS light = IC.Device[StructureWallLight].Name["Status Light"]
```

### Variables and Constants

```basic
VAR temperature = 0
VAR pressure = 101.325
CONST THRESHOLD = 25
CONST MAX_TEMP = 350
```

### Device Read/Write

```basic
' Read property
temperature = sensor.Temperature
pressure = sensor.Pressure
isOn = light.On

' Write property
light.On = 1
light.Color = 2        ' Green
light.Setting = temperature
```

### Control Flow

```basic
' IF/THEN/ELSE/ENDIF
IF temperature > THRESHOLD THEN
    light.Color = 4    ' Red
ELSEIF temperature > 20 THEN
    light.Color = 5    ' Yellow
ELSE
    light.Color = 2    ' Green
ENDIF

' Single-line IF
IF x > 0 THEN y = 1 ENDIF

' FOR loop
FOR i = 1 TO 10
    total = total + i
NEXT i

' WHILE loop
WHILE pressure < 100
    vent.On = 1
    YIELD
WEND

' Labels and GOTO
MainLoop:
    ' code here
    YIELD
    GOTO MainLoop

' Subroutines
GOSUB CheckSensors
' ...
CheckSensors:
    temp = sensor.Temperature
RETURN
```

### Operators

```basic
' Math
result = a + b - c * d / e
result = a % b           ' Modulo
result = a ^ 2           ' Power

' Comparison
IF a = b THEN            ' Equal
IF a <> b THEN           ' Not equal
IF a > b THEN
IF a >= b THEN
IF a < b THEN
IF a <= b THEN

' Logical
IF a AND b THEN
IF a OR b THEN
IF NOT a THEN

' Increment/Decrement (prefix only currently)
++counter
--counter
```

### Built-in Functions

```basic
' Math functions
x = ABS(value)
x = FLOOR(value)
x = CEIL(value)
x = ROUND(value)
x = SQRT(value)
x = SIN(angle)
x = COS(angle)
x = TAN(angle)
x = ASIN(value)
x = ACOS(value)
x = ATAN(value)
x = ATAN2(y, x)
x = MIN(a, b)
x = MAX(a, b)
x = LOG(value)
x = EXP(value)

' Random
x = RAND()              ' 0.0 to 1.0
```

### Batch Operations

```basic
' Read from all devices of a type
avgTemp = ALL[StructureGasSensor].Temperature.Average
sumPower = ALL[StructureBattery].Charge.Sum
minPress = ALL[StructureGasSensor].Pressure.Min
maxTemp = ALL[StructureGasSensor].Temperature.Max
count = ALL[StructureGasSensor].Count
```

### Special Instructions

```basic
YIELD                   ' Pause execution (required in loops)
SLEEP 5                 ' Wait 5 ticks
HCF                     ' Halt and catch fire (stop execution)
```

### Color Constants (need to use numbers currently)

```
Blue=0, Grey=1, Green=2, Orange=3, Red=4
Yellow=5, White=6, Black=7, Brown=8, Khaki=9
Pink=10, Purple=11
```

---

## Current Development Status

### Version History (Recent)

| Version | Date | Changes |
|---------|------|---------|
| v3.0.18 | 2025-12-04 | Loop mode fix (auto-resume after YIELD) |
| v3.0.19 | 2025-12-04 | NamedDevice syntax, Main label, IF block structure, error line reporting |
| v3.0.20 | 2025-12-04 | VS → Editor sync fix (IC10 panel now shows compiled code) |

### v3.0.20 Key Fixes

1. **VS Code Sync (BUG-001)**: Fixed `VisualScripting_BasicCodeGenerated` in MainWindow.xaml.cs to set BOTH `BasicEditor.Text` AND `MipsOutput.Text` when VS generates code. Previously only BASIC was synced, IC10 panel showed wrong content.

2. **Loop Mode**: Works correctly - executes continuously, pauses at YIELD for configurable interval, auto-resumes.

3. **NamedDevice Syntax**: Now generates `ALIAS name = IC.Device[type].Name["label"]` correctly.

4. **Error Reporting**: Parser errors now point to IF statement start line, not EOF.

---

## Visual Scripting Phase 2

**Plan Document:** `docs/plans/2025-12-04-visual-scripting-phase2.md`
**Backlog:** `docs/plans/2025-12-05-consolidated-backlog.md`

### Completed Features

| Feature | Version | Status |
|---------|---------|--------|
| A.1 Loop Mode | v3.0.18 | ✅ DONE |
| VS → Editor Sync | v3.0.20 | ✅ DONE |

### Phase 2 Implementation Priority

| Priority | ID | Feature | Effort | Status |
|----------|-----|---------|--------|--------|
| 1 | A.2 | **Pause Button** | Low | Not Started |
| 2 | D.1 | Negative Constants Bug | Low | Not Started |
| 3 | D.2 | XOR Operator Bug | Low | Not Started |
| 4 | A.3 | Named Devices in Simulator | Medium | Not Started |
| 5 | A.4 | Auto-Refresh Code | Low | Not Started |
| 6 | B.1 | 12 VS Example Scripts | Low-Med | Not Started |
| 7 | D.3 | i++/i-- Postfix Operators | Low | Not Started |
| 8 | C.1 | Bidirectional Sync | High | Not Started |
| 9 | C.2 | Load Script → Visual | Medium | Not Started |

### Version Targets

| Version | Features |
|---------|----------|
| v3.0.21 | A.2 Pause Button, D.1 Negative Constants, D.2 XOR Fix |
| v3.0.22 | A.3 Named Devices in Simulator |
| v3.0.23 | A.4 Auto-Refresh, D.3 i++/i-- |
| v3.0.24 | B.1 Examples (12 scripts) |
| v3.1.0 | C.1 Bidirectional Sync, C.2 Load Script Visual |

---

## Detailed Feature Specifications

### A.2 Pause Button (Next Up)

**Problem:** Loop mode only has Stop/Start. No way to pause and edit values.

**Solution:** Add Pause/Resume toggle button to simulator toolbar.

**States:**
- STOPPED: Not running, PC=0, can edit
- RUNNING: Executing (Run mode), cannot edit
- LOOPING: Executing continuously, cannot edit
- PAUSED: Frozen mid-execution, CAN edit
- YIELDING: Brief pause at yield, cannot edit

**Implementation in `UI/SimulatorWindow.xaml.cs`:**
```csharp
private bool _isPaused = false;

private void PauseButton_Click(object sender, RoutedEventArgs e)
{
    if (_isPaused)
    {
        _isPaused = false;
        if (_isLooping) ScheduleLoopResume();
        else _runTimer.Start();
    }
    else
    {
        _isPaused = true;
        _runTimer.Stop();
    }
    UpdateUI();
}
```

### A.3 Named Devices in Simulator

**Problem:** Only d0-d5 pin devices shown. Named devices (ALIAS with IC.Device) are invisible.

**Solution:** Parse ALIAS statements, display in separate "Named Devices" panel with editable properties.

**UI Mockup:**
```
Named Devices
├── myLight (StructureWallLight) "Test Light"
│   ├── On: 1
│   ├── Color: 2
│   └── Setting: 0

Pin Devices
├── d0: [empty]
├── d1: [empty]
...
```

### B.1 VS Example Scripts (12 total)

**Location:** `Data/VisualScripting/Examples/`

**Basic Examples (1-4):**
1. Hello Yield - Minimal loop
2. Counter - Increment variable
3. Sensor Read - Read temperature
4. LED Blinker - Toggle light

**Control Examples (5-8):**
5. Thermostat - ON/OFF control
6. Pressure Regulator - Maintain pressure
7. State Machine - Multi-state transitions
8. Day/Night Lights - Automatic lighting

**Automation Examples (9-12):**
9. Airlock Controller - Safe cycling
10. Solar Tracker - Panel angle adjustment
11. Battery Monitor - Power management
12. Furnace Automation - Auto-start

---

## Bug Fixes Pending

### D.1 Negative Constants

```basic
CONST X = -90    ' Fails: "Unexpected token"
```
**Fix:** Modify lexer to handle unary minus in const declarations.

### D.2 XOR Operator

```basic
result = a XOR b    ' Generates "jal b" instead of "xor"
```
**Fix:** Parser treating XOR operand as label. Add XOR to operator token list.

### D.3 Postfix Increment/Decrement

```basic
i++    ' Should work (prefix ++i already works)
i--    ' Should work
```

---

## Key File Locations

### Main Application
- `UI/MainWindow.xaml.cs` - Main editor window (5000+ lines)
- `UI/SimulatorWindow.xaml.cs` - IC10 simulator
- `src/Parser/Parser.cs` - BASIC parser
- `src/Generator/MipsGenerator.cs` - IC10 code generator

### Visual Scripting
- `UI/VisualScripting/VisualScriptingWindow.xaml.cs` - VS window
- `UI/VisualScripting/CodeGen/GraphToBasicGenerator.cs` - Node → BASIC
- `UI/VisualScripting/Nodes/` - Node implementations
- `UI/VisualScripting/ViewModels/CodePanelViewModel.cs` - Code display

### MCP Server
- `Basic10.Mcp/Program.cs` - Entry point
- `Basic10.Mcp/McpServer.cs` - MCP protocol handler
- `Basic10.Mcp/HttpBridge.cs` - HTTP API bridge

### Configuration
- `BasicToMips.csproj` - Project file, version number
- `Data/Devices.json` - Device definitions (types, properties)

### Documentation
- `docs/plans/2025-12-04-visual-scripting-phase2.md` - Current phase plan
- `docs/plans/2025-12-05-consolidated-backlog.md` - All pending tasks
- `docs/FeatureRoadmap.md` - Long-term roadmap
- `docs/Test Documents/test plans/` - Test plans
- `docs/Test Documents/test results/` - Test results

---

## Build & Package Procedures

### Build
```bash
cd "C:/Development/Stationeers Stuff/BASICtoMIPS_ByDogTired"
dotnet build
```

### Publish (Release)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### Build MCP Server
```bash
cd Basic10.Mcp
dotnet publish -c Release -o ../publish/mcp
```

### Create Package
```bash
# Clean old package
rm -f "BasIC-10v3.0.20.zip"

# Create zip
powershell -Command "Compress-Archive -Path 'publish/*' -DestinationPath 'BasIC-10v3.0.20.zip' -Force"
```

### Package Contents
- `Basic_10.exe` - Main application
- `mcp/Basic10.Mcp.exe` - MCP server (BUILT, not source)
- `Data/` - Device definitions, examples
- `docs/*.md` - User documentation (NOT plans/test docs)

**Note:** `docs/plans/` and `docs/Test Documents/` are excluded from packages (dev-only).

---

## MCP Configuration

### For Claude Code

Add to `~/.claude/settings.json` or use:
```bash
claude mcp add basic10 -- "C:/Development/Stationeers Stuff/BASICtoMIPS_ByDogTired/publish/mcp/Basic10.Mcp.exe"
```

**Requirements:**
- Basic_10.exe must be running (hosts HTTP API on port 19410)
- MCP server bridges Claude Code (stdio) ↔ Basic_10 (HTTP)

### Available MCP Commands
- `basic10_compile` - Compile BASIC code
- `basic10_get_code` - Get current editor content
- `basic10_set_code` - Set editor content
- `basic10_simulator_*` - Simulator control
- `basic10_vs_*` - Visual Scripting control

---

## Testing Procedures

### Test Documentation Location
- Plans: `docs/Test Documents/test plans/`
- Results: `docs/Test Documents/test results/`

### Naming Convention
- Plan: `YYYY-MM-DD-vX.Y.Z-feature.md`
- Results: `YYYY-MM-DD-vX.Y.Z-feature-results.md`

### After Making Changes
1. Update version in `BasicToMips.csproj`
2. Write test plan in `test plans/`
3. Build and test
4. Document results in `test results/`
5. If pass: Package and release

---

## Recent Session Context

### What Was Just Completed (2025-12-05)
1. Fixed BUG-001: VS code now syncs IC10 to main editor
2. Created consolidated backlog of all unfinished tasks
3. Updated phase 2 plan with Part D (bug fixes)
4. Expanded VS examples from 6 to 12
5. Fixed MCP package (was including source, now includes built binaries)
6. Created `examples/SolidGeneratorMonitor.bas` as a sample script

### Next Steps
1. **Implement A.2 Pause Button** - Highest priority
2. Fix D.1 Negative Constants
3. Fix D.2 XOR Operator
4. Test and package v3.0.21

### Open Questions (from Phase 2 Plan)
1. Unvisualizable code: Show warning? Partial graph? "Code Block" node?
2. VS always on: Should VS be opt-in per script?
3. Simultaneous editing: What if user edits both views?
4. Performance threshold: How large before we disable auto-sync?
5. Position storage: Comments in code or separate .vs.json file?

---

## Stationeers-Specific Notes

### Common Device Types
- `StructureGasSensor` - Temperature, Pressure, gas readings
- `StructureWallLight` - On, Color, Setting
- `StructureActiveVent` - On, Mode (0=out, 1=in)
- `StructureSolidFuelGenerator` - PowerGeneration, Fuel, BurnTime
- `StructureBatteryLarge` - Charge, ChargeRatio, PowerPotential
- `StructureConsole` - Setting (display value), On, Color
- `StructureSolarPanel` - Horizontal, Vertical, PowerGeneration
- `StructureDaylightSensor` - SolarAngle, Mode

### IC10 Limits
- **128 lines maximum** - Compiler warns when exceeded
- **18 registers** (r0-r15, ra, sp)
- **512 stack entries**
- YIELD required in loops to prevent game freeze

---

**End of Continuation Document**
