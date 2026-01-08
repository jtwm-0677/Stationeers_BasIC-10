using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Orientation = System.Windows.Controls.Orientation;

namespace BasicToMips.UI.Services;

public class CodeSnippet
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}

public class Example
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Code { get; set; } = "";
}

public class DocumentationService
{
    #region Start Here Tab - Complete Beginner Guide

    public void PopulateStartHere(StackPanel panel)
    {
        panel.Children.Clear();

        // Welcome Section
        AddHeader(panel, "Welcome to Stationeers Basic-10!", true);
        AddParagraph(panel, "This compiler lets you write programs in BASIC (a beginner-friendly language) and automatically converts them to IC10 MIPS assembly that runs on Stationeers programmable chips.");

        AddHeader(panel, "What is IC10?");
        AddParagraph(panel, "IC10 is the programming language used by Integrated Circuits (ICs) in Stationeers. It's based on MIPS assembly - a low-level language that can be difficult to learn. That's where Basic-10 comes in!");

        AddHeader(panel, "Why Use This Compiler?");
        AddBulletList(panel, new[]
        {
            "Write readable BASIC code instead of cryptic assembly",
            "Automatic variable management (no manual register allocation)",
            "Built-in functions for math, timing, and more",
            "Real-time error checking as you type",
            "One-click deployment to Stationeers"
        });

        AddHeader(panel, "Your First Program");
        AddParagraph(panel, "Let's create a simple program that blinks a light:");
        AddCodeBlock(panel, @"# My first IC10 program - Blink a light
ALIAS light d0    # Connect a light to pin d0

main:
    light.On = 1  # Turn light on
    SLEEP 1       # Wait 1 second
    light.On = 0  # Turn light off
    SLEEP 1       # Wait 1 second
    GOTO main     # Repeat forever
END");

        AddHeader(panel, "Understanding the Code");
        AddBulletList(panel, new[]
        {
            "Lines starting with # are comments (ignored by compiler)",
            "ALIAS creates a friendly name for device pins (d0-d5)",
            "main: is a label - a named location in your code",
            "SLEEP pauses execution (in seconds)",
            "GOTO jumps to a label",
            "END marks the end of your program"
        });

        AddHeader(panel, "The Main Loop Pattern");
        AddParagraph(panel, "Almost every IC10 program uses a main loop that runs forever:");
        AddCodeBlock(panel, @"main:
    # Your code here - runs every tick
    YIELD         # IMPORTANT: Let game process
    GOTO main     # Loop back
END");
        AddWarning(panel, "IMPORTANT: Always include YIELD or SLEEP in loops! Without it, your program will freeze the game.");

        AddHeader(panel, "Connecting Devices");
        AddParagraph(panel, "IC chips have 6 device pins (d0-d5). Connect devices in-game, then reference them in code:");
        AddCodeBlock(panel, @"# Give meaningful names to your pins
ALIAS sensor d0      # Gas sensor on pin 0
ALIAS heater d1      # Wall heater on pin 1
ALIAS display d2     # LED display on pin 2

# Now use the friendly names
VAR temp = sensor.Temperature
heater.On = 1
display.Setting = 42");

        AddHeader(panel, "Reading and Writing Device Properties");
        AddCodeBlock(panel, @"# Reading a property (uses VAR)
VAR temperature = sensor.Temperature
VAR isOn = light.On

# Writing a property (uses =)
light.On = 1          # Turn on
pump.Setting = 100    # Set to 100
door.Open = 0         # Close door");

        AddHeader(panel, "Making Decisions with IF");
        AddCodeBlock(panel, @"IF temperature > 300 THEN
    heater.On = 0     # Too hot, turn off
ELSEIF temperature < 290 THEN
    heater.On = 1     # Too cold, turn on
ELSE
    # Temperature is just right
ENDIF");

        AddHeader(panel, "Quick Start Checklist");
        AddBulletList(panel, new[]
        {
            "1. Write your BASIC code in the top editor",
            "2. Press F5 or click 'Compile' to generate IC10",
            "3. Check for errors in the status bar",
            "4. Click 'Save & Deploy' to send to Stationeers",
            "5. In-game: Load the script on your IC chip"
        });

        AddHeader(panel, "Built-in Constants");
        AddParagraph(panel, "These constants are built into the compiler - use them by name without needing DEFINE:");

        AddSubHeader(panel, "Color Constants (for lights, displays)");
        AddCodeBlock(panel, @"# Use color names directly:
light.Color = Blue      # 0
light.Color = Gray      # 1  (or Grey)
light.Color = Green     # 2
light.Color = Orange    # 3
light.Color = Red       # 4
light.Color = Yellow    # 5
light.Color = White     # 6
light.Color = Black     # 7
light.Color = Brown     # 8
light.Color = Khaki     # 9
light.Color = Pink      # 10
light.Color = Purple    # 11");

        AddSubHeader(panel, "Slot Type Constants");
        AddCodeBlock(panel, @"# For slot operations:
Import   # 0 (also: Input)
Export   # 1 (also: Output)
Content  # 2
Fuel     # 3");

        AddTip(panel, "Press Ctrl+Space for autocomplete suggestions while typing!");
    }

    #endregion

    #region Syntax Tab - Complete Language Reference

    public void PopulateSyntax(StackPanel panel)
    {
        panel.Children.Clear();

        AddHeader(panel, "Complete Syntax Reference", true);

        // Variables Section
        AddHeader(panel, "Variables & Constants");
        AddSyntaxEntry(panel, "VAR name = value", "Declare a variable with initial value", "VAR temperature = 0\nVAR count = 10\nVAR ratio = 0.5");
        AddSyntaxEntry(panel, "CONST name = value", "Declare a compile-time constant (cannot change)", "CONST MAX_TEMP = 373.15\nCONST PI = 3.14159");
        AddSyntaxEntry(panel, "DEFINE name value", "Alternative constant syntax (no = sign)", "DEFINE TARGET_PRESSURE 101\nDEFINE SENSOR_HASH -1234567");
        AddSyntaxEntry(panel, "LET name = value", "Classic BASIC variable assignment", "LET x = 5\nLET result = x + 10");

        // Device Section
        AddHeader(panel, "Device Declarations");
        AddSyntaxEntry(panel, "ALIAS name device", "Create named alias for device pin", "ALIAS sensor d0\nALIAS pump d1");
        AddSyntaxEntry(panel, "ALIAS name THIS", "Alias for the IC chip itself (db)", "ALIAS chip THIS");
        AddSyntaxEntry(panel, "DEVICE name \"PrefabName\"", "Named device reference (bypasses pin limit)", "DEVICE sensor \"StructureGasSensor\"\nDEVICE furnace \"StructureFurnace\"");

        // Device Access
        AddHeader(panel, "Device Property Access");
        AddSyntaxEntry(panel, "device.Property", "Read a device property", "VAR t = sensor.Temperature\nVAR p = sensor.Pressure");
        AddSyntaxEntry(panel, "device.Property = value", "Write a device property", "pump.On = 1\npump.Setting = 100");
        AddSyntaxEntry(panel, "device.Slot[n].Property", "Access device slot property", "VAR hash = sorter.Slot[0].OccupantHash");

        // Labels and Jumps
        AddHeader(panel, "Labels & Jumps");
        AddSyntaxEntry(panel, "labelname:", "Define a label (jump target)", "main:\n     #code here\nstart:\n     #more code");
        AddSyntaxEntry(panel, "GOTO label", "Unconditional jump to label", "GOTO main\nGOTO errorHandler");
        AddSyntaxEntry(panel, "GOSUB label", "Call subroutine (saves return address)", "GOSUB calculateTemp\nGOSUB updateDisplay");
        AddSyntaxEntry(panel, "RETURN", "Return from subroutine", "myFunction:\n     #do stuff\n    RETURN");

        // Conditionals
        AddHeader(panel, "Conditional Statements");
        AddSyntaxEntry(panel, "IF condition THEN ... ENDIF", "Simple conditional block", "IF temp > 100 THEN\n    heater.On = 0\nENDIF");
        AddSyntaxEntry(panel, "IF ... ELSE ... ENDIF", "Conditional with else branch", "IF temp > 100 THEN\n    heater.On = 0\nELSE\n    heater.On = 1\nENDIF");
        AddSyntaxEntry(panel, "IF ... ELSEIF ... ELSE ... ENDIF", "Multiple conditions", "IF temp > 100 THEN\n    mode = 0\nELSEIF temp < 50 THEN\n    mode = 1\nELSE\n    mode = 2\nENDIF");
        AddSyntaxEntry(panel, "SELECT CASE ... END SELECT", "Switch statement", "SELECT CASE mode\n    CASE 0\n         #handle 0\n    CASE 1\n         #handle 1\n    DEFAULT\n         #default\nEND SELECT");

        // Loops
        AddHeader(panel, "Loops");
        AddSyntaxEntry(panel, "WHILE condition ... WEND", "While loop (condition at start)", "WHILE temp < 100\n    heater.On = 1\n    YIELD\nWEND");
        AddSyntaxEntry(panel, "DO ... LOOP UNTIL condition", "Do-until loop (condition at end)", "DO\n    pump.On = 1\n    YIELD\nLOOP UNTIL pressure > 100");
        AddSyntaxEntry(panel, "FOR var = start TO end ... NEXT", "Counting loop", "FOR i = 1 TO 10\n    display.Setting = i\n    YIELD\nNEXT i");
        AddSyntaxEntry(panel, "FOR var = start TO end STEP n", "Loop with custom step", "FOR i = 10 TO 0 STEP -1\n     #countdown\nNEXT i");
        AddSyntaxEntry(panel, "BREAK", "Exit loop early", "WHILE 1\n    IF done THEN BREAK\n    YIELD\nWEND");
        AddSyntaxEntry(panel, "CONTINUE", "Skip to next iteration", "FOR i = 1 TO 10\n    IF skip THEN CONTINUE\n     #process\nNEXT i");

        // Control Flow
        AddHeader(panel, "Program Control");
        AddSyntaxEntry(panel, "YIELD", "Yield one game tick (required in loops)", "main:\n     #code\n    YIELD\n    GOTO main");
        AddSyntaxEntry(panel, "SLEEP seconds", "Pause execution for time", "SLEEP 0.5\nSLEEP 2");
        AddSyntaxEntry(panel, "WAIT(seconds)", "Alternative sleep syntax", "WAIT(1)\nWAIT(0.25)");
        AddSyntaxEntry(panel, "END", "End program execution", "IF error THEN END");

        // Operators
        AddHeader(panel, "Operators");
        AddSubHeader(panel, "Arithmetic Operators");
        AddCompactList(panel, new[]
        {
            ("a + b", "Addition"),
            ("a - b", "Subtraction"),
            ("a * b", "Multiplication"),
            ("a / b", "Division"),
            ("a MOD b", "Modulo (remainder)"),
            ("a ^ b", "Power/Exponent"),
            ("-a", "Negation")
        });

        AddSubHeader(panel, "Comparison Operators");
        AddCompactList(panel, new[]
        {
            ("a = b or a == b", "Equal to"),
            ("a <> b or a != b", "Not equal to"),
            ("a < b", "Less than"),
            ("a > b", "Greater than"),
            ("a <= b", "Less than or equal"),
            ("a >= b", "Greater than or equal")
        });

        AddSubHeader(panel, "Logical Operators");
        AddCompactList(panel, new[]
        {
            ("a AND b or a && b", "Logical AND"),
            ("a OR b or a || b", "Logical OR"),
            ("NOT a or !a", "Logical NOT")
        });

        AddSubHeader(panel, "Bitwise Operators");
        AddCompactList(panel, new[]
        {
            ("a & b or BAND(a,b)", "Bitwise AND"),
            ("a | b or BOR(a,b)", "Bitwise OR"),
            ("a ^ b or BXOR(a,b)", "Bitwise XOR"),
            ("~a or BNOT(a)", "Bitwise NOT"),
            ("a << n or SHL(a,n)", "Shift left n bits"),
            ("a >> n or SHR(a,n)", "Shift right n bits")
        });
        AddCodeBlock(panel, @"VAR a = 1
VAR b = a << 4    # b = 16 (shift left 4 bits)
VAR c = 16 >> 2   # c = 4 (shift right 2 bits)
VAR d = 5 ^ 3     # d = 6 (XOR: 101 ^ 011 = 110)");

        AddSubHeader(panel, "Compound Assignment Operators");
        AddCompactList(panel, new[]
        {
            ("x += n", "Add n to x (x = x + n)"),
            ("x -= n", "Subtract n from x (x = x - n)"),
            ("x *= n", "Multiply x by n (x = x * n)"),
            ("x /= n", "Divide x by n (x = x / n)")
        });
        AddCodeBlock(panel, @"VAR x = 10
x += 5    # x is now 15
x -= 3    # x is now 12
x *= 2    # x is now 24
x /= 4    # x is now 6");

        AddSubHeader(panel, "Increment/Decrement Operators");
        AddCompactList(panel, new[]
        {
            ("++x", "Prefix increment (increment first, return new value)"),
            ("x++", "Postfix increment (return old value, then increment)"),
            ("--x", "Prefix decrement (decrement first, return new value)"),
            ("x--", "Postfix decrement (return old value, then decrement)")
        });
        AddCodeBlock(panel, @"VAR x = 10
VAR y = ++x    # x=11, y=11 (prefix: increment first)
VAR z = x++    # x=12, z=11 (postfix: return old value)

# Standalone usage in loops
VAR i = 0
WHILE i < 5
    ++i    # Works as statement
WEND");

        // Comments
        AddHeader(panel, "Comments");
        AddSyntaxEntry(panel, "# comment", "Comment (preserved in IC10 output)", "# This is a comment\nVAR x = 5  # inline comment");
        AddSyntaxEntry(panel, "REM comment", "REM keyword comment (ignored)", "REM This is also a comment");

        // Subroutines
        AddHeader(panel, "Subroutines & Functions");
        AddSyntaxEntry(panel, "SUB name ... END SUB", "Define a subroutine", "SUB UpdateDisplay\n    display.Setting = temp\nEND SUB");
        AddSyntaxEntry(panel, "FUNCTION name ... END FUNCTION", "Define a function", "FUNCTION GetAverage\n    RETURN (a + b) / 2\nEND FUNCTION");
        AddSyntaxEntry(panel, "CALL name", "Call a subroutine", "CALL UpdateDisplay\nCALL ProcessInput");
        AddSyntaxEntry(panel, "EXIT SUB / EXIT FUNCTION", "Exit early from sub/function", "IF error THEN EXIT SUB");
    }

    #endregion

    #region Functions Tab - All Built-in Functions

    public void PopulateFunctions(StackPanel panel)
    {
        panel.Children.Clear();

        AddHeader(panel, "Built-in Functions Reference", true);

        // Math Functions
        AddHeader(panel, "Math Functions");
        AddFunctionEntry(panel, "ABS(x)", "Returns the absolute value of x", "VAR distance = ABS(target - current)\n#ABS(-5) = 5, ABS(5) = 5");
        AddFunctionEntry(panel, "SQRT(x)", "Returns the square root of x", "VAR length = SQRT(x*x + y*y)\n#SQRT(16) = 4, SQRT(2) = 1.414...");
        AddFunctionEntry(panel, "MIN(a, b)", "Returns the smaller of two values", "VAR clamped = MIN(value, 100)\n#MIN(5, 3) = 3");
        AddFunctionEntry(panel, "MAX(a, b)", "Returns the larger of two values", "VAR clamped = MAX(value, 0)\n#MAX(5, 3) = 5");
        AddFunctionEntry(panel, "CEIL(x)", "Rounds up to nearest integer", "VAR slots = CEIL(items / stackSize)\n#CEIL(3.1) = 4, CEIL(-3.1) = -3");
        AddFunctionEntry(panel, "FLOOR(x)", "Rounds down to nearest integer", "VAR whole = FLOOR(value)\n#FLOOR(3.9) = 3, FLOOR(-3.9) = -4");
        AddFunctionEntry(panel, "ROUND(x)", "Rounds to nearest integer", "VAR rounded = ROUND(temp)\n#ROUND(3.5) = 4, ROUND(3.4) = 3");
        AddFunctionEntry(panel, "TRUNC(x)", "Truncates decimal part (towards zero)", "VAR integer = TRUNC(value)\n#TRUNC(3.9) = 3, TRUNC(-3.9) = -3");
        AddFunctionEntry(panel, "SGN(x)", "Returns sign: -1, 0, or 1", "VAR direction = SGN(velocity)\n#SGN(-5) = -1, SGN(0) = 0, SGN(5) = 1");
        AddFunctionEntry(panel, "RND() or RAND", "Random number between 0 and 1", "VAR chance = RND()\nIF RND() < 0.5 THEN  #50% chance");

        // Trigonometry
        AddHeader(panel, "Trigonometry (all angles in RADIANS)");
        AddTip(panel, "To convert degrees to radians: radians = degrees * 3.14159 / 180");
        AddFunctionEntry(panel, "SIN(x)", "Sine of angle x", "VAR y = SIN(angle)\n#SIN(0) = 0, SIN(PI/2) = 1");
        AddFunctionEntry(panel, "COS(x)", "Cosine of angle x", "VAR x = COS(angle)\n#COS(0) = 1, COS(PI/2) = 0");
        AddFunctionEntry(panel, "TAN(x)", "Tangent of angle x", "VAR slope = TAN(angle)\n#TAN(0) = 0, TAN(PI/4) = 1");
        AddFunctionEntry(panel, "ASIN(x)", "Inverse sine (returns radians)", "VAR angle = ASIN(ratio)\n#ASIN(1) = PI/2");
        AddFunctionEntry(panel, "ACOS(x)", "Inverse cosine (returns radians)", "VAR angle = ACOS(ratio)\n#ACOS(1) = 0");
        AddFunctionEntry(panel, "ATAN(x)", "Inverse tangent (returns radians)", "VAR angle = ATAN(slope)\n#ATAN(1) = PI/4");
        AddFunctionEntry(panel, "ATAN2(y, x)", "Two-argument arctangent (handles all quadrants)", "VAR heading = ATAN2(dy, dx)\n#Better than ATAN for direction calculations");

        // Exponential & Logarithmic
        AddHeader(panel, "Exponential & Logarithmic");
        AddFunctionEntry(panel, "EXP(x)", "e raised to power x (e^x)", "VAR growth = EXP(rate * time)\n#EXP(1) = 2.718..., EXP(0) = 1");
        AddFunctionEntry(panel, "LOG(x)", "Natural logarithm (base e)", "VAR decay = LOG(value)\n#LOG(2.718) ≈ 1, LOG(1) = 0");

        // Control Functions
        AddHeader(panel, "Control Functions");
        AddFunctionEntry(panel, "YIELD", "Pause for one game tick. REQUIRED in all loops!", "main:\n     #process\n    YIELD    #Let game update\n    GOTO main");
        AddFunctionEntry(panel, "SLEEP n", "Pause for n seconds", "SLEEP 0.5    #Half second\nSLEEP 2      #Two seconds");
        AddFunctionEntry(panel, "WAIT(n)", "Same as SLEEP (alternate syntax)", "WAIT(1)      #One second\nWAIT(0.25)   #Quarter second");
        AddFunctionEntry(panel, "END", "Stop program execution", "IF fatalError THEN END");

        // Stack Operations
        AddHeader(panel, "Stack Operations");
        AddParagraph(panel, "The stack is a 512-value storage area. Think of it like a stack of plates - last in, first out.");
        AddFunctionEntry(panel, "PUSH value", "Push a value onto the stack", "PUSH currentState\nPUSH 42");
        AddFunctionEntry(panel, "POP variable", "Pop value from stack into variable", "POP savedState\nPOP result");
        AddFunctionEntry(panel, "PEEK variable", "Read top value without removing it", "PEEK topValue\n#Stack unchanged");

        // Batch Operations
        AddHeader(panel, "Batch Operations");
        AddParagraph(panel, "Batch operations let you read/write ALL devices of a type at once, without using device pins!");
        AddFunctionEntry(panel, "BATCHREAD(hash, property, mode)", "Read from all devices matching hash", "# Get average temperature from all sensors\nVAR avgTemp = BATCHREAD(SENSOR_HASH, Temperature, 0)");
        AddFunctionEntry(panel, "BATCHWRITE(hash, property, value)", "Write to all devices matching hash", "#Turn off all lights\nBATCHWRITE(LIGHT_HASH, On, 0)");

        AddSubHeader(panel, "Batch Read Modes:");
        AddCompactList(panel, new[]
        {
            ("Mode 0 - Average", "Average of all values"),
            ("Mode 1 - Sum", "Total of all values"),
            ("Mode 2 - Minimum", "Lowest value found"),
            ("Mode 3 - Maximum", "Highest value found")
        });
        AddCodeBlock(panel, @"DEFINE BATTERY_HASH -1388288459
' Get minimum charge (find the most depleted battery)
VAR lowestCharge = BATCHREAD(BATTERY_HASH, Charge, 2)
' Get total power from all batteries
VAR totalPower = BATCHREAD(BATTERY_HASH, PowerGeneration, 1)");
    }

    #endregion

    #region Devices Tab - Device Properties & Operations

    public void PopulateDevices(StackPanel panel)
    {
        panel.Children.Clear();

        AddHeader(panel, "Device Reference", true);

        // Device Pins
        AddHeader(panel, "Device Pins");
        AddParagraph(panel, "Each IC chip has 6 device pins (d0-d5) plus special pins:");
        AddCompactList(panel, new[]
        {
            ("d0, d1, d2, d3, d4, d5", "Standard device pins (connect in-game)"),
            ("db", "The IC housing/chip itself"),
            ("THIS", "Alias for db (use with ALIAS)"),
        });
        AddCodeBlock(panel, "ALIAS sensor d0      #Device on pin 0\nALIAS chip THIS      #The IC chip itself\nVAR myHash = chip.PrefabHash");

        // Common Properties
        AddHeader(panel, "Universal Properties (Most Devices)");
        AddPropertyList(panel, new[]
        {
            ("On", "0/1", "R/W", "Device on/off state"),
            ("Power", "Watts", "R", "Current power consumption/generation"),
            ("Error", "0/1", "R", "Device error state"),
            ("Lock", "0/1", "R/W", "Lock state (prevents interaction)"),
            ("PrefabHash", "integer", "R", "Device type identifier"),
            ("ReferenceId", "integer", "R", "Unique device ID"),
            ("NameHash", "integer", "R", "Hash of custom name")
        });

        // Sensor Properties
        AddHeader(panel, "Atmosphere Sensors");
        AddPropertyList(panel, new[]
        {
            ("Temperature", "Kelvin", "R", "Gas temperature (K = C + 273.15)"),
            ("Pressure", "kPa", "R", "Total gas pressure"),
            ("RatioOxygen", "0-1", "R", "Oxygen ratio in mix"),
            ("RatioCarbonDioxide", "0-1", "R", "CO2 ratio in mix"),
            ("RatioNitrogen", "0-1", "R", "Nitrogen ratio in mix"),
            ("RatioNitrousOxide", "0-1", "R", "N2O ratio"),
            ("RatioPollutant", "0-1", "R", "Pollutant (X) ratio"),
            ("RatioVolatiles", "0-1", "R", "Volatiles ratio"),
            ("RatioWater", "0-1", "R", "Water vapor ratio"),
            ("TotalMoles", "moles", "R", "Total gas quantity")
        });
        AddTip(panel, "Temperature Conversions:\n- Kelvin to Celsius: C = K - 273.15\n- Celsius to Kelvin: K = C + 273.15\n- Room temp: ~293K (20C)");

        // Valve/Pump Properties
        AddHeader(panel, "Valves, Pumps & Vents");
        AddPropertyList(panel, new[]
        {
            ("Setting", "varies", "R/W", "Target pressure/volume"),
            ("Mode", "0-2", "R/W", "Operating mode"),
            ("Open", "0/1", "R/W", "Open/closed state"),
            ("Ratio", "0-1", "R", "Current flow ratio")
        });
        AddCodeBlock(panel, "#Active Vent Modes:\n#Mode 0 = Outward (default)\n#Mode 1 = Inward\nvent.Mode = 1    #Pull air in\nvent.On = 1");

        // Power Properties
        AddHeader(panel, "Power Devices");
        AddPropertyList(panel, new[]
        {
            ("Charge", "0-1", "R", "Battery charge level (0-100%)"),
            ("PowerGeneration", "Watts", "R", "Current power output"),
            ("PowerRequired", "Watts", "R", "Power needed by device"),
            ("PowerActual", "Watts", "R", "Power actually received"),
            ("SolarAngle", "degrees", "R", "Sun angle (solar panels)")
        });
        AddCodeBlock(panel, "#Solar tracking\npanel.Horizontal = panel.SolarAngle\npanel.Vertical = 60    #Typical tilt");

        // Display Properties
        AddHeader(panel, "Displays & Lights");
        AddPropertyList(panel, new[]
        {
            ("Setting", "number", "R/W", "Display value"),
            ("Color", "integer", "R/W", "RGB color (decimal)"),
            ("Brightness", "0-1", "R/W", "Light intensity"),
            ("On", "0/1", "R/W", "Light on/off")
        });
        AddCodeBlock(panel, "#Common colors (decimal RGB)\nDEFINE RED 16711680       ##FF0000\nDEFINE GREEN 65280        ##00FF00\nDEFINE BLUE 255           ##0000FF\nDEFINE YELLOW 16776960    ##FFFF00\nDEFINE WHITE 16777215     ##FFFFFF\n\nlight.Color = RED\nlight.On = 1");

        // Logic I/O
        AddHeader(panel, "Logic I/O (Buttons, Dials, Levers)");
        AddPropertyList(panel, new[]
        {
            ("Setting", "varies", "R/W", "Current value"),
            ("Activate", "0/1", "R", "Momentary activation"),
        });
        AddCodeBlock(panel, "#Button edge detection\nVAR lastButton = 0\nVAR currentButton = button.Setting\nIF currentButton = 1 AND lastButton = 0 THEN\n     #Button just pressed!\nENDIF\nlastButton = currentButton");

        // Sorting/Manufacturing
        AddHeader(panel, "Sorters & Manufacturing");
        AddPropertyList(panel, new[]
        {
            ("Occupied", "0/1", "R", "Has item inside"),
            ("OccupantHash", "integer", "R", "Hash of item inside"),
            ("Quantity", "integer", "R", "Stack quantity"),
            ("Mode", "0-2", "R/W", "Output mode/direction"),
            ("ExportCount", "integer", "R/W", "Items to export"),
            ("ImportCount", "integer", "R/W", "Items to import")
        });

        // Slot Operations
        AddHeader(panel, "Slot Operations");
        AddParagraph(panel, "Many devices have slots (inventories) you can read:");
        AddCodeBlock(panel, "#Read item in slot 0 of a device\nVAR hash = device.Slot[0].OccupantHash\nVAR qty = device.Slot[0].Quantity\nVAR maxQty = device.Slot[0].MaxQuantity\n\n#Check if slot is occupied\nVAR hasItem = device.Slot[0].Occupied");

        // Named Device References
        AddHeader(panel, "Named Device References (Advanced)");
        AddParagraph(panel, "Bypass the 6-pin limit by referencing devices by their prefab name:");
        AddCodeBlock(panel, "#Reference devices by type name\nDEVICE sensor \"StructureGasSensor\"\nDEVICE furnace \"StructureFurnace\"\nDEVICE battery \"StructureBatteryLarge\"\n\n#Use like regular aliases\nVAR temp = sensor.Temperature\nfurnace.On = 1");
        AddWarning(panel, "Named devices must be in the same network/room as the IC chip!");

        // Device Hashes
        AddHeader(panel, "Finding Device Hashes");
        AddParagraph(panel, "Use Tools > Device Hash Database to look up device hashes, or read them in-game:");
        AddCodeBlock(panel, "#Read a device's hash\nVAR hash = sensor.PrefabHash\n#Then use for batch operations\nVAR avgTemp = BATCHREAD(hash, Temperature, 0)");
    }

    #endregion

    #region IC10 Reference Tab - MIPS Instructions

    public void PopulateIC10Reference(StackPanel panel)
    {
        panel.Children.Clear();

        AddHeader(panel, "IC10 MIPS Instruction Reference", true);
        AddParagraph(panel, "This reference shows the IC10 assembly that your BASIC code compiles to. Understanding IC10 helps with debugging and optimization.");

        // Registers
        AddHeader(panel, "Registers");
        AddCompactList(panel, new[]
        {
            ("r0 - r15", "General purpose registers (16 total)"),
            ("sp", "Stack pointer"),
            ("ra", "Return address (for subroutines)"),
            ("d0 - d5", "Device references"),
            ("db", "IC housing device")
        });

        // Basic Operations
        AddHeader(panel, "Math Operations");
        AddIC10Entry(panel, "add r0 r1 r2", "r0 = r1 + r2", "Addition");
        AddIC10Entry(panel, "sub r0 r1 r2", "r0 = r1 - r2", "Subtraction");
        AddIC10Entry(panel, "mul r0 r1 r2", "r0 = r1 * r2", "Multiplication");
        AddIC10Entry(panel, "div r0 r1 r2", "r0 = r1 / r2", "Division");
        AddIC10Entry(panel, "mod r0 r1 r2", "r0 = r1 MOD r2", "Modulo");
        AddIC10Entry(panel, "exp r0 r1", "r0 = e^r1", "Exponential");
        AddIC10Entry(panel, "log r0 r1", "r0 = ln(r1)", "Natural log");
        AddIC10Entry(panel, "sqrt r0 r1", "r0 = sqrt(r1)", "Square root");
        AddIC10Entry(panel, "abs r0 r1", "r0 = |r1|", "Absolute value");
        AddIC10Entry(panel, "round r0 r1", "r0 = round(r1)", "Round to integer");
        AddIC10Entry(panel, "trunc r0 r1", "r0 = trunc(r1)", "Truncate");
        AddIC10Entry(panel, "ceil r0 r1", "r0 = ceil(r1)", "Round up");
        AddIC10Entry(panel, "floor r0 r1", "r0 = floor(r1)", "Round down");
        AddIC10Entry(panel, "min r0 r1 r2", "r0 = min(r1,r2)", "Minimum");
        AddIC10Entry(panel, "max r0 r1 r2", "r0 = max(r1,r2)", "Maximum");
        AddIC10Entry(panel, "rand r0", "r0 = random(0-1)", "Random number");

        // Trig
        AddHeader(panel, "Trigonometry");
        AddIC10Entry(panel, "sin r0 r1", "r0 = sin(r1)", "Sine (radians)");
        AddIC10Entry(panel, "cos r0 r1", "r0 = cos(r1)", "Cosine");
        AddIC10Entry(panel, "tan r0 r1", "r0 = tan(r1)", "Tangent");
        AddIC10Entry(panel, "asin r0 r1", "r0 = asin(r1)", "Inverse sine");
        AddIC10Entry(panel, "acos r0 r1", "r0 = acos(r1)", "Inverse cosine");
        AddIC10Entry(panel, "atan r0 r1", "r0 = atan(r1)", "Inverse tangent");
        AddIC10Entry(panel, "atan2 r0 r1 r2", "r0 = atan2(r1,r2)", "Two-arg atan");

        // Logic
        AddHeader(panel, "Logic & Comparison");
        AddIC10Entry(panel, "and r0 r1 r2", "r0 = r1 AND r2", "Bitwise AND");
        AddIC10Entry(panel, "or r0 r1 r2", "r0 = r1 OR r2", "Bitwise OR");
        AddIC10Entry(panel, "xor r0 r1 r2", "r0 = r1 XOR r2", "Bitwise XOR");
        AddIC10Entry(panel, "nor r0 r1 r2", "r0 = NOT(r1 OR r2)", "NOR");
        AddIC10Entry(panel, "not r0 r1", "r0 = NOT r1", "Bitwise NOT");
        AddIC10Entry(panel, "sll r0 r1 r2", "r0 = r1 << r2", "Shift left logical");
        AddIC10Entry(panel, "srl r0 r1 r2", "r0 = r1 >> r2", "Shift right logical");
        AddIC10Entry(panel, "sra r0 r1 r2", "r0 = r1 >>> r2", "Shift right arithmetic");
        AddIC10Entry(panel, "slt r0 r1 r2", "r0 = (r1 < r2)", "Set if less than");
        AddIC10Entry(panel, "sgt r0 r1 r2", "r0 = (r1 > r2)", "Set if greater");
        AddIC10Entry(panel, "sle r0 r1 r2", "r0 = (r1 <= r2)", "Set if less/equal");
        AddIC10Entry(panel, "sge r0 r1 r2", "r0 = (r1 >= r2)", "Set if greater/equal");
        AddIC10Entry(panel, "seq r0 r1 r2", "r0 = (r1 == r2)", "Set if equal");
        AddIC10Entry(panel, "sne r0 r1 r2", "r0 = (r1 != r2)", "Set if not equal");

        // Branching
        AddHeader(panel, "Branching & Jumps");
        AddIC10Entry(panel, "j label", "goto label", "Unconditional jump");
        AddIC10Entry(panel, "jr r0", "goto address in r0", "Jump to register");
        AddIC10Entry(panel, "jal label", "call subroutine", "Jump and link (saves ra)");
        AddIC10Entry(panel, "beq r0 r1 label", "if r0==r1 goto", "Branch if equal");
        AddIC10Entry(panel, "bne r0 r1 label", "if r0!=r1 goto", "Branch if not equal");
        AddIC10Entry(panel, "blt r0 r1 label", "if r0<r1 goto", "Branch if less than");
        AddIC10Entry(panel, "bgt r0 r1 label", "if r0>r1 goto", "Branch if greater");
        AddIC10Entry(panel, "ble r0 r1 label", "if r0<=r1 goto", "Branch if less/equal");
        AddIC10Entry(panel, "bge r0 r1 label", "if r0>=r1 goto", "Branch if greater/equal");

        // Device I/O
        AddHeader(panel, "Device Operations");
        AddIC10Entry(panel, "l r0 d0 Property", "r0 = d0.Property", "Load from device");
        AddIC10Entry(panel, "s d0 Property r0", "d0.Property = r0", "Store to device");
        AddIC10Entry(panel, "ls r0 d0 slot Prop", "r0 = d0.Slot(n).Prop", "Load slot property");
        AddIC10Entry(panel, "lb r0 hash Prop mode", "batch read", "Load batch");
        AddIC10Entry(panel, "sb hash Prop r0", "batch write", "Store batch");

        // Stack & Memory
        AddHeader(panel, "Stack Operations");
        AddIC10Entry(panel, "push r0", "stack.push(r0)", "Push to stack");
        AddIC10Entry(panel, "pop r0", "r0 = stack.pop()", "Pop from stack");
        AddIC10Entry(panel, "peek r0", "r0 = stack.peek()", "Peek stack top");

        // Special
        AddHeader(panel, "Special Instructions");
        AddIC10Entry(panel, "move r0 r1", "r0 = r1", "Copy value");
        AddIC10Entry(panel, "yield", "pause 1 tick", "Yield execution");
        AddIC10Entry(panel, "sleep r0", "pause r0 seconds", "Sleep for time");
        AddIC10Entry(panel, "alias name d0", "name = d0", "Create alias");
        AddIC10Entry(panel, "define name value", "const name = value", "Define constant");

        AddTip(panel, "IC10 has a 128-line limit. The compiler optimizes to fit within this constraint.");
    }

    #endregion

    #region Patterns Tab - Tips, Tricks & Common Patterns

    public void PopulatePatterns(StackPanel panel)
    {
        panel.Children.Clear();

        AddHeader(panel, "Tips, Tricks & Patterns", true);

        // Essential Patterns
        AddHeader(panel, "Essential Patterns");

        AddSubHeader(panel, "The Main Loop");
        AddParagraph(panel, "Every IC10 program needs a main loop that runs continuously:");
        AddCodeBlock(panel, @"main:
     #Read sensors
    VAR temp = sensor.Temperature

     #Make decisions
    IF temp > 300 THEN
        heater.On = 0
    ENDIF

     #IMPORTANT: Always yield!
    YIELD
    GOTO main
END");

        AddSubHeader(panel, "Hysteresis (Prevent Rapid Switching)");
        AddParagraph(panel, "Without hysteresis, devices turn on/off rapidly around the threshold. Add a dead band:");
        AddCodeBlock(panel, @"CONST TARGET = 100
CONST TOLERANCE = 5     #Dead band of +/- 5

main:
    VAR value = sensor.Setting

    IF value < TARGET - TOLERANCE THEN
        device.On = 1     #Turn on when well below target
    ELSEIF value > TARGET + TOLERANCE THEN
        device.On = 0     #Turn off when well above target
    ENDIF
     #If between TARGET-5 and TARGET+5, keep current state

    YIELD
    GOTO main");

        AddSubHeader(panel, "Edge Detection (Detect Button Press)");
        AddParagraph(panel, "Detect when a button is pressed (not just held):");
        AddCodeBlock(panel, @"VAR lastButton = 0

main:
    VAR currentButton = button.Setting

    IF currentButton = 1 AND lastButton = 0 THEN
         #Button was JUST pressed (rising edge)
         #Do something once
    ENDIF

    lastButton = currentButton
    YIELD
    GOTO main");

        AddSubHeader(panel, "State Machine");
        AddParagraph(panel, "For complex multi-step processes (like airlocks):");
        AddCodeBlock(panel, @"VAR state = 0

main:
    IF state = 0 THEN
        GOSUB IdleState
    ELSEIF state = 1 THEN
        GOSUB ProcessingState
    ELSEIF state = 2 THEN
        GOSUB CompleteState
    ENDIF

    YIELD
    GOTO main

IdleState:
     #Wait for trigger
    IF trigger = 1 THEN state = 1
    RETURN

ProcessingState:
     #Do work
    IF done = 1 THEN state = 2
    RETURN

CompleteState:
     #Cleanup then reset
    state = 0
    RETURN");

        // Optimization Tips
        AddHeader(panel, "Optimization Tips");

        AddSubHeader(panel, "Minimize Device Reads");
        AddParagraph(panel, "Each device read takes time. Read once and reuse:");
        AddCodeBlock(panel, @"#BAD - reads temperature 3 times
IF sensor.Temperature > 100 THEN
ELSEIF sensor.Temperature > 50 THEN
ELSEIF sensor.Temperature > 0 THEN
ENDIF

' GOOD - reads once
VAR temp = sensor.Temperature
IF temp > 100 THEN
ELSEIF temp > 50 THEN
ELSEIF temp > 0 THEN
ENDIF");

        AddSubHeader(panel, "Use Constants for Magic Numbers");
        AddCodeBlock(panel, @"#BAD - what do these numbers mean?
IF temp > 373.15 THEN
IF pressure < 101 THEN

' GOOD - self-documenting
CONST BOILING_POINT = 373.15
CONST NORMAL_PRESSURE = 101

IF temp > BOILING_POINT THEN
IF pressure < NORMAL_PRESSURE THEN");

        AddSubHeader(panel, "Use Batch Operations for Many Devices");
        AddParagraph(panel, "Instead of wiring 6 lights individually:");
        AddCodeBlock(panel, @"#Instead of:
light1.On = 1
light2.On = 1
light3.On = 1
' ... limited to 6

' Use batch operations:
DEFINE LIGHT_HASH -1234567890
BATCHWRITE(LIGHT_HASH, On, 1)   #All lights!");

        AddSubHeader(panel, "Using Compound Assignment");
        AddParagraph(panel, "Use += and -= for accumulators and counters:");
        AddCodeBlock(panel, @"VAR total = 0
VAR count = 0

main:
    total += sensor.Reading    # Accumulate readings
    count += 1                 # Count iterations

    VAR average = total / count
    YIELD
    GOTO main");

        AddSubHeader(panel, "Prefix vs Postfix Increment");
        AddParagraph(panel, "Use prefix (++x) when you only need to increment. Use postfix (x++) when you need the old value first:");
        AddCodeBlock(panel, @"# Prefix - increment first, then use
VAR i = 0
WHILE i < 5
    ++i    # Just incrementing
WEND

# Postfix - use old value, then increment
VAR index = 0
display.Setting = index++    # Shows 0, then index becomes 1");

        AddSubHeader(panel, "Bit Shifting for Efficiency");
        AddParagraph(panel, "Use bit shifts for power-of-2 math and flag manipulation:");
        AddCodeBlock(panel, @"# Fast multiply/divide by powers of 2
x = x << 1    # Same as x * 2
x = x >> 2    # Same as x / 4

# Flag manipulation
flags = flags | (1 << bitNum)     # Set bit
flags = flags & ~(1 << bitNum)    # Clear bit
isSet = (flags >> bitNum) & 1     # Check bit");

        // Common Mistakes
        AddHeader(panel, "Common Mistakes to Avoid");

        AddWarning(panel, "Forgetting YIELD in loops");
        AddCodeBlock(panel, @"#BAD - will freeze the game!
WHILE 1
     #no yield
WEND

' GOOD
WHILE 1
    YIELD
WEND");

        AddWarning(panel, "Integer division gotcha");
        AddCodeBlock(panel, @"#All numbers are floating point in IC10
VAR result = 5 / 2     #= 2.5 (not 2)

' To get integer division:
VAR intResult = FLOOR(5 / 2)   #= 2");

        AddWarning(panel, "Comparing floating point values");
        AddCodeBlock(panel, @"#BAD - may fail due to precision
IF value = 0.3 THEN

' GOOD - use small tolerance
IF ABS(value - 0.3) < 0.001 THEN");

        // Useful Formulas
        AddHeader(panel, "Useful Formulas");
        AddCodeBlock(panel, @"#Clamp value between min and max
VAR clamped = MAX(minVal, MIN(maxVal, value))

' Linear interpolation (lerp)
VAR lerped = a + (b - a) * t    #t from 0 to 1

' Map value from one range to another
VAR mapped = (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin

' Celsius to Kelvin
VAR kelvin = celsius + 273.15

' Kelvin to Celsius
VAR celsius = kelvin - 273.15

' Percentage to ratio
VAR ratio = percent / 100

' Ratio to percentage
VAR percent = ratio * 100");

        // Debug Tips
        AddHeader(panel, "Debugging Tips");
        AddBulletList(panel, new[]
        {
            "Use LED displays to show variable values",
            "Use lights with colors for status indicators",
            "Add comments to track your logic",
            "Test one feature at a time",
            "Check the IC10 output to verify compilation",
            "Enable 'Include Source Line Numbers' in Build menu"
        });
    }

    #endregion

    #region Examples Tab

    public void PopulateExamples(StackPanel panel, Action<string> loadCallback)
    {
        panel.Children.Clear();

        var examples = GetExamples();

        foreach (var example in examples)
        {
            var expander = new Expander
            {
                Header = example.Name,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var content = new StackPanel { Margin = new Thickness(8) };

            content.Children.Add(new TextBlock
            {
                Text = example.Description,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
                Margin = new Thickness(0, 0, 0, 8)
            });

            var loadButton = new Button
            {
                Content = "Load Example",
                Style = (Style)Application.Current.FindResource("ModernButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 4, 12, 4)
            };
            loadButton.Click += (s, e) => loadCallback(example.Code);
            content.Children.Add(loadButton);

            expander.Content = content;
            panel.Children.Add(expander);
        }
    }

    #endregion

    #region Helper Methods - UI Building

    private void AddHeader(StackPanel panel, string text, bool isMainHeader = false)
    {
        panel.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.Bold,
            FontSize = isMainHeader ? 16 : 13,
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            Margin = new Thickness(0, isMainHeader ? 0 : 12, 0, 6)
        });
    }

    private void AddSubHeader(StackPanel panel, string text)
    {
        panel.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Foreground = (Brush)Application.Current.FindResource("PrimaryTextBrush"),
            Margin = new Thickness(0, 8, 0, 4)
        });
    }

    private void AddParagraph(StackPanel panel, string text)
    {
        panel.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
            Margin = new Thickness(0, 0, 0, 6),
            FontSize = 11
        });
    }

    private void AddCodeBlock(StackPanel panel, string code)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 4, 0, 8)
        };

        border.Child = new TextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 220, 254)),
            TextWrapping = TextWrapping.Wrap
        };

        panel.Children.Add(border);
    }

    private void AddBulletList(StackPanel panel, string[] items)
    {
        foreach (var item in items)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
            sp.Children.Add(new TextBlock
            {
                Text = "•",
                Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
                Margin = new Thickness(0, 0, 6, 0),
                FontSize = 11
            });
            sp.Children.Add(new TextBlock
            {
                Text = item,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
                FontSize = 11
            });
            panel.Children.Add(sp);
        }
    }

    private void AddSeparator(StackPanel panel)
    {
        panel.Children.Add(new Border
        {
            Height = 1,
            Background = (Brush)Application.Current.FindResource("SeparatorBrush"),
            Margin = new Thickness(0, 16, 0, 16)
        });
    }

    private void AddCompactList(StackPanel panel, (string syntax, string desc)[] items)
    {
        foreach (var (syntax, desc) in items)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            sp.Children.Add(new TextBlock
            {
                Text = syntax,
                FontFamily = new FontFamily("Cascadia Code, Consolas"),
                FontSize = 11,
                Foreground = (Brush)Application.Current.FindResource("WarningBrush"),
                MinWidth = 150
            });
            sp.Children.Add(new TextBlock
            {
                Text = "  " + desc,
                Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
                FontSize = 11
            });
            panel.Children.Add(sp);
        }
    }

    private void AddPropertyList(StackPanel panel, (string name, string type, string access, string desc)[] items)
    {
        foreach (var (name, type, access, desc) in items)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = name,
                FontFamily = new FontFamily("Cascadia Code, Consolas"),
                FontSize = 11,
                Foreground = (Brush)Application.Current.FindResource("WarningBrush"),
                FontWeight = FontWeights.SemiBold
            });
            header.Children.Add(new TextBlock
            {
                Text = $" ({type}) [{access}]",
                FontSize = 10,
                Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush")
            });

            sp.Children.Add(header);
            sp.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 10,
                Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
                Margin = new Thickness(12, 0, 0, 0)
            });
            panel.Children.Add(sp);
        }
    }

    private void AddSyntaxEntry(StackPanel panel, string syntax, string description, string example)
    {
        var container = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };

        container.Children.Add(new TextBlock
        {
            Text = syntax,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 11,
            Foreground = (Brush)Application.Current.FindResource("WarningBrush"),
            FontWeight = FontWeights.SemiBold
        });

        container.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 10,
            Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
            Margin = new Thickness(8, 2, 0, 2)
        });

        var exampleBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 3, 6, 3),
            Margin = new Thickness(8, 2, 0, 0)
        };
        exampleBorder.Child = new TextBlock
        {
            Text = example,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 220, 254)),
            TextWrapping = TextWrapping.Wrap
        };
        container.Children.Add(exampleBorder);

        panel.Children.Add(container);
    }

    private void AddFunctionEntry(StackPanel panel, string signature, string description, string example)
    {
        AddSyntaxEntry(panel, signature, description, example);
    }

    private void AddIC10Entry(StackPanel panel, string instruction, string meaning, string description)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

        sp.Children.Add(new TextBlock
        {
            Text = instruction,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 10,
            Foreground = (Brush)Application.Current.FindResource("WarningBrush"),
            Width = 140
        });

        sp.Children.Add(new TextBlock
        {
            Text = meaning,
            FontFamily = new FontFamily("Cascadia Code, Consolas"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 220, 254)),
            Width = 100
        });

        sp.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 10,
            Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush")
        });

        panel.Children.Add(sp);
    }

    private void AddTip(StackPanel panel, string text)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 0, 200, 83)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 83)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 4, 0, 8)
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = "TIP: ",
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 83)),
            FontSize = 10
        });
        sp.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = (Brush)Application.Current.FindResource("PrimaryTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 10
        });

        border.Child = sp;
        panel.Children.Add(border);
    }

    private void AddWarning(StackPanel panel, string text)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 152, 0)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            Margin = new Thickness(0, 4, 0, 8)
        };

        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = "WARNING: ",
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            FontSize = 10
        });
        sp.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = (Brush)Application.Current.FindResource("PrimaryTextBrush"),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 10
        });

        border.Child = sp;
        panel.Children.Add(border);
    }

    #endregion

    #region Legacy Methods (kept for compatibility)

    public void PopulateQuickReference(StackPanel panel)
    {
        // Redirects to StartHere for compatibility
        PopulateStartHere(panel);
    }

    public List<CodeSnippet> GetSnippets()
    {
        return new List<CodeSnippet>
        {
            new() { Name = "Device Alias", Code = "ALIAS sensor d0\n" },
            new() { Name = "Named Device", Code = "DEVICE sensor \"StructureGasSensor\"\n" },
            new() { Name = "If-Then-Else", Code = "IF condition THEN\n    # code\nELSE\n    # code\nENDIF\n" },
            new() { Name = "While Loop", Code = "WHILE condition\n    # code\n    YIELD\nWEND\n" },
            new() { Name = "For Loop", Code = "FOR i = 1 TO 10\n    # code\nNEXT i\n" },
            new() { Name = "Main Loop", Code = "main:\n    # code\n    YIELD\n    GOTO main\n" },
            new() { Name = "Counter (++/--)", Code = "VAR count = 0\nVAR lastBtn = 0\n\n# In main loop:\nVAR btn = button.Setting\nIF btn = 1 AND lastBtn = 0 THEN\n    ++count    # or --count to decrement\nENDIF\nlastBtn = btn\ndisplay.Setting = count\n" },
            new() { Name = "Accumulator (+=)", Code = "VAR total = 0\nVAR count = 0\n\n# In main loop:\ntotal += sensor.Reading\ncount += 1\nVAR average = total / count\n" },
            new() { Name = "Temperature Check", Code = "VAR temp = sensor.Temperature\nIF temp > 100 THEN\n    cooler.On = 1\nELSE\n    cooler.On = 0\nENDIF\n" },
            new() { Name = "Pressure Control", Code = "VAR pressure = sensor.Pressure\nIF pressure < 50 THEN\n    pump.On = 1\nELSEIF pressure > 100 THEN\n    pump.On = 0\nENDIF\n" },
            new() { Name = "Hysteresis Pattern", Code = "CONST TARGET = 100\nCONST TOLERANCE = 5\n\nIF value < TARGET - TOLERANCE THEN\n    device.On = 1\nELSEIF value > TARGET + TOLERANCE THEN\n    device.On = 0\nENDIF\n" },
            new() { Name = "Edge Detection", Code = "VAR lastState = 0\nVAR current = button.Setting\nIF current = 1 AND lastState = 0 THEN\n    # Button pressed!\nENDIF\nlastState = current\n" },
            new() { Name = "Batch Read", Code = "DEFINE SENSOR_HASH -1234567\nVAR avgTemp = BATCHREAD(SENSOR_HASH, Temperature, 0)\n" },
            new() { Name = "State Machine", Code = "VAR state = 0\n\nmain:\n    IF state = 0 THEN GOSUB Idle\n    IF state = 1 THEN GOSUB Working\n    YIELD\n    GOTO main\n\nIdle:\n    IF trigger THEN state = 1\n    RETURN\n\nWorking:\n    # do work\n    IF done THEN state = 0\n    RETURN\n" },
            new() { Name = "Bit Flags", Code = "VAR flags = 0\nCONST BIT_POWER = 0\nCONST BIT_TEMP = 1\n\n# Set bit:\nflags = flags | (1 << BIT_POWER)\n# Clear bit:\nflags = flags & ~(1 << BIT_TEMP)\n# Check bit:\nVAR isSet = (flags >> BIT_POWER) & 1\n" }
        };
    }

    public List<Example> GetExamples()
    {
        return new List<Example>
        {
            // Beginner Examples
            new()
            {
                Name = "01: Blink Light",
                Description = "The simplest program - toggles a light on and off every second.\nDevices: d0 = Wall Light",
                Code = @"#Blink Light - The simplest IC10 program
ALIAS light d0

main:
    light.On = 1
    SLEEP 1
    light.On = 0
    SLEEP 1
    GOTO main
END
"
            },
            new()
            {
                Name = "02: Button Toggle",
                Description = "Toggle a device on/off with each button press using edge detection.\nDevices: d0 = Logic Button, d1 = Device to control",
                Code = @"#Button Toggle with Edge Detection
ALIAS button d0
ALIAS device d1

VAR lastState = 0
VAR currentState = 0
VAR deviceOn = 0

main:
    currentState = button.Setting

    IF currentState = 1 AND lastState = 0 THEN
        IF deviceOn = 0 THEN
            deviceOn = 1
        ELSE
            deviceOn = 0
        ENDIF
        device.On = deviceOn
    ENDIF

    lastState = currentState
    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "03: Thermostat",
                Description = "Maintains room temperature with hysteresis to prevent rapid cycling.\nDevices: d0 = Gas Sensor, d1 = Wall Heater, d2 = Wall Cooler",
                Code = @"#Simple Thermostat with Hysteresis
ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2

DEFINE TARGET_TEMP 293.15    #20C in Kelvin
DEFINE TOLERANCE 2

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
"
            },
            new()
            {
                Name = "04: Pressure Regulator",
                Description = "Maintains room pressure within a safe range.\nDevices: d0 = Gas Sensor, d1 = Active Vent",
                Code = @"#Room Pressure Regulator
ALIAS sensor d0
ALIAS vent d1

DEFINE TARGET_PRESSURE 101
DEFINE LOW_THRESHOLD 95
DEFINE HIGH_THRESHOLD 105

VAR pressure = 0

main:
    pressure = sensor.Pressure

    IF pressure < LOW_THRESHOLD THEN
         #Pressurize - vent inward
        vent.On = 1
        vent.Mode = 1
    ELSEIF pressure > HIGH_THRESHOLD THEN
         #Depressurize - vent outward
        vent.On = 1
        vent.Mode = 0
    ELSE
        vent.On = 0
    ENDIF

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "05: Oxygen Monitor",
                Description = "Monitors O2 levels and displays status using color-coded light.\nDevices: d0 = Gas Sensor, d1 = Wall Light, d2 = LED Display",
                Code = @"#Oxygen Monitor with Color-Coded Alarm
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
        alarm.Color = 16711680    #Red - Danger
    ELSEIF oxygenRatio > MAX_OXYGEN THEN
        alarm.On = 1
        alarm.Color = 16776960    #Yellow - Warning
    ELSE
        alarm.On = 1
        alarm.Color = 65280       #Green - Safe
    ENDIF

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "06: Solar Tracker",
                Description = "Automatically positions solar panels to track the sun.\nDevices: d0 = Any Solar Panel",
                Code = @"#Simple Solar Panel Tracker
ALIAS panel d0

VAR solarAngle = 0

main:
    solarAngle = panel.SolarAngle
    panel.Horizontal = solarAngle
    panel.Vertical = 60

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "07: Battery Backup",
                Description = "Manages battery with automatic generator backup using hysteresis.\nDevices: d0 = Battery, d1 = Generator, d2 = LED Display",
                Code = @"#Battery Backup System
ALIAS battery d0
ALIAS generator d1
ALIAS display d2

DEFINE LOW_CHARGE 0.20
DEFINE HIGH_CHARGE 0.90

VAR charge = 0
VAR genOn = 0

main:
    charge = battery.Charge

    IF charge < LOW_CHARGE THEN
        genOn = 1
    ELSEIF charge > HIGH_CHARGE THEN
        genOn = 0
    ENDIF

    generator.On = genOn
    display.Setting = charge * 100

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "08: Airlock Controller",
                Description = "Full airlock automation with state machine and safety interlocks.\nDevices: d0 = Inner Door, d1 = Outer Door, d2 = Pump, d3 = Gas Sensor, d4 = Inner Button, d5 = Outer Button",
                Code = @"#Airlock Controller with State Machine
ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3
ALIAS innerButton d4
ALIAS outerButton d5

DEFINE VACUUM 1
DEFINE PRESSURIZED 90

VAR pressure = 0
VAR state = 0

main:
    pressure = sensor.Pressure

    IF state = 0 THEN
        GOSUB IdleState
    ELSEIF state = 1 THEN
        GOSUB Depressurize
    ELSEIF state = 2 THEN
        GOSUB Pressurize
    ENDIF

    YIELD
    GOTO main

IdleState:
    pump.On = 0
    IF innerButton.Setting = 1 THEN
        outerDoor.Open = 0
        outerDoor.Lock = 1
        state = 2
    ELSEIF outerButton.Setting = 1 THEN
        innerDoor.Open = 0
        innerDoor.Lock = 1
        state = 1
    ENDIF
    RETURN

Depressurize:
    innerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 0
    IF pressure < VACUUM THEN
        pump.On = 0
        outerDoor.Lock = 0
        outerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

Pressurize:
    outerDoor.Lock = 1
    pump.On = 1
    pump.Mode = 1
    IF pressure > PRESSURIZED THEN
        pump.On = 0
        innerDoor.Lock = 0
        innerDoor.Open = 1
        state = 0
    ENDIF
    RETURN

END
"
            },
            new()
            {
                Name = "09: Furnace Controller",
                Description = "Automates furnace operation with temperature monitoring and safety limits.\nDevices: d0 = Furnace, d1 = Input Chute, d2 = Output Chute",
                Code = @"#Furnace Controller with Safety
ALIAS furnace d0
ALIAS input d1
ALIAS output d2

CONST TARGET_TEMP 500
CONST MAX_TEMP 600

main:
    VAR temp = furnace.Temperature
    VAR hasInput = input.Occupied

     #Safety check - emergency shutoff
    IF temp > MAX_TEMP THEN
        furnace.On = 0
        GOTO main
    ENDIF

     #Normal operation
    IF hasInput = 1 AND temp < TARGET_TEMP THEN
        furnace.On = 1
    ELSEIF temp >= TARGET_TEMP THEN
        furnace.On = 0
    ENDIF

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "10: Batch Solar Array",
                Description = "Control unlimited solar panels using batch operations.\nDevices: d0 = LED Display (optional)",
                Code = @"#Batch Solar Array Controller
ALIAS display d0

DEFINE SOLAR_HASH -539224550

VAR solarAngle = 0
VAR totalPower = 0

main:
     #Read average solar angle from all panels
    solarAngle = BATCHREAD(SOLAR_HASH, SolarAngle, 0)

     #Set all panels to track the sun
    BATCHWRITE(SOLAR_HASH, Horizontal, solarAngle)
    BATCHWRITE(SOLAR_HASH, Vertical, 60)

     #Sum total power generation
    totalPower = BATCHREAD(SOLAR_HASH, PowerGeneration, 1)
    display.Setting = totalPower

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "11: Item Sorter",
                Description = "Sorts items by hash value using a sorter.\nDevices: d0 = Sorter",
                Code = @"#Hash-Based Item Sorter
ALIAS sorter d0

' Define item hashes to sort
DEFINE IRON_ORE -707307845
DEFINE COPPER_ORE -404336834
DEFINE GOLD_ORE 226410516

VAR itemHash = 0

main:
    itemHash = sorter.OccupantHash

    IF itemHash = IRON_ORE THEN
        sorter.Mode = 1
    ELSEIF itemHash = COPPER_ORE THEN
        sorter.Mode = 2
    ELSEIF itemHash = GOLD_ORE THEN
        sorter.Mode = 3
    ELSE
        sorter.Mode = 0    #Default output
    ENDIF

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "12: Base Status Monitor",
                Description = "Comprehensive base monitoring using batch operations.\nDevices: d0-d3 = LED Displays, d4 = Alarm Light",
                Code = @"#Base Status Monitor
ALIAS powerDisp d0
ALIAS o2Disp d1
ALIAS pressDisp d2
ALIAS tempDisp d3
ALIAS alarm d4

DEFINE BATTERY_HASH -1388288459
DEFINE SENSOR_HASH 1255689925

VAR power = 0
VAR oxygen = 0
VAR pressure = 0
VAR temp = 0
VAR alarmState = 0

main:
     #Read values using batch (average)
    power = BATCHREAD(BATTERY_HASH, Charge, 2)
    oxygen = BATCHREAD(SENSOR_HASH, RatioOxygen, 0)
    pressure = BATCHREAD(SENSOR_HASH, Pressure, 0)
    temp = BATCHREAD(SENSOR_HASH, Temperature, 0)

     #Update displays
    powerDisp.Setting = power * 100
    o2Disp.Setting = oxygen * 100
    pressDisp.Setting = pressure
    tempDisp.Setting = temp - 273.15

     #Check for alarm conditions
    alarmState = 0
    IF power < 0.2 THEN alarmState = 1
    IF oxygen < 0.18 THEN alarmState = 1
    IF oxygen > 0.25 THEN alarmState = 1
    IF pressure < 80 THEN alarmState = 1
    IF pressure > 120 THEN alarmState = 1

    alarm.On = 1
    IF alarmState = 1 THEN
        alarm.Color = 16711680   #Red
    ELSE
        alarm.Color = 65280      #Green
    ENDIF

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "13: Dial Pump Control",
                Description = "Control a pump's pressure setting using a dial.\nDevices: d0 = Dial, d1 = Pump/Vent",
                Code = @"#Dial-Controlled Pump
ALIAS dial d0
ALIAS pump d1

VAR dialValue = 0
VAR targetPressure = 0

main:
    dialValue = dial.Setting
     #Scale dial (0-100) to pressure (0-200 kPa)
    targetPressure = dialValue * 2

    pump.Setting = targetPressure
    pump.On = 1

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "14: Math Demo",
                Description = "Demonstrates all available math functions for reference.",
                Code = @"#Math Functions Demo
ALIAS display d0

VAR x = 0
VAR result = 0

main:
     #Basic math
    result = ABS(-5)           #= 5
    result = SQRT(16)          #= 4
    result = 2 ^ 3             #= 8
    result = 10 MOD 3          #= 1

     #Trigonometry (radians)
    result = SIN(3.14159 / 2)  #= 1
    result = COS(0)            #= 1
    result = TAN(0)            #= 0

     #Inverse trig
    result = ASIN(1)           #= 1.57...
    result = ACOS(0)           #= 1.57...
    result = ATAN(1)           #= 0.785...
    result = ATAN2(1, 1)       #= 0.785...

     #Logarithms
    result = LOG(2.718)        #= ~1
    result = EXP(1)            #= 2.718...

     #Rounding
    result = CEIL(3.2)         #= 4
    result = FLOOR(3.8)        #= 3
    result = ROUND(3.5)        #= 4
    result = TRUNC(3.9)        #= 3

     #Comparison
    result = MIN(5, 3)         #= 3
    result = MAX(5, 3)         #= 5

     #Random
    result = RAND              #= 0.0 to 1.0

    display.Setting = result
    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "15: Greenhouse Controller",
                Description = "Multi-system plant management with atmosphere control.\nDevices: d0 = Gas Sensor, d1 = Grow Light, d2 = CO2 Vent, d3 = Water Pump",
                Code = @"#Greenhouse Controller
ALIAS sensor d0
ALIAS light d1
ALIAS co2vent d2
ALIAS waterPump d3

DEFINE MIN_CO2 0.01
DEFINE MAX_CO2 0.05
DEFINE MIN_PRESSURE 90
DEFINE MAX_PRESSURE 110

VAR co2Ratio = 0
VAR pressure = 0

main:
    co2Ratio = sensor.RatioCarbonDioxide
    pressure = sensor.Pressure

     #CO2 control for plant growth
    IF co2Ratio < MIN_CO2 THEN
        co2vent.On = 1
    ELSEIF co2Ratio > MAX_CO2 THEN
        co2vent.On = 0
    ENDIF

     #Pressure maintenance
    IF pressure < MIN_PRESSURE THEN
        waterPump.On = 0
    ELSEIF pressure > MAX_PRESSURE THEN
        waterPump.On = 1
    ENDIF

     #Grow light always on during operation
    light.On = 1

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "16: Counter with Increment/Decrement",
                Description = "Demonstrates ++/-- operators for counting.\nDevices: d0 = LED Display, d1 = Up Button, d2 = Down Button",
                Code = @"# Counter Demo - Increment/Decrement Operators
ALIAS display d0
ALIAS upBtn d1
ALIAS downBtn d2

VAR count = 0
VAR lastUp = 0
VAR lastDown = 0

main:
    VAR currentUp = upBtn.Setting
    VAR currentDown = downBtn.Setting

    # Edge detection with increment
    IF currentUp = 1 AND lastUp = 0 THEN
        ++count
    ENDIF

    IF currentDown = 1 AND lastDown = 0 THEN
        --count
    ENDIF

    lastUp = currentUp
    lastDown = currentDown
    display.Setting = count

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "17: Smooth Value Adjustment",
                Description = "Uses compound assignment for gradual changes.\nDevices: d0 = Dial, d1 = Pump, d2 = LED Display",
                Code = @"# Smooth Adjustment with Compound Assignment
ALIAS dial d0
ALIAS pump d1
ALIAS display d2

VAR targetPressure = 50
VAR currentSetting = 0
CONST STEP = 2

main:
    targetPressure = dial.Setting

    # Gradually adjust toward target
    IF currentSetting < targetPressure THEN
        currentSetting += STEP
    ELSEIF currentSetting > targetPressure THEN
        currentSetting -= STEP
    ENDIF

    pump.Setting = currentSetting
    display.Setting = currentSetting

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "18: Bit Flag Status System",
                Description = "Uses bit shifts for compact status tracking.\nDevices: d0 = Gas Sensor, d1 = LED Display",
                Code = @"# Status Flag System using Bit Shifts
ALIAS sensor d0
ALIAS display d1

# Status bits: bit0=power, bit1=temp, bit2=pressure, bit3=oxygen
VAR status = 0
CONST POWER_BIT = 0
CONST TEMP_BIT = 1
CONST PRESSURE_BIT = 2
CONST OXYGEN_BIT = 3

main:
    status = 0

    IF sensor.Power > 0 THEN
        status = status | (1 << POWER_BIT)
    ENDIF

    IF sensor.Temperature > 250 AND sensor.Temperature < 320 THEN
        status = status | (1 << TEMP_BIT)
    ENDIF

    IF sensor.Pressure > 80 AND sensor.Pressure < 120 THEN
        status = status | (1 << PRESSURE_BIT)
    ENDIF

    IF sensor.RatioOxygen > 0.18 AND sensor.RatioOxygen < 0.25 THEN
        status = status | (1 << OXYGEN_BIT)
    ENDIF

    # Display status (15 = all OK)
    display.Setting = status

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "19: Loop with BREAK/CONTINUE",
                Description = "Demonstrates loop control statements.\nDevices: d0 = Sorter, d1 = LED Display",
                Code = @"# Search with Loop Control
ALIAS sorter d0
ALIAS display d1

DEFINE TARGET_HASH -707307845

VAR found = 0

main:
    found = 0

    FOR slot = 0 TO 5
        VAR hash = sorter.Slot(slot).OccupantHash

        # Skip empty slots
        IF hash = 0 THEN CONTINUE

        # Found target - exit early
        IF hash = TARGET_HASH THEN
            found = 1
            BREAK
        ENDIF
    NEXT slot

    display.Setting = found

    YIELD
    GOTO main
END
"
            }
        };
    }

    /// <summary>
    /// Populates comprehensive documentation combining Quick Start and Language Reference.
    /// </summary>
    public void PopulateComprehensiveDocs(StackPanel panel)
    {
        panel.Children.Clear();

        // Title section
        AddHeader(panel, "Basic-10 Compiler Documentation", true);
        AddParagraph(panel, "Complete reference for the Stationeers Basic-10 to IC10 MIPS compiler.");
        AddSeparator(panel);

        // Quick Start section
        AddHeader(panel, "━━━━━━━━━━ QUICK START GUIDE ━━━━━━━━━━", true);
        PopulateStartHereContent(panel);

        AddSeparator(panel);

        // Language Reference section
        AddHeader(panel, "━━━━━━━━━━ LANGUAGE REFERENCE ━━━━━━━━━━", true);
        PopulateSyntaxContent(panel);
    }

    /// <summary>
    /// Populates Start Here content without clearing the panel.
    /// </summary>
    private void PopulateStartHereContent(StackPanel panel)
    {
        // Welcome Section
        AddHeader(panel, "Welcome to Stationeers Basic-10!");
        AddParagraph(panel, "This compiler lets you write programs in BASIC (a beginner-friendly language) and automatically converts them to IC10 MIPS assembly that runs on Stationeers programmable chips.");

        AddHeader(panel, "What is IC10?");
        AddParagraph(panel, "IC10 is the programming language used by Integrated Circuits (ICs) in Stationeers. It's based on MIPS assembly - a low-level language that can be difficult to learn. That's where Basic-10 comes in!");

        AddHeader(panel, "Why Use This Compiler?");
        AddBulletList(panel, new[]
        {
            "Write readable BASIC code instead of cryptic assembly",
            "Automatic variable management (no manual register allocation)",
            "Built-in functions for math, timing, and more",
            "Real-time error checking as you type",
            "One-click deployment to Stationeers"
        });

        AddHeader(panel, "Your First Program");
        AddParagraph(panel, "Let's create a simple program that blinks a light:");
        AddCodeBlock(panel, @"# My first IC10 program - Blink a light
ALIAS light d0    # Connect a light to pin d0

main:
    light.On = 1  # Turn light on
    SLEEP 1       # Wait 1 second
    light.On = 0  # Turn light off
    SLEEP 1       # Wait 1 second
    GOTO main     # Repeat forever
END");

        AddHeader(panel, "Understanding the Code");
        AddBulletList(panel, new[]
        {
            "Lines starting with # are comments (ignored by compiler)",
            "ALIAS creates a friendly name for device pins (d0-d5)",
            "main: is a label - a named location in your code",
            "SLEEP pauses execution (in seconds)",
            "GOTO jumps to a label",
            "END marks the end of your program"
        });

        AddHeader(panel, "The Main Loop Pattern");
        AddParagraph(panel, "Almost every IC10 program uses a main loop that runs forever:");
        AddCodeBlock(panel, @"main:
    # Your code here - runs every tick
    YIELD         # IMPORTANT: Let game process
    GOTO main     # Loop back
END");
        AddWarning(panel, "IMPORTANT: Always include YIELD or SLEEP in loops! Without it, your program will freeze the game.");

        AddHeader(panel, "Connecting Devices");
        AddParagraph(panel, "IC chips have 6 device pins (d0-d5). Connect devices in-game, then reference them in code:");
        AddCodeBlock(panel, @"# Give meaningful names to your pins
ALIAS sensor d0      # Gas sensor on pin 0
ALIAS heater d1      # Wall heater on pin 1
ALIAS display d2     # LED display on pin 2

# Now use the friendly names
VAR temp = sensor.Temperature
heater.On = 1
display.Setting = 42");

        AddHeader(panel, "Reading and Writing Device Properties");
        AddCodeBlock(panel, @"# Reading a property (uses VAR)
VAR temperature = sensor.Temperature
VAR isOn = light.On

# Writing a property (uses =)
light.On = 1          # Turn on
pump.Setting = 100    # Set to 100
door.Open = 0         # Close door");

        AddHeader(panel, "Making Decisions with IF");
        AddCodeBlock(panel, @"IF temperature > 300 THEN
    heater.On = 0     # Too hot, turn off
ELSEIF temperature < 290 THEN
    heater.On = 1     # Too cold, turn on
ELSE
    # Temperature is just right
ENDIF");

        AddHeader(panel, "Quick Start Checklist");
        AddBulletList(panel, new[]
        {
            "1. Write your BASIC code in the top editor",
            "2. Press F5 or click 'Compile' to generate IC10",
            "3. Check for errors in the status bar",
            "4. Click 'Save & Deploy' to send to Stationeers",
            "5. In-game: Load the script on your IC chip"
        });

        AddTip(panel, "Press Ctrl+Space for autocomplete suggestions while typing!");
    }

    /// <summary>
    /// Populates Syntax content without clearing the panel.
    /// </summary>
    private void PopulateSyntaxContent(StackPanel panel)
    {
        AddHeader(panel, "Complete Syntax Reference");

        // Comments
        AddSubHeader(panel, "Comments");
        AddCodeBlock(panel, @"# This is a comment (IC10 style)
' This is also a comment (BASIC style)
REM This is a traditional BASIC comment");

        // Variables
        AddSubHeader(panel, "Variables");
        AddCodeBlock(panel, @"VAR x = 10           # Declare and initialize
VAR y                # Declare (default 0)
LET z = x + y        # LET is also supported
x = 42               # Reassign value");

        // Constants
        AddSubHeader(panel, "Constants");
        AddCodeBlock(panel, @"CONST PI = 3.14159   # Named constant
DEFINE MAX_TEMP 500  # IC10 style define");

        // Device Access
        AddSubHeader(panel, "Device Access");
        AddCodeBlock(panel, @"# Simple pin aliases
ALIAS sensor d0
ALIAS pump d1

# Read properties
VAR temp = sensor.Temperature
VAR pressure = sensor.Pressure

# Write properties
pump.On = 1
pump.Setting = 100

# Slot access
VAR qty = storage.Slot[0].Quantity
storage.Slot[1].Lock = 1");

        // Advanced Device Access
        AddSubHeader(panel, "Advanced Device Access");
        AddCodeBlock(panel, @"# Batch operations (all devices of a type)
ALIAS allSensors = IC.Device[StructureGasSensor]
VAR avgTemp = allSensors.Temperature.Average
VAR maxTemp = allSensors.Temperature.Maximum

# Named device reference (bypass 6-pin limit!)
ALIAS roomSensor = IC.Device[StructureGasSensor].Name[""Room1""]
VAR roomTemp = roomSensor.Temperature");

        // Operators
        AddSubHeader(panel, "Operators");
        AddParagraph(panel, "Arithmetic: + - * / % ^ (power)");
        AddParagraph(panel, "Comparison: = <> < > <= >= ~= (approx equal)");
        AddParagraph(panel, "Logical: AND OR NOT");
        AddParagraph(panel, "Bitwise: BAND BOR BXOR BNOT << >> >>>");

        // Control Flow
        AddSubHeader(panel, "Control Flow - IF");
        AddCodeBlock(panel, @"IF condition THEN
    # code
ELSEIF other_condition THEN
    # code
ELSE
    # code
ENDIF");

        AddSubHeader(panel, "Control Flow - FOR Loop");
        AddCodeBlock(panel, @"FOR i = 1 TO 10
    # code runs 10 times
NEXT

FOR j = 10 TO 0 STEP -2
    # count down by 2
NEXT");

        AddSubHeader(panel, "Control Flow - WHILE Loop");
        AddCodeBlock(panel, @"WHILE condition
    # code
WEND");

        AddSubHeader(panel, "Control Flow - DO Loop");
        AddCodeBlock(panel, @"DO WHILE condition
    # code
LOOP

DO
    # code
LOOP UNTIL condition");

        AddSubHeader(panel, "Control Flow - SELECT CASE");
        AddCodeBlock(panel, @"SELECT CASE mode
    CASE 0
        # mode 0 code
    CASE 1, 2
        # mode 1 or 2
    CASE ELSE
        # default
END SELECT");

        // Branching
        AddSubHeader(panel, "Branching");
        AddCodeBlock(panel, @"GOTO label          # Jump to label
GOSUB subroutine    # Call subroutine
RETURN              # Return from GOSUB

ON index GOTO a, b, c   # Computed GOTO
ON index GOSUB x, y, z  # Computed GOSUB");

        // Subroutines and Functions
        AddSubHeader(panel, "Subroutines and Functions");
        AddCodeBlock(panel, @"# Subroutine (no return value)
SUB UpdateDisplay(value)
    display.Setting = value
END SUB

CALL UpdateDisplay(42)

# Function (returns value)
FUNCTION Clamp(val, min, max)
    IF val < min THEN RETURN min
    IF val > max THEN RETURN max
    RETURN val
END FUNCTION

VAR result = Clamp(150, 0, 100)");

        // Arrays
        AddSubHeader(panel, "Arrays");
        AddCodeBlock(panel, @"DIM values(10)       # Declare array
ARRAY data[5]        # Alternative syntax

values(0) = 42       # Set element
VAR x = values(0)    # Get element");

        // Built-in Functions
        AddSubHeader(panel, "Built-in Math Functions");
        AddCodeBlock(panel, @"ABS(x)      # Absolute value
SQRT(x)     # Square root
SIN(x)      # Sine (radians)
COS(x)      # Cosine
TAN(x)      # Tangent
ASIN(x)     # Arc sine
ACOS(x)     # Arc cosine
ATAN(x)     # Arc tangent
ATAN2(y,x)  # Two-argument arctangent
LOG(x)      # Natural logarithm
EXP(x)      # e^x
CEIL(x)     # Round up
FLOOR(x)    # Round down
ROUND(x)    # Round to nearest
TRUNC(x)    # Truncate
MIN(a,b)    # Minimum
MAX(a,b)    # Maximum
POW(x,y)    # x^y
RAND()      # Random 0-1");

        // Timing
        AddSubHeader(panel, "Timing");
        AddCodeBlock(panel, @"SLEEP seconds   # Pause execution
YIELD           # Yield to game (1 tick)");

        // Stack Operations
        AddSubHeader(panel, "Stack Operations");
        AddCodeBlock(panel, @"PUSH value      # Push to stack
POP variable    # Pop from stack
PEEK variable   # Read top without popping");

        // Special Functions
        AddSubHeader(panel, "Special Functions");
        AddCodeBlock(panel, @"HASH(""string"")  # Get string hash
IIF(cond, t, f) # Inline if
INRANGE(v,a,b)  # Check if v in [a,b]
LERP(a,b,t)     # Linear interpolate");

        AddTip(panel, "Use the sidebar documentation for more detailed information and examples!");
    }

    #endregion
}
