' ============================================
' EXAMPLE 16: Named Device Reference
' ============================================
' DIFFICULTY: Advanced
'
' DESCRIPTION:
' Demonstrates how to control MORE than 6 devices
' using named device references. This bypasses
' the IC10's 6-pin limitation completely!
'
' DEVICE CONNECTIONS:
' NONE NEEDED! All devices are referenced by name.
'
' SETUP REQUIREMENTS:
' 1. Place and connect devices to the same network
' 2. Use a Labeler tool to name each device:
'    - Name sensors: "Living Room Sensor", etc.
'    - Name heaters: "Living Room Heater", etc.
'
' HOW IT WORKS:
' - IC.Device[hash].Name["label"] references by name
' - Uses lbn/sbn instructions (batch named)
' - No physical wire connection needed
' - Unlimited device control from one IC10!
'
' NOTE: Device type hashes can be found in the
' Device Hash Database (F4 in the compiler)
' ============================================

' --- NAMED DEVICE REFERENCES ---
' Sensors (all ItemStructureGasSensor type)
ALIAS livingRoomSensor = IC.Device["ItemStructureGasSensor"].Name["Living Room Sensor"]
ALIAS bedroomSensor = IC.Device["ItemStructureGasSensor"].Name["Bedroom Sensor"]
ALIAS kitchenSensor = IC.Device["ItemStructureGasSensor"].Name["Kitchen Sensor"]
ALIAS bathroomSensor = IC.Device["ItemStructureGasSensor"].Name["Bathroom Sensor"]

' Heaters (all ItemStructureWallHeater type)
ALIAS livingRoomHeater = IC.Device["ItemStructureWallHeater"].Name["Living Room Heater"]
ALIAS bedroomHeater = IC.Device["ItemStructureWallHeater"].Name["Bedroom Heater"]
ALIAS kitchenHeater = IC.Device["ItemStructureWallHeater"].Name["Kitchen Heater"]
ALIAS bathroomHeater = IC.Device["ItemStructureWallHeater"].Name["Bathroom Heater"]

' Status lights (all ItemStructureWallLight type)
ALIAS livingRoomLight = IC.Device["ItemStructureWallLight"].Name["Living Room Status"]
ALIAS bedroomLight = IC.Device["ItemStructureWallLight"].Name["Bedroom Status"]

' Constants
DEFINE TARGET_TEMP 293.15   ' 20°C in Kelvin
DEFINE TOLERANCE 2

' Variables
VAR temp = 0

main:
    ' --- LIVING ROOM ---
    temp = livingRoomSensor.Temperature
    IF temp < TARGET_TEMP - TOLERANCE THEN
        livingRoomHeater.On = 1
        livingRoomLight.Color = 255         ' Blue = heating
    ELSEIF temp > TARGET_TEMP + TOLERANCE THEN
        livingRoomHeater.On = 0
        livingRoomLight.Color = 65280       ' Green = OK
    ELSE
        livingRoomHeater.On = 0
        livingRoomLight.Color = 65280       ' Green = OK
    ENDIF

    ' --- BEDROOM ---
    temp = bedroomSensor.Temperature
    IF temp < TARGET_TEMP - TOLERANCE THEN
        bedroomHeater.On = 1
        bedroomLight.Color = 255
    ELSE
        bedroomHeater.On = 0
        bedroomLight.Color = 65280
    ENDIF

    ' --- KITCHEN ---
    temp = kitchenSensor.Temperature
    IF temp < TARGET_TEMP - TOLERANCE THEN
        kitchenHeater.On = 1
    ELSE
        kitchenHeater.On = 0
    ENDIF

    ' --- BATHROOM ---
    temp = bathroomSensor.Temperature
    IF temp < TARGET_TEMP - TOLERANCE THEN
        bathroomHeater.On = 1
    ELSE
        bathroomHeater.On = 0
    ENDIF

    YIELD
    GOTO main

END
