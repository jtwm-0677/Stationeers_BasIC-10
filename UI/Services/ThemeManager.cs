using System.Windows;

namespace BasicToMips.UI.Services;

public static class ThemeManager
{
    public static string CurrentTheme { get; private set; } = "Dark";

    public static void ApplyTheme(string theme)
    {
        CurrentTheme = theme;

        // Get the application's merged dictionaries
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

        // Find and remove existing theme
        ResourceDictionary? existingTheme = null;
        foreach (var dict in mergedDictionaries)
        {
            var source = dict.Source?.ToString() ?? "";
            if (source.Contains("DarkTheme.xaml") || source.Contains("LightTheme.xaml"))
            {
                existingTheme = dict;
                break;
            }
        }

        if (existingTheme != null)
        {
            mergedDictionaries.Remove(existingTheme);
        }

        // Add new theme
        var themeUri = theme == "Light"
            ? new Uri("UI/Themes/LightTheme.xaml", UriKind.Relative)
            : new Uri("UI/Themes/DarkTheme.xaml", UriKind.Relative);

        var newTheme = new ResourceDictionary { Source = themeUri };
        mergedDictionaries.Insert(0, newTheme);
    }

    public static void Initialize(string theme)
    {
        CurrentTheme = theme;
        ApplyTheme(theme);
    }
}
