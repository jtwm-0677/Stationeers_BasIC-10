using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

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
    public void PopulateQuickReference(StackPanel panel)
    {
        panel.Children.Clear();

        AddSection(panel, "Variables & Constants", new[]
        {
            ("VAR name = value", "Declare a variable"),
            ("CONST name = value", "Declare a constant"),
            ("ALIAS name device", "Create device alias"),
            ("DEFINE name value", "Define a constant")
        });

        AddSection(panel, "Control Flow", new[]
        {
            ("IF condition THEN ... ENDIF", "Conditional block"),
            ("IF ... ELSE ... ENDIF", "If-else block"),
            ("WHILE condition ... ENDWHILE", "While loop"),
            ("FOR i = start TO end ... NEXT", "For loop"),
            ("GOTO label", "Jump to label"),
            ("GOSUB label / RETURN", "Subroutine call")
        });

        AddSection(panel, "Device Access", new[]
        {
            ("device.Property", "Read device property"),
            ("device.Property = value", "Write device property"),
            ("d0, d1, ... d5", "Device slots 0-5"),
            ("db", "Batch device operations")
        });

        AddSection(panel, "Operators", new[]
        {
            ("+ - * / MOD", "Arithmetic"),
            ("= <> < > <= >=", "Comparison"),
            ("AND OR NOT", "Logical"),
            ("^", "Power")
        });
    }

    public void PopulateFunctions(StackPanel panel)
    {
        panel.Children.Clear();

        AddSection(panel, "Math Functions", new[]
        {
            ("ABS(x)", "Absolute value"),
            ("SQRT(x)", "Square root"),
            ("SIN(x) / COS(x) / TAN(x)", "Trigonometry"),
            ("ASIN(x) / ACOS(x) / ATAN(x)", "Inverse trig"),
            ("ATAN2(y, x)", "Two-argument arctangent"),
            ("EXP(x) / LOG(x)", "Exponential and natural log"),
            ("CEIL(x) / FLOOR(x)", "Ceiling and floor"),
            ("ROUND(x) / TRUNC(x)", "Round and truncate"),
            ("MIN(a, b) / MAX(a, b)", "Minimum and maximum"),
            ("RND()", "Random number 0-1"),
            ("SGN(x)", "Sign (-1, 0, or 1)")
        });

        AddSection(panel, "Control Functions", new[]
        {
            ("YIELD", "Yield one game tick"),
            ("SLEEP n", "Sleep for n seconds"),
            ("END", "End program execution")
        });

        AddSection(panel, "Device Properties", new[]
        {
            ("Temperature", "Current temperature"),
            ("Pressure", "Current pressure"),
            ("Power", "Power level"),
            ("On", "Device on/off state"),
            ("Setting", "Device setting value"),
            ("Ratio", "Ratio value"),
            ("Open", "Open/closed state"),
            ("Lock", "Lock state")
        });
    }

    public void PopulateExamples(StackPanel panel, Action<string> loadCallback)
    {
        panel.Children.Clear();

        var examples = GetExamples();

        foreach (var example in examples)
        {
            var expander = new Expander
            {
                Header = example.Name,
                Margin = new Thickness(0, 0, 0, 8)
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

    private void AddSection(StackPanel panel, string title, (string syntax, string desc)[] items)
    {
        var header = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.FindResource("AccentBrush"),
            Margin = new Thickness(0, 8, 0, 4)
        };
        panel.Children.Add(header);

        foreach (var (syntax, desc) in items)
        {
            var itemPanel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

            itemPanel.Children.Add(new TextBlock
            {
                Text = syntax,
                FontFamily = new FontFamily("Cascadia Code, Consolas"),
                FontSize = 12,
                Foreground = (Brush)Application.Current.FindResource("WarningBrush")
            });

            itemPanel.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11,
                Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
                Margin = new Thickness(8, 0, 0, 0)
            });

            panel.Children.Add(itemPanel);
        }
    }

    public List<CodeSnippet> GetSnippets()
    {
        return new List<CodeSnippet>
        {
            new() { Name = "Device Alias", Code = "ALIAS sensor d0\n" },
            new() { Name = "If-Then-Else", Code = "IF condition THEN\n    ' code\nELSE\n    ' code\nENDIF\n" },
            new() { Name = "While Loop", Code = "WHILE condition\n    ' code\n    YIELD\nENDWHILE\n" },
            new() { Name = "For Loop", Code = "FOR i = 1 TO 10\n    ' code\nNEXT i\n" },
            new() { Name = "Main Loop", Code = "main:\n    ' code\n    YIELD\n    GOTO main\n" },
            new() { Name = "Temperature Check", Code = "VAR temp = sensor.Temperature\nIF temp > 100 THEN\n    cooler.On = 1\nELSE\n    cooler.On = 0\nENDIF\n" },
            new() { Name = "Pressure Control", Code = "VAR pressure = sensor.Pressure\nIF pressure < 50 THEN\n    pump.On = 1\nELSEIF pressure > 100 THEN\n    pump.On = 0\nENDIF\n" }
        };
    }

    public List<Example> GetExamples()
    {
        return new List<Example>
        {
            new()
            {
                Name = "Hello World",
                Description = "A simple program that turns a light on and off.",
                Code = @"' Hello World - Basic IC10 Example
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
                Name = "Temperature Controller",
                Description = "Monitors temperature and controls heating/cooling.",
                Code = @"' Temperature Controller
' Maintains temperature within a target range

ALIAS sensor d0
ALIAS heater d1
ALIAS cooler d2

CONST TARGET = 20
CONST TOLERANCE = 2

main:
    VAR temp = sensor.Temperature

    IF temp < TARGET - TOLERANCE THEN
        heater.On = 1
        cooler.On = 0
    ELSEIF temp > TARGET + TOLERANCE THEN
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
                Name = "Airlock Controller",
                Description = "Controls an airlock with pressure equalization.",
                Code = @"' Airlock Controller
' Manages inner/outer doors with pressure safety

ALIAS innerDoor d0
ALIAS outerDoor d1
ALIAS pump d2
ALIAS sensor d3
ALIAS button d4

CONST SAFE_PRESSURE = 100
CONST VACUUM = 1

VAR state = 0  ' 0=idle, 1=depressurizing, 2=pressurizing

main:
    VAR pressed = button.Setting
    VAR pressure = sensor.Pressure

    IF pressed = 1 AND state = 0 THEN
        ' Start cycle - close all doors
        innerDoor.Open = 0
        outerDoor.Open = 0
        state = 1
        pump.On = 1
    ENDIF

    IF state = 1 THEN
        ' Depressurizing
        IF pressure < VACUUM THEN
            pump.On = 0
            outerDoor.Open = 1
            state = 2
        ENDIF
    ENDIF

    IF state = 2 THEN
        ' Wait for outer door cycle
        IF button.Setting = 1 THEN
            outerDoor.Open = 0
            state = 3
        ENDIF
    ENDIF

    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "Solar Tracker",
                Description = "Automatically adjusts solar panel angle for maximum efficiency.",
                Code = @"' Solar Panel Tracker
' Optimizes solar panel angle throughout the day

ALIAS panel d0
ALIAS sensor d1

CONST STEP = 5
CONST THRESHOLD = 0.01

VAR bestAngle = 0
VAR maxPower = 0
VAR currentAngle = 0

' Initial scan
FOR angle = 0 TO 180 STEP STEP
    panel.Setting = angle
    SLEEP 0.2
    VAR power = sensor.SolarAngle
    IF power > maxPower THEN
        maxPower = power
        bestAngle = angle
    ENDIF
NEXT angle

panel.Setting = bestAngle

' Fine-tune loop
main:
    VAR current = sensor.SolarAngle

    ' Try small adjustments
    panel.Setting = bestAngle + 1
    SLEEP 0.1
    VAR test = sensor.SolarAngle

    IF test > current + THRESHOLD THEN
        bestAngle = bestAngle + 1
    ELSE
        panel.Setting = bestAngle - 1
        SLEEP 0.1
        test = sensor.SolarAngle
        IF test > current + THRESHOLD THEN
            bestAngle = bestAngle - 1
        ENDIF
    ENDIF

    panel.Setting = bestAngle
    YIELD
    GOTO main
END
"
            },
            new()
            {
                Name = "Furnace Controller",
                Description = "Automates furnace operation with temperature monitoring.",
                Code = @"' Furnace Controller
' Automatically manages smelting operations

ALIAS furnace d0
ALIAS input d1
ALIAS output d2

CONST TARGET_TEMP = 500
CONST MAX_TEMP = 600

main:
    VAR temp = furnace.Temperature
    VAR hasInput = input.Occupied

    ' Safety check
    IF temp > MAX_TEMP THEN
        furnace.On = 0
        GOTO main
    ENDIF

    ' Normal operation
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
                Name = "Counter Display",
                Description = "Displays a counting number on a console.",
                Code = @"' Counter Display
' Demonstrates variables and display output

ALIAS display d0

VAR count = 0

main:
    display.Setting = count
    count = count + 1

    IF count > 999 THEN
        count = 0
    ENDIF

    SLEEP 1
    GOTO main
END
"
            }
        };
    }

    public string GetQuickStartGuide()
    {
        return @"# Quick Start Guide

## Getting Started

Welcome to BASIC-IC10, a powerful BASIC compiler for Stationeers!

### Your First Program

1. Write your BASIC code in the top editor
2. Press F5 or click 'Compile' to generate IC10 code
3. Click 'Save & Deploy' to automatically deploy to Stationeers

### Basic Syntax

**Variables:**
```
VAR temperature = 0
CONST MAX_TEMP = 100
```

**Device Access:**
```
ALIAS sensor d0
VAR temp = sensor.Temperature
sensor.On = 1
```

**Control Flow:**
```
IF temp > 100 THEN
    ' do something
ENDIF
```

### Tips

- Use Ctrl+Space for auto-complete suggestions
- Press F1 anytime for help
- Check the Examples tab for working code samples

### Common Device Properties

- Temperature, Pressure, Power
- On, Open, Lock, Setting
- Ratio, Quantity, Occupied
";
    }

    public string GetLanguageReference()
    {
        return @"# BASIC-IC10 Language Reference

## Variables

### VAR - Variable Declaration
```
VAR name = expression
```
Declares a variable with an initial value.

### CONST - Constant Declaration
```
CONST name = value
```
Declares a compile-time constant.

### ALIAS - Device Alias
```
ALIAS name device
```
Creates a named alias for a device (d0-d5, db).

## Control Structures

### IF-THEN-ELSE
```
IF condition THEN
    statements
ELSEIF condition THEN
    statements
ELSE
    statements
ENDIF
```

### WHILE Loop
```
WHILE condition
    statements
    YIELD
ENDWHILE
```

### FOR Loop
```
FOR variable = start TO end [STEP increment]
    statements
NEXT variable
```

### GOTO and Labels
```
label:
    statements
    GOTO label
```

### GOSUB and RETURN
```
GOSUB subroutine

subroutine:
    statements
    RETURN
```

## Operators

### Arithmetic
- `+` Addition
- `-` Subtraction
- `*` Multiplication
- `/` Division
- `MOD` Modulo
- `^` Power

### Comparison
- `=` Equal
- `<>` Not equal
- `<` Less than
- `>` Greater than
- `<=` Less than or equal
- `>=` Greater than or equal

### Logical
- `AND` Logical and
- `OR` Logical or
- `NOT` Logical not

## Built-in Functions

### Math Functions
- `ABS(x)` - Absolute value
- `SQRT(x)` - Square root
- `SIN(x)`, `COS(x)`, `TAN(x)` - Trigonometry
- `ASIN(x)`, `ACOS(x)`, `ATAN(x)` - Inverse trig
- `ATAN2(y, x)` - Two-argument arctangent
- `EXP(x)` - e^x
- `LOG(x)` - Natural logarithm
- `CEIL(x)` - Ceiling
- `FLOOR(x)` - Floor
- `ROUND(x)` - Round to nearest
- `TRUNC(x)` - Truncate to integer
- `MIN(a, b)` - Minimum
- `MAX(a, b)` - Maximum
- `RND()` - Random 0-1
- `SGN(x)` - Sign

### Control Functions
- `YIELD` - Yield one game tick
- `SLEEP n` - Sleep for n seconds
- `END` - End program

## Device Access

### Reading Properties
```
VAR value = device.Property
```

### Writing Properties
```
device.Property = value
```

### Common Properties
- Temperature, Pressure, Power
- On, Open, Lock, Setting
- Ratio, Quantity, Occupied
- Mode, Error, Charge
";
    }
}
