' ================================================
' Water Electrolyzer Automation
' ================================================
' Automatically manages electrolyzer operation
' based on water input and gas output levels.
'
' DEVICES NEEDED:
'   d0 = Electrolyzer
'   d1 = Gas Sensor (output room)
'   d2 = Tank (water input)
'   d3 = LED Display (status)
'
' OPERATION:
'   - Monitors water tank level
'   - Monitors output room pressure
'   - Prevents overpressure
'   - Displays H2/O2 production status
'
' OUTPUT GASES:
'   Hydrogen (H2) and Oxygen (O2) at 2:1 ratio
' ================================================

ALIAS electrolyzer d0
ALIAS sensor d1
ALIAS waterTank d2
ALIAS display d3

' Thresholds
DEFINE MIN_WATER 0.1        ' Minimum water ratio
DEFINE MAX_PRESSURE 5000    ' Max output pressure (kPa)
DEFINE TARGET_PRESSURE 2000 ' Normal operating pressure

VAR waterLevel = 0
VAR pressure = 0
VAR oxygenRatio = 0
VAR running = 0

main:
    ' Read sensors
    waterLevel = waterTank.Ratio
    pressure = sensor.Pressure
    oxygenRatio = sensor.RatioOxygen

    ' Determine if we should run
    running = 1

    ' Check water supply
    IF waterLevel < MIN_WATER THEN
        running = 0
    ENDIF

    ' Check output pressure
    IF pressure > MAX_PRESSURE THEN
        running = 0
    ENDIF

    ' Apply control
    electrolyzer.On = running

    ' Display oxygen percentage
    display.Setting = oxygenRatio * 100

    YIELD
    GOTO main
END
