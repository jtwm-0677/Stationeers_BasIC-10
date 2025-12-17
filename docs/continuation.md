# Stationeers Automation Project - Continuation Document

**Last Updated:** December 15, 2025
**Status:** Smart Airlock System Complete, Weather Warning System Complete

---

## Project Overview

Building automation systems for Stationeers using the Basic-10 compiler. Design philosophy emphasizes:
- **NASA Aesthetic:** Abundant displays, technical look, information overload
- **Safety First:** Prevent CO2/pollutant contamination from Mars atmosphere
- **Modular ICs:** Multiple chips (128-line limit per IC, no Extended Scripting)

---

## Completed Systems

### 1. Smart Airlock System (5 ICs)

A comprehensive airlock automation system with full safety interlocks and maintenance capabilities.

| # | Script Name | Lines | Function |
|---|---|---|---|
| 1 | Smart Airlock Display | 41 | Pressure/temperature displays, status indicators |
| 2 | Smart Airlock Controller | 123 | Main state machine, vent control, door safety |
| 3 | Smart Airlock Timer | 97 | Auto-close countdown, manual override |
| 4 | Smart Airlock Maintenance | 109 | Purge/refill 8-state cycle |
| 5 | Smart Airlock Maint Display | 47 | Step indicators for maintenance |

**Location:** `C:\Users\jtwm0\Documents\My Games\Stationeers\scripts\`

#### Device List - Smart Airlock

**Sensors:**
- "Interior Sensor" - StructureGasSensor (inside base)
- "Airlock Sensor" - StructureGasSensor (in airlock)
- "Exterior Sensor" - StructureGasSensor (outside)
- "Airlock Reserve Tank Monitor" - StructurePipeGasSensor (on reserve tank, target 4000 kPa)

**Doors:**
- "Interior" - StructureCompositeDoor
- "Exterior" - StructureCompositeDoor

**Vents (both inside airlock):**
- "Interior Vent" - StructureActiveVent (connects to base atmosphere)
- "Exterior Vent" - StructureActiveVent (connects to reserve tank via pipe)

**Buttons:**
- "To Interior" - inside airlock
- "To Exterior" - inside airlock
- "Call Interior" - inside base
- "Call Exterior" - outside
- "Purge Refill" - ModularDeviceRoundButton (orange)

**Displays:**
- "Internal Pressure", "Airlock Pressure", "External Pressure" - ModularDeviceLEDdisplay2
- Temperature displays (K and C versions for each zone)
- "AutoClose Countdown" - ModularDeviceLEDdisplay2
- "Maintenance Step" - ModularDeviceLEDdisplay2

**Status Indicators (ModularDeviceLabelDiode2):**
- "PRESSURIZED INTERNAL" (Green)
- "PRESSURIZED EXTERNAL" (Blue)
- "PRESSURIZING" (Orange, blinking)
- "DEPRESSURIZING" (Orange, blinking)
- "AIRLOCK IDLE" (Green)
- "AUTOMATIC CLOSE" (Green)
- "OVERRIDE" (Red, blinking)

**Maintenance Step Indicators (ModularDeviceLabelDiode2, Red when active):**
- "OPEN EXTERIOR"
- "PURGE STORAGE"
- "VACUUM HOLD"
- "CLOSE EXTERIOR"
- "PURGE MARTIAN"
- "OPEN INTERIOR"
- "REFILL TANK"

**Override Switches:**
- "Override Interior", "Override Airlock", "Override Exterior"

**Dial:**
- "AutoClose Setting" - ModularDeviceDialSmall (seconds before auto-close)

#### Key Safety Features

1. **Door Interlock:** One door always locked unless override active
2. **Button Ignore:** Interior buttons ignored if interior door open (prevents contamination)
3. **Vacuum Hold:** 2-second hold before pressurizing (purges Mars atmosphere traces)
4. **Pressure-Based Lock Restore:** After override, locks set based on pressure (>45 kPa = lock exterior)

#### Maintenance Cycle States

| State | Name | Action |
|---|---|---|
| 0 | IDLE | Waiting for "Purge Refill" button |
| 1 | OPEN_EXTERIOR | Opening exterior door (waits for confirmation) |
| 2 | PURGE_BASE_STORAGE | Evacuating airlock to reserve tank |
| 3 | WAIT_VACUUM | 2-second hold at vacuum |
| 4 | CLOSE_EXTERIOR | Closing exterior door |
| 5 | PURGE_MARTIAN | Venting any Mars atmosphere outside |
| 6 | OPEN_INTERIOR | Opening interior door |
| 7 | REFILL | Refilling reserve tank from base (to 4000 kPa) |

---

### 2. Weather Warning System (1 IC)

| Script | Lines | Function |
|---|---|---|
| WeatherWarning System | 24 | Storm countdown, alerts, Klaxon sounds |

**Devices:**
- "Weather Station" - StructureWeatherStation
- "TimeUntilStorm" - ModularDeviceLEDdisplay2 (Mode 7, Color 4)
- "STORM INCOMING" - ModularDeviceLabelDiode2 (Orange)
- "STORM ACTIVE" - ModularDeviceLabelDiode2 (Red, blinking)
- "Warning Speaker" - StructureKlaxon

**Behavior:**
- Mode 1 (incoming): Orange indicator ON, Sound 18, Activate speaker
- Mode 2 (active): Red blinking indicator ON, Sound 16, Activate speaker
- Countdown display shows `NextWeatherEventTime`

**Note:** Klaxon uses `Activate` property to trigger sound, not `On`.

---

## Technical Reference

### LabelDiode2 Colors
| Color | Value |
|---|---|
| Blue | 0 |
| Green | 2 |
| Orange | 3 |
| Red | 4 |

### Active Vent Modes
| Mode | Direction |
|---|---|
| 0 | Outward (push/evacuate) |
| 1 | Inward (pull/fill) |

### Pressure Reference
- Interior (base): ~101 kPa
- Exterior (Mars): ~2 kPa
- Vacuum threshold: <1 kPa
- Reserve tank target: 4000 kPa

### Common Gotchas
1. **Case Sensitivity:** `stormActive` != `StormActive` - aliases are case-sensitive
2. **YIELD Required:** Always include YIELD in loops for device values to update
3. **128-Line Limit:** Split large scripts into multiple ICs
4. **Door State Verification:** Always check door is actually open/closed before running vents
5. **Klaxon Activation:** Use `Activate = 1` to play sound, not just `On = 1`

---

## Pending/Future Work

No pending tasks. Potential future additions:
- Power monitoring system
- Greenhouse automation
- Mining operations controller
- Rocket launch procedures

---

## Session Notes

### Bugs Encountered and Fixed

1. **Door Safety Backwards:** Initial logic allowed buttons when door was open instead of closed. Fixed by checking if TARGET door is CLOSED.

2. **Override Lock Restore:** Both doors got locked when override turned off. Fixed with pressure-based logic.

3. **Maintenance Explosion:** State 1 transitioned immediately without waiting for exterior door to actually open, causing overpressurization. Fixed by adding door state verification.

4. **Case Sensitivity:** `StormActive` vs `stormActive` caused undefined variable in IC10 output.

5. **Klaxon Sound:** Used `On` instead of `Activate` - sound wouldn't play.

---

## File Locations

- **Scripts:** `C:\Users\jtwm0\Documents\My Games\Stationeers\scripts\`
- **Project:** `C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired\`
- **Documentation:** `C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired\docs\`
