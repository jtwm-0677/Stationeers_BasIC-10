# BASIC-IC10 Code Examples

A collection of ready-to-use example programs for common Stationeers automation tasks. All examples are included in the `examples/` folder and can be loaded directly into the BASIC-IC10 Compiler.

## Loading Examples

**In the BASIC-IC10 Compiler:**
1. Click **File > Open** (or Ctrl+O)
2. Navigate to the `examples/` folder
3. Select any `.bas` file
4. Press **F5** to compile
5. Press **F9** to test in the simulator

**Or use the built-in example loader:**
- Click the **"Load Example"** dropdown in the toolbar
- Select from categorized examples

---

## Example Categories

| # | File | Difficulty | Description |
|---|------|------------|-------------|
| 01 | blink_light.bas | Beginner | Toggle a light on/off |
| 02 | button_toggle.bas | Beginner | Button with edge detection |
| 03 | thermostat.bas | Beginner | Temperature control |
| 04 | pressure_regulator.bas | Intermediate | Room pressure management |
| 05 | oxygen_monitor.bas | Intermediate | O2 alarm with color coding |
| 06 | solar_tracker.bas | Beginner | Automatic solar positioning |
| 07 | battery_backup.bas | Intermediate | Generator backup system |
| 08 | airlock.bas | Advanced | Full airlock state machine |
| 09 | furnace_controller.bas | Intermediate | Smelting temperature control |
| 10 | greenhouse.bas | Advanced | Multi-system plant control |
| 11 | batch_solar_array.bas | Advanced | Batch device operations |
| 12 | base_status_monitor.bas | Advanced | Comprehensive monitoring |
| 13 | math_demo.bas | Reference | Math function examples |
| 14 | dial_pump_control.bas | Beginner | Analog dial input |
| 15 | item_sorter.bas | Intermediate | Hash-based sorting |

---

## Beginner Examples

### 01: Blink a Light

The simplest IC10 program - toggles a light on and off every second.

**Devices:** d0 = Wall Light

```basic
ALIAS light d0

main:
    light.On = 1
    SLEEP 1
    light.On = 0
    SLEEP 1
    GOTO main
END
```

**Compiled IC10:**
```
alias light d0
main:
s light On 1
sleep 1
s light On 0
sleep 1
j main
```

---

### 02: Button Toggle

Toggle a device on/off with each button press. Demonstrates edge detection.

**Devices:** d0 = Logic Button, d1 = Device to control

**Key Concept:** Edge detection triggers only on the button press (0→1 transition), not while held.

```basic
ALIAS button d0
ALIAS device d1

VAR lastState = 0
VAR currentState = 0
VAR deviceOn = 0

main:
    currentState = button.Setting

    IF currentState = 1 AND lastState = 0 THEN
        IF deviceOn = 0 THEN
            deviceOn = 1
        ELSE
            deviceOn = 0
        ENDIF
        device.On = deviceOn
    ENDIF

    lastState = currentState
    YIELD
    GOTO main
END
```

---

### 03: Simple Thermostat

Maintains room temperature with hysteresis to prevent rapid on/off cycling.

**Devices:** d0 = Gas Sensor, d1 = Wall Heater, d2 = Wall Cooler

**Key Concept:** Hysteresis uses a tolerance band (±2°) to prevent oscillation.

```basic
ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2

DEFINE TARGET_TEMP 293.15   ' 20°C in Kelvin
DEFINE TOLERANCE 2

VAR temp = 0

main:
    temp = sensor.Temperature

    IF temp < TARGET_TEMP - TOLERANCE THEN
        heater.On = 1
        cooler.On = 0
    ELSEIF temp > TARGET_TEMP + TOLERANCE THEN
        heater.On = 0
        cooler.On = 1
    ELSE
        heater.On = 0
        cooler.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

---

### 06: Solar Panel Tracker

Automatically positions solar panels to track the sun.

**Devices:** d0 = Any Solar Panel (controls all networked panels)

```basic
ALIAS panel d0

VAR solarAngle = 0

main:
    solarAngle = panel.SolarAngle
    panel.Horizontal = solarAngle
    panel.Vertical = 60

    YIELD
    GOTO main
END
```

---

## Intermediate Examples

### 05: Oxygen Monitor with Alarm

Monitors O2 levels and displays status using color-coded light.

**Devices:** d0 = Gas Sensor, d1 = Wall Light, d2 = LED Display

**Color Reference:**
- Red (16711680): Danger - Low O2
- Yellow (16776960): Warning - High O2
- Green (65280): Safe

```basic
ALIAS sensor d0
ALIAS alarm d1
ALIAS display d2

DEFINE MIN_OXYGEN 0.18
DEFINE MAX_OXYGEN 0.23

VAR oxygenRatio = 0
VAR oxygenPercent = 0

main:
    oxygenRatio = sensor.RatioOxygen
    oxygenPercent = oxygenRatio * 100
    display.Setting = oxygenPercent

    IF oxygenRatio < MIN_OXYGEN THEN
        alarm.On = 1
        alarm.Color = 16711680   # Red
    ELSEIF oxygenRatio > MAX_OXYGEN THEN
        alarm.On = 1
        alarm.Color = 16776960   # Yellow
    ELSE
        alarm.On = 1
        alarm.Color = 65280      # Green
    ENDIF

    YIELD
    GOTO main
END
```

---

### 07: Battery Backup System

Manages battery with automatic generator backup using hysteresis.

**Devices:** d0 = Battery, d1 = Generator, d2 = LED Display

```basic
ALIAS battery d0
ALIAS generator d1
ALIAS display d2

DEFINE LOW_CHARGE 0.20
DEFINE HIGH_CHARGE 0.90

VAR charge = 0
VAR genOn = 0

main:
    charge = battery.Charge

    IF charge < LOW_CHARGE THEN
        genOn = 1
    ELSEIF charge > HIGH_CHARGE THEN
        genOn = 0
    ENDIF

    generator.On = genOn
    display.Setting = charge * 100

    YIELD
    GOTO main
END
```

---

## Advanced Examples

### 08: Airlock Controller

Full airlock automation with state machine and safety interlocks.

**Devices:** d0 = Inner Door, d1 = Outer Door, d2 = Pump, d3 = Gas Sensor, d4 = Inner Button, d5 = Outer Button

**State Machine:**
- State 0: Idle (waiting for request)
- State 1: Depressurizing (pumping air out)
- State 2: Pressurizing (pumping air in)

```basic
ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3
ALIAS innerButton d4
ALIAS outerButton d5

DEFINE VACUUM 1
DEFINE PRESSURIZED 90

VAR pressure = 0
VAR state = 0

main:
    pressure = sensor.Pressure

    IF state = 0 THEN
        GOSUB IdleState
    ELSEIF state = 1 THEN
        GOSUB Depressurize
    ELSEIF state = 2 THEN
        GOSUB Pressurize
    ENDIF

    YIELD
    GOTO main

IdleState:
    pump.On = 0
    IF innerButton.Setting = 1 THEN
        outerDoor.Open = 0
        outerDoor.Lock = 1
        state = 2
    ELSEIF outerButton.Setting = 1 THEN
        innerDoor.Open = 0
        innerDoor.Lock = 1
        state = 1
    ENDIF
    RETURN

Depressurize:
    innerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 0
    IF pressure < VACUUM THEN
        pump.On = 0
        outerDoor.Lock = 0
        outerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

Pressurize:
    outerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 1
    IF pressure > PRESSURIZED THEN
        pump.On = 0
        innerDoor.Lock = 0
        innerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

END
```

---

### 11: Batch Solar Array

Control unlimited solar panels using batch operations.

**Devices:** d0 = LED Display (optional)

**Batch Modes:**
- 0 = Average
- 1 = Sum
- 2 = Minimum
- 3 = Maximum

```basic
ALIAS display d0

DEFINE SOLAR_HASH -539224550

VAR solarAngle = 0
VAR totalPower = 0

main:
    solarAngle = BATCHREAD(SOLAR_HASH, SolarAngle, 0)

    BATCHWRITE(SOLAR_HASH, Horizontal, solarAngle)
    BATCHWRITE(SOLAR_HASH, Vertical, 60)

    totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)
    display.Setting = totalPower

    YIELD
    GOTO main
END
```

---

### 12: Base Status Monitor

Comprehensive base monitoring using batch operations.

**Devices:** d0-d3 = LED Displays, d4 = Alarm Light

```basic
ALIAS powerDisp d0
ALIAS o2Disp d1
ALIAS pressDisp d2
ALIAS tempDisp d3
ALIAS alarm d4

DEFINE BATTERY_HASH -1388288459
DEFINE SENSOR_HASH 1255689925

VAR power = 0
VAR oxygen = 0
VAR pressure = 0
VAR temp = 0
VAR alarmState = 0

main:
    power = BATCHREAD(BATTERY_HASH, Charge, 2)
    oxygen = BATCHREAD(SENSOR_HASH, RatioOxygen, 0)
    pressure = BATCHREAD(SENSOR_HASH, Pressure, 0)
    temp = BATCHREAD(SENSOR_HASH, Temperature, 0)

    powerDisp.Setting = power * 100
    o2Disp.Setting = oxygen * 100
    pressDisp.Setting = pressure
    tempDisp.Setting = temp - 273.15

    alarmState = 0
    IF power < 0.2 THEN alarmState = 1
    IF oxygen < 0.18 THEN alarmState = 1
    IF oxygen > 0.25 THEN alarmState = 1
    IF pressure < 80 THEN alarmState = 1
    IF pressure > 120 THEN alarmState = 1

    alarm.On = 1
    IF alarmState = 1 THEN
        alarm.Color = 16711680
    ELSE
        alarm.Color = 65280
    ENDIF

    YIELD
    GOTO main
END
```

---

## Reference: Math Functions

All available math functions:

| Function | Description | Example |
|----------|-------------|---------|
| `ABS(x)` | Absolute value | `ABS(-5)` → 5 |
| `SQRT(x)` | Square root | `SQRT(16)` → 4 |
| `SIN(x)` | Sine (radians) | `SIN(3.14159/2)` → 1 |
| `COS(x)` | Cosine (radians) | `COS(0)` → 1 |
| `TAN(x)` | Tangent (radians) | `TAN(0)` → 0 |
| `ASIN(x)` | Arc sine | `ASIN(1)` → 1.57... |
| `ACOS(x)` | Arc cosine | `ACOS(0)` → 1.57... |
| `ATAN(x)` | Arc tangent | `ATAN(1)` → 0.785... |
| `ATAN2(y,x)` | Arc tangent of y/x | `ATAN2(1,1)` → 0.785... |
| `LOG(x)` | Natural logarithm | `LOG(2.718)` → 1 |
| `EXP(x)` | e^x | `EXP(1)` → 2.718... |
| `CEIL(x)` | Round up | `CEIL(3.2)` → 4 |
| `FLOOR(x)` | Round down | `FLOOR(3.8)` → 3 |
| `ROUND(x)` | Round nearest | `ROUND(3.5)` → 4 |
| `TRUNC(x)` | Truncate | `TRUNC(3.9)` → 3 |
| `MIN(a,b)` | Smaller value | `MIN(5,3)` → 3 |
| `MAX(a,b)` | Larger value | `MAX(5,3)` → 5 |
| `RAND` | Random 0-1 | `RAND` → 0.xxxxx |
| `x ^ y` | Power | `2 ^ 3` → 8 |

---

## Reference: Common Device Properties

### All Devices
- `.On` - Power state (0 or 1)
- `.Setting` - Configurable value
- `.Error` - Error state (0 = OK)

### Gas Sensors
- `.Temperature` - In Kelvin
- `.Pressure` - In kPa
- `.RatioOxygen` - O2 ratio (0-1)
- `.RatioCarbonDioxide` - CO2 ratio (0-1)

### Batteries
- `.Charge` - Charge level (0-1)

### Solar Panels
- `.SolarAngle` - Sun position
- `.Horizontal` - Panel rotation
- `.Vertical` - Panel tilt
- `.PowerGeneration` - Current output

### Doors
- `.Open` - Open state (0/1)
- `.Lock` - Lock state (0/1)

### Pumps/Vents
- `.Mode` - Direction (0=out, 1=in)
- `.Setting` - Target pressure

---

## Tips for Success

1. **Always use YIELD** in loops - required for device values to update
2. **Use hysteresis** to prevent rapid on/off switching
3. **Test in simulator** (F9) before deploying
4. **Watch line count** - IC10 has 128-line limit
5. **Use batch operations** for multiple identical devices
6. **Check .Error property** to detect malfunctions
7. **Document with comments** - they're stripped during compilation
