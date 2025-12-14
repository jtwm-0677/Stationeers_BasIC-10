# Clock/Calendar System Setup Guide

## Overview

Planet-agnostic timekeeping system using inflection point detection for sunrise.

| Script | Lines | Mode | Purpose |
|--------|-------|------|---------|
| Master Clock v2 | 84 | Standard | Sunrise detection, tick/day counting |
| Calendar v2 | 135 | Extended | Date calculation, elapsed time |
| Day_Progress v2 | 38 | Standard | Day/night percentages |
| Time Display | existing | Standard | 12-hour clock display |

---

## Master Clock v2

### Devices

**Sensor:**
| Type | Name |
|------|------|
| StructureDaylightSensor | `Daylight Sensor` |

**Memory Chips (StructureLogicMemory):**
| Name | Purpose |
|------|---------|
| `Day Count Memory` | Total days elapsed |
| `Tick Memory` | Current tick (0-2399) |
| `State Memory` | Internal: 0=FALLING, 1=RISING |
| `Reference Memory` | Internal: angle tracking |
| `Init Memory` | Internal: 0=needs init, 1=ready |

---

## Calendar v2

**Requires Extended Scripting Mode (135 lines)**

### Input Memory (from Master Clock v2)
| Name |
|------|
| `Day Count Memory` |

### Output Memory Chips (StructureLogicMemory)
| Name | Value |
|------|-------|
| `Day of Month` | 1-31 |
| `Month Memory` | 1-12 |
| `Year Memory` | 2025+ |
| `Weekday Memory` | 0-6 (Thu=0, Fri=1... Wed=6) |
| `Weeks Elapsed Memory` | Total weeks (days/7) |
| `Months Elapsed Memory` | Total months (days/30) |
| `Years Elapsed Memory` | Total years (days/365) |

### Displays
| Type | Name | Shows |
|------|------|-------|
| ModularDeviceLEDdisplay2 | `Day Display` | Day of month |
| ModularDeviceLEDdisplay2 | `Month Display` | Month number |
| ModularDeviceLEDdisplay3 | `Year Display` | Year |
| ModularDeviceLEDdisplay3 | `Month Name` | "Jan", "Feb", etc. |
| ModularDeviceLEDdisplay3 | `Weekday Name` | "Thu", "Fri", etc. |
| ModularDeviceLEDdisplay3 | `Days Elapsed` | Total days survived |

---

## Day_Progress v2

### Input Memory (from Master Clock v2)
| Name |
|------|
| `Tick Memory` |

### Output Memory Chips (StructureLogicMemory)
| Name | Value |
|------|-------|
| `Day Progress Memory` | 0-100% through daytime |
| `Night Progress Memory` | 0-100% through nighttime |

---

## Time Display (Existing)

### Input Memory (from Master Clock v2)
| Name |
|------|
| `Tick Memory` |

### Displays
| Type | Name | Shows |
|------|------|-------|
| ModularDeviceLEDdisplay2 | `Hour Display` | Hour (1-12) |
| ModularDeviceLEDdisplay2 | `Minute Display` | Minutes (0-59) |
| ModularDeviceLEDdisplay2 | `AMPM Display` | "AM" or "PM" |

---

## Complete Device Summary

### Memory Chips: 14 total
| From Script | Chips |
|-------------|-------|
| Master Clock v2 | 5 |
| Calendar v2 | 7 (+ reads Day Count Memory) |
| Day_Progress v2 | 2 (+ reads Tick Memory) |

### Displays: 9 total
| Type | Count |
|------|-------|
| ModularDeviceLEDdisplay2 | 5 |
| ModularDeviceLEDdisplay3 | 4 |

### Other
- 1x StructureDaylightSensor

---

## Tick Reference

| Tick | Time | Event |
|------|------|-------|
| 0 | 12:00 AM | Midnight |
| 600 | 6:00 AM | Sunrise (day reset) |
| 1200 | 12:00 PM | Solar noon |
| 1800 | 6:00 PM | Sunset |
| 2400 | - | Wraps to 0 |

1 tick = 0.5 real seconds | 2400 ticks = 20 real minutes = 1 game day
