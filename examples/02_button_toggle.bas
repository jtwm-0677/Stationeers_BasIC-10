' ============================================
' EXAMPLE 02: Button Toggle
' ============================================
' DIFFICULTY: Beginner
'
' DESCRIPTION:
' Toggles a device on/off each time a button is
' pressed. Demonstrates edge detection - the
' device only toggles on the button press, not
' while held.
'
' DEVICE CONNECTIONS:
' d0 = Logic Button
' d1 = Any device to toggle (light, machine, etc.)
'
' HOW IT WORKS:
' - Tracks previous button state
' - Detects "rising edge" (0 to 1 transition)
' - Toggles device state on each press
' - Ignores button hold and release
' ============================================

ALIAS button d0
ALIAS device d1

VAR lastState = 0
VAR currentState = 0
VAR deviceOn = 0

main:
    currentState = button.Setting

    ' Detect button press (rising edge)
    IF currentState = 1 AND lastState = 0 THEN
        ' Toggle device state
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
