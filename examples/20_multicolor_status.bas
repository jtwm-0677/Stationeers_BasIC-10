' ================================================
' Multi-Color Status Display
' ================================================
' Creates a comprehensive status display using
' multiple colored LEDs to show system health.
'
' DEVICES NEEDED:
'   d0 = Gas Sensor (atmosphere)
'   d1 = Battery (power)
'   d2 = Wall Light (oxygen status)
'   d3 = Wall Light (pressure status)
'   d4 = Wall Light (power status)
'   d5 = Wall Light (master alarm)
'
' COLOR CODING:
'   Green = Good
'   Yellow = Warning
'   Red = Critical
'   Blue = Info/Low
' ================================================

ALIAS sensor d0
ALIAS battery d1
ALIAS o2Light d2
ALIAS pressLight d3
ALIAS powerLight d4
ALIAS masterAlarm d5

' Thresholds
DEFINE O2_LOW 0.16
DEFINE O2_GOOD 0.20
DEFINE O2_HIGH 0.24
DEFINE PRESS_LOW 80
DEFINE PRESS_HIGH 120
DEFINE POWER_LOW 0.20
DEFINE POWER_GOOD 0.50

' Colors
DEFINE GREEN 65280
DEFINE YELLOW 16776960
DEFINE RED 16711680
DEFINE BLUE 255

VAR oxygen = 0
VAR pressure = 0
VAR charge = 0
VAR hasAlarm = 0

main:
    ' Read all sensors
    oxygen = sensor.RatioOxygen
    pressure = sensor.Pressure
    charge = battery.Charge

    hasAlarm = 0

    ' === Oxygen Status ===
    o2Light.On = 1
    IF oxygen < O2_LOW THEN
        o2Light.Color = RED
        hasAlarm = 1
    ELSEIF oxygen > O2_HIGH THEN
        o2Light.Color = YELLOW
        hasAlarm = 1
    ELSEIF oxygen >= O2_GOOD THEN
        o2Light.Color = GREEN
    ELSE
        o2Light.Color = BLUE
    ENDIF

    ' === Pressure Status ===
    pressLight.On = 1
    IF pressure < PRESS_LOW THEN
        pressLight.Color = RED
        hasAlarm = 1
    ELSEIF pressure > PRESS_HIGH THEN
        pressLight.Color = YELLOW
        hasAlarm = 1
    ELSE
        pressLight.Color = GREEN
    ENDIF

    ' === Power Status ===
    powerLight.On = 1
    IF charge < POWER_LOW THEN
        powerLight.Color = RED
        hasAlarm = 1
    ELSEIF charge < POWER_GOOD THEN
        powerLight.Color = YELLOW
    ELSE
        powerLight.Color = GREEN
    ENDIF

    ' === Master Alarm ===
    masterAlarm.On = 1
    IF hasAlarm = 1 THEN
        masterAlarm.Color = RED
    ELSE
        masterAlarm.Color = GREEN
    ENDIF

    YIELD
    GOTO main
END
