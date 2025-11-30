# BASIC Language Reference

## Table of Contents
1. [Program Structure](#program-structure)
2. [Comments](#comments)
3. [Variables and Constants](#variables-and-constants)
4. [Data Types](#data-types)
5. [Operators](#operators)
6. [Control Flow](#control-flow)
7. [Loops](#loops)
8. [Subroutines and Functions](#subroutines-and-functions)
9. [Device Operations](#device-operations)
10. [Built-in Functions](#built-in-functions)
11. [Batch Operations](#batch-operations)
12. [Stack Operations](#stack-operations)
13. [Advanced Features](#advanced-features)

---

## Program Structure

### Basic Program Layout

```basic
' Comments and documentation
' Describe what your program does

' Aliases (device assignments)
ALIAS deviceName d0

' Constants
DEFINE CONSTANT_NAME value

' Variables
VAR variableName = initialValue

' Main program
main:
    ' Your code here
    YIELD
    GOTO main

' Subroutines
MySub:
    ' Subroutine code
    RETURN

END
```

### Program Execution

1. Aliases and defines are processed first
2. Execution begins at line 0 (or first executable line)
3. Program runs until END, HCF (halt and catch fire), or infinite loop
4. YIELD allows one game tick to pass

---

## Comments

### Single Line Comments

```basic
' This is a comment
REM This is also a comment
x = 5  ' Inline comment
```

### Best Practices

```basic
' ============================================
' Program: Solar Tracker
' Author: Your Name
' Description: Tracks sun position for panels
' ============================================

' --- Initialization ---
ALIAS panel d0

' Calculate optimal angle
angle = SolarAngle  ' Get current sun position
```

**Important Notes:**
- BASIC comments (`'` and `REM`) are stripped during compilation and don't count toward the IC10 128-line limit
- The compiled IC10 assembly uses `#` for comments (added by the compiler for debug info)
- IC10 MIPS only recognizes `#` as the comment character - never use `'` or `REM` in raw IC10 code

### Hybrid Mode Comments

The BASIC editor also accepts IC10-style `#` comments for hybrid mode:

```basic
' BASIC comment
REM Also a BASIC comment
# IC10-style comment (also valid in BASIC editor)

VAR x = 10  ' inline BASIC comment
VAR y = 20  # inline IC10-style comment (works too)
```

This allows you to:
- Paste IC10 code with `#` comments into the BASIC editor
- Mix both comment styles in hybrid code
- Maintain IC10 comments when editing decompiled code

---

## Variables and Constants

### Variable Declaration

```basic
' Explicit declaration with initialization
VAR temperature = 0
VAR pressure = 101.325
VAR isActive = 1

' LET statement (traditional BASIC)
LET x = 10
LET y = x + 5

' Direct assignment (implicit declaration)
counter = 0
result = counter + 1
```

### Constants with DEFINE

```basic
' Numeric constants
DEFINE MAX_TEMP 373.15      ' 100°C in Kelvin
DEFINE MIN_PRESSURE 50
DEFINE PI 3.14159
DEFINE TARGET_RATIO 0.21    ' Oxygen ratio

' Device hashes
DEFINE SOLAR_HASH -539224550
DEFINE GAS_SENSOR_HASH 1255689925
```

### Array Declaration (DIM)

```basic
' Fixed-size arrays
DIM values(10)              ' Array of 10 elements
DIM temperatures(5)

' Accessing array elements
values(0) = 100
values(1) = 200
x = values(0) + values(1)
```

### Variable Scope

All variables in BASIC-IC10 are global. There is no local scope.

```basic
VAR globalVar = 10

MySub:
    globalVar = 20     ' Modifies the global variable
    RETURN

main:
    GOSUB MySub
    ' globalVar is now 20
```

---

## Data Types

BASIC-IC10 uses a single numeric type (double-precision floating point), consistent with IC10's register type.

### Numeric Literals

```basic
x = 42              ' Integer
y = 3.14159         ' Decimal
z = -273.15         ' Negative
w = 1.5e6           ' Scientific notation (1,500,000)
```

### Boolean Values

```basic
' 0 = false, non-zero = true
isOn = 1            ' true
isOff = 0           ' false

' Comparisons return 0 or 1
result = (x > 5)    ' 1 if true, 0 if false
```

### Special Values

```basic
' NaN (Not a Number)
x = 0 / 0           ' Results in NaN
IF ISNAN(x) THEN
    ' Handle NaN case
ENDIF

' Infinity
y = 1 / 0           ' Positive infinity
z = -1 / 0          ' Negative infinity
```

---

## Operators

### Arithmetic Operators

| Operator | Description | Example | IC10 |
|----------|-------------|---------|------|
| `+` | Addition | `x + y` | add |
| `-` | Subtraction | `x - y` | sub |
| `*` | Multiplication | `x * y` | mul |
| `/` | Division | `x / y` | div |
| `%` or `MOD` | Modulo | `x % y` | mod |
| `^` | Power | `x ^ y` | exp+log |
| `-` (unary) | Negation | `-x` | sub r0 |

### Comparison Operators

| Operator | Description | Example | IC10 |
|----------|-------------|---------|------|
| `=` or `==` | Equal | `x = y` | seq |
| `<>` or `!=` | Not equal | `x <> y` | sne |
| `<` | Less than | `x < y` | slt |
| `>` | Greater than | `x > y` | sgt |
| `<=` | Less or equal | `x <= y` | sle |
| `>=` | Greater or equal | `x >= y` | sge |

### Logical Operators

| Operator | Description | Example | IC10 |
|----------|-------------|---------|------|
| `AND` | Logical AND | `a AND b` | and |
| `OR` | Logical OR | `a OR b` | or |
| `NOT` | Logical NOT | `NOT a` | seqz |
| `XOR` | Exclusive OR | `a XOR b` | xor |

### Bitwise Operators

| Operator | Description | Example | IC10 |
|----------|-------------|---------|------|
| `BAND` or `&` | Bitwise AND | `a BAND b` | and |
| `BOR` or `\|` | Bitwise OR | `a BOR b` | or |
| `BXOR` | Bitwise XOR | `a BXOR b` | xor |
| `BNOT` or `~` | Bitwise NOT | `BNOT a` | nor |
| `SHL` or `<<` | Shift left | `a SHL 2` | sll |
| `SHR` or `>>` | Shift right | `a SHR 2` | srl |
| `SAR` | Arithmetic shift right | `a SAR 2` | sra |

### Operator Precedence (Highest to Lowest)

1. `()` - Parentheses
2. `-`, `NOT`, `BNOT` - Unary operators
3. `^` - Exponentiation
4. `*`, `/`, `%`, `MOD` - Multiplication/Division
5. `+`, `-` - Addition/Subtraction
6. `SHL`, `SHR`, `SAR` - Bit shifts
7. `<`, `>`, `<=`, `>=` - Comparisons
8. `=`, `==`, `<>`, `!=` - Equality
9. `BAND`, `&` - Bitwise AND
10. `BXOR` - Bitwise XOR
11. `BOR`, `|` - Bitwise OR
12. `AND` - Logical AND
13. `OR`, `XOR` - Logical OR/XOR

---

## Control Flow

### IF...THEN...ELSE...ENDIF

#### Single-Line IF

```basic
IF condition THEN statement

' Examples
IF temp > 100 THEN heater.On = 0
IF pressure < 50 THEN GOTO alarm
```

#### Multi-Line IF

```basic
IF condition THEN
    ' statements when true
ENDIF

IF condition THEN
    ' statements when true
ELSE
    ' statements when false
ENDIF

IF condition1 THEN
    ' first condition
ELSEIF condition2 THEN
    ' second condition
ELSEIF condition3 THEN
    ' third condition
ELSE
    ' default case
ENDIF
```

#### Complex Conditions

```basic
IF temp > 300 AND pressure > 1000 THEN
    ' Both conditions true
ENDIF

IF status = 0 OR error = 1 THEN
    ' Either condition true
ENDIF

IF NOT (temp < 200) THEN
    ' Negated condition
ENDIF
```

### SELECT...CASE (Pseudo-implementation)

BASIC-IC10 doesn't have native SELECT CASE, but you can simulate it:

```basic
' Simulated SELECT CASE
IF mode = 0 THEN
    GOTO mode0Handler
ELSEIF mode = 1 THEN
    GOTO mode1Handler
ELSEIF mode = 2 THEN
    GOTO mode2Handler
ELSE
    GOTO defaultHandler
ENDIF

mode0Handler:
    ' Handle mode 0
    GOTO endSelect
mode1Handler:
    ' Handle mode 1
    GOTO endSelect
mode2Handler:
    ' Handle mode 2
    GOTO endSelect
defaultHandler:
    ' Default handling
endSelect:
```

### GOTO

```basic
' Unconditional jump
GOTO labelName

' Example: Main loop
main:
    ' Processing
    YIELD
    GOTO main
```

### Labels

```basic
' Labels must end with colon
myLabel:
    ' Code here

' Labels can contain letters, numbers, underscores
processData:
step_2:
loop1:
```

---

## Loops

### FOR...NEXT Loop

```basic
' Basic FOR loop
FOR i = 1 TO 10
    ' Loop body (executes 10 times)
NEXT i

' FOR with STEP
FOR i = 0 TO 100 STEP 10
    ' i = 0, 10, 20, ... 100
NEXT i

' Counting down
FOR i = 10 TO 1 STEP -1
    ' i = 10, 9, 8, ... 1
NEXT i

' Nested loops
FOR x = 0 TO 3
    FOR y = 0 TO 3
        ' Process grid cell (x, y)
    NEXT y
NEXT x
```

### WHILE...WEND Loop

```basic
' WHILE loop
WHILE condition
    ' Loop body
WEND

' Example: Wait for condition
WHILE sensor.Temperature > 300
    heater.On = 0
    YIELD
WEND
heater.On = 1
```

### DO...LOOP (Alternative)

```basic
' DO WHILE (check at start)
DO WHILE condition
    ' Loop body
LOOP

' DO UNTIL (check at end)
DO
    ' Loop body
LOOP UNTIL condition
```

### Loop with GOTO (Manual)

```basic
' Simple loop with GOTO
counter = 0
loopStart:
    IF counter >= 10 THEN GOTO loopEnd
    ' Loop body
    counter = counter + 1
    GOTO loopStart
loopEnd:
```

### Breaking Out of Loops

```basic
' Use GOTO to exit early
FOR i = 1 TO 100
    IF errorCondition THEN GOTO exitLoop
    ' Normal processing
NEXT i
exitLoop:
' Continue after loop
```

### Infinite Loops (Main Program Loop)

IC10 programs in Stationeers typically run continuously, checking sensors and controlling devices each tick. There are several ways to create the main program loop:

#### Option 1: WHILE TRUE (Recommended)
```basic
' Clean, structured infinite loop
ALIAS sensor d0
VAR temp = 0

WHILE TRUE
    temp = sensor.Temperature
    IF temp > 300 THEN
        PRINT "Hot!"
    ENDIF
    YIELD
WEND
```
**When to use:** Best for simple programs with a single continuous loop. Clear and readable.

#### Option 2: DO LOOP
```basic
' Alternative infinite loop syntax
ALIAS sensor d0
VAR temp = 0

DO
    temp = sensor.Temperature
    IF temp > 300 THEN
        PRINT "Hot!"
    ENDIF
    YIELD
LOOP
```
**When to use:** Equivalent to WHILE TRUE. Use whichever you find more readable.

#### Option 3: GOTO Label (Traditional BASIC)
```basic
' Classic BASIC main loop pattern
ALIAS sensor d0
VAR temp = 0

main:
    temp = sensor.Temperature
    IF temp > 300 THEN
        PRINT "Hot!"
    ENDIF
    YIELD
    GOTO main
```
**When to use:** Useful when you have multiple entry points or state machines where you jump between different sections of code. Also familiar to IC10 programmers.

#### Choosing the Right Pattern

| Pattern | Best For |
|---------|----------|
| `WHILE TRUE` | Simple continuous loops, clean readable code |
| `DO LOOP` | Same as WHILE TRUE, personal preference |
| `GOTO label` | State machines, multiple entry points, complex flow |

All three patterns compile to identical IC10 code, so choose based on readability and your program's structure.

> **Important:** Always include `YIELD` in your main loop! Without YIELD, the IC10 will crash due to infinite loop detection. YIELD pauses execution for one game tick.

---

## Subroutines and Functions

### GOSUB...RETURN

```basic
' Calling a subroutine
GOSUB mySubroutine

' Subroutine definition
mySubroutine:
    ' Subroutine code
    x = x + 1
    RETURN

' Multiple subroutines
main:
    GOSUB initialize
    GOSUB processData
    GOSUB updateDisplay
    YIELD
    GOTO main

initialize:
    temp = 0
    pressure = 0
    RETURN

processData:
    temp = sensor.Temperature
    pressure = sensor.Pressure
    RETURN

updateDisplay:
    display.Setting = temp
    RETURN
```

### SUB...END SUB (Named Subroutines)

```basic
SUB CalculateAverage
    ' Parameters passed via global variables
    result = (value1 + value2 + value3) / 3
END SUB

' Calling
value1 = 10
value2 = 20
value3 = 30
CALL CalculateAverage
' result now contains 20
```

### FUNCTION...END FUNCTION

```basic
FUNCTION Clamp(value, minVal, maxVal)
    IF value < minVal THEN
        Clamp = minVal
    ELSEIF value > maxVal THEN
        Clamp = maxVal
    ELSE
        Clamp = value
    ENDIF
END FUNCTION

' Using the function
safeValue = Clamp(input, 0, 100)
```

Note: Functions and SUBs compile to label-based code with global variables in IC10.

---

## Device Operations

### Device Aliases

```basic
' Assign friendly names to device ports
ALIAS sensor d0          ' d0 = Gas Sensor
ALIAS heater d1          ' d1 = Wall Heater
ALIAS display d2         ' d2 = Console/LED Display
ALIAS pump d3            ' d3 = Volume Pump
ALIAS vent d4            ' d4 = Active Vent
ALIAS logic d5           ' d5 = Logic Memory
```

### Advanced Device References (Named Devices)

IC10 chips have only 6 device pins (d0-d5), which can be limiting for complex automation. BASIC-IC10 supports advanced device referencing that bypasses this limitation using batch operations.

#### IC.Pin - Direct Pin Reference

Explicit pin reference (equivalent to simple d0-d5):

```basic
ALIAS sensor = IC.Pin[0]      ' Same as: ALIAS sensor d0
ALIAS heater = IC.Pin[1]      ' Same as: ALIAS heater d1
```

#### IC.Device - Batch Operations by Type

Reference ALL devices of a specific type on the network:

```basic
' Control all solar panels at once
ALIAS panels = IC.Device[-539224550]        ' Solar Panel hash
ALIAS panels = IC.Device["StructureSolarPanel"]  ' Or use type name

' Usage - affects ALL panels on network:
panels.Horizontal = solarAngle
panels.Vertical = 60
```

**Batch read modes** (used automatically for reads):
- Mode 0: Average of all matching devices
- Mode 1: Sum of all matching devices
- Mode 2: Minimum value
- Mode 3: Maximum value

```basic
' Get average temperature from all sensors
ALIAS sensors = IC.Device["StructureGasSensor"]
avgTemp = sensors.Temperature  ' Average from all sensors
```

#### IC.Device.Name - Named Device Reference (KEY FEATURE!)

**This is the most important feature for bypassing the 6-pin limit!**

Reference a SPECIFIC device by its custom name (set with labeler):

```basic
' Reference devices by their labeler-assigned names
' NOTE: Use "Structure*" prefix for placed structures, not "ItemStructure*"
ALIAS bedroomLight = IC.Device["StructureWallLight"].Name["Bedroom Light"]
ALIAS kitchenSensor = IC.Device["StructureGasSensor"].Name["Kitchen Sensor"]
ALIAS mainPump = IC.Device["StructurePumpVolume"].Name["Main Pump"]

' Now use them like any other device
bedroomLight.On = 1
temp = kitchenSensor.Temperature
mainPump.Setting = 500
```

**How it works:**
1. Label your devices in-game using a Labeler
2. Use `IC.Device[type].Name["Label"]` syntax (use `Structure*` prefix for placed structures)
3. The compiler generates `lbn`/`sbn` instructions that target specific named devices
4. You can reference UNLIMITED devices this way!

**Example - Control 10+ devices with one IC10 chip:**

```basic
' Temperature control across multiple rooms
ALIAS room1_sensor = IC.Device["StructureGasSensor"].Name["Room 1 Sensor"]
ALIAS room1_heater = IC.Device["StructureWallHeater"].Name["Room 1 Heater"]
ALIAS room2_sensor = IC.Device["StructureGasSensor"].Name["Room 2 Sensor"]
ALIAS room2_heater = IC.Device["StructureWallHeater"].Name["Room 2 Heater"]
ALIAS room3_sensor = IC.Device["StructureGasSensor"].Name["Room 3 Sensor"]
ALIAS room3_heater = IC.Device["StructureWallHeater"].Name["Room 3 Heater"]

' ... add as many as you need!

main:
    ' Control Room 1
    IF room1_sensor.Temperature < 293 THEN
        room1_heater.On = 1
    ELSE
        room1_heater.On = 0
    ENDIF

    ' Control Room 2
    IF room2_sensor.Temperature < 293 THEN
        room2_heater.On = 1
    ELSE
        room2_heater.On = 0
    ENDIF

    YIELD
    GOTO main
END
```

#### IC.ID - Reference by ID

Reference a device by its Reference ID (from configuration card):

```basic
ALIAS myDevice = IC.ID[123456789]
```

#### IC.Port.Channel - Channel Communication

For IC socket-based communication:

```basic
ALIAS comm = IC.Port[0].Channel[1]
ALIAS pinComm = IC.Pin[0].Port[0].Channel[1]
```

### Reading Device Properties

```basic
' Using dot notation
temp = sensor.Temperature
pressure = sensor.Pressure
isOn = heater.On

' Common readable properties
ratio = sensor.RatioOxygen
moles = sensor.TotalMoles
power = generator.PowerGeneration
charge = battery.Charge
```

### Writing Device Properties

```basic
' Setting device properties
heater.On = 1
heater.Setting = 500        ' Power setting
pump.Setting = 100          ' Target pressure/volume
display.Setting = temp      ' Display value

' Common writable properties
device.On = 1               ' Turn on
device.Lock = 0             ' Unlock
device.Mode = 2             ' Set mode
device.Open = 1             ' Open (vents, doors)
```

### Reading Device Slots

```basic
' Read item in device slot
slotOccupied = sensor[0].Occupied
itemHash = storage[0].OccupantHash
quantity = storage[0].Quantity
damage = tool[0].Damage
```

### Writing Device Slots

```basic
' Write to device slots
sorter[0].SortingClass = targetClass
machine[0].Quantity = 10
```

### Complete Property Reference

#### Universal Properties
| Property | Read | Write | Description |
|----------|------|-------|-------------|
| On | Yes | Yes | Power state (0/1) |
| Setting | Yes | Yes | Target setting |
| Mode | Yes | Yes | Operating mode |
| Error | Yes | No | Error state |
| Lock | Yes | Yes | Lock state |
| Power | Yes | No | Has power |
| Open | Yes | Yes | Open state (vents/doors) |
| Activate | No | Yes | Trigger activation |

#### Atmospheric Properties
| Property | Read | Write | Description |
|----------|------|-------|-------------|
| Temperature | Yes | No | Temperature (Kelvin) |
| Pressure | Yes | No | Pressure (kPa) |
| TotalMoles | Yes | No | Total gas moles |
| RatioOxygen | Yes | No | O2 ratio (0-1) |
| RatioCarbonDioxide | Yes | No | CO2 ratio (0-1) |
| RatioNitrogen | Yes | No | N2 ratio (0-1) |
| RatioVolatiles | Yes | No | H2 ratio (0-1) |
| RatioWater | Yes | No | Steam ratio (0-1) |
| RatioPollutant | Yes | No | Pollutant ratio (0-1) |
| RatioNitrousOxide | Yes | No | N2O ratio (0-1) |

#### Power Properties
| Property | Read | Write | Description |
|----------|------|-------|-------------|
| Charge | Yes | No | Battery charge (0-1) |
| ChargeRatio | Yes | No | Same as Charge |
| PowerRequired | Yes | No | Power demand (W) |
| PowerActual | Yes | No | Current draw (W) |
| PowerGeneration | Yes | No | Power output (W) |
| PowerPotential | Yes | No | Max possible (W) |

#### Solar Panel Properties
| Property | Read | Write | Description |
|----------|------|-------|-------------|
| Horizontal | Yes | Yes | Horizontal angle |
| Vertical | Yes | Yes | Vertical angle |
| SolarAngle | Yes | No | Current sun angle |
| SolarIrradiance | Yes | No | Light intensity |

#### Storage Properties
| Property | Read | Write | Description |
|----------|------|-------|-------------|
| Quantity | Yes | No | Item count |
| MaxQuantity | Yes | No | Capacity |
| Ratio | Yes | No | Fill ratio |
| PrefabHash | Yes | No | Item type hash |

---

## Built-in Functions

### Mathematical Functions

| Function | Description | Example | IC10 |
|----------|-------------|---------|------|
| `ABS(x)` | Absolute value | `ABS(-5)` → 5 | abs |
| `SQRT(x)` | Square root | `SQRT(16)` → 4 | sqrt |
| `FLOOR(x)` | Round down | `FLOOR(3.7)` → 3 | floor |
| `CEIL(x)` | Round up | `CEIL(3.2)` → 4 | ceil |
| `ROUND(x)` | Round to nearest | `ROUND(3.5)` → 4 | round |
| `TRUNC(x)` | Truncate decimal | `TRUNC(3.9)` → 3 | trunc |
| `MIN(a,b)` | Minimum | `MIN(3, 7)` → 3 | min |
| `MAX(a,b)` | Maximum | `MAX(3, 7)` → 7 | max |
| `LOG(x)` | Natural logarithm | `LOG(2.718)` → 1 | log |
| `EXP(x)` | e^x | `EXP(1)` → 2.718 | exp |
| `RAND()` | Random 0-1 | `RAND()` → 0.xxx | rand |

### Trigonometric Functions

| Function | Description | Example | IC10 |
|----------|-------------|---------|------|
| `SIN(x)` | Sine (radians) | `SIN(0)` → 0 | sin |
| `COS(x)` | Cosine (radians) | `COS(0)` → 1 | cos |
| `TAN(x)` | Tangent (radians) | `TAN(0)` → 0 | tan |
| `ASIN(x)` | Arc sine | `ASIN(0)` → 0 | asin |
| `ACOS(x)` | Arc cosine | `ACOS(1)` → 0 | acos |
| `ATAN(x)` | Arc tangent | `ATAN(0)` → 0 | atan |
| `ATAN2(y,x)` | Two-argument arctangent | `ATAN2(1,1)` | atan2 |

### Special Functions

| Function | Description | Example | IC10 |
|----------|-------------|---------|------|
| `ISNAN(x)` | Check if NaN | `ISNAN(0/0)` → 1 | snan |
| `SELECT(c,t,f)` | Conditional select | `SELECT(1>0, 5, 10)` → 5 | select |
| `HASH(s)` | CRC-32 string hash | `HASH("On")` → 1112093520 | - |
| `CLAMP(v,min,max)` | Clamp to range | `CLAMP(150,0,100)` → 100 | max+min |
| `LERP(a,b,t)` | Linear interpolate | `LERP(0,100,0.5)` → 50 | - |
| `INRANGE(v,min,max)` | Check if in range | `INRANGE(50,0,100)` → 1 | - |

### Approximate Equality

| Function | Description | IC10 |
|----------|-------------|------|
| `APPROX(a,b,eps)` | Check if a ≈ b within epsilon | sap |
| `APPROXZ(a,eps)` | Check if a ≈ 0 within epsilon | sapz |

### Conversion Functions

```basic
' Temperature conversions
kelvinToCelsius = kelvin - 273.15
celsiusToKelvin = celsius + 273.15
fahrenheitToCelsius = (fahrenheit - 32) * 5/9

' Angle conversions
radians = degrees * 3.14159 / 180
degrees = radians * 180 / 3.14159
```

---

## Batch Operations

Batch operations work with multiple devices of the same type simultaneously.

### BATCHREAD

```basic
' Read from all devices of a type
BATCHREAD(prefabHash, property, mode)

' Modes:
' 0 = Average
' 1 = Sum
' 2 = Minimum
' 3 = Maximum

' Examples
DEFINE SOLAR_HASH -539224550

' Total power from all solar panels
totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)    ' Sum

' Average temperature across all gas sensors
avgTemp = BATCHREAD(GAS_SENSOR_HASH, Temperature, 0)      ' Average

' Minimum charge across all batteries
minCharge = BATCHREAD(BATTERY_HASH, Charge, 2)            ' Minimum
```

### BATCHWRITE

```basic
' Write to all devices of a type
BATCHWRITE(prefabHash, property, value)

' Examples
' Turn on all wall heaters
BATCHWRITE(HEATER_HASH, On, 1)

' Set all pumps to same pressure
BATCHWRITE(PUMP_HASH, Setting, 100)
```

### Named Batch Operations

```basic
' Read from devices with specific name
value = BATCHNAMEREAD(nameHash, property, mode)

' Write to devices with specific name
BATCHNAMEWRITE(nameHash, property, value)

' Example
DEFINE MY_SENSORS_NAME 12345678    ' Hash of "MySensors"
temp = BATCHNAMEREAD(MY_SENSORS_NAME, Temperature, 0)
```

### Batch Slot Operations

```basic
' Read slot property from all devices of type
value = BATCHSLOTREAD(prefabHash, slotIndex, property, mode)

' Example: Count total items in slot 0 of all storage
totalItems = BATCHSLOTREAD(STORAGE_HASH, 0, Quantity, 1)  ' Sum
```

---

## Stack Operations

IC10 has a 512-value stack for temporary storage.

### PUSH and POP

```basic
' Push value onto stack
PUSH value
PUSH 42
PUSH temperature

' Pop value from stack
POP variable
POP result
```

### PEEK

```basic
' Read top of stack without removing
PEEK variable
PEEK topValue
```

### Stack Usage Example

```basic
' Subroutine with multiple return values
CalculateStats:
    PUSH sum
    PUSH average
    PUSH count
    RETURN

main:
    GOSUB CalculateStats
    POP myCount
    POP myAverage
    POP mySum
```

### Stack as Array

```basic
' Use stack as temporary array
FOR i = 0 TO 9
    PUSH readings(i)
NEXT i

' Process in reverse order
FOR i = 0 TO 9
    POP value
    ' Process value
NEXT i
```

---

## Advanced Features

### Ternary Expression

```basic
' Conditional assignment
result = IF condition THEN trueValue ELSE falseValue

' Example
status = IF temp > 100 THEN 1 ELSE 0

' Compiles to SELECT instruction
```

### Multiple Assignment

```basic
' Not directly supported, use separate statements
x = 10
y = 10
z = 10
```

### Inline Operations

```basic
' Increment/Decrement
x = x + 1
x = x - 1

' Compound assignment (use explicit form)
total = total + value
count = count * 2
```

### Device Existence Check

```basic
' Check if device is connected
IF sensor THEN
    temp = sensor.Temperature
ELSE
    temp = 0
ENDIF
```

### Reagent Operations

```basic
' Read reagent values from devices
reagentAmount = REAGENTREAD(device, reagentHash, mode)

' Modes:
' 0 = Contents (current amount)
' 1 = Required (amount needed)
' 2 = Recipe (amount in recipe)
```

### Slot Reagent Operations

```basic
' Read reagent from specific slot
amount = SLOTREAGENTREAD(device, slot, reagentHash, mode)
```

### Line Number Access

```basic
' Get current line number (useful for debugging)
currentLine = LINENUMBER

' Jump to specific line number (advanced)
GOTO lineNumber
```

### HCF - Halt and Catch Fire

```basic
' Stop execution permanently
IF criticalError THEN
    HCF    ' Halts the IC10
ENDIF
```

### YIELD vs SLEEP

```basic
' YIELD - Pause for one game tick, allow device updates
main:
    ' Read sensors
    temp = sensor.Temperature
    YIELD    ' Required for device values to update
    GOTO main

' SLEEP - Pause for specified seconds
SLEEP 1      ' Wait 1 second
SLEEP 0.5    ' Wait 0.5 seconds
SLEEP 0      ' Same as YIELD
```

### Register Aliases

```basic
' Direct register access (advanced)
REGISTER r0 AS counter
REGISTER r1 AS temp
REGISTER r2 AS result

counter = 0
temp = sensor.Temperature
result = counter + temp
```

---

## Code Examples

### Complete Temperature Controller

```basic
' ============================================
' Smart Temperature Controller
' Controls heating/cooling to maintain temp
' ============================================

' Devices
ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2
ALIAS display d3

' Constants
DEFINE TARGET 293.15    ' 20°C
DEFINE TOLERANCE 2      ' ±2°
DEFINE UPDATE_INTERVAL 2

' Variables
VAR currentTemp = 0
VAR mode = 0            ' 0=off, 1=heat, 2=cool
VAR timer = 0

' Main program
main:
    GOSUB ReadSensors
    GOSUB ControlLogic
    GOSUB UpdateOutputs
    GOSUB UpdateDisplay

    timer = timer + 1
    YIELD
    GOTO main

ReadSensors:
    currentTemp = sensor.Temperature
    RETURN

ControlLogic:
    IF currentTemp < TARGET - TOLERANCE THEN
        mode = 1    ' Need heating
    ELSEIF currentTemp > TARGET + TOLERANCE THEN
        mode = 2    ' Need cooling
    ELSEIF currentTemp >= TARGET - 1 AND currentTemp <= TARGET + 1 THEN
        mode = 0    ' At target, turn off
    ENDIF
    RETURN

UpdateOutputs:
    IF mode = 1 THEN
        heater.On = 1
        cooler.On = 0
    ELSEIF mode = 2 THEN
        heater.On = 0
        cooler.On = 1
    ELSE
        heater.On = 0
        cooler.On = 0
    ENDIF
    RETURN

UpdateDisplay:
    display.Setting = currentTemp - 273.15    ' Show Celsius
    RETURN

END
```

### Solar Panel Tracker

```basic
' Solar Panel Sun Tracker
ALIAS panel d0

VAR horizontal = 0
VAR vertical = 0
VAR solarAngle = 0

main:
    solarAngle = panel.SolarAngle

    ' Calculate optimal angles
    horizontal = solarAngle
    vertical = 60    ' Fixed tilt for latitude

    ' Apply to panel
    panel.Horizontal = horizontal
    panel.Vertical = vertical

    YIELD
    GOTO main
END
```

### Airlock Controller

```basic
' Airlock Door Controller
ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3

DEFINE VACUUM_THRESHOLD 1
DEFINE PRESSURIZE_THRESHOLD 90

VAR pressure = 0
VAR state = 0    ' 0=idle, 1=depressurize, 2=pressurize

main:
    pressure = sensor.Pressure

    IF state = 0 THEN
        ' Idle - both doors can be controlled manually
        pump.On = 0
    ELSEIF state = 1 THEN
        ' Depressurizing
        GOSUB Depressurize
    ELSEIF state = 2 THEN
        ' Pressurizing
        GOSUB Pressurize
    ENDIF

    YIELD
    GOTO main

Depressurize:
    innerDoor.Open = 0
    innerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 0    ' Outward

    IF pressure < VACUUM_THRESHOLD THEN
        outerDoor.Lock = 0
        state = 0
    ENDIF
    RETURN

Pressurize:
    outerDoor.Open = 0
    outerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 1    ' Inward

    IF pressure > PRESSURIZE_THRESHOLD THEN
        innerDoor.Lock = 0
        state = 0
    ENDIF
    RETURN

END
```

---

## Best Practices

### Code Organization

1. Start with comments explaining the program
2. Define all aliases at the top
3. Define constants next
4. Declare variables
5. Main loop
6. Subroutines at the end
7. End with END statement

### Performance

1. Use YIELD, not SLEEP 0
2. Cache device reads in variables
3. Use batch operations for multiple devices
4. Avoid unnecessary calculations in loops

### Readability

1. Use meaningful variable names
2. Comment complex logic
3. Use consistent indentation
4. Group related operations

### Reliability

1. Check for edge cases (NaN, division by zero)
2. Use hysteresis to prevent oscillation
3. Include error handling where possible
4. Test with the simulator before deploying

---

## Extensible Device Database

The Device Hash Database can be extended with custom JSON files. This allows you to add new devices from Stationeers game updates without waiting for compiler updates.

### Custom Device File Locations

The compiler checks these locations (in order):

1. `CustomDevices.json` in the compiler's folder
2. Any `.json` file in the `CustomDevices/` subfolder
3. `Documents/BASIC-IC10/CustomDevices/*.json`

### JSON File Format

```json
{
  "devices": [
    {
      "prefabName": "StructureMyDevice",
      "category": "Custom",
      "displayName": "My Device",
      "description": "Description of the device"
    }
  ],
  "logicTypes": [
    {
      "name": "NewProperty",
      "displayName": "New Property",
      "description": "A new logic property"
    }
  ],
  "slotLogicTypes": [
    {
      "name": "NewSlotProperty",
      "displayName": "New Slot Property",
      "description": "A new slot logic property"
    }
  ]
}
```

### Important Notes

- Use `Structure*` prefix for placed structures (not `ItemStructure*`)
- `ItemStructure*` is for items in inventory
- `Structure*` is for built/placed structures that IC10 can address
- Prefab names and hashes can be found in the in-game Stationpedia

### Finding Prefab Names

1. Open the Stationpedia in-game
2. Search for the device
3. Look at the PrefabHash line for the prefab name
4. For placed structures, use the name without "Item" prefix

### Reloading Custom Devices

After adding or modifying JSON files:
- Use **Tools > Reload Custom Devices** in the compiler
- Or restart the application

Custom devices will appear in the Device Hash Database (F4) after reload.
