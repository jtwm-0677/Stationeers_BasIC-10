' ============================================
' EXAMPLE 08: Airlock Controller
' ============================================
' DIFFICULTY: Advanced
'
' DESCRIPTION:
' Full airlock automation with safety interlocks.
' Prevents both doors from opening simultaneously
' and manages pressurization/depressurization.
'
' DEVICE CONNECTIONS:
' d0 = Inner Door (to habitat)
' d1 = Outer Door (to outside/vacuum)
' d2 = Active Vent or Pump
' d3 = Airlock Gas Sensor
' d4 = Inner Button (request entry)
' d5 = Outer Button (request exit)
'
' HOW IT WORKS:
' State Machine:
' - State 0 (Idle): Wait for button press
' - State 1 (Depressurizing): Pump air out
' - State 2 (Pressurizing): Pump air in
'
' Safety:
' - Doors are locked during transitions
' - Only one door can open at a time
' - Pressure must be correct before opening
'
' NOTES:
' - VACUUM threshold ~1 kPa
' - PRESSURIZED threshold ~90 kPa
' - Adjust pump Mode: 0=out, 1=in
' ============================================

ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3
ALIAS innerButton d4
ALIAS outerButton d5

DEFINE VACUUM 1
DEFINE PRESSURIZED 90

VAR pressure = 0
VAR state = 0
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

' --- IDLE STATE ---
' Wait for button press, start appropriate cycle
IdleState:
    pump.On = 0

    IF innerRequest = 1 THEN
        ' Want to enter - need to pressurize
        outerDoor.Open = 0
        outerDoor.Lock = 1
        state = 2
    ELSEIF outerRequest = 1 THEN
        ' Want to exit - need to depressurize
        innerDoor.Open = 0
        innerDoor.Lock = 1
        state = 1
    ENDIF
    RETURN

' --- DEPRESSURIZE ---
' Pump air out until vacuum reached
Depressurize:
    innerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 0       ' Outward

    IF pressure < VACUUM THEN
        ' Vacuum reached - open outer door
        pump.On = 0
        outerDoor.Lock = 0
        outerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

' --- PRESSURIZE ---
' Pump air in until pressurized
Pressurize:
    outerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 1       ' Inward

    IF pressure > PRESSURIZED THEN
        ' Pressurized - open inner door
        pump.On = 0
        innerDoor.Lock = 0
        innerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

END
