using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using BasicToMips.Shared;

namespace BasicToMips.Editor.Completion;

public class BasicCompletionItem : ICompletionData
{
    public BasicCompletionItem(string text, string description, CompletionItemType type)
    {
        Text = text;
        Description = description;
        ItemType = type;
    }

    public string Text { get; }
    public object Description { get; }
    public CompletionItemType ItemType { get; }

    public ImageSource? Image => null;

    public object Content => Text;

    public double Priority => ItemType switch
    {
        CompletionItemType.Keyword => 1.0,
        CompletionItemType.Function => 0.9,
        CompletionItemType.Property => 0.8,
        CompletionItemType.Snippet => 0.7,
        _ => 0.5
    };

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        var textToInsert = Text;

        // Add parentheses for functions
        if (ItemType == CompletionItemType.Function && !Text.EndsWith(")"))
        {
            textToInsert = Text + "()";
        }

        textArea.Document.Replace(completionSegment, textToInsert);

        // Position cursor inside parentheses for functions
        if (ItemType == CompletionItemType.Function)
        {
            textArea.Caret.Offset -= 1;
        }
    }
}

public enum CompletionItemType
{
    Keyword,
    Function,
    Property,
    Snippet,
    Variable,
    Device
}

public static class BasicCompletionData
{
    private static readonly List<BasicCompletionItem> AllItems = new()
    {
        // Keywords
        new("IF", "Conditional statement: IF condition THEN ... ENDIF", CompletionItemType.Keyword),
        new("THEN", "Used with IF statement", CompletionItemType.Keyword),
        new("ELSE", "Alternative branch in IF statement", CompletionItemType.Keyword),
        new("ELSEIF", "Additional condition in IF statement", CompletionItemType.Keyword),
        new("ENDIF", "End of IF block", CompletionItemType.Keyword),
        new("FOR", "For loop: FOR var = start TO end ... NEXT", CompletionItemType.Keyword),
        new("TO", "Used with FOR loop to specify end value", CompletionItemType.Keyword),
        new("STEP", "Optional increment in FOR loop", CompletionItemType.Keyword),
        new("NEXT", "End of FOR loop", CompletionItemType.Keyword),
        new("WHILE", "While loop: WHILE condition ... ENDWHILE", CompletionItemType.Keyword),
        new("ENDWHILE", "End of WHILE loop", CompletionItemType.Keyword),
        new("WEND", "End of WHILE loop (alternative)", CompletionItemType.Keyword),
        new("DO", "Do loop: DO ... LOOP UNTIL/WHILE condition", CompletionItemType.Keyword),
        new("LOOP", "End of DO loop", CompletionItemType.Keyword),
        new("UNTIL", "Loop until condition is true", CompletionItemType.Keyword),
        new("GOTO", "Jump to label: GOTO labelname", CompletionItemType.Keyword),
        new("GOSUB", "Call subroutine: GOSUB labelname", CompletionItemType.Keyword),
        new("RETURN", "Return from subroutine", CompletionItemType.Keyword),
        new("END", "End program execution", CompletionItemType.Keyword),
        new("BREAK", "Exit current loop", CompletionItemType.Keyword),
        new("CONTINUE", "Skip to next iteration", CompletionItemType.Keyword),

        // Declaration keywords
        new("VAR", "Declare a variable: VAR name = value", CompletionItemType.Keyword),
        new("CONST", "Declare a constant: CONST name = value", CompletionItemType.Keyword),
        new("ALIAS", "Create device alias: ALIAS name d0", CompletionItemType.Keyword),
        new("DEFINE", "Define constant: DEFINE name value", CompletionItemType.Keyword),
        new("DIM", "Declare array: DIM array(size)", CompletionItemType.Keyword),
        new("ARRAY", "Declare array: ARRAY name[size]", CompletionItemType.Keyword),
        new("LET", "Assign value: LET var = value", CompletionItemType.Keyword),

        // Control keywords
        new("YIELD", "Yield execution for one game tick", CompletionItemType.Keyword),
        new("SLEEP", "Sleep for specified seconds: SLEEP 1", CompletionItemType.Keyword),
        new("PRINT", "Output value (debug)", CompletionItemType.Keyword),
        new("INPUT", "Read input value", CompletionItemType.Keyword),

        // Logical operators
        new("AND", "Logical AND operator", CompletionItemType.Keyword),
        new("OR", "Logical OR operator", CompletionItemType.Keyword),
        new("NOT", "Logical NOT operator", CompletionItemType.Keyword),
        new("MOD", "Modulo operator", CompletionItemType.Keyword),

        // Boolean literals
        new("TRUE", "Boolean true (1)", CompletionItemType.Keyword),
        new("FALSE", "Boolean false (0)", CompletionItemType.Keyword),

        // Math functions
        new("ABS", "Absolute value: ABS(x)", CompletionItemType.Function),
        new("SQRT", "Square root: SQRT(x)", CompletionItemType.Function),
        new("SQR", "Square root: SQR(x)", CompletionItemType.Function),
        new("SIN", "Sine (radians): SIN(x)", CompletionItemType.Function),
        new("COS", "Cosine (radians): COS(x)", CompletionItemType.Function),
        new("TAN", "Tangent (radians): TAN(x)", CompletionItemType.Function),
        new("ASIN", "Arc sine: ASIN(x)", CompletionItemType.Function),
        new("ACOS", "Arc cosine: ACOS(x)", CompletionItemType.Function),
        new("ATAN", "Arc tangent: ATAN(x)", CompletionItemType.Function),
        new("ATAN2", "Two-argument arc tangent: ATAN2(y, x)", CompletionItemType.Function),
        new("EXP", "Exponential e^x: EXP(x)", CompletionItemType.Function),
        new("LOG", "Natural logarithm: LOG(x)", CompletionItemType.Function),
        new("LOG10", "Base-10 logarithm: LOG10(x)", CompletionItemType.Function),
        new("CEIL", "Ceiling (round up): CEIL(x)", CompletionItemType.Function),
        new("FLOOR", "Floor (round down): FLOOR(x)", CompletionItemType.Function),
        new("ROUND", "Round to nearest: ROUND(x)", CompletionItemType.Function),
        new("TRUNC", "Truncate to integer: TRUNC(x)", CompletionItemType.Function),
        new("INT", "Integer part: INT(x)", CompletionItemType.Function),
        new("MIN", "Minimum of two values: MIN(a, b)", CompletionItemType.Function),
        new("MAX", "Maximum of two values: MAX(a, b)", CompletionItemType.Function),
        new("RND", "Random number 0-1: RND()", CompletionItemType.Function),
        new("RAND", "Random number 0-1: RAND()", CompletionItemType.Function),
        new("SGN", "Sign of number (-1, 0, 1): SGN(x)", CompletionItemType.Function),
        new("POW", "Power: POW(base, exp)", CompletionItemType.Function),
        new("IIF", "Inline if: IIF(cond, true_val, false_val)", CompletionItemType.Function),
        new("INRANGE", "Check if in range: INRANGE(x, min, max)", CompletionItemType.Function),
        new("LERP", "Linear interpolation: LERP(a, b, t)", CompletionItemType.Function),

        // Device references
        new("d0", "Device slot 0", CompletionItemType.Device),
        new("d1", "Device slot 1", CompletionItemType.Device),
        new("d2", "Device slot 2", CompletionItemType.Device),
        new("d3", "Device slot 3", CompletionItemType.Device),
        new("d4", "Device slot 4", CompletionItemType.Device),
        new("d5", "Device slot 5", CompletionItemType.Device),
        new("db", "Batch device operations", CompletionItemType.Device),

        // Common properties
        new("Temperature", "Device temperature", CompletionItemType.Property),
        new("Pressure", "Device pressure", CompletionItemType.Property),
        new("Power", "Device power level", CompletionItemType.Property),
        new("On", "Device on/off state (0 or 1)", CompletionItemType.Property),
        new("Open", "Door/vent open state", CompletionItemType.Property),
        new("Lock", "Device lock state", CompletionItemType.Property),
        new("Setting", "Device setting value", CompletionItemType.Property),
        new("Ratio", "Device ratio (0-1)", CompletionItemType.Property),
        new("Quantity", "Item quantity", CompletionItemType.Property),
        new("Occupied", "Slot occupied state", CompletionItemType.Property),
        new("Mode", "Device mode", CompletionItemType.Property),
        new("Error", "Device error state", CompletionItemType.Property),
        new("Charge", "Battery charge level", CompletionItemType.Property),
        new("SolarAngle", "Solar panel angle", CompletionItemType.Property),
        new("Activate", "Activation state", CompletionItemType.Property),
        new("Vertical", "Vertical angle", CompletionItemType.Property),
        new("Horizontal", "Horizontal angle", CompletionItemType.Property),
        new("RatioOxygen", "Oxygen ratio in atmosphere", CompletionItemType.Property),
        new("RatioCarbonDioxide", "CO2 ratio in atmosphere", CompletionItemType.Property),
        new("RatioNitrogen", "Nitrogen ratio", CompletionItemType.Property),
        new("RatioVolatiles", "Volatiles ratio", CompletionItemType.Property),
        new("TotalMoles", "Total moles in atmosphere", CompletionItemType.Property),
        new("Color", "Light/display color (0-11)", CompletionItemType.Property),

        // Built-in color constants
        new("Blue", "Color constant (0)", CompletionItemType.Variable),
        new("Gray", "Color constant (1)", CompletionItemType.Variable),
        new("Grey", "Color constant (1) - alias for Gray", CompletionItemType.Variable),
        new("Green", "Color constant (2)", CompletionItemType.Variable),
        new("Orange", "Color constant (3)", CompletionItemType.Variable),
        new("Red", "Color constant (4)", CompletionItemType.Variable),
        new("Yellow", "Color constant (5)", CompletionItemType.Variable),
        new("White", "Color constant (6)", CompletionItemType.Variable),
        new("Black", "Color constant (7)", CompletionItemType.Variable),
        new("Brown", "Color constant (8)", CompletionItemType.Variable),
        new("Khaki", "Color constant (9)", CompletionItemType.Variable),
        new("Pink", "Color constant (10)", CompletionItemType.Variable),
        new("Purple", "Color constant (11)", CompletionItemType.Variable),

        // Built-in slot type constants
        new("Import", "Slot type constant (0)", CompletionItemType.Variable),
        new("Export", "Slot type constant (1)", CompletionItemType.Variable),
        new("Content", "Slot type constant (2)", CompletionItemType.Variable),
        new("Fuel", "Slot type constant (3)", CompletionItemType.Variable),

        // Code snippets
        new("IF condition THEN\n    \nENDIF", "If block template", CompletionItemType.Snippet),
        new("FOR i = 1 TO 10\n    \nNEXT i", "For loop template", CompletionItemType.Snippet),
        new("WHILE condition\n    YIELD\nENDWHILE", "While loop template", CompletionItemType.Snippet),
        new("main:\n    \n    YIELD\n    GOTO main", "Main loop template", CompletionItemType.Snippet)
    };

    // Store metadata from last compilation for dynamic completions
    private static SourceMetadata? _currentMetadata;

    /// <summary>
    /// Update completion data with metadata extracted from source code.
    /// Called after successful compilation.
    /// </summary>
    public static void UpdateFromMetadata(SourceMetadata? metadata)
    {
        _currentMetadata = metadata;
    }

    public static List<ICompletionData> GetCompletionData(TextEditor editor, string prefix)
    {
        var results = new List<ICompletionData>();
        var lowerPrefix = prefix.ToLowerInvariant();

        // Check if we're after a dot (property access)
        var offset = editor.CaretOffset;
        var checkOffset = offset - prefix.Length - 1;
        var isPropertyAccess = prefix.Contains('.') ||
            (checkOffset >= 0 && editor.Document.GetCharAt(checkOffset) == '.');

        // Add static completion items
        foreach (var item in AllItems)
        {
            // Filter by context
            if (isPropertyAccess)
            {
                // Only show properties after a dot
                if (item.ItemType != CompletionItemType.Property)
                    continue;
            }
            else
            {
                // Don't show properties without dot context (unless specifically typing property name)
                if (item.ItemType == CompletionItemType.Property && !string.IsNullOrEmpty(prefix))
                {
                    if (!item.Text.ToLowerInvariant().StartsWith(lowerPrefix))
                        continue;
                }
            }

            // Match by prefix
            if (string.IsNullOrEmpty(prefix) ||
                item.Text.ToLowerInvariant().StartsWith(lowerPrefix) ||
                item.Text.ToLowerInvariant().Contains(lowerPrefix))
            {
                results.Add(item);
            }
        }

        // Add dynamic completion items from metadata
        if (_currentMetadata != null && !isPropertyAccess)
        {
            // Add declared variables
            foreach (var variable in _currentMetadata.Variables)
            {
                if (string.IsNullOrEmpty(prefix) ||
                    variable.ToLowerInvariant().StartsWith(lowerPrefix) ||
                    variable.ToLowerInvariant().Contains(lowerPrefix))
                {
                    // Avoid duplicates
                    if (!results.Any(r => r.Text == variable))
                    {
                        results.Add(new BasicCompletionItem(variable, "Variable declared in source", CompletionItemType.Variable));
                    }
                }
            }

            // Add declared constants
            foreach (var (name, value) in _currentMetadata.Constants)
            {
                if (string.IsNullOrEmpty(prefix) ||
                    name.ToLowerInvariant().StartsWith(lowerPrefix) ||
                    name.ToLowerInvariant().Contains(lowerPrefix))
                {
                    if (!results.Any(r => r.Text == name))
                    {
                        results.Add(new BasicCompletionItem(name, $"Constant = {value}", CompletionItemType.Variable));
                    }
                }
            }

            // Add declared labels
            foreach (var label in _currentMetadata.Labels)
            {
                if (string.IsNullOrEmpty(prefix) ||
                    label.ToLowerInvariant().StartsWith(lowerPrefix) ||
                    label.ToLowerInvariant().Contains(lowerPrefix))
                {
                    if (!results.Any(r => r.Text == label))
                    {
                        results.Add(new BasicCompletionItem(label, "Label declared in source", CompletionItemType.Keyword));
                    }
                }
            }

            // Add declared device aliases
            foreach (var (aliasName, deviceType) in _currentMetadata.DeviceTypes)
            {
                if (string.IsNullOrEmpty(prefix) ||
                    aliasName.ToLowerInvariant().StartsWith(lowerPrefix) ||
                    aliasName.ToLowerInvariant().Contains(lowerPrefix))
                {
                    if (!results.Any(r => r.Text == aliasName))
                    {
                        results.Add(new BasicCompletionItem(aliasName, $"Device alias ({deviceType})", CompletionItemType.Device));
                    }
                }
            }

            // Add user-defined functions
            foreach (var (funcName, paramCount) in _currentMetadata.Functions)
            {
                if (string.IsNullOrEmpty(prefix) ||
                    funcName.ToLowerInvariant().StartsWith(lowerPrefix) ||
                    funcName.ToLowerInvariant().Contains(lowerPrefix))
                {
                    if (!results.Any(r => r.Text == funcName))
                    {
                        results.Add(new BasicCompletionItem(funcName, $"User function ({paramCount} params)", CompletionItemType.Function));
                    }
                }
            }
        }

        // Add device-specific properties when accessing known device type
        if (isPropertyAccess && _currentMetadata != null)
        {
            // Try to determine which device is being accessed
            var lineStart = editor.Document.GetLineByOffset(offset).Offset;
            var textBeforeDot = editor.Document.GetText(lineStart, offset - lineStart);
            var parts = textBeforeDot.Split('.');
            if (parts.Length >= 1)
            {
                var deviceName = parts[^1].Trim();
                // Check if this device has type info
                if (_currentMetadata.DeviceTypes.TryGetValue(deviceName, out var deviceType))
                {
                    // Check if we have property hints for this type
                    if (_currentMetadata.DeviceProperties.TryGetValue(deviceType, out var properties))
                    {
                        foreach (var prop in properties)
                        {
                            if (string.IsNullOrEmpty(prefix) ||
                                prop.ToLowerInvariant().StartsWith(lowerPrefix))
                            {
                                if (!results.Any(r => r.Text == prop))
                                {
                                    results.Add(new BasicCompletionItem(prop, $"Property of {deviceType}", CompletionItemType.Property));
                                }
                            }
                        }
                    }
                }
            }
        }

        // Sort by relevance
        return results
            .OrderByDescending(x => ((BasicCompletionItem)x).Priority)
            .ThenBy(x => x.Text)
            .ToList();
    }
}
