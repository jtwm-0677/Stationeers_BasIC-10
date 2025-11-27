' ============================================
' EXAMPLE 09: Furnace Temperature Controller
' ============================================
' DIFFICULTY: Intermediate
'
' DESCRIPTION:
' Maintains furnace at optimal smelting temperature.
' Displays current temperature and status.
'
' DEVICE CONNECTIONS:
' d0 = Furnace (Arc Furnace or similar)
' d1 = LED Display (shows temperature)
' d2 = Wall Light (status indicator)
'
' HOW IT WORKS:
' - Monitors furnace internal temperature
' - Turns on furnace when below target
' - Turns off when at/above target
' - Status light shows state:
'   * Blue  = Heating
'   * Green = At temperature
'   * Red   = Error/problem
'
' NOTES:
' - Different materials need different temps
' - Iron: ~800K, Steel: ~900K
' - Adjust TARGET_TEMP for your needs
' ============================================

ALIAS furnace d0
ALIAS display d1
ALIAS status d2

DEFINE TARGET_TEMP 800      ' 800K for basic smelting

VAR temp = 0
VAR error = 0

main:
    temp = furnace.Temperature
    error = furnace.Error

    ' Update display with temperature
    display.Setting = temp

    ' Control furnace
    IF error > 0 THEN
        ' Error state - red light
        furnace.On = 0
        status.On = 1
        status.Color = 16711680
    ELSEIF temp < TARGET_TEMP THEN
        ' Heating - blue light
        furnace.On = 1
        status.On = 1
        status.Color = 255
    ELSE
        ' At temperature - green light
        furnace.On = 0
        status.On = 1
        status.Color = 65280
    ENDIF

    YIELD
    GOTO main

END
