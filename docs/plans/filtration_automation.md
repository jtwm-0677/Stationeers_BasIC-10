# Filtration Automation System Plan

## Overview
Comprehensive automation system for a 6-stage gas filtration loop with safety controls, pressure management, and liquid detection.

### Gas Processing Flow
```
Main Base Atmosphere
    ↓
Pollutants Scrubber → Pollutants Tank
    ↓
CO2 Scrubber → CO2 Tank
    ↓
Nitrogen Scrubber → Nitrogen Tank
    ↓
Volatiles Scrubber → Volatiles Tank
    ↓
Oxygen Scrubber → Oxygen Tank
    ↓
Nitrous Oxide Scrubber → NoX Tank
    ↓
(Unfiltered recirculates back to filtration loop)

Nitrogen Tank + Oxygen Tank → Gas Mixer (70/30) → "Breathable" → Base
```

---

## Zone 2: Recirculation Loop

### Devices
| Device | Name | Function |
|--------|------|----------|
| Turbo Volume Pump (x2) | "Filtration Supply" | Pressurize recirculation loop |
| Pipe Analyzer | "Filtration Loop Sensor" | Monitor loop pressure |

### Pressure Control Logic
- **Target Pressure**: 25-28 MPa
- **Maximum Pressure**: 40 MPa (hard cutoff)
- **Volume Scaling**: Lower pressure = higher pump volume

```
IF pressure >= 40 MPa THEN
    Pumps OFF (safety cutoff)
ELSEIF pressure < 25 MPa THEN
    Pumps ON, high volume
ELSEIF pressure > 28 MPa THEN
    Pumps ON, low volume
ELSE
    Pumps ON, maintain volume
ENDIF
```

---

## Emergency Overpressure System

### Trigger Conditions
- ANY "Filtration Loop Sensor" reads >= 40 MPa

### Emergency Actions
1. ALL filtration systems OFF
2. ALL pumps OFF
3. Alarm sounds
4. All LED lights turn RED
5. "SYSTEM ALARM" label diodes flash RED on ALL panels

### Devices Needed
| Device | Name | Function |
|--------|------|----------|
| Alarm/Speaker/Siren | TBD | Sound alarm |
| Label Diode (multiple) | "SYSTEM ALARM" | Flash red on emergency |

---

## Smart Filtration Control

### Gas Ratio Monitoring Sources
1. Filtration system input ratios
2. Base sensors:
   - "Interior Gas Sensor"
   - "Doc Gas Sensor"
   - "Duck Gas Sensor"
   - "MFG Gas Sensor"
3. Pipe analyzers throughout system

### Filtration Auto-On Rules

| Gas | Turn ON Condition |
|-----|-------------------|
| Pollutants | If detected ANYWHERE (base sensor, pipe, or filtration input) - immediate ON |
| Volatiles | If detected ANYWHERE (base sensor, pipe, or filtration input) - immediate ON |
| CO2 | If ratio > TBD% |
| Nitrogen | If ratio > TBD% (goal is 70%) |
| Oxygen | If ratio > TBD% (goal is 30%) |
| NoX | If ratio > TBD% |

### Filtration Auto-Off Rules
- Turn off when input gas ratio falls below safe threshold
- Turn off when specific tank pressure too high
- Saves energy and physical filters

---

## Tank Monitoring

### Tank Names (TBD - confirm exact names)
| Tank | Name |
|------|------|
| Pollutants | "Pollutants Tank" ? |
| CO2 | "CO2 Tank" ? |
| Nitrogen | "Nitrogen Tank" ? |
| Volatiles | "Volatiles Tank" ? |
| Oxygen | "Oxygen Tank" ? |
| NoX | "NoX Tank" ? |
| Mixed (70/30) | "Breathable" ? |

### Tank Protection Logic
- Monitor each tank's pressure via built-in logic
- If tank pressure exceeds threshold, stop corresponding filtration
- Prevent tank/pipe explosions

---

## Liquid Detection System

### Monitoring
- ALL pipe analyzers throughout system
- Check for liquid presence (RatioLiquid* properties)

### Alert Actions
1. Illuminate label diode
2. Flash diode
3. Sound alarm
4. (Future: trigger pipe heating or liquid drain)

### Devices Needed
| Device | Name | Function |
|--------|------|----------|
| Label Diode (per location) | TBD | Flash on liquid detection |
| Alarm/Speaker | TBD | Sound alert |

---

## IC Housing Plan

| IC # | Function | Priority |
|------|----------|----------|
| 1 | Emergency Controller - Overpressure detection, master shutoff, alarms | HIGH |
| 2 | Zone 2 Pump Controller - Pressure-based pump volume | HIGH |
| 3 | Smart Filtration Controller - Gas ratio auto on/off | MEDIUM |
| 4 | Tank Pressure Monitor - Overpressure protection per tank | MEDIUM |
| 5 | Liquid Detection Controller - Monitor pipes, trigger alerts | MEDIUM |

---

## Questions to Answer Before Building

1. **Alarm device** - What type? What name?
2. **Tank names** - Confirm exact names for all tanks
3. **Gas ratio thresholds** - What % triggers each filtration on/off?
4. **How many "SYSTEM ALARM" lights?** - Locations/count
5. **Liquid detection locations** - Which pipe analyzers to monitor?
6. **Pipe heating devices** - Do you have these? Names?
7. **Liquid drain valves** - Do you have these? Names?

---

## Existing ICs (Already Built)

| IC | Function | Status |
|----|----------|--------|
| Temperature Controller | 12 temp displays | Complete |
| Pressure Controller | 12 pressure displays | Complete |
| Power Controller | 6 switches + 6 lights | Complete |
| Gas Ratios A | 36 sliders (Pollutants, CO2, Nitrogen) | Complete |
| Gas Ratios B | 36 sliders (Volatiles, Oxygen, NoX) | Complete |

---

## Device Naming Convention

All devices follow pattern: `"[Gas/System] [Function]"`

Examples:
- "Pollutants Scrubber"
- "Filtration Supply"
- "Filtration Loop Sensor"
- "SYSTEM ALARM"
- "Interior Gas Sensor"
