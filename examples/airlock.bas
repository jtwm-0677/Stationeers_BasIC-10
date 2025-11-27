REM Airlock Controller
REM Controls an airlock with inner door, outer door, and vent

ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS vent d2
ALIAS button d3
ALIAS pressureSensor d4

DEFINE SAFE_PRESSURE 101
DEFINE VACUUM_THRESHOLD 1

LET state = 0    ' 0=idle, 1=depressurizing, 2=pressurizing
LET targetSide = 0  ' 0=inner, 1=outer

REM Main control loop
10 INPUT buttonPressed

   IF buttonPressed = 1 AND state = 0 THEN
       REM Start cycle
       LET state = 1
       GOSUB 100  ' Close all doors
   ENDIF

   IF state = 1 THEN
       GOSUB 200  ' Depressurize
   ENDIF

   IF state = 2 THEN
       GOSUB 300  ' Pressurize
   ENDIF

   YIELD
   GOTO 10

REM Subroutine: Close all doors
100 LET innerOpen = 0
    LET outerOpen = 0
    RETURN

REM Subroutine: Depressurize
200 INPUT pressure
    IF pressure < VACUUM_THRESHOLD THEN
        LET state = 2
        LET outerOpen = 1
    ENDIF
    RETURN

REM Subroutine: Pressurize
300 INPUT pressure
    IF pressure > SAFE_PRESSURE THEN
        LET state = 0
        LET innerOpen = 1
    ENDIF
    RETURN

END
