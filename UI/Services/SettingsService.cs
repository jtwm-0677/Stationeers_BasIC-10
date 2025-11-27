using System.IO;
using System.Text.Json;

namespace BasicToMips.UI.Services;

public class SettingsService
{
    private readonly string _settingsPath;

    public string? StationeersPath { get; set; }
    public bool ShowDocumentation { get; set; } = true;
    public bool AutoCompile { get; set; } = true;
    public List<string> RecentFiles { get; set; } = new();
    public double FontSize { get; set; } = 14;
    public bool WordWrap { get; set; } = false;
    public int OptimizationLevel { get; set; } = 1;
    public string Theme { get; set; } = "Dark"; // "Dark" or "Light"

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(appData, "BasicToMips");
        Directory.CreateDirectory(settingsDir);
        _settingsPath = Path.Combine(settingsDir, "settings.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);
                if (settings != null)
                {
                    StationeersPath = settings.StationeersPath;
                    ShowDocumentation = settings.ShowDocumentation;
                    AutoCompile = settings.AutoCompile;
                    RecentFiles = settings.RecentFiles ?? new List<string>();
                    FontSize = settings.FontSize > 0 ? settings.FontSize : 14;
                    WordWrap = settings.WordWrap;
                    OptimizationLevel = settings.OptimizationLevel;
                    Theme = settings.Theme ?? "Dark";
                }
            }
        }
        catch
        {
            // Use defaults if settings can't be loaded
        }
    }

    public void Save()
    {
        try
        {
            var settings = new SettingsData
            {
                StationeersPath = StationeersPath,
                ShowDocumentation = ShowDocumentation,
                AutoCompile = AutoCompile,
                RecentFiles = RecentFiles,
                FontSize = FontSize,
                WordWrap = WordWrap,
                OptimizationLevel = OptimizationLevel,
                Theme = Theme
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public void AddRecentFile(string path)
    {
        RecentFiles.Remove(path);
        RecentFiles.Insert(0, path);
        if (RecentFiles.Count > 10)
        {
            RecentFiles = RecentFiles.Take(10).ToList();
        }
        Save();
    }

    private class SettingsData
    {
        public string? StationeersPath { get; set; }
        public bool ShowDocumentation { get; set; }
        public bool AutoCompile { get; set; }
        public List<string>? RecentFiles { get; set; }
        public double FontSize { get; set; }
        public bool WordWrap { get; set; }
        public int OptimizationLevel { get; set; }
        public string? Theme { get; set; }
    }
}
