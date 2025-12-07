# Visual Scripting Node System - Phase 1B

This folder contains the core node system for Basic-10's visual scripting feature (v3.0).

## Architecture Overview

The node system is built with these core components:

### Core Classes

1. **NodeBase.cs** - Abstract base class for all nodes
   - Defines common properties: Id, Position (X,Y), Size (Width,Height), Label
   - Manages input/output pins collections
   - Provides hit testing for selection and pin clicks
   - Defines abstract methods: `Validate()` and `GenerateCode()`

2. **NodePin.cs** - Pin definition for node connections
   - Properties: Id, Name, PinType (Input/Output), DataType
   - Tracks connections to other pins via Guid list
   - Calculates position relative to parent node
   - Pin layout: 12px diameter, 8px from edge, 24px spacing

3. **PinColors.cs** - Static color definitions for data types
   - Execution: White (#FFFFFF)
   - Number: Blue (#4A9EFF)
   - Boolean: Green (#44FF44) / Red (#FF4444)
   - Device: Orange (#FFA500)
   - String: Purple (#AA44FF)

4. **NodeControl.xaml / .cs** - WPF visual rendering
   - Displays node with rounded corners (8px radius)
   - Category-colored header with icon + label
   - Input pins on left, output pins on right
   - Selection highlight with glow effect
   - Drag-to-move with event raising for undo tracking

5. **NodeSerializer.cs** - JSON serialization
   - Converts nodes to/from JSON using System.Text.Json
   - Serializes base properties + node-specific properties via reflection
   - Preserves pin connections and configuration

6. **NodeFactory.cs** - Node creation and registration
   - Register node types: `RegisterNodeType<T>()`
   - Create instances: `CreateNode(typeName)`
   - Query types by category for palette UI

### Concrete Node Implementations

**Basic Nodes:**

1. **CommentNode.cs** - Documentation/annotation node
   - Category: Comments
   - No execution pins (non-functional)
   - Properties: CommentText, CommentColor
   - Generates: REM statements

2. **VariableNode.cs** - Variable declaration/assignment
   - Category: Variables
   - Execution pins + data pins
   - Properties: VariableName, VariableType, InitialValue, IsDeclaration
   - Generates: VAR or LET statements
   - Validates: identifier syntax, numeric values

3. **MathNode.cs** - Mathematical operations
   - Category: Math
   - Operations: Add, Subtract, Multiply, Divide, Modulo, Power, Abs, Sqrt, Min, Max
   - Adjusts pins based on operation (unary vs binary)
   - Generates: expression fragments

**Device Declaration Nodes (Phase 3B):**

4. **PinDeviceNode.cs** - Physical pin device alias
   - Category: Devices
   - Properties: AliasName, PinNumber (0-5)
   - Output: Device pin
   - Generates: `ALIAS aliasName dN`

5. **NamedDeviceNode.cs** - Named device reference (bypasses 6-pin limit)
   - Category: Devices
   - Properties: AliasName, PrefabName (with autocomplete)
   - Output: Device pin
   - Generates: `DEVICE aliasName "PrefabName"`

6. **ThisDeviceNode.cs** - IC chip self-reference
   - Category: Devices
   - Properties: AliasName, UseDirectReference
   - Output: Device pin
   - Generates: `ALIAS aliasName db` or uses db directly

**Device I/O Nodes (Phase 3B):**

7. **ReadPropertyNode.cs** - Read device property
   - Category: Devices
   - Input: Device pin
   - Properties: PropertyName (dropdown with common properties)
   - Output: Value (Number)
   - Generates: `device.PropertyName`

8. **WritePropertyNode.cs** - Write device property
   - Category: Devices
   - Inputs: Exec, Device, Value
   - Properties: PropertyName (dropdown)
   - Output: Exec
   - Generates: `device.PropertyName = value`

9. **SlotReadNode.cs** - Read device slot property
   - Category: Devices
   - Inputs: Device, SlotIndex
   - Properties: PropertyName
   - Output: Value
   - Generates: `device.Slot(index).PropertyName`

10. **SlotWriteNode.cs** - Write device slot property
    - Category: Devices
    - Inputs: Exec, Device, SlotIndex, Value
    - Properties: PropertyName
    - Output: Exec
    - Generates: `device.Slot(index).PropertyName = value`

**Batch Operations (Phase 3B):**

11. **BatchReadNode.cs** - Batch read from all devices
    - Category: Devices
    - Input: DeviceHash
    - Properties: PropertyName, Mode (Average/Sum/Min/Max)
    - Output: Value
    - Generates: `BATCHREAD(hash, Property, mode)`

12. **BatchWriteNode.cs** - Batch write to all devices
    - Category: Devices
    - Inputs: Exec, DeviceHash, Value
    - Properties: PropertyName
    - Output: Exec
    - Generates: `BATCHWRITE(hash, Property, value)`

**Stack Operations (Phase 3B):**

13. **PushNode.cs** - Push value to stack
    - Category: Devices
    - Inputs: Exec, Value
    - Output: Exec
    - Generates: `PUSH value`

14. **PopNode.cs** - Pop value from stack
    - Category: Devices
    - Input: Exec
    - Properties: VariableName
    - Outputs: Exec, Value
    - Generates: `POP variableName`

15. **PeekNode.cs** - Peek at stack top
    - Category: Devices
    - Input: Exec
    - Properties: VariableName
    - Outputs: Exec, Value
    - Generates: `PEEK variableName`

**Utility Nodes (Phase 3B):**

16. **HashNode.cs** - Calculate device/string hash
    - Category: Devices
    - Properties: StringValue, CreateDefine, DefineName
    - Output: Hash (Number)
    - Generates: `DEFINE name hash` or compile-time hash
    - Uses DeviceDatabase for CRC32 calculation

17. **DeviceDatabaseLookup.cs** - Helper class (not a node)
    - Provides autocomplete for device prefabs
    - Property suggestions from device database
    - Integration with Data/DeviceDatabase.cs

## Visual Style

**Minimal/Clean Design:**
- Background: Dark gray (#2D2D2D)
- Border: 1px subtle (#3D3D3D), 2px accent when selected
- Selection: Blue glow (#4A9EFF) with drop shadow
- Text: White, 12px Segoe UI
- Header: Category-specific color

**Pin Layout:**
- Diameter: 12px circles
- Edge offset: 8px from node edge
- Vertical spacing: 24px between pins
- Stroke: 1px normally, 2px when connected

## Usage Example

```csharp
// 1. Create factory and register node types
var factory = new NodeFactory();
factory.RegisterNodeType<CommentNode>();
factory.RegisterNodeType<VariableNode>();
factory.RegisterNodeType<MathNode>();

// 2. Create a node
var varNode = new VariableNode
{
    X = 100,
    Y = 100,
    VariableName = "temperature",
    InitialValue = "25.5"
};
varNode.Initialize();

// 3. Connect nodes
var mathNode = factory.CreateNode("Math") as MathNode;
NodeSystemExample.ConnectNodes(varNode, "temperature", mathNode, "A");

// 4. Serialize
var json = NodeSerializer.SerializeNode(varNode);
var jsonString = json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

// 5. Deserialize
var deserializedNode = NodeSerializer.DeserializeNode(json, factory);

// 6. Validate
if (varNode.Validate(out string errorMessage))
{
    var code = varNode.GenerateCode(); // "VAR temperature = 25.5"
}
```

See **NodeSystemExample.cs** for complete working examples.

## Creating Custom Nodes

To create a new node type:

```csharp
public class MyCustomNode : NodeBase
{
    public override string NodeType => "MyCustom";
    public override string Category => "Custom";

    // Custom properties
    public string MyProperty { get; set; } = "default";

    public MyCustomNode()
    {
        Label = "My Custom Node";
        Width = 200;
        Height = 100;
    }

    public override void Initialize()
    {
        base.Initialize();

        // Define pins
        AddInputPin("In", DataType.Execution);
        AddOutputPin("Out", DataType.Execution);
        AddInputPin("Value", DataType.Number);

        // Calculate height
        Height = CalculateMinHeight();
    }

    public override bool Validate(out string errorMessage)
    {
        if (string.IsNullOrEmpty(MyProperty))
        {
            errorMessage = "MyProperty cannot be empty";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public override string GenerateCode()
    {
        return $"REM Custom: {MyProperty}";
    }
}

// Register it
factory.RegisterNodeType<MyCustomNode>();
```

## Next Steps (Phase 1C)

The canvas system will be built next to:
- Display and manage multiple nodes
- Handle selection and dragging
- Render connection wires between pins
- Support zoom and pan
- Implement grid snapping

## Technical Notes

- **WPF Framework**: Uses System.Windows.Controls.UserControl (not WinForms)
- **Serialization**: System.Text.Json with reflection for custom properties
- **Type Safety**: DataType enum enforces connection compatibility
- **Undo/Redo**: Event-driven architecture supports command pattern
- **Performance**: Minimal rendering with efficient hit testing

## File Structure

```
UI/VisualScripting/Nodes/
├── NodeBase.cs                  # Abstract base class
├── NodePin.cs                   # Pin definition + DataType enum
├── PinColors.cs                 # Color constants
├── NodeControl.xaml             # Visual rendering (XAML)
├── NodeControl.xaml.cs          # Visual rendering (code-behind)
├── NodeSerializer.cs            # JSON serialization
├── NodeFactory.cs               # Node creation + registration
├── CommentNode.cs               # Basic: comment/annotation
├── VariableNode.cs              # Basic: VAR/LET
├── MathNode.cs                  # Basic: math operations
├── PinDeviceNode.cs             # Device: pin alias (d0-d5)
├── NamedDeviceNode.cs           # Device: named device reference
├── ThisDeviceNode.cs            # Device: IC chip self-reference
├── ReadPropertyNode.cs          # Device I/O: read property
├── WritePropertyNode.cs         # Device I/O: write property
├── SlotReadNode.cs              # Device I/O: read slot
├── SlotWriteNode.cs             # Device I/O: write slot
├── BatchReadNode.cs             # Batch: read operation
├── BatchWriteNode.cs            # Batch: write operation
├── PushNode.cs                  # Stack: push value
├── PopNode.cs                   # Stack: pop value
├── PeekNode.cs                  # Stack: peek value
├── HashNode.cs                  # Utility: calculate hash
├── DeviceDatabaseLookup.cs      # Utility: device DB helper
├── NodeSystemExample.cs         # Usage examples
└── README.md                    # This file
```

## Dependencies

- .NET 8.0 WPF
- System.Text.Json (built-in)
- No external packages required

---

**Status**: Phase 1B Complete, Phase 3B Complete (Device & I/O Nodes)
**Version**: Basic-10 v3.0.0+
**Author**: Dog Tired Studios
