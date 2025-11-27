# BASIC-IC10 Code Examples

A collection of ready-to-use example programs for common Stationeers automation tasks.

## Table of Contents
1. [Basic Examples](#basic-examples)
2. [Atmospheric Control](#atmospheric-control)
3. [Power Management](#power-management)
4. [Manufacturing Automation](#manufacturing-automation)
5. [Advanced Systems](#advanced-systems)

---

## Basic Examples

### 1. Blink a Light

The simplest program - toggles a light on and off.

```basic
' Blink a Light
' Connect a wall light to d0

ALIAS light d0

main:
    light.On = 1
    SLEEP 1
    light.On = 0
    SLEEP 1
    GOTO main
END
```

### 2. Button Toggle

Toggle a device on/off with a button press.

```basic
' Button Toggle
' d0 = Logic Button
' d1 = Device to toggle

ALIAS button d0
ALIAS device d1

VAR lastState = 0
VAR currentState = 0
VAR deviceOn = 0

main:
    currentState = button.Setting

    ' Detect button press (rising edge)
    IF currentState = 1 AND lastState = 0 THEN
        ' Toggle device
        deviceOn = NOT deviceOn
        device.On = deviceOn
    ENDIF

    lastState = currentState
    YIELD
    GOTO main
END
```

### 3. Dial-Controlled Setting

Use a dial to control a device's setting.

```basic
' Dial-Controlled Pump
' d0 = Dial (0-100)
' d1 = Volume Pump

ALIAS dial d0
ALIAS pump d1

VAR targetPressure = 0

main:
    ' Map dial (0-100) to pressure (0-1000 kPa)
    targetPressure = dial.Setting * 10
    pump.Setting = targetPressure

    YIELD
    GOTO main
END
```

### 4. LED Display Counter

Display a counting value on an LED display.

```basic
' LED Counter
' d0 = LED Display

ALIAS display d0

VAR counter = 0

main:
    display.Setting = counter
    counter = counter + 1

    IF counter > 999 THEN
        counter = 0
    ENDIF

    SLEEP 1
    GOTO main
END
```

---

## Atmospheric Control

### 5. Simple Thermostat

Maintains temperature within a range.

```basic
' Simple Thermostat
' d0 = Gas Sensor
' d1 = Wall Heater
' d2 = Wall Cooler

ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2

DEFINE TARGET_TEMP 293.15    ' 20°C in Kelvin
DEFINE TOLERANCE 2           ' ±2 degrees

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

### 6. Pressure Regulator

Maintains pressure in a room.

```basic
' Pressure Regulator
' d0 = Gas Sensor (room)
' d1 = Active Vent (intake)
' d2 = Active Vent (exhaust)

ALIAS sensor d0
ALIAS intake d1
ALIAS exhaust d2

DEFINE TARGET_PRESSURE 101.325  ' 1 atm
DEFINE LOW_THRESHOLD 90
DEFINE HIGH_THRESHOLD 110

VAR pressure = 0

main:
    pressure = sensor.Pressure

    IF pressure < LOW_THRESHOLD THEN
        ' Low pressure - bring in air
        intake.On = 1
        intake.Open = 1
        intake.Mode = 1      ' Inward
        exhaust.On = 0
    ELSEIF pressure > HIGH_THRESHOLD THEN
        ' High pressure - vent air
        intake.On = 0
        exhaust.On = 1
        exhaust.Open = 1
        exhaust.Mode = 0     ' Outward
    ELSE
        ' Pressure OK - idle
        intake.On = 0
        exhaust.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

### 7. Oxygen Monitor with Alarm

Monitors O2 levels and triggers alarm.

```basic
' Oxygen Monitor
' d0 = Gas Sensor
' d1 = Wall Light (alarm indicator)
' d2 = Console (display)

ALIAS sensor d0
ALIAS alarm d1
ALIAS display d2

DEFINE MIN_OXYGEN 0.16       ' 16% minimum safe O2
DEFINE MAX_OXYGEN 0.25       ' 25% maximum safe O2

VAR oxygenRatio = 0
VAR oxygenPercent = 0

main:
    oxygenRatio = sensor.RatioOxygen
    oxygenPercent = oxygenRatio * 100

    display.Setting = oxygenPercent

    IF oxygenRatio < MIN_OXYGEN THEN
        ' Low oxygen alarm
        alarm.On = 1
        alarm.Color = 16711680   ' Red
    ELSEIF oxygenRatio > MAX_OXYGEN THEN
        ' High oxygen warning
        alarm.On = 1
        alarm.Color = 16776960   ' Yellow
    ELSE
        ' Normal - green
        alarm.On = 1
        alarm.Color = 65280      ' Green
    ENDIF

    YIELD
    GOTO main
END
```

### 8. Advanced Atmosphere Controller

Full atmospheric control with temperature, pressure, and O2.

```basic
' Advanced Atmosphere Controller
' d0 = Room Gas Sensor
' d1 = Wall Heater
' d2 = Wall Cooler
' d3 = Intake Vent
' d4 = Exhaust Vent
' d5 = Console Display

ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2
ALIAS intake d3
ALIAS exhaust d4
ALIAS display d5

' Temperature settings (Kelvin)
DEFINE TEMP_TARGET 293.15
DEFINE TEMP_TOLERANCE 3

' Pressure settings (kPa)
DEFINE PRESS_TARGET 101
DEFINE PRESS_TOLERANCE 10

' Oxygen settings (ratio)
DEFINE O2_MIN 0.18
DEFINE O2_MAX 0.23

VAR temp = 0
VAR pressure = 0
VAR oxygen = 0
VAR status = 0

main:
    GOSUB ReadSensors
    GOSUB ControlTemperature
    GOSUB ControlPressure
    GOSUB UpdateDisplay

    YIELD
    GOTO main

ReadSensors:
    temp = sensor.Temperature
    pressure = sensor.Pressure
    oxygen = sensor.RatioOxygen
    RETURN

ControlTemperature:
    IF temp < TEMP_TARGET - TEMP_TOLERANCE THEN
        heater.On = 1
        cooler.On = 0
    ELSEIF temp > TEMP_TARGET + TEMP_TOLERANCE THEN
        heater.On = 0
        cooler.On = 1
    ELSE
        heater.On = 0
        cooler.On = 0
    ENDIF
    RETURN

ControlPressure:
    ' Also considers oxygen level
    IF pressure < PRESS_TARGET - PRESS_TOLERANCE THEN
        intake.On = 1
        intake.Mode = 1
        exhaust.On = 0
    ELSEIF pressure > PRESS_TARGET + PRESS_TOLERANCE THEN
        intake.On = 0
        exhaust.On = 1
        exhaust.Mode = 0
    ELSEIF oxygen < O2_MIN THEN
        ' Low O2 - need fresh air
        intake.On = 1
        intake.Mode = 1
        exhaust.On = 1
        exhaust.Mode = 0
    ELSE
        intake.On = 0
        exhaust.On = 0
    ENDIF
    RETURN

UpdateDisplay:
    ' Cycle through displays
    status = status + 1
    IF status > 2 THEN status = 0

    IF status = 0 THEN
        display.Setting = temp - 273.15     ' Celsius
    ELSEIF status = 1 THEN
        display.Setting = pressure
    ELSE
        display.Setting = oxygen * 100      ' Percent
    ENDIF
    RETURN

END
```

---

## Power Management

### 9. Solar Panel Tracker

Tracks the sun for optimal power generation.

```basic
' Solar Panel Tracker
' d0 = Solar Panel (one panel controls all)

ALIAS panel d0

VAR solarAngle = 0

main:
    solarAngle = panel.SolarAngle

    ' Set horizontal to track sun
    panel.Horizontal = solarAngle

    ' Set vertical for optimal angle (adjust for your latitude)
    panel.Vertical = 60

    YIELD
    GOTO main
END
```

### 10. Battery Monitor with Generator Backup

Manages battery charge with backup generator.

```basic
' Battery Backup System
' d0 = Battery (Large)
' d1 = Solid Fuel Generator
' d2 = Console Display

ALIAS battery d0
ALIAS generator d1
ALIAS display d2

DEFINE LOW_CHARGE 0.20      ' Start generator below 20%
DEFINE HIGH_CHARGE 0.90     ' Stop generator above 90%

VAR charge = 0
VAR genOn = 0

main:
    charge = battery.Charge

    ' Hysteresis control
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

### 11. Load Shedding Controller

Disconnects non-essential loads when power is low.

```basic
' Load Shedding Controller
' d0 = APC (monitors power)
' d1 = Low priority circuit
' d2 = Medium priority circuit
' d3 = High priority (always on)

ALIAS apc d0
ALIAS lowPriority d1
ALIAS medPriority d2
ALIAS highPriority d3

DEFINE CRITICAL_POWER 500
DEFINE LOW_POWER 1000

VAR available = 0

main:
    available = apc.PowerPotential - apc.PowerActual

    IF available < CRITICAL_POWER THEN
        ' Critical - only essential
        lowPriority.On = 0
        medPriority.On = 0
    ELSEIF available < LOW_POWER THEN
        ' Low - reduce load
        lowPriority.On = 0
        medPriority.On = 1
    ELSE
        ' Normal - all on
        lowPriority.On = 1
        medPriority.On = 1
    ENDIF

    ' High priority always on
    highPriority.On = 1

    YIELD
    GOTO main
END
```

### 12. Smart Solar Array (Batch Operations)

Controls all solar panels using batch operations.

```basic
' Smart Solar Array Controller
' Uses batch operations - no direct device connections needed

DEFINE SOLAR_HASH -539224550

VAR solarAngle = 0
VAR totalPower = 0
VAR panelCount = 0

main:
    ' Get solar angle from any panel
    solarAngle = BATCHREAD(SOLAR_HASH, SolarAngle, 0)

    ' Set all panels to track sun
    BATCHWRITE(SOLAR_HASH, Horizontal, solarAngle)
    BATCHWRITE(SOLAR_HASH, Vertical, 60)

    ' Monitor total power
    totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)

    YIELD
    GOTO main
END
```

---

## Manufacturing Automation

### 13. Furnace Temperature Controller

Maintains furnace at optimal temperature.

```basic
' Furnace Controller
' d0 = Furnace
' d1 = Console (display)

ALIAS furnace d0
ALIAS display d1

DEFINE TARGET_TEMP 800      ' 800K for smelting

VAR temp = 0

main:
    temp = furnace.Temperature

    ' Turn on if below target
    IF temp < TARGET_TEMP THEN
        furnace.On = 1
    ELSE
        furnace.On = 0
    ENDIF

    display.Setting = temp

    YIELD
    GOTO main
END
```

### 14. Autolathe Queue Monitor

Monitors autolathe production.

```basic
' Autolathe Monitor
' d0 = Autolathe
' d1 = LED Display (completions)
' d2 = Wall Light (status)

ALIAS lathe d0
ALIAS counter d1
ALIAS status d2

VAR completions = 0
VAR isWorking = 0

main:
    completions = lathe.Completions
    isWorking = lathe.On

    counter.Setting = completions

    IF lathe.Error > 0 THEN
        ' Error state - red
        status.On = 1
        status.Color = 16711680
    ELSEIF isWorking > 0 THEN
        ' Working - blue
        status.On = 1
        status.Color = 255
    ELSE
        ' Idle - green
        status.On = 1
        status.Color = 65280
    ENDIF

    YIELD
    GOTO main
END
```

### 15. Sorter Controller

Controls sorting of items based on type.

```basic
' Item Sorter
' d0 = Sorter
' d1 = Stacker (output)

ALIAS sorter d0
ALIAS output d1

' Define item hashes to sort
DEFINE IRON_HASH 226410516
DEFINE COPPER_HASH -707307845
DEFINE GOLD_HASH -929742000

VAR itemHash = 0

main:
    ' Check what's in the sorter
    itemHash = sorter[0].OccupantHash

    IF itemHash = IRON_HASH THEN
        sorter.Mode = 1     ' Output to slot 1
    ELSEIF itemHash = COPPER_HASH THEN
        sorter.Mode = 2     ' Output to slot 2
    ELSEIF itemHash = GOLD_HASH THEN
        sorter.Mode = 3     ' Output to slot 3
    ELSE
        sorter.Mode = 0     ' Default output
    ENDIF

    YIELD
    GOTO main
END
```

---

## Advanced Systems

### 16. Airlock Controller

Full airlock automation with safety interlocks.

```basic
' Airlock Controller
' d0 = Inner Door
' d1 = Outer Door
' d2 = Pump
' d3 = Airlock Gas Sensor
' d4 = Inner Button
' d5 = Outer Button

ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3
ALIAS innerButton d4
ALIAS outerButton d5

DEFINE VACUUM 1
DEFINE PRESSURIZED 90

VAR pressure = 0
VAR state = 0       ' 0=idle, 1=depressurizing, 2=pressurizing
VAR innerRequest = 0
VAR outerRequest = 0

main:
    pressure = sensor.Pressure
    innerRequest = innerButton.Setting
    outerRequest = outerButton.Setting

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

    IF innerRequest = 1 THEN
        ' Want to enter - pressurize
        outerDoor.Open = 0
        outerDoor.Lock = 1
        state = 2
    ELSEIF outerRequest = 1 THEN
        ' Want to exit - depressurize
        innerDoor.Open = 0
        innerDoor.Lock = 1
        state = 1
    ENDIF
    RETURN

Depressurize:
    innerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 0       ' Outward

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
    pump.Mode = 1       ' Inward

    IF pressure > PRESSURIZED THEN
        pump.On = 0
        innerDoor.Lock = 0
        innerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

END
```

### 17. Greenhouse Controller

Automated greenhouse with light, water, and atmosphere.

```basic
' Greenhouse Controller
' d0 = Hydroponics Tray
' d1 = Grow Light
' d2 = Gas Sensor
' d3 = Wall Heater
' d4 = Volume Pump (CO2)
' d5 = Console Display

ALIAS tray d0
ALIAS light d1
ALIAS sensor d2
ALIAS heater d3
ALIAS co2pump d4
ALIAS display d5

DEFINE OPTIMAL_TEMP 303.15      ' 30°C
DEFINE TEMP_TOLERANCE 5
DEFINE MIN_CO2 0.02             ' 2% CO2

VAR temp = 0
VAR co2 = 0
VAR growth = 0
VAR mature = 0

main:
    temp = sensor.Temperature
    co2 = sensor.RatioCarbonDioxide
    growth = tray.Growth
    mature = tray.Mature

    GOSUB ControlLight
    GOSUB ControlTemperature
    GOSUB ControlCO2
    GOSUB UpdateDisplay

    YIELD
    GOTO main

ControlLight:
    ' Keep light on during growth
    IF mature = 0 THEN
        light.On = 1
    ELSE
        light.On = 0
    ENDIF
    RETURN

ControlTemperature:
    IF temp < OPTIMAL_TEMP - TEMP_TOLERANCE THEN
        heater.On = 1
    ELSEIF temp > OPTIMAL_TEMP + TEMP_TOLERANCE THEN
        heater.On = 0
    ENDIF
    RETURN

ControlCO2:
    IF co2 < MIN_CO2 THEN
        co2pump.On = 1
    ELSE
        co2pump.On = 0
    ENDIF
    RETURN

UpdateDisplay:
    display.Setting = growth * 100
    RETURN

END
```

### 18. Mining Drill Controller

Automates mining drill operation.

```basic
' Mining Drill Controller
' d0 = Mining Drill
' d1 = Storage Crate
' d2 = Console Display

ALIAS drill d0
ALIAS storage d1
ALIAS display d2

DEFINE MAX_STORAGE_RATIO 0.9

VAR storageRatio = 0
VAR drilling = 0

main:
    storageRatio = storage.Ratio
    display.Setting = storageRatio * 100

    ' Stop drilling if storage is full
    IF storageRatio > MAX_STORAGE_RATIO THEN
        drill.On = 0
        drilling = 0
    ELSE
        drill.On = 1
        drilling = 1
    ENDIF

    YIELD
    GOTO main
END
```

### 19. Multi-Zone Temperature System

Controls multiple heating zones.

```basic
' Multi-Zone Temperature System
' Uses batch operations for scalability

DEFINE SENSOR_HASH 1255689925    ' Gas Sensor hash
DEFINE HEATER_HASH -1253014094   ' Wall Heater hash
DEFINE COOLER_HASH 1621028804    ' Wall Cooler hash

DEFINE TARGET_TEMP 293.15
DEFINE TOLERANCE 3

VAR avgTemp = 0
VAR minTemp = 0
VAR maxTemp = 0

main:
    ' Get temperature stats from all sensors
    avgTemp = BATCHREAD(SENSOR_HASH, Temperature, 0)
    minTemp = BATCHREAD(SENSOR_HASH, Temperature, 2)
    maxTemp = BATCHREAD(SENSOR_HASH, Temperature, 3)

    ' Control all heaters
    IF avgTemp < TARGET_TEMP - TOLERANCE THEN
        BATCHWRITE(HEATER_HASH, On, 1)
        BATCHWRITE(COOLER_HASH, On, 0)
    ELSEIF avgTemp > TARGET_TEMP + TOLERANCE THEN
        BATCHWRITE(HEATER_HASH, On, 0)
        BATCHWRITE(COOLER_HASH, On, 1)
    ELSE
        BATCHWRITE(HEATER_HASH, On, 0)
        BATCHWRITE(COOLER_HASH, On, 0)
    ENDIF

    YIELD
    GOTO main
END
```

### 20. Complete Base Status Monitor

Monitors all critical base systems.

```basic
' Base Status Monitor
' d0 = Console (main display)
' d1 = LED Display (power)
' d2 = LED Display (O2)
' d3 = LED Display (pressure)
' d4 = LED Display (temp)
' d5 = Alarm Light

ALIAS mainDisplay d0
ALIAS powerDisplay d1
ALIAS o2Display d2
ALIAS pressDisplay d3
ALIAS tempDisplay d4
ALIAS alarm d5

' Device hashes
DEFINE BATTERY_HASH -1388288459
DEFINE SENSOR_HASH 1255689925

' Thresholds
DEFINE POWER_LOW 0.2
DEFINE O2_LOW 0.18
DEFINE O2_HIGH 0.25
DEFINE PRESS_LOW 80
DEFINE PRESS_HIGH 120
DEFINE TEMP_LOW 283.15      ' 10°C
DEFINE TEMP_HIGH 308.15     ' 35°C

VAR power = 0
VAR oxygen = 0
VAR pressure = 0
VAR temp = 0
VAR alarmState = 0

main:
    ' Read all values using batch
    power = BATCHREAD(BATTERY_HASH, Charge, 2)          ' Minimum
    oxygen = BATCHREAD(SENSOR_HASH, RatioOxygen, 0)     ' Average
    pressure = BATCHREAD(SENSOR_HASH, Pressure, 0)      ' Average
    temp = BATCHREAD(SENSOR_HASH, Temperature, 0)       ' Average

    ' Update displays
    powerDisplay.Setting = power * 100
    o2Display.Setting = oxygen * 100
    pressDisplay.Setting = pressure
    tempDisplay.Setting = temp - 273.15

    ' Check for alarms
    alarmState = 0

    IF power < POWER_LOW THEN
        alarmState = 1
    ENDIF

    IF oxygen < O2_LOW OR oxygen > O2_HIGH THEN
        alarmState = 1
    ENDIF

    IF pressure < PRESS_LOW OR pressure > PRESS_HIGH THEN
        alarmState = 1
    ENDIF

    IF temp < TEMP_LOW OR temp > TEMP_HIGH THEN
        alarmState = 1
    ENDIF

    ' Set alarm
    IF alarmState = 1 THEN
        alarm.On = 1
        alarm.Color = 16711680  ' Red
    ELSE
        alarm.On = 1
        alarm.Color = 65280     ' Green
    ENDIF

    YIELD
    GOTO main
END
```

---

## Tips for Your Own Programs

1. **Start Simple**: Begin with basic examples and add complexity
2. **Test in Simulator**: Use F9 to test before deploying
3. **Use Comments**: Document your code for future reference
4. **Monitor Line Count**: Stay under 128 lines
5. **Use YIELD**: Required for device values to update
6. **Batch Operations**: More efficient for multiple similar devices
7. **Hysteresis**: Prevent oscillation in control systems
8. **Error Handling**: Check for error states on devices
