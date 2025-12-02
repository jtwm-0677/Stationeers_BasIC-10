# Custom Airlock System Plan

## Overview
3-position airlock system capable of cycling to:
- **Exterior**: Mars atmosphere (2.5kPa)
- **Interior**: Base atmosphere (matched pressure)
- **Sealed**: Pressurized chamber (matched to base pressure)

## Requirements
- Evacuate ALL Mars atmosphere before pressurizing with base atmosphere
- Match base pressure within ±1%
- Automatic door unlock/open when cycle complete
- Occupancy-based lighting
- Use .Average for pressure readings across multiple sensors

---

## Device List

### Atmosphere Control
| Device Type | Placeholder Name | Function |
|-------------|------------------|----------|
| Active Vent | "Airlock Vent Interior" | Base atmosphere in/out, connected to 6 tanks |
| Active Vent | "Airlock Vent Exterior" | Mars atmosphere in/out |
| Digital Valve | "Airlock Overpressure Valve" | Tank protection |
| Pipe Analyzer | "Airlock Tank Pressure" | Monitor storage tanks |

### Doors
| Device Type | Placeholder Name | Function |
|-------------|------------------|----------|
| Door | "Airlock Door Interior" | Base side |
| Door | "Airlock Door Exterior" | Mars side |

### Sensors
| Device Type | Placeholder Name | Function |
|-------------|------------------|----------|
| Gas Sensor(s) | "Airlock Sensor" | Inside airlock (averaged) |
| Gas Sensor(s) | "Base Sensor" | Inside base (averaged) |
| Occupancy Sensor | "Airlock Occupancy" | Detect presence |

### Controls
| Device Type | Placeholder Name | Function |
|-------------|------------------|----------|
| ModularDeviceUtilityButton2x2 | "Cycle Exterior" | Request Mars exit |
| ModularDeviceUtilityButton2x2 | "Cycle Interior" | Request base entry |
| ModularDeviceUtilityButton2x2 | "Cycle Airlock" | Request sealed chamber |

### State Management
| Device Type | Placeholder Name | Function |
|-------------|------------------|----------|
| Logic Memory | "Airlock Cycle State" | Current cycle mode (0=idle, 1=ext, 2=int, 3=sealed) |
| Logic Memory | "Airlock Atmo Ready" | Atmosphere status (0=not ready, 1=ready) |

### Lighting
| Device Type | Placeholder Name | Function |
|-------------|------------------|----------|
| Light(s) | "Airlock Light" | On when occupied |

### IC Housings
| Placeholder Name | Function |
|------------------|----------|
| "Airlock IC Cycle" | Cycle state machine |
| "Airlock IC Atmo" | Atmosphere controller |
| "Airlock IC Door" | Door controller |
| "Airlock IC Light" | Lighting controller |

---

## Chip 1 - Cycle Controller (State Machine)

```basic
# Meta: Author: ThunderDuck
# Airlock Cycle Controller - State Machine
# Reads buttons, manages cycle state

# Buttons
ALIAS btnExterior = IC.Device[ModularDeviceUtilityButton2x2].Name["Cycle Exterior"]
ALIAS btnInterior = IC.Device[ModularDeviceUtilityButton2x2].Name["Cycle Interior"]
ALIAS btnAirlock = IC.Device[ModularDeviceUtilityButton2x2].Name["Cycle Airlock"]

# State memory
ALIAS cycleState = IC.Device[StructureLogicMemory].Name["Airlock Cycle State"]
ALIAS atmoReady = IC.Device[StructureLogicMemory].Name["Airlock Atmo Ready"]

# Cycle states: 0=idle, 1=exterior, 2=interior, 3=sealed

var currentState = 0

Main:
    currentState = cycleState.Setting

    # Only accept new cycle request when idle (state 0) or ready
    IF currentState == 0 THEN
        IF btnExterior.Activate == 1 THEN
            cycleState.Setting = 1
        ELSEIF btnInterior.Activate == 1 THEN
            cycleState.Setting = 2
        ELSEIF btnAirlock.Activate == 1 THEN
            cycleState.Setting = 3
        ENDIF
    ENDIF

    # Reset to idle when atmosphere ready and door cycle complete
    # (Door controller will set atmoReady back to 0 after opening door)

    YIELD
    GOTO Main
END
```

---

## Chip 2 - Atmosphere Controller

```basic
# Meta: Author: ThunderDuck
# Airlock Atmosphere Controller
# Handles evacuation and pressurization

# Vents
ALIAS ventInterior = IC.Device[StructureActiveVent].Name["Airlock Vent Interior"]
ALIAS ventExterior = IC.Device[StructureActiveVent].Name["Airlock Vent Exterior"]
ALIAS overpressureValve = IC.Device[StructureDigitalValve].Name["Airlock Overpressure Valve"]

# Sensors
ALIAS airlockSensors = IC.Device[StructureGasSensor].Name["Airlock Sensor"]
ALIAS baseSensors = IC.Device[StructureGasSensor].Name["Base Sensor"]

# State memory
ALIAS cycleState = IC.Device[StructureLogicMemory].Name["Airlock Cycle State"]
ALIAS atmoReady = IC.Device[StructureLogicMemory].Name["Airlock Atmo Ready"]

# Vent modes
const MODE_OFF = 0
const MODE_INWARD = 1
const MODE_OUTWARD = 2

# Pressure targets
const MARS_PRESSURE = 2.5
const VACUUM_THRESHOLD = 0.1

# Variables
var currentCycle = 0
var airlockPressure = 0
var basePressure = 0
var targetPressure = 0
var pressureTolerance = 0

Main:
    currentCycle = cycleState.Setting
    airlockPressure = airlockSensors.Pressure.Average
    basePressure = baseSensors.Pressure.Average
    pressureTolerance = basePressure * 0.01  # ±1%

    IF currentCycle == 0 THEN
        # Idle - all vents off
        ventInterior.On = 0
        ventExterior.On = 0
        atmoReady.Setting = 0

    ELSEIF currentCycle == 1 THEN
        # Exterior cycle: evacuate completely, then fill with 2.5kPa Mars
        IF airlockPressure > VACUUM_THRESHOLD THEN
            # Phase 1: Evacuate via interior vent (save atmosphere)
            ventInterior.Mode = MODE_OUTWARD
            ventInterior.On = 1
            ventExterior.On = 0
            atmoReady.Setting = 0
        ELSEIF airlockPressure < MARS_PRESSURE THEN
            # Phase 2: Fill with Mars atmosphere
            ventInterior.On = 0
            ventExterior.Mode = MODE_INWARD
            ventExterior.On = 1
            atmoReady.Setting = 0
        ELSE
            # Ready
            ventInterior.On = 0
            ventExterior.On = 0
            atmoReady.Setting = 1
        ENDIF

    ELSEIF currentCycle == 2 OR currentCycle == 3 THEN
        # Interior or Sealed cycle: evacuate completely, then match base pressure
        targetPressure = basePressure

        IF airlockPressure > VACUUM_THRESHOLD THEN
            # Phase 1: Evacuate via exterior vent (vent to Mars)
            ventExterior.Mode = MODE_OUTWARD
            ventExterior.On = 1
            ventInterior.On = 0
            atmoReady.Setting = 0
        ELSEIF airlockPressure < (targetPressure - pressureTolerance) THEN
            # Phase 2: Pressurize from interior tanks
            ventExterior.On = 0
            ventInterior.Mode = MODE_INWARD
            ventInterior.On = 1
            atmoReady.Setting = 0
        ELSEIF airlockPressure > (targetPressure + pressureTolerance) THEN
            # Over pressure - vent a little
            ventInterior.Mode = MODE_OUTWARD
            ventInterior.On = 1
            ventExterior.On = 0
            atmoReady.Setting = 0
        ELSE
            # Ready - within tolerance
            ventInterior.On = 0
            ventExterior.On = 0
            atmoReady.Setting = 1
        ENDIF
    ENDIF

    YIELD
    GOTO Main
END
```

---

## Chip 3 - Door Controller

```basic
# Meta: Author: ThunderDuck
# Airlock Door Controller
# Manages door states with safety lockouts

# Doors
ALIAS doorInterior = IC.Device[StructureCompositeDoor].Name["Airlock Door Interior"]
ALIAS doorExterior = IC.Device[StructureCompositeDoor].Name["Airlock Door Exterior"]

# State memory
ALIAS cycleState = IC.Device[StructureLogicMemory].Name["Airlock Cycle State"]
ALIAS atmoReady = IC.Device[StructureLogicMemory].Name["Airlock Atmo Ready"]

# Variables
var currentCycle = 0
var ready = 0

Main:
    currentCycle = cycleState.Setting
    ready = atmoReady.Setting

    IF currentCycle == 0 THEN
        # Idle - both doors locked closed
        doorInterior.Open = 0
        doorExterior.Open = 0

    ELSEIF currentCycle == 1 THEN
        # Exterior cycle
        doorInterior.Open = 0  # Always keep interior closed
        IF ready == 1 THEN
            doorExterior.Open = 1  # Open exterior when ready
        ELSE
            doorExterior.Open = 0
        ENDIF

    ELSEIF currentCycle == 2 THEN
        # Interior cycle
        doorExterior.Open = 0  # Always keep exterior closed
        IF ready == 1 THEN
            doorInterior.Open = 1  # Open interior when ready
        ELSE
            doorInterior.Open = 0
        ENDIF

    ELSEIF currentCycle == 3 THEN
        # Sealed cycle - both doors closed
        doorInterior.Open = 0
        doorExterior.Open = 0
        # Could add indicator light here when ready
    ENDIF

    YIELD
    GOTO Main
END
```

---

## Chip 4 - Lighting Controller

```basic
# Meta: Author: ThunderDuck
# Airlock Lighting Controller
# Simple occupancy-based lighting

# Sensors
ALIAS occupancy = IC.Device[StructureOccupancySensor].Name["Airlock Occupancy"]

# Lights
ALIAS lights = IC.Device[StructureLight].Name["Airlock Light"]

# Variables
var occupied = 0

Main:
    occupied = occupancy.Activate
    lights.On = occupied

    YIELD
    GOTO Main
END
```

---

## TODO
- [ ] Get actual device names from user
- [ ] Update device types (door type, light type, etc.)
- [ ] Test each chip individually
- [ ] Add reset/cancel functionality
- [ ] Add status displays
- [ ] Add indicator lights for cycle states
