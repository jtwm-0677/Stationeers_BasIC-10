REM Temperature Monitor
REM Reads temperature from a sensor and controls a cooler

ALIAS sensor d0
ALIAS cooler d1

DEFINE TARGET_TEMP 20
DEFINE HYSTERESIS 2

REM Main loop
10 INPUT temp

   IF temp > TARGET_TEMP + HYSTERESIS THEN
       LET coolerOn = 1
   ELSEIF temp < TARGET_TEMP - HYSTERESIS THEN
       LET coolerOn = 0
   ENDIF

   PRINT temp
   YIELD
   GOTO 10
