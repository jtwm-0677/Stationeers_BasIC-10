using System.Text.Json.Serialization;
using System.Windows.Media;

namespace BasicToMips.Editor.Highlighting;

public class SyntaxColorSettings
{
    // Color properties stored as hex strings for JSON serialization
    public string Keywords { get; set; } = "#569CD6";           // IF, THEN, WHILE
    public string Declarations { get; set; } = "#4EC9B0";       // VAR, LET, ALIAS
    public string DeviceRefs { get; set; } = "#9CDCFE";         // d0, d1, db
    public string Properties { get; set; } = "#9CDCFE";         // .Temperature, .On
    public string Functions { get; set; } = "#DCDCAA";          // ABS, SIN, MAX
    public string Labels { get; set; } = "#C586C0";             // main:
    public string Strings { get; set; } = "#CE9178";            // "text"
    public string Numbers { get; set; } = "#B5CEA8";            // 123, 3.14
    public string Comments { get; set; } = "#6A9955";           // ' comment
    public string Booleans { get; set; } = "#569CD6";           // TRUE, FALSE
    public string Operators { get; set; } = "#D4D4D4";          // +, -, *, /
    public string Brackets { get; set; } = "#FFD700";           // [ ] array brackets
    public string EditorBackground { get; set; } = "#1E1E1E";   // Editor background

    public string PresetName { get; set; } = "Default";

    // Convert hex string to Color
    public static Color HexToColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromRgb(
                Convert.ToByte(hex.Substring(0, 2), 16),
                Convert.ToByte(hex.Substring(2, 2), 16),
                Convert.ToByte(hex.Substring(4, 2), 16));
        }
        return Colors.White;
    }

    // Convert Color to hex string
    public static string ColorToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // Get Color objects for use in highlighting
    public Color GetKeywordsColor() => HexToColor(Keywords);
    public Color GetDeclarationsColor() => HexToColor(Declarations);
    public Color GetDeviceRefsColor() => HexToColor(DeviceRefs);
    public Color GetPropertiesColor() => HexToColor(Properties);
    public Color GetFunctionsColor() => HexToColor(Functions);
    public Color GetLabelsColor() => HexToColor(Labels);
    public Color GetStringsColor() => HexToColor(Strings);
    public Color GetNumbersColor() => HexToColor(Numbers);
    public Color GetCommentsColor() => HexToColor(Comments);
    public Color GetBooleansColor() => HexToColor(Booleans);
    public Color GetOperatorsColor() => HexToColor(Operators);
    public Color GetBracketsColor() => HexToColor(Brackets);
    public Color GetEditorBackgroundColor() => HexToColor(EditorBackground);

    // Create a deep copy
    public SyntaxColorSettings Clone()
    {
        return new SyntaxColorSettings
        {
            Keywords = Keywords,
            Declarations = Declarations,
            DeviceRefs = DeviceRefs,
            Properties = Properties,
            Functions = Functions,
            Labels = Labels,
            Strings = Strings,
            Numbers = Numbers,
            Comments = Comments,
            Booleans = Booleans,
            Operators = Operators,
            Brackets = Brackets,
            EditorBackground = EditorBackground,
            PresetName = PresetName
        };
    }

    // Preset definitions
    public static SyntaxColorSettings GetPreset(string name)
    {
        return name switch
        {
            "Default" => GetDefaultPreset(),
            "Protanopia" => GetProtanopiaPreset(),
            "Deuteranopia" => GetDeuteranopiaPreset(),
            "Tritanopia" => GetTritanopiaPreset(),
            "High Contrast" => GetHighContrastPreset(),
            "Monochrome" => GetMonochromePreset(),
            _ => GetDefaultPreset()
        };
    }

    public static string[] GetPresetNames() => new[]
    {
        "Default",
        "Protanopia",
        "Deuteranopia",
        "Tritanopia",
        "High Contrast",
        "Monochrome",
        "Custom"
    };

    private static SyntaxColorSettings GetDefaultPreset() => new()
    {
        PresetName = "Default",
        Keywords = "#569CD6",
        Declarations = "#4EC9B0",
        DeviceRefs = "#9CDCFE",
        Properties = "#9CDCFE",
        Functions = "#DCDCAA",
        Labels = "#C586C0",
        Strings = "#CE9178",
        Numbers = "#B5CEA8",
        Comments = "#6A9955",
        Booleans = "#569CD6",
        Operators = "#D4D4D4",
        Brackets = "#FFD700",
        EditorBackground = "#1E1E1E"
    };

    // Protanopia (red-blind): Avoid red, use blue/yellow distinction
    private static SyntaxColorSettings GetProtanopiaPreset() => new()
    {
        PresetName = "Protanopia",
        Keywords = "#6699FF",       // Bright blue
        Declarations = "#00CCCC",   // Cyan
        DeviceRefs = "#FFCC00",     // Yellow
        Properties = "#FFCC00",     // Yellow
        Functions = "#99CCFF",      // Light blue
        Labels = "#FF99FF",         // Pink (safe)
        Strings = "#FFFF66",        // Bright yellow
        Numbers = "#66FFCC",        // Cyan-green
        Comments = "#999999",       // Gray
        Booleans = "#6699FF",       // Bright blue
        Operators = "#FFFFFF",      // White
        Brackets = "#FF99FF",       // Pink (safe for protanopia)
        EditorBackground = "#1E1E1E"
    };

    // Deuteranopia (green-blind): Avoid green, use blue/orange distinction
    private static SyntaxColorSettings GetDeuteranopiaPreset() => new()
    {
        PresetName = "Deuteranopia",
        Keywords = "#6699FF",       // Bright blue
        Declarations = "#FF9933",   // Orange
        DeviceRefs = "#FFCC00",     // Yellow
        Properties = "#FFCC00",     // Yellow
        Functions = "#CC99FF",      // Lavender
        Labels = "#FF66CC",         // Pink
        Strings = "#FFFF66",        // Bright yellow
        Numbers = "#66CCFF",        // Sky blue
        Comments = "#999999",       // Gray
        Booleans = "#6699FF",       // Bright blue
        Operators = "#FFFFFF",      // White
        Brackets = "#FF9933",       // Orange (safe for deuteranopia)
        EditorBackground = "#1E1E1E"
    };

    // Tritanopia (blue-blind): Avoid blue, use red/green distinction
    private static SyntaxColorSettings GetTritanopiaPreset() => new()
    {
        PresetName = "Tritanopia",
        Keywords = "#FF6666",       // Coral red
        Declarations = "#66FF66",   // Bright green
        DeviceRefs = "#FFCC66",     // Gold
        Properties = "#FFCC66",     // Gold
        Functions = "#FF99CC",      // Pink
        Labels = "#CC66FF",         // Violet
        Strings = "#FFFF99",        // Pale yellow
        Numbers = "#99FF99",        // Light green
        Comments = "#AAAAAA",       // Gray
        Booleans = "#FF6666",       // Coral red
        Operators = "#FFFFFF",      // White
        Brackets = "#FFCC66",       // Gold (safe for tritanopia)
        EditorBackground = "#1E1E1E"
    };

    // High Contrast: Maximum distinction with bold primary colors
    private static SyntaxColorSettings GetHighContrastPreset() => new()
    {
        PresetName = "High Contrast",
        Keywords = "#FF0000",       // Pure red
        Declarations = "#00FF00",   // Pure green
        DeviceRefs = "#FFFF00",     // Pure yellow
        Properties = "#00FFFF",     // Pure cyan
        Functions = "#FF00FF",      // Pure magenta
        Labels = "#FFA500",         // Orange
        Strings = "#FFD700",        // Gold
        Numbers = "#00FF7F",        // Spring green
        Comments = "#808080",       // Gray
        Booleans = "#FF0000",       // Pure red
        Operators = "#FFFFFF",      // White
        Brackets = "#00FFFF",       // Pure cyan
        EditorBackground = "#000000"  // Pure black for maximum contrast
    };

    // Monochrome: Grayscale with brightness distinction
    private static SyntaxColorSettings GetMonochromePreset() => new()
    {
        PresetName = "Monochrome",
        Keywords = "#FFFFFF",       // White (brightest)
        Declarations = "#E0E0E0",   // Very light gray
        DeviceRefs = "#C0C0C0",     // Light gray
        Properties = "#C0C0C0",     // Light gray
        Functions = "#A0A0A0",      // Medium-light gray
        Labels = "#FFFFFF",         // White
        Strings = "#808080",        // Medium gray
        Numbers = "#B0B0B0",        // Light-medium gray
        Comments = "#606060",       // Dark gray
        Booleans = "#FFFFFF",       // White
        Operators = "#D0D0D0",      // Near-white
        Brackets = "#E0E0E0",       // Very light gray
        EditorBackground = "#1E1E1E"
    };
}
