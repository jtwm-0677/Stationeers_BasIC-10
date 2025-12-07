' ============================================
' TEST: Device Alias Registry
' ============================================
' This tests the new DEVICE statement support
' in the simulator for Visual Scripting
' ============================================

' --- Named Devices (Visual Scripting syntax) ---
DEVICE sensor "StructureGasSensor"
DEVICE furnace "StructureFurnace"
DEVICE panel "StructureSolarPanel"

' --- Constants ---
DEFINE TARGET_TEMP 293.15

' --- Variables ---
VAR temp = 0
VAR pressure = 0
VAR power = 0

main:
    ' Read from named devices
    temp = sensor.Temperature
    pressure = sensor.Pressure

    ' Read solar panel power
    power = panel.Power

    ' Control furnace based on temperature
    IF temp < TARGET_TEMP THEN
        furnace.On = 1
        furnace.Setting = 100
    ELSE
        furnace.On = 0
        furnace.Setting = 0
    ENDIF

    YIELD
    GOTO main

END
