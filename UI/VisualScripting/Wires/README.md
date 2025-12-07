# Wire/Connection System - Phase 2A

This folder contains the wire rendering and connection system for the Basic-10 Visual Scripting feature.

## Overview

The wire system provides visual connections between nodes with:
- Smooth bezier curve rendering
- Type-safe connections with validation
- Interactive wire creation with drag-and-drop
- Particle flow animation during simulation
- Full serialization support
- Undo/redo integration

## Architecture

### Core Components

#### 1. Wire.cs
The data model for wire connections.

**Key Features:**
- Stores source and target node/pin references
- Calculates bezier curve points for rendering
- Type compatibility validation
- Hit testing for mouse interaction

**Type Compatibility Rules:**
- Execution → Execution only
- Number ↔ Boolean (bidirectional conversion)
- Device → Device only
- String → String only

#### 2. WireRenderer.cs
Handles bezier curve rendering with anti-aliasing.

**Rendering States:**
- Default: 2px stroke
- Hovered: 3px stroke
- Selected: 3px stroke + glow effect
- Temporary (dragging): Dashed line

**Color Coding:**
- Uses PinColors.GetColor() based on DataType
- Invalid connections shown in red during creation

#### 3. WireAnimation.cs
Particle flow animation system (simulation mode only).

**Features:**
- Particles flow from source to target at ~100px/second
- Brightness pulse effect on value changes
- 3 particles per wire by default
- Enable/disable toggle for performance

**Usage:**
```csharp
var animator = new WireAnimation();
animator.IsEnabled = true; // Enable during simulation
animator.InitializeWire(wire.Id);
animator.NotifyValueChange(wire.Id); // Trigger pulse
animator.Update(deltaTime);
animator.RenderParticles(context, wire);
```

#### 4. ConnectionManager.cs
Manages all wire connections and validation.

**Core Methods:**
```csharp
// Create connection with validation
Wire? CreateConnection(NodePin sourcePin, NodePin targetPin, out string errorMessage);

// Remove connections
bool RemoveConnection(Guid wireId);
int RemoveConnectionsForNode(Guid nodeId);
int RemoveConnectionsForPin(Guid pinId);

// Query connections
IEnumerable<Wire> GetConnectionsForNode(Guid nodeId);
IEnumerable<Wire> GetConnectionsForPin(Guid pinId);
bool IsConnected(NodePin sourcePin, NodePin targetPin);

// Validation
bool ValidateConnection(NodePin sourcePin, NodePin targetPin, out string errorMessage);
bool WouldCreateCycle(NodeBase sourceNode, NodeBase targetNode);
```

**Events:**
- `ConnectionCreated` - Raised when wire is created
- `ConnectionRemoved` - Raised when wire is removed
- `ConnectionValidationFailed` - Raised when validation fails

#### 5. WireCreationTool.cs
Interactive wire creation with drag-and-drop.

**Workflow:**
1. Click and drag from output pin
2. Temporary wire follows mouse cursor
3. Hover over compatible input pins (shows green glow)
4. Hover over incompatible pins (shows red X)
5. Release on valid pin to create connection
6. Press Escape or right-click to cancel

**Integration:**
```csharp
var tool = new WireCreationTool(connectionManager, undoRedoManager);

// Handle input events
tool.HandleMouseDown(mousePos, hitPin);
tool.HandleMouseMove(mousePos, hoveredPin);
tool.HandleMouseUp(mousePos, hitPin);
tool.HandleKeyDown(key);
tool.HandleRightMouseDown();

// Render temporary wire during creation
tool.RenderTemporaryWire(context);
tool.RenderPinFeedback(context, pin, pinCenter);
```

**Undo/Redo Actions:**
- `CreateConnectionAction` - Undoable wire creation
- `RemoveConnectionAction` - Undoable wire removal

#### 6. WireSerializer.cs
JSON serialization for saving/loading wires.

**Features:**
- Serialize wires to JSON format
- Deserialize and reconnect wires after loading nodes
- Wire copying with ID remapping (for copy/paste)
- Validation after deserialization

**File Format:**
```json
[
  {
    "Id": "guid",
    "SourceNodeId": "guid",
    "SourcePinId": "guid",
    "TargetNodeId": "guid",
    "TargetPinId": "guid",
    "DataType": "Number"
  }
]
```

**Usage:**
```csharp
// Save
string json = WireSerializer.SerializeWires(wires);
WireSerializer.SaveToFile(filePath, wires);

// Load
var nodes = GetAllNodes(); // Dictionary<Guid, NodeBase>
var wires = WireSerializer.DeserializeWires(json, nodes);
var wires = WireSerializer.LoadFromFile(filePath, nodes);

// Validate
var errors = WireSerializer.ValidateWires(wires);

// Copy wire (for copy/paste)
var copiedWire = WireSerializer.CopyWire(wire, nodeIdMapping, pinIdMapping, nodes);
```

## Visual Feedback

### Pin Feedback During Wire Creation
- **Valid Target**: Green glow (128 alpha, #44FF44)
- **Invalid Target**: Red X overlay (2px stroke, #FF4444)
- **Compatible Pins**: Highlighted automatically

### Wire States
- **Normal**: Type color at full opacity
- **Hovered**: Thicker stroke (3px)
- **Selected**: Thicker stroke + white glow (4px shadow at 80 alpha)
- **Dragging**: Dashed line (4-2 pattern) at 200 alpha
- **Invalid**: Red color (#FF4444)

## Integration Example

```csharp
// Setup
var connectionManager = new ConnectionManager();
var wireAnimation = new WireAnimation();
var wireCreationTool = new WireCreationTool(connectionManager, undoRedoManager);

// Event handlers
connectionManager.ConnectionCreated += (s, e) => {
    wireAnimation.InitializeWire(e.Wire.Id);
    InvalidateVisual(); // Trigger redraw
};

connectionManager.ConnectionRemoved += (s, e) => {
    wireAnimation.RemoveWire(e.Wire.Id);
    InvalidateVisual();
};

// Input handling (in canvas MouseDown)
if (wireCreationTool.HandleMouseDown(mousePos, hitPin))
    return; // Wire creation started

// Rendering (in OnRender)
foreach (var wire in connectionManager.Wires)
{
    WireRenderer.RenderWire(drawingContext, wire);
    if (wireAnimation.IsEnabled)
        wireAnimation.RenderParticles(drawingContext, wire);
}

if (wireCreationTool.IsDragging)
    wireCreationTool.RenderTemporaryWire(drawingContext);

// Animation update (CompositionTarget.Rendering)
wireAnimation.Update(deltaTime);
```

## Performance Considerations

- **Hit Testing**: Uses bezier sampling (20 points) for accurate detection
- **Animation**: Only enabled during simulation
- **Rendering**: Uses frozen StreamGeometry for optimal performance
- **Validation**: Cached in ConnectionManager to avoid repeated checks

## Future Enhancements (Phase 3+)

- Wire rerouting (drag middle of wire to create control points)
- Wire bundles (group related wires)
- Wire labels (show data values during debugging)
- Multi-wire creation (shift-drag to create multiple connections)
- Smart wire routing (auto-avoid nodes)
- Wire breakpoints (pause execution at specific connections)

## Dependencies

- `BasicToMips.UI.VisualScripting.Nodes` - Node and pin definitions
- `BasicToMips.UI.VisualScripting.Canvas` - UndoRedoManager
- System.Windows.Media - WPF rendering
- System.Text.Json - Serialization

## Testing Checklist

- [ ] Create connection between compatible pins
- [ ] Reject connection between incompatible types
- [ ] Reject self-connections (node to itself)
- [ ] Reject cycles (A→B→A)
- [ ] Multiple outputs to different inputs
- [ ] Only one input connection per input pin
- [ ] Undo/redo wire creation
- [ ] Undo/redo wire deletion
- [ ] Save/load wires correctly
- [ ] Wire hover detection
- [ ] Wire selection
- [ ] Particle animation during simulation
- [ ] Brightness pulse on value change
- [ ] Drag from output pin
- [ ] Cancel with Escape or right-click
- [ ] Visual feedback on compatible/incompatible pins
