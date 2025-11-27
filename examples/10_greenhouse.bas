' ============================================
' EXAMPLE 10: Greenhouse Controller
' ============================================
' DIFFICULTY: Advanced
'
' DESCRIPTION:
' Automated greenhouse management with temperature,
' lighting, and CO2 control. Monitors plant growth
' and adjusts environment for optimal conditions.
'
' DEVICE CONNECTIONS:
' d0 = Hydroponics Tray
' d1 = Grow Light
' d2 = Gas Sensor
' d3 = Wall Heater
' d4 = Active Vent (CO2 supply)
' d5 = LED Display (growth percentage)
'
' HOW IT WORKS:
' - Temperature: Maintains ~30°C for plant growth
' - Lighting: On during growth, off when mature
' - CO2: Maintains minimum 2% for photosynthesis
' - Display: Shows growth percentage (0-100%)
'
' SUBROUTINES:
' - ControlLight: Manage grow lights
' - ControlTemp: Heater management
' - ControlCO2: CO2 vent management
' - UpdateDisplay: Refresh growth display
' ============================================

ALIAS tray d0
ALIAS light d1
ALIAS sensor d2
ALIAS heater d3
ALIAS co2vent d4
ALIAS display d5

DEFINE OPTIMAL_TEMP 303.15      ' 30°C in Kelvin
DEFINE TEMP_TOLERANCE 5
DEFINE MIN_CO2 0.02             ' 2% CO2 minimum

VAR temp = 0
VAR co2 = 0
VAR growth = 0
VAR mature = 0

main:
    ' Read all sensors
    temp = sensor.Temperature
    co2 = sensor.RatioCarbonDioxide
    growth = tray.Growth
    mature = tray.Mature

    ' Run control subroutines
    GOSUB ControlLight
    GOSUB ControlTemp
    GOSUB ControlCO2
    GOSUB UpdateDisplay

    YIELD
    GOTO main

' --- LIGHTING CONTROL ---
' Light on during growth, off when mature
ControlLight:
    IF mature = 0 THEN
        light.On = 1
    ELSE
        light.On = 0
    ENDIF
    RETURN

' --- TEMPERATURE CONTROL ---
' Maintain optimal growing temperature
ControlTemp:
    IF temp < OPTIMAL_TEMP - TEMP_TOLERANCE THEN
        heater.On = 1
    ELSE
        heater.On = 0
    ENDIF
    RETURN

' --- CO2 CONTROL ---
' Maintain minimum CO2 for photosynthesis
ControlCO2:
    IF co2 < MIN_CO2 THEN
        co2vent.On = 1
    ELSE
        co2vent.On = 0
    ENDIF
    RETURN

' --- UPDATE DISPLAY ---
' Show growth as percentage
UpdateDisplay:
    display.Setting = growth * 100
    RETURN

END
