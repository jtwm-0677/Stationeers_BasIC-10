REM Solar Panel Tracker
REM Automatically rotates solar panels to face the sun

ALIAS panel d0
ALIAS lightSensor d1

DEFINE STEP_SIZE 5
DEFINE THRESHOLD 0.01

LET currentAngle = 0
LET maxLight = 0
LET bestAngle = 0

REM Scan for best angle
FOR angle = 0 TO 360 STEP STEP_SIZE
    LET currentAngle = angle

    REM Wait for panel to move
    SLEEP 0.5

    REM Read light level
    INPUT lightLevel

    IF lightLevel > maxLight THEN
        LET maxLight = lightLevel
        LET bestAngle = angle
    ENDIF
NEXT angle

REM Move to best angle
LET currentAngle = bestAngle
PRINT "Best angle:"
PRINT bestAngle

REM Fine-tune tracking loop
10 INPUT lightLevel

   REM Small adjustments
   LET testAngle = currentAngle + 1
   SLEEP 0.2
   INPUT newLight

   IF newLight > lightLevel + THRESHOLD THEN
       LET currentAngle = testAngle
   ELSE
       LET testAngle = currentAngle - 1
       SLEEP 0.2
       INPUT newLight
       IF newLight > lightLevel + THRESHOLD THEN
           LET currentAngle = testAngle
       ENDIF
   ENDIF

   YIELD
   GOTO 10
