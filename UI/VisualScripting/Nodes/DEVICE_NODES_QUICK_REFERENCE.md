# Device Nodes Quick Reference

Quick reference guide for Basic-10 Visual Scripting device nodes (Phase 3B).

## Node Types Overview

| Node | Category | Icon | Purpose |
|------|----------|------|---------|
| PinDeviceNode | Devices | 🔌 | Alias device on physical pin (d0-d5) |
| NamedDeviceNode | Devices | 📡 | Reference device by prefab name |
| ThisDeviceNode | Devices | 💾 | Reference the IC chip itself |
| ReadPropertyNode | Devices | 📖 | Read property from device |
| WritePropertyNode | Devices | ✏️ | Write property to device |
| SlotReadNode | Devices | 📦 | Read property from device slot |
| SlotWriteNode | Devices | 📝 | Write property to device slot |
| BatchReadNode | Devices | 📊 | Batch read from all devices |
| BatchWriteNode | Devices | 📢 | Batch write to all devices |
| PushNode | Devices | ⬆️ | Push value to stack |
| PopNode | Devices | ⬇️ | Pop value from stack |
| PeekNode | Devices | 👁️ | Peek at stack top |
| HashNode | Devices | #️⃣ | Calculate device/string hash |

## Common Usage Patterns

### Pattern 1: Read Temperature from Gas Sensor
```
[PinDeviceNode: d0] → [ReadPropertyNode: Temperature] → [Variable]
```
Generated code:
```basic
ALIAS sensor d0
temp = sensor.Temperature
```

### Pattern 2: Control Furnace
```
[NamedDeviceNode: "StructureFurnace"] → [WritePropertyNode: On] ← [Constant: 1]
```
Generated code:
```basic
DEVICE furnace "StructureFurnace"
furnace.On = 1
```

### Pattern 3: Check Slot Contents
```
[Device] → [SlotReadNode: OccupantHash] ← [Constant: 0] → [Variable]
```
Generated code:
```basic
item = device.Slot(0).OccupantHash
```

### Pattern 4: Batch Control All Vents
```
[HashNode: "StructureActiveVent"] → [BatchWriteNode: On] ← [Constant: 1]
```
Generated code:
```basic
DEFINE VENT_HASH -1129453144
BATCHWRITE(VENT_HASH, On, 1)
```

### Pattern 5: Average Temperature Across Sensors
```
[HashNode: "StructureGasSensor"] → [BatchReadNode: Temperature, Average] → [Variable]
```
Generated code:
```basic
DEFINE SENSOR_HASH 1915566498
avgTemp = BATCHREAD(SENSOR_HASH, Temperature, 0)
```

## Property Reference

### Most Common Device Properties
| Property | Type | Read | Write | Description |
|----------|------|------|-------|-------------|
| On | Number | ✅ | ✅ | Device power state (0/1) |
| Temperature | Number | ✅ | ❌ | Temperature in Kelvin |
| Pressure | Number | ✅ | ❌ | Pressure in kPa |
| Setting | Number | ✅ | ✅ | Device setting/target value |
| Mode | Number | ✅ | ✅ | Operating mode |
| Power | Number | ✅ | ❌ | Current power consumption/generation |
| Error | Number | ✅ | ❌ | Error state (0/1) |
| Lock | Number | ✅ | ✅ | Device lock state (0/1) |
| Open | Number | ✅ | ✅ | Valve/door open state (0-100) |
| Ratio | Number | ✅ | ✅ | Ratio setting (0-100) |

### Atmospheric Properties (Gas Sensors)
| Property | Description |
|----------|-------------|
| RatioOxygen | Oxygen ratio (0-1) |
| RatioCarbonDioxide | CO2 ratio (0-1) |
| RatioNitrogen | Nitrogen ratio (0-1) |
| RatioPollutant | Pollutant ratio (0-1) |
| RatioVolatiles | Volatiles ratio (0-1) |
| RatioWater | Water vapor ratio (0-1) |
| TotalMoles | Total moles in atmosphere |

### Slot Properties (Inventories/Furnaces)
| Property | Type | Description |
|----------|------|-------------|
| Occupied | Number | Slot has item (0/1) |
| OccupantHash | Number | Hash of item in slot |
| Quantity | Number | Quantity of item |
| Damage | Number | Item damage percentage |
| Efficiency | Number | Item efficiency |
| Health | Number | Item health/durability |
| Growth | Number | Plant growth stage |

### Power Properties
| Property | Description |
|----------|-------------|
| Charge | Battery charge (0-1 or absolute) |
| PowerGeneration | Power being generated (W) |
| PowerRequired | Power required (W) |
| PowerActual | Actual power consumption (W) |
| MaxPower | Maximum power capacity (W) |

## Batch Modes

| Mode | Value | Description | Example Use |
|------|-------|-------------|-------------|
| Average | 0 | Average of all values | Average temperature across sensors |
| Sum | 1 | Sum of all values | Total power generation |
| Minimum | 2 | Minimum value | Lowest pressure in system |
| Maximum | 3 | Maximum value | Highest temperature reading |

## Common Device Prefab Names

### Atmospheric
- `StructureActiveVent` - Active Vent
- `StructureGasSensor` - Gas Sensor
- `StructureWallHeater` - Wall Heater
- `StructureWallCooler` - Wall Cooler

### Power
- `StructureBattery` - Battery
- `StructureSolarPanel` - Solar Panel
- `StructureAreaPowerControl` - APC (Area Power Controller)

### Logic
- `StructureLogicMemory` - Memory (IC10 Housing)
- `StructureConsole` - Console
- `StructureLEDDisplay` - LED Display

### Processing
- `StructureFurnace` - Furnace
- `StructureAutolathe` - Autolathe
- `StructureCentrifuge` - Centrifuge

### Storage
- `StructureGasStorage` - Gas Tank
- `StructureChute` - Chute
- `StructureLocker` - Locker

## Pin Connections Quick Guide

### Device Pins (Orange)
- **Output from**: PinDeviceNode, NamedDeviceNode, ThisDeviceNode
- **Input to**: ReadPropertyNode, WritePropertyNode, SlotReadNode, SlotWriteNode

### Number Pins (Blue)
- **Output from**: ReadPropertyNode, SlotReadNode, BatchReadNode, PopNode, PeekNode, HashNode
- **Input to**: WritePropertyNode, SlotWriteNode, BatchWriteNode, PushNode, SlotIndex parameters

### Execution Pins (White)
- **Flow control**: Connect execution pins to sequence operations
- **Input to**: WritePropertyNode, SlotWriteNode, BatchWriteNode, PushNode, PopNode, PeekNode
- **Output from**: All write/stack nodes for chaining

## Validation Rules

### Identifier Validation
- Must start with a letter
- Can contain letters, numbers, underscores
- Case-sensitive
- Examples: `sensor`, `temp_1`, `myDevice`

### Pin Number Validation
- Must be 0-5 (6 total pins on IC10)
- Integer only
- Common: d0 = closest to IC chip

### Hash Calculation
- Uses Stationeers CRC32 algorithm
- Same hash as in-game HASH() function
- DeviceDatabase.CalculateHash(string)

## Stack Operations Usage

### When to Use Stack
1. **Temporary storage** - Save values during complex calculations
2. **Function calls** - Pass parameters/return values
3. **Loop state** - Save loop variables
4. **Interrupt handling** - Preserve state

### Stack Example: Save and Restore
```
[Variable: temp] → [PushNode]
    ↓
[Complex Operations]
    ↓
[PopNode: savedTemp] → [WritePropertyNode]
```

## DeviceDatabaseLookup Helper

Use in UI for autocomplete:
```csharp
// Get device suggestions
var devices = DeviceDatabaseLookup.GetDevicePrefabSuggestions("Furnace", 10);

// Get property suggestions
var properties = DeviceDatabaseLookup.GetCommonProperties();

// Get device hash
int hash = DeviceDatabaseLookup.GetDeviceHash("StructureActiveVent");

// Get batch modes
var modes = DeviceDatabaseLookup.GetBatchModes();
```

## Error Prevention Tips

1. **Always connect Device pins** - Devices must be declared before use
2. **Validate property names** - Use dropdown suggestions
3. **Check pin numbers** - Only 0-5 valid
4. **Match data types** - Can't connect Number to Device pin
5. **Use appropriate batch mode** - Average for ratios, Sum for totals
6. **Stack discipline** - Every PUSH should have matching POP
7. **Hash verification** - Use HashNode for compile-time constants

## Performance Tips

1. **Cache device hashes** - Use DEFINE with HashNode
2. **Batch operations** - Faster than iterating devices
3. **Avoid repeated reads** - Store values in variables
4. **Use db for self-reference** - No alias needed
5. **Minimize stack operations** - CPU cycles are precious

## Common Mistakes to Avoid

❌ **Wrong**: Reading from db without alias
```basic
temp = db.Temperature  # Error: db is IC chip, not sensor
```

✅ **Correct**: Alias a device first
```basic
ALIAS sensor d0
temp = sensor.Temperature
```

❌ **Wrong**: Batch write without hash
```basic
BATCHWRITE("StructureActiveVent", On, 1)  # Error: needs hash
```

✅ **Correct**: Calculate hash first
```basic
DEFINE VENT_HASH -1129453144
BATCHWRITE(VENT_HASH, On, 1)
```

❌ **Wrong**: Writing to read-only property
```basic
sensor.Temperature = 300  # Error: can't set temperature
```

✅ **Correct**: Write to writable property
```basic
heater.Setting = 300  # OK: Setting is writable
```

## Code Generation Reference

### Device Declaration
| Node | Generated Code |
|------|----------------|
| PinDeviceNode(d0, "sensor") | `ALIAS sensor d0` |
| NamedDeviceNode("temp", "StructureFurnace") | `DEVICE temp "StructureFurnace"` |
| ThisDeviceNode("chip") | `ALIAS chip db` |

### Property Access
| Node | Generated Code |
|------|----------------|
| ReadPropertyNode(sensor, Temperature) | `sensor.Temperature` |
| WritePropertyNode(heater, On, 1) | `heater.On = 1` |
| SlotReadNode(furnace, 0, Occupied) | `furnace.Slot(0).Occupied` |
| SlotWriteNode(chute, 1, Quantity, 10) | `chute.Slot(1).Quantity = 10` |

### Batch Operations
| Node | Generated Code |
|------|----------------|
| BatchReadNode(hash, Pressure, Average) | `BATCHREAD(hash, Pressure, 0)` |
| BatchWriteNode(hash, Mode, 1) | `BATCHWRITE(hash, Mode, 1)` |

### Stack Operations
| Node | Generated Code |
|------|----------------|
| PushNode(temp) | `PUSH temp` |
| PopNode(result) | `POP result` |
| PeekNode(top) | `PEEK top` |

## Visual Scripting Best Practices

1. **Group related nodes** - Keep device operations together
2. **Use comments** - Document complex logic
3. **Label clearly** - Give nodes meaningful names
4. **Color code** - Use pins colors for visual organization
5. **Test incrementally** - Validate each node before connecting
6. **Think in flow** - Execution flows left-to-right, top-to-bottom
7. **Minimize wire crossing** - Keep graph clean and readable

---

**Quick Links:**
- Full Documentation: [README.md](README.md)
- Implementation Summary: [PHASE_3B_SUMMARY.md](PHASE_3B_SUMMARY.md)
- Example Code: [NodeSystemExample.cs](NodeSystemExample.cs)
