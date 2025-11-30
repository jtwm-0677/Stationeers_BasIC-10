' ================================================
' Logic Memory State Machine
' ================================================
' Demonstrates using Logic Memory for persistent
' state storage across program cycles.
'
' DEVICES NEEDED:
'   d0 = Logic Memory (state storage)
'   d1 = Logic Switch (input trigger)
'   d2 = Wall Light (output 1)
'   d3 = Wall Light (output 2)
'   d4 = Wall Light (output 3)
'   d5 = LED Display (state display)
'
' STATES:
'   0 = Idle (all off)
'   1 = Stage 1 (light 1 on)
'   2 = Stage 2 (lights 1+2 on)
'   3 = Stage 3 (all lights on)
'   Button press advances to next state
'   State 3 -> 0 wraps around
' ================================================

ALIAS memory d0
ALIAS button d1
ALIAS light1 d2
ALIAS light2 d3
ALIAS light3 d4
ALIAS display d5

VAR state = 0
VAR lastButton = 0
VAR currentButton = 0

main:
    ' Load state from memory (persists across resets)
    state = memory.Setting

    ' Read button with edge detection
    currentButton = button.Setting

    IF currentButton = 1 AND lastButton = 0 THEN
        ' Button just pressed - advance state
        state = state + 1
        IF state > 3 THEN
            state = 0
        ENDIF
        ' Save new state to memory
        memory.Setting = state
    ENDIF

    lastButton = currentButton

    ' Apply state to outputs
    IF state = 0 THEN
        light1.On = 0
        light2.On = 0
        light3.On = 0
    ELSEIF state = 1 THEN
        light1.On = 1
        light2.On = 0
        light3.On = 0
    ELSEIF state = 2 THEN
        light1.On = 1
        light2.On = 1
        light3.On = 0
    ELSEIF state = 3 THEN
        light1.On = 1
        light2.On = 1
        light3.On = 1
    ENDIF

    ' Display current state
    display.Setting = state

    YIELD
    GOTO main
END
