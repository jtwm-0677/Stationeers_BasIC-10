using System.IO;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;

namespace BasicToMips.Editor.RetroEffects;

/// <summary>
/// Manages retro font styling for the editor
/// </summary>
public static class RetroFontManager
{
    private static readonly Dictionary<TextEditor, FontFamily> _originalFonts = new();
    private static FontFamily? _appleFontFamily;
    private static FontFamily? _trs80FontFamily;
    private static bool _fontsInitialized = false;

    // Font choice options
    public const string FontDefault = "Default";
    public const string FontApple = "Apple";
    public const string FontTRS80 = "TRS80";

    // Classic programming fonts in order of preference (for Default)
    private static readonly string[] DefaultRetroFontNames = new[]
    {
        "Consolas",           // Windows default monospace (clean, readable)
        "Lucida Console",     // Classic Windows console font
        "Courier New",        // Universal fallback
    };

    private static void InitializeFonts()
    {
        if (_fontsInitialized) return;
        _fontsInitialized = true;

        try
        {
            // Get the application's base directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var appleFontPath = Path.Combine(baseDir, "Apple.ttf");
            var trs80FontPath = Path.Combine(baseDir, "trs80computer.ttf");

            // Try loading from file system first (works better with single-file publish)
            if (File.Exists(appleFontPath))
            {
                // Use file:// URI for fonts in the app directory
                var fontDir = new Uri(baseDir);
                _appleFontFamily = new FontFamily(fontDir, "./#Apple ][");
            }

            if (File.Exists(trs80FontPath))
            {
                var fontDir = new Uri(baseDir);
                _trs80FontFamily = new FontFamily(fontDir, "./#TRS-80 Color Computer");
            }

            // If file system loading didn't work, try embedded resource
            if (_appleFontFamily == null)
            {
                try
                {
                    _appleFontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Apple.ttf#Apple ][");
                }
                catch { }
            }

            if (_trs80FontFamily == null)
            {
                try
                {
                    _trs80FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./trs80computer.ttf#TRS-80 Color Computer");
                }
                catch { }
            }
        }
        catch
        {
            // Fonts won't load, will fall back to system fonts
        }
    }

    public static void EnableRetroFont(TextEditor editor, string fontChoice = FontDefault)
    {
        InitializeFonts();

        // Store original font if not already stored
        if (!_originalFonts.ContainsKey(editor))
        {
            _originalFonts[editor] = editor.FontFamily;
        }

        FontFamily? selectedFont = fontChoice switch
        {
            FontApple => _appleFontFamily,
            FontTRS80 => _trs80FontFamily,
            _ => null
        };

        // Fall back to the system retro chain for the Default choice, or if a bundled font
        // (Apple/TRS-80) failed to load. Never leave the editor on the system UI font. (#5)
        selectedFont ??= new FontFamily(string.Join(", ", DefaultRetroFontNames));

        editor.FontFamily = selectedFont;
    }

    public static void DisableRetroFont(TextEditor editor)
    {
        if (_originalFonts.TryGetValue(editor, out var originalFont))
        {
            editor.FontFamily = originalFont;
        }
    }

    public static void SetEnabled(TextEditor editor, bool enabled, string fontChoice = FontDefault)
    {
        if (enabled)
        {
            EnableRetroFont(editor, fontChoice);
        }
        else
        {
            DisableRetroFont(editor);
        }
    }
}
