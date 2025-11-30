' ================================================
' Arc Furnace Temperature Controller
' ================================================
' Maintains optimal smelting temperature with
' automatic power management and safety shutoff.
'
' DEVICES NEEDED:
'   d0 = Arc Furnace
'   d1 = LED Display (temperature readout)
'   d2 = Wall Light (status indicator)
'
' TEMPERATURE TARGETS:
'   Iron/Copper/Gold: 600K minimum
'   Steel: 800K minimum
'   Silicon: 900K minimum
'   Invar/Electrum: 700K minimum
'
' COLOR CODES:
'   Blue = Cold (below target)
'   Green = Optimal (at target)
'   Yellow = Hot (above target)
'   Red = Critical (overheating)
' ================================================

ALIAS furnace d0
ALIAS display d1
ALIAS status d2

' Temperature targets (Kelvin)
DEFINE TARGET_TEMP 800      ' Adjust for material
DEFINE TOLERANCE 50
DEFINE MAX_TEMP 1500        ' Safety cutoff

' Color values
DEFINE COLOR_BLUE 255
DEFINE COLOR_GREEN 65280
DEFINE COLOR_YELLOW 16776960
DEFINE COLOR_RED 16711680

VAR temp = 0
VAR tempCelsius = 0

main:
    temp = furnace.Temperature
    tempCelsius = temp - 273.15
    display.Setting = tempCelsius

    ' Safety check - emergency shutoff
    IF temp > MAX_TEMP THEN
        furnace.On = 0
        status.Color = COLOR_RED
        GOTO main
    ENDIF

    ' Temperature control with hysteresis
    IF temp < TARGET_TEMP - TOLERANCE THEN
        ' Too cold - turn on and heat up
        furnace.On = 1
        status.Color = COLOR_BLUE
    ELSEIF temp > TARGET_TEMP + TOLERANCE THEN
        ' Too hot - turn off and cool down
        furnace.On = 0
        status.Color = COLOR_YELLOW
    ELSE
        ' At target - maintain
        furnace.On = 1
        status.Color = COLOR_GREEN
    ENDIF

    YIELD
    GOTO main
END
