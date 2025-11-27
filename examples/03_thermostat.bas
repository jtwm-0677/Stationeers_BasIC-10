' ============================================
' EXAMPLE 03: Simple Thermostat
' ============================================
' DIFFICULTY: Beginner
'
' DESCRIPTION:
' Maintains room temperature within a target range
' using a heater and cooler. Uses hysteresis to
' prevent rapid on/off cycling.
'
' DEVICE CONNECTIONS:
' d0 = Gas Sensor (reads room temperature)
' d1 = Wall Heater
' d2 = Wall Cooler (optional)
'
' HOW IT WORKS:
' - Reads temperature from sensor (in Kelvin)
' - If too cold: turn on heater, off cooler
' - If too hot: turn off heater, on cooler
' - If in range: both off (save power)
' - TOLERANCE prevents oscillation
'
' NOTES:
' - Temperature in Stationeers is in Kelvin
' - 293.15K = 20°C = 68°F
' - Adjust TARGET_TEMP for your preference
' ============================================

ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2

DEFINE TARGET_TEMP 293.15   ' 20°C in Kelvin
DEFINE TOLERANCE 2          ' ±2 degrees hysteresis

VAR temp = 0

main:
    temp = sensor.Temperature

    IF temp < TARGET_TEMP - TOLERANCE THEN
        ' Too cold - heat up
        heater.On = 1
        cooler.On = 0
    ELSEIF temp > TARGET_TEMP + TOLERANCE THEN
        ' Too hot - cool down
        heater.On = 0
        cooler.On = 1
    ELSE
        ' In range - save power
        heater.On = 0
        cooler.On = 0
    ENDIF

    YIELD
    GOTO main

END
