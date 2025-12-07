# Visual Scripting Bug Fixes - Design Document

**Date:** 2025-12-04
**Status:** Approved for Implementation
**Version Target:** v3.0.13

---

## Overview

This document outlines the design for fixing all outstanding Visual Scripting bugs identified in the QA Final Report. Implementation will use 6 parallel agents for maximum efficiency.

---

## Bug Summary

| Priority | Bug | Agent |
|----------|-----|-------|
| HIGH | Simulator alias mapping | 1A |
| HIGH | Simulator device pool for batch ops | 1B |
| MEDIUM | Dropdown text invisible | 2A |
| MEDIUM | Wire removal - selection + delete key | 2B |
| MEDIUM | Wire removal - right-click + drag | 2C |
| LOW | Comment node code output | 2D |

---

## Phase 1: Simulator Device System

### Agent 1A: Device Alias Registry

**Purpose:** Map named device aliases to virtual devices for simulation.

**New Class:** `DeviceAliasRegistry.cs`

```csharp
public class DeviceAliasRegistry
{
    private readonly Dictionary<string, VirtualDevice> _aliases = new();

    // Register a device alias from DEVICE declaration
    public void RegisterAlias(string alias, string prefabName);

    // Resolve alias to virtual device
    public VirtualDevice? GetDevice(string alias);

    // Check if alias exists
    public bool HasAlias(string alias);

    // Clear all aliases (for new simulation)
    public void Clear();
}
```

**Integration Points:**
- `Simulator.cs` - Initialize registry from parsed DEVICE statements before run
- `Simulator.cs` - When executing `device.Property`, check alias registry first
- Falls back to `d0-d5` pins if alias not found

**Virtual Device Structure:**
```csharp
public class VirtualDevice
{
    public string Alias { get; set; }
    public string PrefabName { get; set; }
    public Dictionary<string, double> Properties { get; set; }

    // Get property value (with defaults based on prefab type)
    public double GetProperty(string name);

    // Set property value
    public void SetProperty(string name, double value);
}
```

### Agent 1B: Device Pool for Batch Operations

**Purpose:** Support `lb` (batch read) and `sb` (batch write) operations.

**New Class:** `DevicePool.cs`

```csharp
public class DevicePool
{
    private readonly Dictionary<int, List<VirtualDevice>> _devicesByHash = new();

    // Add device to pool (called when DEVICE registered)
    public void AddDevice(VirtualDevice device);

    // Get all devices matching prefab hash
    public IEnumerable<VirtualDevice> GetDevicesByHash(int prefabHash);

    // Batch read - returns aggregated value based on mode
    public double BatchRead(int prefabHash, string property, BatchMode mode);

    // Batch write - writes to all matching devices
    public void BatchWrite(int prefabHash, string property, double value);

    // Clear pool
    public void Clear();
}

public enum BatchMode
{
    Average,    // lb default
    Sum,
    Minimum,
    Maximum
}
```

**Integration Points:**
- `DeviceAliasRegistry` calls `DevicePool.AddDevice()` when registering
- `Simulator.cs` - Route `lb`/`sb` instructions to DevicePool
- Hash calculation uses same algorithm as Stationeers game

---

## Phase 2: UI Fixes

### Agent 2A: Dropdown Text Visibility

**Problem:** ComboBox selected value invisible when dropdown closed.

**Root Cause:** Default WPF ComboBox styling has dark text on dark background in our theme.

**Fix Location:** `NodeControl.xaml.cs` - `RenderEditableProperties()` method

**Solution:**
```csharp
var comboBox = new ComboBox
{
    Width = 120,
    Height = 24,
    Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
    Foreground = new SolidColorBrush(Colors.Black),
    BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
    // ... other properties
};

// Add ItemContainerStyle for dropdown items
var itemStyle = new Style(typeof(ComboBoxItem));
itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, Brushes.White));
itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, Brushes.Black));
comboBox.ItemContainerStyle = itemStyle;
```

**Nodes Affected:**
- ReadProperty (PropertyName dropdown)
- WriteProperty (PropertyName dropdown)
- Compare (Operator dropdown)
- Increment (Type dropdown)
- CompoundAssign (Operator dropdown)
- PinDevice (Pin dropdown) - already fixed, verify still working

### Agent 2B: Wire Selection + Delete Key

**Problem:** No way to select wires or delete them with keyboard.

**Changes to `WireVisual.cs`:**
```csharp
public class WireVisual
{
    public bool IsSelected { get; set; }
    public bool IsHovered { get; set; }

    // Visual feedback
    public Brush StrokeBrush => IsSelected ? SelectedBrush : (IsHovered ? HoverBrush : NormalBrush);
    public double StrokeThickness => IsSelected ? 3.0 : 2.0;
}
```

**Changes to `VisualCanvas.xaml.cs`:**
```csharp
// Track selected wire
private WireVisual? _selectedWire;

// Wire hit testing
private WireVisual? HitTestWire(Point position)
{
    foreach (var wire in _wires)
    {
        if (wire.GetDistanceToPoint(position) < 5.0)
            return wire;
    }
    return null;
}

// Mouse click handler - select wire
private void Canvas_MouseLeftButtonDown(...)
{
    var wire = HitTestWire(position);
    if (wire != null)
    {
        SelectWire(wire);
        e.Handled = true;
    }
}

// Keyboard handler
private void Canvas_KeyDown(...)
{
    if (e.Key == Key.Delete && _selectedWire != null)
    {
        DeleteWire(_selectedWire);
        _selectedWire = null;
    }
}
```

**Wire Distance Calculation:**
- Use bezier curve distance algorithm
- Sample curve at multiple points, find minimum distance to click point

### Agent 2C: Wire Right-Click Menu + Drag Disconnect

**Right-Click Context Menu:**
```csharp
private void Canvas_MouseRightButtonDown(...)
{
    var wire = HitTestWire(position);
    if (wire != null)
    {
        var menu = new ContextMenu();
        var deleteItem = new MenuItem { Header = "Delete Wire" };
        deleteItem.Click += (s, args) => DeleteWire(wire);
        menu.Items.Add(deleteItem);
        menu.IsOpen = true;
    }
}
```

**Drag-to-Disconnect:**
```csharp
// When starting drag near a connected pin
private void StartWireDrag(NodePin pin)
{
    // Find wire connected to this pin
    var wire = FindWireConnectedTo(pin);
    if (wire != null)
    {
        _draggingWire = wire;
        _dragSourcePin = pin;
        // Temporarily disconnect visual
    }
}

// During drag - wire follows mouse
private void UpdateWireDrag(Point mousePosition)
{
    // Update temp wire endpoint to mouse position
}

// On drag end
private void EndWireDrag(Point mousePosition)
{
    var targetPin = HitTestPin(mousePosition);
    if (targetPin != null && IsCompatiblePin(targetPin))
    {
        // Reconnect to new pin
        ReconnectWire(_draggingWire, targetPin);
    }
    else
    {
        // Dropped in empty space - delete wire
        DeleteWire(_draggingWire);
    }
}
```

### Agent 2D: Comment Node Code Output

**Problem:** CommentNode exists but doesn't generate code output.

**Fix Location:** `CommentNode.cs`

```csharp
public override string GenerateCode()
{
    if (string.IsNullOrWhiteSpace(CommentText))
        return string.Empty;

    // Handle multi-line comments
    var lines = CommentText.Split('\n');
    var result = new StringBuilder();
    foreach (var line in lines)
    {
        result.AppendLine($"# {line.TrimEnd('\r')}");
    }
    return result.ToString().TrimEnd();
}
```

**Fix Location:** Code generator (ensure comments included in output)
- Check if CommentNode is filtered out during code generation
- Ensure non-executable nodes (comments) are still processed for output

---

## Agent Assignment Summary

| Agent | Files to Modify | Estimated Complexity |
|-------|-----------------|---------------------|
| 1A | New: DeviceAliasRegistry.cs, VirtualDevice.cs; Modify: Simulator.cs | Medium |
| 1B | New: DevicePool.cs; Modify: Simulator.cs, DeviceAliasRegistry.cs | Medium |
| 2A | Modify: NodeControl.xaml.cs | Low |
| 2B | Modify: WireVisual.cs, VisualCanvas.xaml.cs | Medium |
| 2C | Modify: VisualCanvas.xaml.cs | Medium |
| 2D | Modify: CommentNode.cs, BasicCodeGenerator.cs | Low |

---

## QA Test Plan

### Simulator Tests

| Test ID | Test Case | Steps | Expected Result |
|---------|-----------|-------|-----------------|
| SIM-01 | Alias resolution | Create script with `DEVICE sensor "StructureGasSensor"`, run simulator | Simulator shows `sensor` in device list |
| SIM-02 | Property read via alias | Add `temp = sensor.Temperature`, step through | `temp` receives value from sensor device |
| SIM-03 | Property write via alias | Add `cooler.On = 1`, step through | Cooler device shows On = 1 |
| SIM-04 | Multiple aliases | Two DEVICE declarations, use both | Both devices tracked separately |
| SIM-05 | Batch read (lb) | Add 3 devices of same type, use lb | Returns correct aggregated value |
| SIM-06 | Batch write (sb) | Use sb to write to device type | All matching devices updated |

### UI Tests

| Test ID | Test Case | Steps | Expected Result |
|---------|-----------|-------|-----------------|
| UI-01 | Dropdown visibility | Add ReadProperty node, select Temperature | "Temperature" visible when dropdown closed |
| UI-02 | All dropdowns | Check Compare, Increment, CompoundAssign dropdowns | All show selected value clearly |
| UI-03 | Wire select | Click on a wire | Wire highlights (thicker, glow) |
| UI-04 | Wire delete key | Select wire, press Delete | Wire removed, pins show disconnected |
| UI-05 | Wire right-click | Right-click wire | Context menu appears with "Delete Wire" |
| UI-06 | Wire menu delete | Click "Delete Wire" in menu | Wire removed |
| UI-07 | Wire drag disconnect | Drag wire end to empty canvas | Wire removed |
| UI-08 | Wire reconnect | Drag wire end to compatible pin | Wire reconnects to new pin |
| UI-09 | Comment output | Add Comment node with text, generate code | `# comment text` in BASIC output |
| UI-10 | Multi-line comment | Comment with multiple lines | Each line prefixed with `#` |

---

## Post-Implementation

After all agents complete:
1. Integrate all changes
2. Build v3.0.13
3. Run test suite
4. Package release
5. Create QA test document
6. Proceed to Phase 2 (Wire Animations) after QA pass

---

**End of Design Document**
