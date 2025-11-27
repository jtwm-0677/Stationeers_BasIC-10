' ============================================
' EXAMPLE 12: Base Status Monitor
' ============================================
' DIFFICULTY: Advanced
'
' DESCRIPTION:
' Comprehensive base monitoring using batch
' operations. Monitors power, atmosphere, and
' triggers alarm when any system is critical.
'
' DEVICE CONNECTIONS:
' d0 = LED Display (power %)
' d1 = LED Display (O2 %)
' d2 = LED Display (pressure)
' d3 = LED Display (temperature °C)
' d4 = Wall Light (alarm indicator)
'
' HOW IT WORKS:
' Uses batch reads to monitor:
' - All batteries (minimum charge)
' - All gas sensors (average O2, pressure, temp)
' Triggers alarm if any value is critical.
'
' THRESHOLDS:
' - Power: <20% = alarm
' - Oxygen: <18% or >25% = alarm
' - Pressure: <80 or >120 kPa = alarm
' - Temp: <10°C or >35°C = alarm
' ============================================

ALIAS powerDisp d0
ALIAS o2Disp d1
ALIAS pressDisp d2
ALIAS tempDisp d3
ALIAS alarm d4

' Device hashes
DEFINE BATTERY_HASH -1388288459
DEFINE SENSOR_HASH 1255689925

' Safety thresholds
DEFINE POWER_LOW 0.2
DEFINE O2_LOW 0.18
DEFINE O2_HIGH 0.25
DEFINE PRESS_LOW 80
DEFINE PRESS_HIGH 120
DEFINE TEMP_LOW 283.15
DEFINE TEMP_HIGH 308.15

VAR power = 0
VAR oxygen = 0
VAR pressure = 0
VAR temp = 0
VAR alarmState = 0

main:
    ' Read values using batch operations
    power = BATCHREAD(BATTERY_HASH, Charge, 2)
    oxygen = BATCHREAD(SENSOR_HASH, RatioOxygen, 0)
    pressure = BATCHREAD(SENSOR_HASH, Pressure, 0)
    temp = BATCHREAD(SENSOR_HASH, Temperature, 0)

    ' Update displays
    powerDisp.Setting = power * 100
    o2Disp.Setting = oxygen * 100
    pressDisp.Setting = pressure
    tempDisp.Setting = temp - 273.15

    ' Check for alarm conditions
    alarmState = 0

    IF power < POWER_LOW THEN
        alarmState = 1
    ENDIF

    IF oxygen < O2_LOW THEN
        alarmState = 1
    ENDIF

    IF oxygen > O2_HIGH THEN
        alarmState = 1
    ENDIF

    IF pressure < PRESS_LOW THEN
        alarmState = 1
    ENDIF

    IF pressure > PRESS_HIGH THEN
        alarmState = 1
    ENDIF

    IF temp < TEMP_LOW THEN
        alarmState = 1
    ENDIF

    IF temp > TEMP_HIGH THEN
        alarmState = 1
    ENDIF

    ' Set alarm light
    alarm.On = 1
    IF alarmState = 1 THEN
        alarm.Color = 16711680  ' Red
    ELSE
        alarm.Color = 65280     ' Green
    ENDIF

    YIELD
    GOTO main

END
