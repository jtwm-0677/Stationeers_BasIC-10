# Phase 6A: Experience Mode System - Quick Reference

## Quick Start

### Change Experience Mode
```csharp
// Switch to a preset mode
ExperienceModeManager.Instance.SetMode(ExperienceLevel.Expert);

// Get current mode
var currentMode = ExperienceModeManager.Instance.CurrentMode;

// Get current settings
var settings = ExperienceModeManager.Instance.CurrentSettings;
```

### Generate Mode-Appropriate Labels
```csharp
var node = new ReadPropertyNode { PropertyName = "Temperature" };
var settings = ExperienceModeManager.Instance.CurrentSettings;

// Get label based on current mode
var label = NodeLabelProvider.GetLabel(node, settings.NodeLabelStyle);

// Or get specific style directly
var friendly = NodeLabelProvider.GetFriendlyLabel(node);   // "Read Temperature from Device"
var mixed = NodeLabelProvider.GetMixedLabel(node);         // "device.Temperature"
var technical = NodeLabelProvider.GetTechnicalLabel(node); // "l r0 d0 Temperature"
```

### Filter Nodes by Mode
```csharp
var factory = new NodeFactory();
var settings = ExperienceModeManager.Instance.CurrentSettings;

// Get filtered nodes for current mode
var availableNodes = factory.GetFilteredNodes(settings);

// Check if specific node type is available
bool canUse = NodePaletteFilter.IsNodeTypeAvailable("Hash", settings);
```

### Subscribe to Mode Changes
```csharp
ExperienceModeManager.Instance.ModeChanged += (sender, e) =>
{
    Console.WriteLine($"Changed from {e.OldMode} to {e.NewMode}");

    // Apply new settings
    if (e.Settings.ShowCodePanel)
    {
        ShowCodePanel();
    }
    else
    {
        HideCodePanel();
    }

    // Update node labels
    foreach (var node in nodes)
    {
        node.Label = NodeLabelProvider.GetLabel(node, e.Settings.NodeLabelStyle);
    }
};
```

## Mode Presets

### Beginner Mode
```csharp
ExperienceModeManager.Instance.SetMode(ExperienceLevel.Beginner);

// Settings:
// - Code panel: HIDDEN
// - Node labels: Friendly ("Turn Light On")
// - Available nodes: ~20 essential nodes
// - Data types: Hidden
// - Errors: Simple messages
```

### Intermediate Mode
```csharp
ExperienceModeManager.Instance.SetMode(ExperienceLevel.Intermediate);

// Settings:
// - Code panel: VISIBLE (BASIC only)
// - Node labels: Mixed ("sensor.Temperature")
// - Available nodes: ~45 nodes
// - Data types: Visible
// - Errors: Detailed messages
```

### Expert Mode
```csharp
ExperienceModeManager.Instance.SetMode(ExperienceLevel.Expert);

// Settings:
// - Code panel: VISIBLE (BASIC + IC10)
// - Node labels: Technical ("l r0 d0 Temperature")
// - Available nodes: ALL 60+ nodes
// - Data types: Visible
// - Errors: Technical with IC10 details
// - Optimization hints: Enabled
```

### Custom Mode
```csharp
// User opens CustomModeDialog to configure
// Or programmatically:
var customSettings = new ExperienceModeSettings
{
    ShowCodePanel = true,
    NodeLabelStyle = NodeLabelStyle.Mixed,
    AvailableNodeCategories = new List<string> { "Variables", "Devices" },
    ShowDataTypes = true,
    ErrorMessageStyle = ErrorMessageStyle.Detailed
};

ExperienceModeManager.Instance.SetCustomSettings(customSettings);
ExperienceModeManager.Instance.SetMode(ExperienceLevel.Custom);
```

## Common Patterns

### Initialize on Window Load
```csharp
public VisualScriptingWindow()
{
    InitializeComponent();

    // Load from settings
    var settingsService = new SettingsService();
    settingsService.Load();
    ExperienceModeManager.Instance.LoadFromSettings(settingsService);

    // Apply to UI
    ApplyExperienceMode(ExperienceModeManager.Instance.CurrentSettings);

    // Subscribe to changes
    ExperienceModeManager.Instance.ModeChanged += OnModeChanged;
}
```

### Save on Window Close
```csharp
protected override void OnClosed(EventArgs e)
{
    base.OnClosed(e);

    // Save to settings
    var settingsService = new SettingsService();
    ExperienceModeManager.Instance.SaveToSettings(settingsService);
    settingsService.Save();
}
```

### Update Node Palette
```csharp
private void RefreshNodePalette()
{
    var settings = ExperienceModeManager.Instance.CurrentSettings;
    var filteredNodes = nodeFactory.GetFilteredNodes(settings);

    nodePalette.Clear();
    foreach (var category in filteredNodes)
    {
        var section = new CategorySection(category.Key);
        foreach (var nodeInfo in category.Value)
        {
            // Use NodeLabelProvider for display name
            var displayName = nodeInfo.DisplayName; // Already set by factory
            section.AddNode(nodeInfo.TypeName, displayName, nodeInfo.Icon);
        }
        nodePalette.AddSection(section);
    }
}
```

### Dynamic Label Updates
```csharp
// When mode changes, update all existing nodes
private void OnModeChanged(object? sender, ModeChangedEventArgs e)
{
    foreach (var node in allNodes)
    {
        // Update visual label (doesn't change node's internal data)
        var newLabel = NodeLabelProvider.GetLabel(node, e.Settings.NodeLabelStyle);
        nodeControl.UpdateDisplayLabel(newLabel);
    }
}
```

## Node Type Lists

### Beginner Nodes (20)
```
Variables: Variable, Constant
Devices: PinDevice, ReadProperty, WriteProperty, ThisDevice
Math: Add, Subtract, Multiply, Divide
Flow: Comment
```

### Intermediate Adds (25 more = 45 total)
```
Variables: Const, Define
Devices: NamedDevice, SlotRead, SlotWrite, BatchRead, BatchWrite
Math: Modulo, Power, Negate, MathFunction, MinMax
Logic: Compare, And, Or, Not
Arrays: Array, ArrayAccess, ArrayAssign
Stack: Push, Pop, Peek
```

### Expert Adds (15+ more = 60+ total)
```
Math: Trig, Atan2, ExpLog
Bitwise: Bitwise, BitwiseNot, Shift
Advanced: Hash, Increment, CompoundAssign, DeviceDatabaseLookup
```

## Label Examples

### Variable Node
```csharp
var node = new VariableNode
{
    VariableName = "temperature",
    IsDeclaration = true,
    InitialValue = "0"
};

// Friendly: "Create Variable 'temperature'"
// Mixed: "VAR temperature"
// Technical: "move r0 0"
```

### Read Property Node
```csharp
var node = new ReadPropertyNode { PropertyName = "Pressure" };

// Friendly: "Read Pressure from Device"
// Mixed: "device.Pressure"
// Technical: "l r0 d0 Pressure"
```

### Compare Node
```csharp
var node = new CompareNode { Comparison = ">" };

// Friendly: "Check if Greater Than"
// Mixed: "A > B"
// Technical: "sgt r0 r1 r2"
```

## Settings Properties Reference

```csharp
public class ExperienceModeSettings
{
    // UI Display
    bool ShowCodePanel           // Show/hide code panel
    bool ShowIC10Toggle          // Show IC10 toggle button
    bool ShowRegisterInfo        // Show register allocation info
    bool ShowLineNumbers         // Show line numbers in code
    bool ShowGridSnap            // Show grid snap controls
    bool ShowAdvancedProperties  // Show advanced node properties

    // Node Display
    NodeLabelStyle NodeLabelStyle         // Friendly/Mixed/Technical
    List<string> AvailableNodeCategories  // Empty = all
    bool ShowExecutionPins               // Show flow control pins
    bool ShowDataTypes                   // Show type indicators

    // Features
    bool ShowOptimizationHints   // Show optimization suggestions
    bool AutoCompile             // Auto-compile on changes

    // Messages
    ErrorMessageStyle ErrorMessageStyle  // Simple/Detailed/Technical

    // Palette
    int PaletteNodeLimit        // Max nodes before "Show More"
}
```

## UI Integration

### Code Panel
```csharp
// CodePanel automatically updates from experience mode
codePanel.ShowIC10Toggle = settings.ShowIC10Toggle;
codePanel.ShowLineNumbers = settings.ShowLineNumbers;
```

### Toolbar
```xaml
<!-- Add to toolbar -->
<local:ExperienceModeSelector x:Name="ModeSelector"
                            ModeChanged="OnModeChanged"/>
```

### Custom Dialog
```csharp
// Open custom mode dialog
var dialog = new CustomModeDialog { Owner = this };
if (dialog.ShowDialog() == true)
{
    // Settings saved automatically
    ExperienceModeManager.Instance.SetMode(ExperienceLevel.Custom);
}
```

## Category Names

```csharp
var categories = new[]
{
    "Variables",        // VAR, CONST, DEFINE
    "Devices",          // Device I/O operations
    "Basic Math",       // +, -, *, /
    "Flow Control",     // IF, WHILE, GOTO (future)
    "Math Functions",   // ABS, SQRT, CEIL, etc.
    "Logic",            // AND, OR, NOT
    "Arrays",           // DIM, array access
    "Comparison",       // ==, !=, <, >, etc.
    "Bitwise",          // AND, OR, XOR, shift
    "Advanced",         // Hash, increments
    "Stack",            // PUSH, POP, PEEK
    "Trigonometry"      // SIN, COS, TAN, etc.
};
```

## Error Message Examples

### Simple (Beginner)
```
"Connect the sensor first"
"Variable name cannot be empty"
"Device not found"
```

### Detailed (Intermediate)
```
"The ReadProperty node requires a device input. Connect a device node to the 'Device' input pin."
"Variable name must start with a letter and contain only letters, numbers, and underscores."
"No device found at pin d0. Make sure a device is connected to pin 0 on the IC housing."
```

### Technical (Expert)
```
"ReadProperty node validation failed: InputPin[0] (Device, DataType.Device) is not connected. IC10 compilation would fail with 'undefined device reference' at line 12."
"Invalid identifier 'my-var': BASIC identifiers must match regex ^[a-zA-Z][a-zA-Z0-9_]*$. Current value violates this constraint at character 2 (hyphen not allowed)."
"Device reference 'd0' unresolved during code generation. IC10 instruction 'l r0 d0 Temperature' will fail at runtime if pin 0 is not occupied. Consider using DEFINE or hash lookup."
```

## Tips

1. **Always subscribe to ModeChanged** if your UI component depends on mode settings
2. **Use NodeLabelProvider consistently** for all node display names
3. **Check IsNodeTypeAvailable** before allowing node creation
4. **Apply settings immediately** - no restart required
5. **Settings persist automatically** via SettingsService
6. **Custom mode is preserved** between sessions
7. **Mode changes don't affect existing graphs** - only UI display

## File Locations

```
Core System:
  UI/VisualScripting/ExperienceMode.cs
  UI/VisualScripting/ExperienceModeSettings.cs
  UI/VisualScripting/ExperienceModeManager.cs

Helpers:
  UI/VisualScripting/NodeLabelProvider.cs
  UI/VisualScripting/NodePaletteFilter.cs

UI Components:
  UI/VisualScripting/ExperienceModeSelector.xaml[.cs]
  UI/VisualScripting/CustomModeDialog.xaml[.cs]

Integration:
  UI/Services/SettingsService.cs
  UI/VisualScripting/Nodes/NodeFactory.cs
  UI/VisualScripting/VisualScriptingWindow.xaml[.cs]
  UI/VisualScripting/CodePanel.xaml.cs
```
