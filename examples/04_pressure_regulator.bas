' ============================================
' EXAMPLE 04: Pressure Regulator
' ============================================
' DIFFICULTY: Intermediate
'
' DESCRIPTION:
' Maintains room pressure at a target level using
' active vents. Automatically adds or removes
' atmosphere as needed.
'
' DEVICE CONNECTIONS:
' d0 = Gas Sensor (in the room)
' d1 = Active Vent (intake - from tank/outside)
' d2 = Active Vent (exhaust - to tank/outside)
'
' HOW IT WORKS:
' - Monitors pressure via gas sensor
' - Low pressure: intake vent pulls air in
' - High pressure: exhaust vent pushes air out
' - Normal: both vents idle
'
' VENT MODES:
' - Mode 0 = Outward (push air out)
' - Mode 1 = Inward (pull air in)
'
' NOTES:
' - 101.325 kPa = 1 atmosphere (Earth sea level)
' - Stationeers habitats typically use 100 kPa
' ============================================

ALIAS sensor d0
ALIAS intake d1
ALIAS exhaust d2

DEFINE TARGET_PRESSURE 101.325
DEFINE LOW_THRESHOLD 90
DEFINE HIGH_THRESHOLD 110

VAR pressure = 0

main:
    pressure = sensor.Pressure

    IF pressure < LOW_THRESHOLD THEN
        ' Low pressure - bring in air
        intake.On = 1
        intake.Mode = 1      ' Inward
        exhaust.On = 0
    ELSEIF pressure > HIGH_THRESHOLD THEN
        ' High pressure - vent air
        intake.On = 0
        exhaust.On = 1
        exhaust.Mode = 0     ' Outward
    ELSE
        ' Pressure OK - idle
        intake.On = 0
        exhaust.On = 0
    ENDIF

    YIELD
    GOTO main

END
