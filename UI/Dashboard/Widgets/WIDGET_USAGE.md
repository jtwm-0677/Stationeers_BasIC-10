# Dashboard Widgets Usage Guide

## Phase 2B Widgets - Complete Implementation

This document describes the five new dashboard widgets created for Basic-10 v3.0 Visual Scripting.

---

## 1. ConsoleOutputWidget

**Purpose:** Debug console for displaying log messages with color-coded severity levels.

### Features:
- Color-coded messages:
  - Info (white)
  - Warning (yellow)
  - Error (red)
  - Success (green)
- Optional timestamps
- Auto-scroll to bottom (toggleable)
- Max 1000 lines (oldest removed automatically)
- Clear all messages
- Copy all to clipboard

### Public API:
```csharp
// Add a message
widget.Log("Compilation started", LogLevel.Info);
widget.Log("Warning: Line count approaching limit", LogLevel.Warning);
widget.Log("Error: Syntax error on line 42", LogLevel.Error);
widget.Log("Build successful!", LogLevel.Success);

// Clear console
widget.Clear();
```

### State Saved:
- ShowTimestamps setting
- AutoScroll setting
- Last 100 messages

---

## 2. LineCounterWidget

**Purpose:** Display BASIC and IC10 line counts with visual progress indicator.

### Features:
- Shows BASIC line count
- Shows IC10 line count vs. 128 limit
- Progress bar with color coding:
  - Green: < 100 lines
  - Yellow: 100-120 lines
  - Red: > 120 lines
- Warning icon when approaching/exceeding limit
- Percentage display
- Compact design

### Public API:
```csharp
// Update line counts
widget.UpdateCounts(basicLines: 45, ic10Lines: 78);
```

### State Saved:
- BasicLines count
- Ic10Lines count

---

## 3. RegisterViewWidget

**Purpose:** Display IC10 register values during simulation.

### Features:
- Shows all 18 registers (r0-r15, sp, ra)
- Hex/Decimal display toggle
- Highlights changed registers (yellow flash)
- Monospace font for values
- "Simulation not running" placeholder when inactive

### Public API:
```csharp
// Update register values
var registers = new Dictionary<string, double>
{
    { "r0", 42.0 },
    { "r1", 100.5 },
    { "sp", 16.0 },
    { "ra", 0.0 }
};
widget.UpdateRegisters(registers);
```

### State Saved:
- Hex/Decimal format preference
- Register values (not restored - fresh on start)

---

## 4. VariableWatchWidget

**Purpose:** Monitor specific variables during debugging.

### Features:
- Watch list of variables
- Shows: Variable Name | Register | Value | Line
- Add variables by name
- Remove individual variables
- Clear all button
- Highlight on value change
- Edit value during pause (double-click)
- "Add variables to watch" empty state

### Public API:
```csharp
// Add variable to watch list
widget.AddVariable("temperature", "r5", "23.4", "Line 15");

// Update variable value
widget.UpdateVariable("temperature", "25.8", "r5", "Line 15");

// Remove variable
widget.RemoveVariable("temperature");
```

### State Saved:
- Watch list (variable names)

---

## 5. DeviceMonitorWidget

**Purpose:** Monitor device states and properties.

### Features:
- Add devices by alias or pin (d0-d5, db)
- Monitor properties: On, Setting, Temperature, Pressure
- Device type color indicators:
  - Blue: Pump
  - Orange: Furnace
  - Green: Sensor
  - Purple: Valve
  - Yellow: Logic
  - Gray: Unknown
- Remove individual devices
- "Add devices to monitor" empty state

### Public API:
```csharp
// Add device to monitor
widget.AddDevice("furnace1", "Furnace");
widget.AddDevice("d0", "Device Pin");

// Update device property
widget.UpdateDeviceProperty("furnace1", "Temperature", "450.2");
widget.UpdateDeviceProperty("furnace1", "On", "1");
widget.UpdateDeviceProperty("d0", "Setting", "100");

// Remove device
widget.RemoveDevice("furnace1");
```

### State Saved:
- Device list with properties

---

## Widget Factory Registration

All widgets are registered in `WidgetFactory.cs`:

```csharp
RegisterWidget<Widgets.ConsoleOutputWidget>("ConsoleOutput", "Console Output");
RegisterWidget<Widgets.LineCounterWidget>("LineCounter", "Line Counter");
RegisterWidget<Widgets.RegisterViewWidget>("RegisterView", "Register View");
RegisterWidget<Widgets.VariableWatchWidget>("VariableWatch", "Variable Watch");
RegisterWidget<Widgets.DeviceMonitorWidget>("DeviceMonitor", "Device Monitor");
```

---

## Visual Styling

All widgets follow the consistent dark theme:

- **Background:** #2D2D2D
- **Panel Background:** #252526
- **Border:** #3F3F46
- **Text:** White, 12-13px Segoe UI
- **Accent:** #4A9EFF (blue)
- **Success:** #4EC9B0 (green)
- **Warning:** #CCC900 (yellow)
- **Error:** #F14C4C (red)
- **Monospace:** Consolas, Cascadia Code, Courier New

---

## Integration Notes

### Compiler Integration
The LineCounterWidget should be updated whenever code is compiled:
```csharp
lineCounterWidget.UpdateCounts(basicCode.Split('\n').Length, ic10Code.Split('\n').Length);
```

### Debugger Integration
The RegisterViewWidget and VariableWatchWidget need integration with a future debugger/simulator:
```csharp
// During simulation step
registerWidget.UpdateRegisters(simulator.GetRegisters());
variableWidget.UpdateVariable("myVar", simulator.GetVariableValue("myVar"));
```

### Build Output Integration
The ConsoleOutputWidget should receive build messages:
```csharp
consoleWidget.Log($"Compiling {filename}...", LogLevel.Info);
consoleWidget.Log($"Generated {lineCount} IC10 lines", LogLevel.Success);
```

### Device Analysis Integration
The DeviceMonitorWidget can display device references found in code:
```csharp
// When parsing device aliases
deviceMonitorWidget.AddDevice("furnace1", "Furnace");
```

---

## File Structure

```
UI/Dashboard/Widgets/
├── TaskChecklistWidget.xaml / .cs      (Phase 1)
├── ConsoleOutputWidget.xaml / .cs      (Phase 2B) ✓
├── LineCounterWidget.xaml / .cs        (Phase 2B) ✓
├── RegisterViewWidget.xaml / .cs       (Phase 2B) ✓
├── VariableWatchWidget.xaml / .cs      (Phase 2B) ✓
├── DeviceMonitorWidget.xaml / .cs      (Phase 2B) ✓
└── WIDGET_USAGE.md                     (This file)
```

---

## Next Steps (Future Phases)

1. **Phase 2C:** Simulation engine to drive RegisterView and VariableWatch
2. **Phase 3:** Node-based visual scripting canvas
3. **Integration:** Connect widgets to compiler events and build process
4. **Persistence:** Save/load dashboard layouts per project
