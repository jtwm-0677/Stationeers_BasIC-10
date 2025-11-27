================================================================================
           BASIC TO IC10 COMPILER - EXAMPLE PROGRAMS
================================================================================

This folder contains example programs demonstrating the BASIC-IC10 compiler
features. Each example is thoroughly documented and ready to compile.

All examples can be opened in the BASIC-IC10 Compiler application, or read
as plain text files for reference.


================================================================================
TABLE OF CONTENTS
================================================================================

BEGINNER EXAMPLES
  01_blink_light.bas      - Toggle a light on/off (Hello World)
  02_button_toggle.bas    - Toggle device with button press (edge detection)
  03_thermostat.bas       - Temperature control with hysteresis
  06_solar_tracker.bas    - Automatic solar panel positioning
  13_math_demo.bas        - Built-in math function reference
  14_dial_pump_control.bas - Analog dial input control

INTERMEDIATE EXAMPLES
  04_pressure_regulator.bas - Room pressure management
  05_oxygen_monitor.bas     - O2 monitoring with color-coded alarm
  07_battery_backup.bas     - Generator backup with hysteresis
  09_furnace_controller.bas - Smelting temperature control
  15_item_sorter.bas        - Hash-based item sorting

ADVANCED EXAMPLES
  08_airlock.bas            - Full airlock state machine
  10_greenhouse.bas         - Multi-system plant growth control
  11_batch_solar_array.bas  - Batch operations for device arrays
  12_base_status_monitor.bas - Comprehensive base monitoring
  16_named_devices.bas      - Named device reference (bypass 6-pin limit!)


================================================================================
QUICK START
================================================================================

1. Open the BASIC-IC10 Compiler application
2. File > Open and select any .bas file from this folder
3. Press F5 to compile
4. Review the IC10 output in the bottom panel
5. Use F9 to test in the simulator
6. Save & Deploy to send to Stationeers


================================================================================
DEVICE CONNECTION REFERENCE
================================================================================

IC10 chips have 6 device ports: d0, d1, d2, d3, d4, d5

Each example lists required device connections in its header comments.

Example:
    ' DEVICE CONNECTIONS:
    ' d0 = Gas Sensor
    ' d1 = Wall Heater
    ' d2 = LED Display

In Stationeers:
1. Place the IC Housing
2. Connect devices using data cables or networks
3. Devices connect in order: first connection = d0, second = d1, etc.


================================================================================
EXAMPLE 01: BLINK A LIGHT
================================================================================
File: 01_blink_light.bas
Difficulty: Beginner

DESCRIPTION:
The simplest possible IC10 program. Toggles a light on and off every second.
Perfect for testing your first IC10 setup.

DEVICES NEEDED:
- d0 = Wall Light (or any light)

BASIC CODE:
-----------
ALIAS light d0

main:
    light.On = 1
    SLEEP 1
    light.On = 0
    SLEEP 1
    GOTO main

END

COMPILED IC10:
--------------
alias light d0
main:
s light On 1
sleep 1
s light On 0
sleep 1
j main

HOW IT WORKS:
1. ALIAS creates a friendly name "light" for device port d0
2. The label "main:" marks where the loop begins
3. light.On = 1 turns the light on
4. SLEEP 1 pauses for 1 second
5. light.On = 0 turns the light off
6. SLEEP 1 pauses again
7. GOTO main jumps back to repeat


================================================================================
EXAMPLE 03: SIMPLE THERMOSTAT
================================================================================
File: 03_thermostat.bas
Difficulty: Beginner

DESCRIPTION:
Maintains room temperature using a heater and cooler. Demonstrates hysteresis
control to prevent rapid on/off switching.

DEVICES NEEDED:
- d0 = Gas Sensor
- d1 = Wall Heater
- d2 = Wall Cooler

BASIC CODE:
-----------
ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2

DEFINE TARGET_TEMP 293.15   ' 20 C in Kelvin
DEFINE TOLERANCE 2          ' +/- 2 degrees

VAR temp = 0

main:
    temp = sensor.Temperature

    IF temp < TARGET_TEMP - TOLERANCE THEN
        heater.On = 1
        cooler.On = 0
    ELSEIF temp > TARGET_TEMP + TOLERANCE THEN
        heater.On = 0
        cooler.On = 1
    ELSE
        heater.On = 0
        cooler.On = 0
    ENDIF

    YIELD
    GOTO main

END

KEY CONCEPTS:
- DEFINE creates compile-time constants (replaced during compilation)
- VAR declares a runtime variable
- IF/ELSEIF/ELSE/ENDIF provides conditional logic
- YIELD allows device values to update (required in loops)
- Hysteresis: tolerance band prevents oscillation


================================================================================
EXAMPLE 05: OXYGEN MONITOR
================================================================================
File: 05_oxygen_monitor.bas
Difficulty: Intermediate

DESCRIPTION:
Monitors oxygen levels and displays status using a color-coded light.

DEVICES NEEDED:
- d0 = Gas Sensor
- d1 = Wall Light (alarm)
- d2 = LED Display

COLOR VALUES REFERENCE:
- Red:    16711680 (hex 0xFF0000)
- Yellow: 16776960 (hex 0xFFFF00)
- Green:  65280    (hex 0x00FF00)
- Blue:   255      (hex 0x0000FF)

OXYGEN SAFETY LEVELS:
- Below 16%: Suffocation danger
- 18-23%: Normal breathable range
- Above 25%: Fire hazard

BASIC CODE:
-----------
ALIAS sensor d0
ALIAS alarm d1
ALIAS display d2

DEFINE MIN_OXYGEN 0.18
DEFINE MAX_OXYGEN 0.23

VAR oxygenRatio = 0
VAR oxygenPercent = 0

main:
    oxygenRatio = sensor.RatioOxygen
    oxygenPercent = oxygenRatio * 100

    display.Setting = oxygenPercent

    IF oxygenRatio < MIN_OXYGEN THEN
        alarm.On = 1
        alarm.Color = 16711680   ' Red - danger
    ELSEIF oxygenRatio > MAX_OXYGEN THEN
        alarm.On = 1
        alarm.Color = 16776960   ' Yellow - warning
    ELSE
        alarm.On = 1
        alarm.Color = 65280      ' Green - safe
    ENDIF

    YIELD
    GOTO main

END


================================================================================
EXAMPLE 08: AIRLOCK CONTROLLER
================================================================================
File: 08_airlock.bas
Difficulty: Advanced

DESCRIPTION:
Full airlock automation with state machine. Handles pressurization cycles
and door interlocks safely.

DEVICES NEEDED:
- d0 = Inner Door
- d1 = Outer Door
- d2 = Active Vent/Pump
- d3 = Airlock Gas Sensor
- d4 = Inner Button
- d5 = Outer Button

STATE MACHINE:
- State 0 (Idle): Both doors can be controlled, waiting for request
- State 1 (Depressurizing): Pumping air out, preparing for exterior access
- State 2 (Pressurizing): Pumping air in, preparing for interior access

SAFETY FEATURES:
- Only one door can be open at a time
- Doors are locked during pressure changes
- Pressure must be correct before door unlocks

KEY CODE SECTIONS:

State Definitions:
    VAR state = 0   ' 0=idle, 1=depressurizing, 2=pressurizing

Main Loop:
    IF state = 0 THEN
        GOSUB IdleState
    ELSEIF state = 1 THEN
        GOSUB Depressurize
    ELSEIF state = 2 THEN
        GOSUB Pressurize
    ENDIF

Subroutine Pattern:
    Depressurize:
        innerDoor.Lock = 1
        pump.On = 1
        pump.Mode = 0       ' Outward

        IF pressure < VACUUM THEN
            pump.On = 0
            outerDoor.Lock = 0
            outerDoor.Open = 1
            state = 0
        ENDIF
        RETURN


================================================================================
EXAMPLE 11: BATCH SOLAR ARRAY
================================================================================
File: 11_batch_solar_array.bas
Difficulty: Advanced

DESCRIPTION:
Controls ALL solar panels on the network using batch operations. No direct
device connections needed - works with unlimited panels.

BATCH OPERATIONS:
- BATCHREAD(hash, property, mode) - Read from all matching devices
- BATCHWRITE(hash, property, value) - Write to all matching devices

BATCH READ MODES:
- 0 = Average of all values
- 1 = Sum of all values
- 2 = Minimum value
- 3 = Maximum value

FINDING DEVICE HASHES:
1. Open BASIC-IC10 Compiler
2. Tools > Device Hash Database (F4)
3. Search for "Solar Panel"
4. Copy the hash value

BASIC CODE:
-----------
ALIAS display d0

DEFINE SOLAR_HASH -539224550    ' ItemStructureSolarPanel

VAR solarAngle = 0
VAR totalPower = 0

main:
    ' Get sun angle (average from all panels)
    solarAngle = BATCHREAD(SOLAR_HASH, SolarAngle, 0)

    ' Set ALL panels to track sun
    BATCHWRITE(SOLAR_HASH, Horizontal, solarAngle)
    BATCHWRITE(SOLAR_HASH, Vertical, 60)

    ' Get total power (sum from all panels)
    totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)

    display.Setting = totalPower

    YIELD
    GOTO main

END


================================================================================
EXAMPLE 13: MATH FUNCTIONS
================================================================================
File: 13_math_demo.bas
Difficulty: Beginner (Reference)

COMPLETE FUNCTION LIST:

TRIGONOMETRY (angles in radians):
    SIN(x)      Sine
    COS(x)      Cosine
    TAN(x)      Tangent
    ASIN(x)     Arc sine
    ACOS(x)     Arc cosine
    ATAN(x)     Arc tangent
    ATAN2(y,x)  Arc tangent of y/x

EXPONENTIAL:
    LOG(x)      Natural logarithm
    EXP(x)      e raised to power x
    x ^ y       x raised to power y
    SQRT(x)     Square root

ROUNDING:
    CEIL(x)     Round up to integer
    FLOOR(x)    Round down to integer
    ROUND(x)    Round to nearest integer
    TRUNC(x)    Truncate (remove decimals)
    ABS(x)      Absolute value

COMPARISON:
    MIN(a, b)   Return smaller value
    MAX(a, b)   Return larger value

RANDOM:
    RAND        Random number 0.0 to 1.0

EXAMPLES:

    VAR angle = 3.14159 / 4     ' 45 degrees
    VAR s = SIN(angle)          ' = 0.707...

    VAR x = SQRT(16)            ' = 4
    VAR y = 2 ^ 10              ' = 1024

    VAR a = FLOOR(3.7)          ' = 3
    VAR b = CEIL(3.2)           ' = 4
    VAR c = ROUND(3.5)          ' = 4

    VAR r = RAND                ' 0.0 to 1.0
    VAR die = FLOOR(RAND * 6) + 1   ' 1 to 6


================================================================================
COMMON DEVICE PROPERTIES
================================================================================

MOST DEVICES:
    .On           - Power state (0/1)
    .Setting      - Configurable value
    .Error        - Error state (0 = OK)

GAS SENSORS:
    .Temperature    - Temperature in Kelvin
    .Pressure       - Pressure in kPa
    .RatioOxygen    - O2 ratio (0-1)
    .RatioCarbonDioxide - CO2 ratio (0-1)
    .RatioNitrogen  - N2 ratio (0-1)
    .RatioVolatiles - Volatiles ratio (0-1)

BATTERIES:
    .Charge         - Charge level (0-1)
    .ChargeRatio    - Same as Charge

SOLAR PANELS:
    .SolarAngle     - Current sun angle
    .Horizontal     - Panel rotation
    .Vertical       - Panel tilt
    .PowerGeneration - Current power output

DOORS:
    .Open           - Open state (0/1)
    .Lock           - Lock state (0/1)

PUMPS/VENTS:
    .Mode           - Direction (0=out, 1=in)
    .Setting        - Target pressure

FURNACES:
    .Temperature    - Internal temperature
    .ImportCount    - Items imported
    .ExportCount    - Items exported


================================================================================
TEMPERATURE REFERENCE
================================================================================

Stationeers uses Kelvin. Common conversions:

    0°C   = 273.15 K
    20°C  = 293.15 K (comfortable room temp)
    25°C  = 298.15 K
    100°C = 373.15 K (water boiling)

To convert Kelvin to Celsius in code:
    celsius = temp - 273.15

Common Stationeers temperatures:
    Comfortable:    290 - 300 K  (17 - 27 C)
    Smelting Iron:  ~800 K
    Smelting Steel: ~900 K


================================================================================
TIPS FOR WRITING PROGRAMS
================================================================================

1. ALWAYS USE YIELD
   Every continuous loop needs YIELD to allow device values to update.
   Without it, sensor readings won't refresh.

2. USE HYSTERESIS
   When controlling on/off devices, use a tolerance band to prevent
   rapid switching. See thermostat example.

3. CHECK LINE COUNT
   IC10 has a 128-line limit. Use aggressive optimization if needed.
   The compiler shows line count in the status bar.

4. TEST IN SIMULATOR
   Press F9 to test code in the simulator before deploying.
   You can modify registers and device values to test scenarios.

5. USE BATCH FOR MULTIPLE DEVICES
   If controlling many identical devices (solar panels, lights),
   batch operations are more efficient than individual connections.

6. HANDLE ERRORS
   Check device.Error property for malfunctions.

7. DOCUMENT YOUR CODE
   Use comments (') to explain logic. They're stripped during
   compilation so they don't count toward line limit.


================================================================================
NAMED DEVICE REFERENCE (BYPASS 6-PIN LIMIT!)
================================================================================

IC10 chips only have 6 device pins (d0-d5). Named device references let you
control UNLIMITED devices from a single IC10 chip!

SETUP:
1. Place devices and connect them to the same data network
2. Use a Labeler tool to give each device a unique name
3. Reference devices by name in your code

SYNTAX:
    ALIAS name = IC.Device["DeviceType"].Name["Device Label"]

EXAMPLE:
    ' Reference sensors by their labels
    ' NOTE: Use "Structure*" prefix for placed structures (not "ItemStructure*")
    ALIAS room1_sensor = IC.Device["StructureGasSensor"].Name["Room 1 Sensor"]
    ALIAS room2_sensor = IC.Device["StructureGasSensor"].Name["Room 2 Sensor"]

    ' Reference heaters by their labels
    ALIAS room1_heater = IC.Device["StructureWallHeater"].Name["Room 1 Heater"]
    ALIAS room2_heater = IC.Device["StructureWallHeater"].Name["Room 2 Heater"]

    ' Use them like normal devices
    temp = room1_sensor.Temperature
    room1_heater.On = 1

DEVICE TYPE NAMES (for placed structures - use Structure* prefix):
    StructureGasSensor       - Gas sensors
    StructureWallHeater      - Wall heaters
    StructureWallCooler      - Wall coolers
    StructureWallLight       - Wall lights
    StructurePumpVolume      - Volume pumps
    StructureActiveVent      - Active vents
    StructureSolarPanel      - Solar panels

Find more device types in the Device Hash Database (F4)

SEE: 16_named_devices.bas for a complete example


================================================================================
KEYBOARD SHORTCUTS (in BASIC-IC10 Compiler)
================================================================================

FILE:
    Ctrl+N          New file
    Ctrl+O          Open file
    Ctrl+S          Save file
    Ctrl+Shift+S    Save As

EDITING:
    Ctrl+Z          Undo
    Ctrl+Y          Redo
    Ctrl+F          Find
    Ctrl+H          Replace
    Ctrl+Space      Auto-complete

BUILD:
    F5              Compile
    F6              Compile and copy to clipboard
    F9              Run simulator

TOOLS:
    F4              Device Hash Database
    F1              Documentation


================================================================================
CUSTOM DEVICE DATABASE (EXTENSIBLE)
================================================================================

The Device Hash Database can be extended with custom JSON files. This allows
you to add new devices from game updates without waiting for compiler updates.

CUSTOM DEVICE LOCATIONS (checked in order):
1. CustomDevices.json in the compiler's folder
2. Any .json file in the CustomDevices/ subfolder
3. Documents/BASIC-IC10/CustomDevices/*.json

JSON FILE FORMAT:
-----------------
{
  "devices": [
    {
      "prefabName": "StructureMyDevice",
      "category": "Custom",
      "displayName": "My Device",
      "description": "Description here"
    }
  ],
  "logicTypes": [
    {
      "name": "NewProperty",
      "displayName": "New Property",
      "description": "A new logic property"
    }
  ]
}

See Data/CustomDevices.template.json for a complete example with comments.

AFTER ADDING FILES:
- Use Tools > Reload Custom Devices
- Or restart the compiler

The Device Hash Database (F4) will show custom devices after reload.


================================================================================
