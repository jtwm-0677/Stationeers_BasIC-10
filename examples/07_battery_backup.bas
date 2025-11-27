' ============================================
' EXAMPLE 07: Battery Backup with Generator
' ============================================
' DIFFICULTY: Intermediate
'
' DESCRIPTION:
' Manages battery power with automatic backup
' generator. Generator starts when battery is
' low and stops when fully charged. Uses
' hysteresis to prevent rapid cycling.
'
' DEVICE CONNECTIONS:
' d0 = Battery (Large Battery recommended)
' d1 = Solid Fuel Generator (or any generator)
' d2 = LED Display (shows charge percentage)
'
' HOW IT WORKS:
' - Monitors battery charge level (0-1 ratio)
' - Below 20%: start generator
' - Above 90%: stop generator
' - Hysteresis prevents on/off oscillation
' - Display shows charge as percentage
'
' NOTES:
' - Charge property is 0 to 1 (ratio)
' - Multiply by 100 for percentage
' - Adjust thresholds based on power needs
' ============================================

ALIAS battery d0
ALIAS generator d1
ALIAS display d2

DEFINE LOW_CHARGE 0.20      ' Start generator at 20%
DEFINE HIGH_CHARGE 0.90     ' Stop generator at 90%

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
    ' Between thresholds: maintain current state

    generator.On = genOn

    ' Display percentage
    display.Setting = charge * 100

    YIELD
    GOTO main

END
