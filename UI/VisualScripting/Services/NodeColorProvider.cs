using System.Windows.Media;
using BasicToMips.Editor.Highlighting;

namespace BasicToMips.UI.VisualScripting.Services;

/// <summary>
/// Provides node header colors based on the current syntax color settings.
/// Maps node categories to corresponding syntax highlighting colors for accessibility.
/// </summary>
public static class NodeColorProvider
{
    private static SyntaxColorSettings? _currentSettings;

    /// <summary>
    /// Update the color settings. Should be called when syntax colors change.
    /// </summary>
    public static void UpdateSettings(SyntaxColorSettings settings)
    {
        _currentSettings = settings;
        ColorsChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Event raised when colors change (for UI refresh).
    /// </summary>
    public static event EventHandler? ColorsChanged;

    /// <summary>
    /// Get the color for a node category based on current syntax settings.
    /// Maps categories to syntax highlighting colors:
    /// - Flow → Keywords (IF, THEN, WHILE are flow control)
    /// - Variables → Declarations (VAR, LET, ALIAS)
    /// - Math → Functions (ABS, SIN, MAX)
    /// - Logic → Booleans (TRUE, FALSE)
    /// - Devices → DeviceRefs (d0, d1, db)
    /// - Comments → Comments
    /// </summary>
    public static Brush GetCategoryColor(string category)
    {
        if (_currentSettings == null)
        {
            // Fallback to default colors if settings not loaded
            return GetDefaultCategoryColor(category);
        }

        var color = category switch
        {
            "Flow" => _currentSettings.GetKeywordsColor(),
            "Variables" => _currentSettings.GetDeclarationsColor(),
            "Math" => _currentSettings.GetFunctionsColor(),
            "Logic" => _currentSettings.GetBooleansColor(),
            "Devices" => _currentSettings.GetDeviceRefsColor(),
            "Comments" => _currentSettings.GetCommentsColor(),
            _ => _currentSettings.GetKeywordsColor() // Default to keywords color
        };

        return new SolidColorBrush(color);
    }

    /// <summary>
    /// Get default colors when settings aren't loaded.
    /// These are accessibility-safe defaults.
    /// </summary>
    private static Brush GetDefaultCategoryColor(string category)
    {
        return category switch
        {
            "Flow" => new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),      // Blue
            "Variables" => new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)), // Teal
            "Math" => new SolidColorBrush(Color.FromRgb(0xDC, 0xDC, 0xAA)),      // Yellow
            "Logic" => new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6)),     // Blue
            "Devices" => new SolidColorBrush(Color.FromRgb(0x9C, 0xDC, 0xFE)),   // Light blue
            "Comments" => new SolidColorBrush(Color.FromRgb(0x6A, 0x99, 0x55)),  // Green
            _ => new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6))            // Default blue
        };
    }

    /// <summary>
    /// Get the current preset name.
    /// </summary>
    public static string CurrentPresetName => _currentSettings?.PresetName ?? "Default";
}
