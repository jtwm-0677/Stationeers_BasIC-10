' ============================================
' EXAMPLE 01: Blink a Light
' ============================================
' DIFFICULTY: Beginner
'
' DESCRIPTION:
' The simplest possible program - toggles a light
' on and off every second. Perfect for testing
' your first IC10 chip setup.
'
' DEVICE CONNECTIONS:
' d0 = Wall Light (or any light source)
'
' HOW IT WORKS:
' 1. Set light On property to 1 (on)
' 2. Wait 1 second
' 3. Set light On property to 0 (off)
' 4. Wait 1 second
' 5. Repeat forever
' ============================================

ALIAS light d0

main:
    light.On = 1
    SLEEP 1
    light.On = 0
    SLEEP 1
    GOTO main

END
