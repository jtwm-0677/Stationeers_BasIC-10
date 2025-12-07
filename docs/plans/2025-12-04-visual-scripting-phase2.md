# Visual Scripting Phase 2: Simulator & VS Improvements

**Date:** 2025-12-04
**Priority:** High
**Status:** In Progress
**See Also:** `2025-12-05-consolidated-backlog.md` for full backlog

---

## Overview

Phase 2 focuses on four areas:
- **Part A**: Simulator enhancements for better debugging workflow
- **Part B**: Visual Scripting examples for new users
- **Part C**: Bidirectional sync between editor and VS canvas
- **Part D**: Bug fixes carried forward from previous testing

---

## Part A: Simulator Enhancements

### A.1 Loop Mode ✓ COMPLETE (v3.0.18)

**Status:** Implemented

Continuous execution mode that mimics real IC chip behavior in Stationeers.

- Loop button in simulator toolbar
- Executes continuously until stopped
- Respects YIELD (pauses briefly at configurable interval, then continues)
- Speed slider adjusts tick interval
- Visual indicator shows "Looping" status

---

### A.2 Pause Button (FR-003)

**Status:** Not Started
**Priority:** High
**Effort:** Low

#### Problem
Loop mode only has Stop/Start. There's no way to pause execution to inspect or edit values without losing loop state. The yield pause window is too narrow to make edits, and editing while running throws errors.

#### Solution
Add a PAUSE button that freezes execution without resetting state.

#### Requirements

| Requirement | Description |
|-------------|-------------|
| Pause Button | Freeze current execution state instantly |
| Resume Button | Continue from exact paused position |
| State Preservation | PC, registers, devices all preserved |
| Edit While Paused | Allow register and device value changes |
| Visual Indicator | Clear "Paused" status distinct from Running/Stopped |

#### UI Changes

**Current Toolbar:**
```
[ Run ] [ Step ] [ Loop ] [ Stop ] [ Reset ]
```

**Proposed Toolbar:**
```
[ Run ] [ Step ] [ Loop ] [ Pause/Resume ] [ Stop ] [ Reset ]
```

- Pause/Resume is a toggle button
- When running/looping: shows "Pause"
- When paused: shows "Resume"
- Disabled when stopped

#### States

| State | Description | Pause Button | Can Edit |
|-------|-------------|--------------|----------|
| STOPPED | Not running, PC=0 | Disabled | Yes |
| RUNNING | Executing (Run mode) | Enabled → Pause | No |
| LOOPING | Executing continuously | Enabled → Pause | No |
| PAUSED | Frozen mid-execution | Shows Resume | Yes |
| YIELDING | Brief yield pause | Enabled → Pause | No |

#### Implementation

**File: `UI/SimulatorWindow.xaml.cs`**

```csharp
// New state
private bool _isPaused = false;

// Pause button click
private void PauseButton_Click(object sender, RoutedEventArgs e)
{
    if (_isPaused)
    {
        // Resume
        _isPaused = false;
        if (_isLooping)
            ScheduleLoopResume();
        else
            _runTimer.Start();
        UpdateUI();
    }
    else
    {
        // Pause
        _isPaused = true;
        _runTimer.Stop();
        UpdateUI();
    }
}

// Update UI to show paused state
private void UpdateUI()
{
    if (_isPaused)
    {
        StatusText.Text = "Paused";
        PauseButton.Content = "Resume";
    }
    // ... existing states
}
```

**File: `UI/SimulatorWindow.xaml`**
```xml
<Button x:Name="PauseButton" Content="Pause" Click="PauseButton_Click"
        IsEnabled="{Binding IsRunning}" />
```

#### MCP Integration

| Command | Description |
|---------|-------------|
| `basic10_simulator_pause` | Pause current execution |
| `basic10_simulator_resume` | Resume from paused state |
| `basic10_simulator_is_paused` | Check if currently paused |

---

### A.3 Named Devices in Simulator UI (FR-002)

**Status:** Not Started
**Priority:** High
**Effort:** Medium

#### Problem
Devices declared with `ALIAS name = IC.Device[type].Name["label"]` are not visible in the simulator's device panel. Only d0-d5 pin devices appear. Users can't inspect or edit named device values.

#### Solution
Parse device aliases from compiled code and display them in the simulator with their friendly names.

#### Requirements

| Requirement | Description |
|-------------|-------------|
| Parse Aliases | Extract ALIAS statements from BASIC/IC10 |
| Display Panel | Show named devices in dedicated section |
| Friendly Names | Show alias name, device type, label |
| Editable | Allow editing device properties by alias |
| Live Update | Values update during simulation |

#### UI Changes

**Current Device Panel:**
```
Pin Devices
├── d0: [empty]
├── d1: [empty]
├── d2: [empty]
├── d3: [empty]
├── d4: [empty]
└── d5: [empty]
```

**Proposed Device Panel:**
```
Named Devices
├── myLight (StructureWallLight) "Test Light"
│   ├── On: 1
│   ├── Color: 0
│   └── Setting: 0
├── mySensor (StructureGasSensor) "Room Sensor"
│   ├── Temperature: 293.15
│   ├── Pressure: 101.325
│   └── Activate: 1
│
Pin Devices
├── d0: [empty]
├── d1: [empty]
...
```

#### Data Source

The source map already contains device alias information:

```csharp
// In SourceMap.cs or similar
public class DeviceAlias
{
    public string AliasName { get; set; }      // "myLight"
    public string DeviceType { get; set; }     // "StructureWallLight"
    public string DeviceLabel { get; set; }    // "Test Light"
    public int? PinIndex { get; set; }         // null for named, 0-5 for pin
}
```

#### Implementation

**File: `UI/SimulatorWindow.xaml.cs`**

```csharp
private List<DeviceAlias> _namedDevices = new();

private void LoadNamedDevices()
{
    _namedDevices.Clear();

    // Parse from source map or BASIC code
    if (_sourceMap?.DeviceAliases != null)
    {
        foreach (var alias in _sourceMap.DeviceAliases)
        {
            if (alias.PinIndex == null) // Named device, not pin
            {
                _namedDevices.Add(alias);
            }
        }
    }

    UpdateNamedDevicesPanel();
}

private void UpdateNamedDevicesPanel()
{
    NamedDevicesPanel.Children.Clear();

    foreach (var device in _namedDevices)
    {
        var expander = CreateDeviceExpander(device);
        NamedDevicesPanel.Children.Add(expander);
    }
}
```

**File: `UI/SimulatorWindow.xaml`**
```xml
<Expander Header="Named Devices" IsExpanded="True">
    <StackPanel x:Name="NamedDevicesPanel" />
</Expander>
```

#### Device Property Loading

Need to load available properties based on device type:

```csharp
private Dictionary<string, double> GetDeviceProperties(string deviceType)
{
    // Load from Devices.json based on deviceType
    var deviceData = _deviceDatabase.GetDevice(deviceType);
    return deviceData.LogicTypes.ToDictionary(
        lt => lt.Name,
        lt => lt.DefaultValue
    );
}
```

---

### A.4 Auto-Refresh Code (FR-001)

**Status:** Not Started
**Priority:** Medium
**Effort:** Low

#### Problem
When code changes in VS or main editor, the simulator continues running the old code. User must close and reopen the simulator to load updated code.

#### Solution
Add a "Reload Code" button and optionally auto-detect code changes.

#### Requirements

| Requirement | Description |
|-------------|-------------|
| Reload Button | Manual button to reload IC10 from editor |
| Change Detection | Detect when MipsOutput.Text changes |
| Confirmation | Ask before reloading if currently running |
| State Preservation | Option to preserve registers/devices |
| Notification | Show "Code reloaded" message |

#### UI Changes

**Toolbar Addition:**
```
[ Run ] [ Step ] [ Loop ] [ Pause ] [ Stop ] [ Reset ] [ Reload ]
                                                        ^^^^^^^^
                                                        NEW
```

#### Implementation

**File: `UI/SimulatorWindow.xaml.cs`**

```csharp
private string _loadedCodeHash = "";

private void ReloadButton_Click(object sender, RoutedEventArgs e)
{
    if (_isRunning || _isLooping)
    {
        var result = MessageBox.Show(
            "Simulator is running. Stop and reload?",
            "Reload Code",
            MessageBoxButton.YesNo);

        if (result != MessageBoxResult.Yes)
            return;

        StopExecution();
    }

    LoadCodeFromEditor();
    SetStatus("Code reloaded");
}

private void LoadCodeFromEditor()
{
    var code = _mainWindow.MipsOutput.Text;
    _loadedCodeHash = ComputeHash(code);
    _simulator.Load(code);
    UpdateUI();
}

// Optional: Auto-detect changes
private void CheckForCodeChanges()
{
    var currentHash = ComputeHash(_mainWindow.MipsOutput.Text);
    if (currentHash != _loadedCodeHash)
    {
        ReloadIndicator.Visibility = Visibility.Visible;
        ReloadIndicator.ToolTip = "Code has changed. Click Reload to update.";
    }
}
```

---

## Part B: Visual Scripting Examples

### B.1 Example Scripts (Feature 1)

**Status:** Not Started
**Priority:** Medium
**Effort:** Low-Medium

#### Problem
Only one non-functional example script exists for the visual programming interface. New users have no starting point.

#### Solution
Add multiple working examples that demonstrate VS capabilities.

#### Example List

**Basic Examples (1-4)** - Getting started with VS fundamentals

| # | Name | Description | Nodes Used |
|---|------|-------------|------------|
| 1 | **Hello Yield** | Minimal loop with YIELD | EntryPoint, Yield, Goto |
| 2 | **Counter** | Increment variable each loop | Variable, Increment, Yield, Goto |
| 3 | **Sensor Read** | Read temperature from sensor | NamedDevice, DeviceRead, Variable |
| 4 | **LED Blinker** | Toggle light on/off each loop | NamedDevice, Variable, DeviceWrite, Yield |

**Control Examples (5-8)** - Conditional logic and control flow

| # | Name | Description | Nodes Used |
|---|------|-------------|------------|
| 5 | **Thermostat** | ON/OFF control based on threshold | NamedDevice, Compare, IF, DeviceWrite |
| 6 | **Pressure Regulator** | Maintain room pressure with vent | NamedDevice, Compare, IF/ELSE, DeviceWrite |
| 7 | **State Machine** | Multi-state with transitions | Variable, Compare, IF, ELSEIF, Labels |
| 8 | **Day/Night Lights** | Lights on at night, off during day | NamedDevice (DaylightSensor), Compare, IF/ELSE |

**Automation Examples (9-12)** - Real-world Stationeers automation

| # | Name | Description | Nodes Used |
|---|------|-------------|------------|
| 9 | **Airlock Controller** | Safe airlock cycling with interlocks | Multiple NamedDevices, State Machine, Compare |
| 10 | **Solar Tracker** | Adjust solar panel angle for max power | NamedDevice (DaylightSensor, Solar), Math, DeviceWrite |
| 11 | **Battery Monitor** | Low power warning + load shedding | NamedDevice (Battery), Compare, IF/ELSEIF, DeviceWrite |
| 12 | **Furnace Automation** | Auto-start furnace when inputs ready | Multiple NamedDevices, AND logic, State tracking |

#### File Structure

```
Data/
  VisualScripting/
    Examples/
      # Basic Examples
      01-hello-yield.vsgraph
      02-counter.vsgraph
      03-sensor-read.vsgraph
      04-led-blinker.vsgraph
      # Control Examples
      05-thermostat.vsgraph
      06-pressure-regulator.vsgraph
      07-state-machine.vsgraph
      08-day-night-lights.vsgraph
      # Automation Examples
      09-airlock-controller.vsgraph
      10-solar-tracker.vsgraph
      11-battery-monitor.vsgraph
      12-furnace-automation.vsgraph
      # Index
      examples.json
```

#### Graph Format (.vsgraph)

```json
{
  "name": "Thermostat",
  "description": "Temperature control with ON/OFF threshold",
  "version": "1.0",
  "nodes": [
    {
      "id": "guid-1",
      "type": "EntryPoint",
      "x": 100,
      "y": 100,
      "properties": {}
    },
    {
      "id": "guid-2",
      "type": "NamedDevice",
      "x": 100,
      "y": 200,
      "properties": {
        "AliasName": "sensor",
        "PrefabName": "StructureGasSensor",
        "DeviceName": "Room Sensor"
      }
    }
    // ... more nodes
  ],
  "wires": [
    {
      "sourceNode": "guid-1",
      "sourcePin": "ExecOut",
      "targetNode": "guid-3",
      "targetPin": "ExecIn"
    }
    // ... more wires
  ]
}
```

#### UI Implementation

**File: `UI/VisualScripting/VisualScriptingWindow.xaml`**
```xml
<Menu>
  <MenuItem Header="File">
    <MenuItem Header="Examples" x:Name="ExamplesMenu" />
  </MenuItem>
</Menu>
```

**File: `UI/VisualScripting/VisualScriptingWindow.xaml.cs`**
```csharp
private void LoadExamplesMenu()
{
    var examplesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "Data", "VisualScripting", "Examples");

    var indexFile = Path.Combine(examplesPath, "examples.json");
    if (!File.Exists(indexFile)) return;

    var index = JsonSerializer.Deserialize<ExampleIndex>(File.ReadAllText(indexFile));

    foreach (var example in index.Examples)
    {
        var menuItem = new MenuItem
        {
            Header = example.Name,
            ToolTip = example.Description,
            Tag = example.File
        };
        menuItem.Click += LoadExample_Click;
        ExamplesMenu.Items.Add(menuItem);
    }
}

private void LoadExample_Click(object sender, RoutedEventArgs e)
{
    var menuItem = (MenuItem)sender;
    var file = (string)menuItem.Tag;
    LoadGraph(file);
}
```

---

## Part C: Visual Scripting Core

### C.1 Bidirectional Sync (Feature 2)

**Status:** Not Started
**Priority:** Medium
**Effort:** High

#### Problem
Changes in Visual Scripting update the main editor, but changes in the main editor do NOT update the visual representation. This is a one-way sync.

#### Solution
Parse BASIC code and generate/update visual graph (reverse of current flow).

#### Current Flow
```
VS Canvas → GraphToBasicGenerator → BASIC Code → Editor
```

#### Proposed Flow (Both Directions)
```
VS Canvas ←→ BASIC Code ←→ Editor
         ↑               ↑
    BasicToGraphGenerator (NEW)
```

#### Challenges

| Challenge | Approach |
|-----------|----------|
| Not all code visualizable | Show warning, partial graph, or "Code Block" node |
| Complex expressions | Single "Expression" node with raw text |
| Inline assembly | "ASM Block" node |
| Node positioning | Auto-layout algorithm or stored positions |
| Performance | Debounce, incremental updates |

#### Implementation Phases

**Phase A: Statement Recognition**
- Parse BASIC into AST (already exists)
- Map simple statements to nodes:
  - `VAR x = 0` → Variable node
  - `x = x + 1` → Math node
  - `IF x > 0 THEN` → IF node
  - `YIELD` → Yield node
  - `GOTO label` → Goto node
  - `label:` → Label node
  - `ALIAS x = ...` → NamedDevice node

**Phase B: Wire Inference**
- Connect execution flow (statement order)
- Connect data flow (variable references)
- Handle branches (IF true/false paths)

**Phase C: Position Metadata**
- Store node positions in code comments or separate file
- Format: `' VS-POS: {"nodeId": {"x": 100, "y": 200}}`
- Or: `script.vs.json` alongside `script.bas`

**Phase D: Incremental Sync**
- Diff-based updates (don't rebuild on every keystroke)
- Preserve existing node positions when possible
- Only update changed nodes

#### Node Type Mapping

| BASIC Construct | VS Node Type |
|-----------------|--------------|
| `VAR x = value` | VariableNode |
| `ALIAS x = IC.Device[...]` | NamedDeviceNode |
| `x = expression` | AssignmentNode or MathNode |
| `IF ... THEN` | IfNode |
| `ELSEIF ... THEN` | ElseIfNode (part of IfNode) |
| `ELSE` | (part of IfNode) |
| `ENDIF` | (closes IfNode) |
| `FOR ... NEXT` | ForLoopNode |
| `WHILE ... WEND` | WhileLoopNode |
| `GOSUB label` | GosubNode |
| `RETURN` | ReturnNode |
| `GOTO label` | GotoNode |
| `label:` | LabelNode |
| `YIELD` | YieldNode |
| `++x` / `--x` | IncrementNode / DecrementNode |
| `x.Property = value` | DeviceWriteNode |
| `x = device.Property` | DeviceReadNode |
| Complex expression | ExpressionNode (raw text) |
| `' comment` | CommentNode |

---

### C.2 Load Script → Visual (Feature 3)

**Status:** Not Started
**Priority:** Medium
**Effort:** Medium (depends on C.1)

#### Problem
Loading a saved script in the main compiler doesn't populate the visual interface.

#### Solution
When a script is loaded, automatically generate a visual representation using the BasicToGraphGenerator from Feature C.1.

#### Requirements

| Requirement | Description |
|-------------|-------------|
| Auto-generate | File → Open triggers VS graph generation |
| Position restore | Load positions from .vs.json if exists |
| Fresh import | Auto-layout for scripts without VS metadata |
| Partial support | Handle unvisualizable code gracefully |

#### Metadata Storage

```
MyScript/
  script.bas      - BASIC source
  script.ic10     - Compiled IC10 (optional)
  script.vs.json  - Visual scripting metadata
  instruction.xml - Stationeers metadata
```

**script.vs.json format:**
```json
{
  "version": "1.0",
  "positions": {
    "node-guid-1": {"x": 100, "y": 100},
    "node-guid-2": {"x": 250, "y": 100}
  },
  "collapsed": ["node-guid-3"],
  "viewport": {"x": 0, "y": 0, "zoom": 1.0}
}
```

#### Implementation

**File: `UI/MainWindow.xaml.cs`**

```csharp
private void OpenFile(string path)
{
    // Load BASIC code
    BasicEditor.Text = File.ReadAllText(path);

    // Check for VS metadata
    var vsMetaPath = Path.ChangeExtension(path, ".vs.json");
    if (File.Exists(vsMetaPath))
    {
        _visualScriptingWindow?.LoadMetadata(vsMetaPath);
    }
    else if (_visualScriptingWindow != null)
    {
        // Generate fresh graph from code
        _visualScriptingWindow.GenerateFromCode(BasicEditor.Text);
    }
}
```

---

## Part D: Bug Fixes (Carried Forward)

### D.1 Negative Constants Don't Parse

**Status:** Not Started
**Priority:** Medium
**Effort:** Low

```basic
CONST X = -90    # Fails: "Unexpected token"
```

**Workaround:** `VAR X = 0 - 90`
**Fix:** Modify lexer to handle unary minus in const declarations.

---

### D.2 XOR Operator Generates Wrong Code

**Status:** Not Started
**Priority:** Medium
**Effort:** Low

```basic
result = a XOR b    # Generates "jal b" instead of "xor"
```

**Fix:** Parser treating XOR operand as label. Add XOR to operator token list.

---

### D.3 Increment/Decrement Postfix (i++, i--)

**Status:** Not Started
**Priority:** Low
**Effort:** Low

```basic
i++    # Should work (prefix ++i already works)
i--    # Should work (prefix --i already works)
```

**Note:** Postfix versions for familiarity with C-style languages.

---

## Implementation Priority

| Priority | Feature | Status | Effort | Impact |
|----------|---------|--------|--------|--------|
| 1 | A.2 Pause Button | Not Started | Low | High |
| 2 | A.1 Loop Mode | **DONE** | - | - |
| 3 | D.1 Negative Constants | Not Started | Low | Medium |
| 4 | D.2 XOR Operator | Not Started | Low | Medium |
| 5 | A.3 Named Devices | Not Started | Medium | High |
| 6 | A.4 Auto-Refresh | Not Started | Low | Medium |
| 7 | B.1 Examples | Not Started | Low-Med | Medium |
| 8 | D.3 i++/i-- Postfix | Not Started | Low | Low |
| 9 | C.1 Bidirectional Sync | Not Started | High | High |
| 10 | C.2 Load Script Visual | Not Started | Medium | Medium |

---

## Open Questions

1. **Unvisualizable code**: Show warning? Partial graph? "Code Block" node?
2. **VS always on**: Should VS be opt-in per script or always available?
3. **Simultaneous editing**: What if user edits both views at once?
4. **Performance threshold**: How large before we warn/disable auto-sync?
5. **Position storage**: Comments in code or separate .vs.json file?

---

## Version Targets

| Version | Features | Focus |
|---------|----------|-------|
| **v3.0.21** | A.2 Pause Button, D.1 Negative Constants, D.2 XOR Fix | Bug fixes + Pause |
| **v3.0.22** | A.3 Named Devices in Simulator | Simulator UX |
| **v3.0.23** | A.4 Auto-Refresh, D.3 i++/i-- | QoL |
| **v3.0.24** | B.1 Examples (12 scripts) | VS Content |
| **v3.1.0** | C.1 Bidirectional Sync, C.2 Load Script Visual | VS Core |

---

## Related Documents

- `2025-12-05-consolidated-backlog.md` - Full backlog of all unfinished tasks
- `../FeatureRoadmap.md` - Long-term feature roadmap
- `feature_roadmap_suggestions.md` - Original feature suggestions from QA

---

**End of Plan**
