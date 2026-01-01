using System.IO;
using System.Text.Json;
using BasicToMips.Editor.Highlighting;
using BasicToMips.Shared;
using BasicToMips.UI.VisualScripting;
using BasicToMips.UI.VisualScripting.Project;
using BasicToMips.UI.VisualScripting.Animations;

namespace BasicToMips.UI.Services;

public class SettingsService
{
    private readonly string _settingsPath;

    public string? StationeersPath { get; set; }
    public bool ShowDocumentation { get; set; } = true;
    public bool AutoCompile { get; set; } = true;
    public bool AutoCompleteEnabled { get; set; } = true;
    public List<string> RecentFiles { get; set; } = new();
    public double FontSize { get; set; } = 14;
    public bool WordWrap { get; set; } = false;
    public int OptimizationLevel { get; set; } = 1;
    public OutputMode OutputMode { get; set; } = OutputMode.Readable;
    public string Theme { get; set; } = "Dark"; // "Dark" or "Light"
    public bool AutoSaveEnabled { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 60; // Default: 1 minute
    public SyntaxColorSettings SyntaxColors { get; set; } = new();
    public string SplitViewMode { get; set; } = "Vertical"; // "Vertical", "Horizontal", or "EditorOnly"

    // Retro visual effects
    public bool BlockCursorEnabled { get; set; } = true;
    public bool CurrentLineHighlightEnabled { get; set; } = true;
    public bool ScanlineOverlayEnabled { get; set; } = false;
    public bool ScreenGlowEnabled { get; set; } = false;
    public bool RetroFontEnabled { get; set; } = false;
    public string RetroFontChoice { get; set; } = "Default"; // "Default", "Apple", "TRS80"
    public bool StartupBeepEnabled { get; set; } = false;

    // MCP Integration / HTTP API settings
    public bool ApiServerEnabled { get; set; } = true;
    public int ApiServerPort { get; set; } = 19410;

    // Script metadata for instruction.xml
    public string ScriptAuthor { get; set; } = "";
    public string ScriptDescription { get; set; } = "";

    // Dashboard window settings
    public bool ShowDashboard { get; set; } = false;

    // Visual Scripting Experience Mode
    public ExperienceLevel ExperienceMode { get; set; } = ExperienceLevel.Beginner;
    public ExperienceModeSettings? CustomModeSettings { get; set; } = null;

    // Recent Visual Script Projects
    public List<RecentProjectData> RecentVisualProjects { get; set; } = new();

    // Visual Script Auto-save settings
    public bool VisualScriptAutoSaveEnabled { get; set; } = true;
    public int VisualScriptAutoSaveIntervalMinutes { get; set; } = 5;

    // Visual Script Animation Settings
    public AnimationSettings? VisualScriptAnimationSettings { get; set; } = null;

    // Version tracking for new feature notifications
    public string LastSeenVersion { get; set; } = "";

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
                    AutoCompleteEnabled = settings.AutoCompleteEnabled;
                    RecentFiles = settings.RecentFiles ?? new List<string>();
                    FontSize = settings.FontSize > 0 ? settings.FontSize : 14;
                    WordWrap = settings.WordWrap;
                    OptimizationLevel = settings.OptimizationLevel;
                    OutputMode = settings.OutputMode;
                    Theme = settings.Theme ?? "Dark";
                    AutoSaveEnabled = settings.AutoSaveEnabled;
                    AutoSaveIntervalSeconds = settings.AutoSaveIntervalSeconds > 0 ? settings.AutoSaveIntervalSeconds : 60;
                    SyntaxColors = settings.SyntaxColors ?? new SyntaxColorSettings();
                    SplitViewMode = settings.SplitViewMode ?? "Vertical";
                    // Retro effects
                    BlockCursorEnabled = settings.BlockCursorEnabled;
                    CurrentLineHighlightEnabled = settings.CurrentLineHighlightEnabled;
                    ScanlineOverlayEnabled = settings.ScanlineOverlayEnabled;
                    ScreenGlowEnabled = settings.ScreenGlowEnabled;
                    RetroFontEnabled = settings.RetroFontEnabled;
                    RetroFontChoice = settings.RetroFontChoice ?? "Default";
                    StartupBeepEnabled = settings.StartupBeepEnabled;
                    // API settings
                    ApiServerEnabled = settings.ApiServerEnabled;
                    ApiServerPort = settings.ApiServerPort > 0 ? settings.ApiServerPort : 19410;
                    // Script metadata
                    ScriptAuthor = settings.ScriptAuthor ?? "";
                    ScriptDescription = settings.ScriptDescription ?? "";
                    // Dashboard
                    ShowDashboard = settings.ShowDashboard;
                    // Experience Mode
                    ExperienceMode = settings.ExperienceMode;
                    CustomModeSettings = settings.CustomModeSettings;
                    // Recent Visual Projects
                    RecentVisualProjects = settings.RecentVisualProjects ?? new List<RecentProjectData>();
                    // Visual Script Auto-save
                    VisualScriptAutoSaveEnabled = settings.VisualScriptAutoSaveEnabled;
                    VisualScriptAutoSaveIntervalMinutes = settings.VisualScriptAutoSaveIntervalMinutes > 0
                        ? settings.VisualScriptAutoSaveIntervalMinutes : 5;
                    // Animation Settings
                    VisualScriptAnimationSettings = settings.VisualScriptAnimationSettings ?? new AnimationSettings();
                    // Version tracking
                    LastSeenVersion = settings.LastSeenVersion ?? "";
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
                AutoCompleteEnabled = AutoCompleteEnabled,
                RecentFiles = RecentFiles,
                FontSize = FontSize,
                WordWrap = WordWrap,
                OptimizationLevel = OptimizationLevel,
                OutputMode = OutputMode,
                Theme = Theme,
                AutoSaveEnabled = AutoSaveEnabled,
                AutoSaveIntervalSeconds = AutoSaveIntervalSeconds,
                SyntaxColors = SyntaxColors,
                SplitViewMode = SplitViewMode,
                // Retro effects
                BlockCursorEnabled = BlockCursorEnabled,
                CurrentLineHighlightEnabled = CurrentLineHighlightEnabled,
                ScanlineOverlayEnabled = ScanlineOverlayEnabled,
                ScreenGlowEnabled = ScreenGlowEnabled,
                RetroFontEnabled = RetroFontEnabled,
                RetroFontChoice = RetroFontChoice,
                StartupBeepEnabled = StartupBeepEnabled,
                // API settings
                ApiServerEnabled = ApiServerEnabled,
                ApiServerPort = ApiServerPort,
                // Script metadata
                ScriptAuthor = ScriptAuthor,
                ScriptDescription = ScriptDescription,
                // Dashboard
                ShowDashboard = ShowDashboard,
                // Experience Mode
                ExperienceMode = ExperienceMode,
                CustomModeSettings = CustomModeSettings,
                // Recent Visual Projects
                RecentVisualProjects = RecentVisualProjects,
                // Visual Script Auto-save
                VisualScriptAutoSaveEnabled = VisualScriptAutoSaveEnabled,
                VisualScriptAutoSaveIntervalMinutes = VisualScriptAutoSaveIntervalMinutes,
                // Animation Settings
                VisualScriptAnimationSettings = VisualScriptAnimationSettings,
                // Version tracking
                LastSeenVersion = LastSeenVersion
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

    /// <summary>
    /// Checks if this is the first run after updating to a version with the hash dictionary feature.
    /// Returns true if the user should see the notification, and marks it as seen.
    /// </summary>
    public bool ShouldShowHashDictionaryNotification(string currentVersion)
    {
        // The hash dictionary was introduced in v3.0.28
        const string hashDictionaryVersion = "3.0.28";

        // If user has seen this version or later, don't show
        if (!string.IsNullOrEmpty(LastSeenVersion))
        {
            if (CompareVersions(LastSeenVersion, hashDictionaryVersion) >= 0)
            {
                return false;
            }
        }

        // Update the last seen version
        LastSeenVersion = currentVersion;
        Save();

        return true;
    }

    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var parts2 = v2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;
            if (p1 != p2) return p1.CompareTo(p2);
        }
        return 0;
    }

    private class SettingsData
    {
        public string? StationeersPath { get; set; }
        public bool ShowDocumentation { get; set; }
        public bool AutoCompile { get; set; }
        public bool AutoCompleteEnabled { get; set; } = true;
        public List<string>? RecentFiles { get; set; }
        public double FontSize { get; set; }
        public bool WordWrap { get; set; }
        public int OptimizationLevel { get; set; }
        public OutputMode OutputMode { get; set; } = OutputMode.Readable;
        public string? Theme { get; set; }
        public bool AutoSaveEnabled { get; set; } = true;
        public int AutoSaveIntervalSeconds { get; set; } = 60;
        public SyntaxColorSettings? SyntaxColors { get; set; }
        public string? SplitViewMode { get; set; }
        // Retro effects
        public bool BlockCursorEnabled { get; set; } = true;
        public bool CurrentLineHighlightEnabled { get; set; } = true;
        public bool ScanlineOverlayEnabled { get; set; } = false;
        public bool ScreenGlowEnabled { get; set; } = false;
        public bool RetroFontEnabled { get; set; } = false;
        public string? RetroFontChoice { get; set; } = "Default";
        public bool StartupBeepEnabled { get; set; } = false;
        // API settings
        public bool ApiServerEnabled { get; set; } = true;
        public int ApiServerPort { get; set; } = 19410;
        // Script metadata
        public string? ScriptAuthor { get; set; }
        public string? ScriptDescription { get; set; }
        // Dashboard
        public bool ShowDashboard { get; set; } = false;
        // Experience Mode
        public ExperienceLevel ExperienceMode { get; set; } = ExperienceLevel.Beginner;
        public ExperienceModeSettings? CustomModeSettings { get; set; } = null;
        // Recent Visual Projects
        public List<RecentProjectData>? RecentVisualProjects { get; set; } = null;
        // Visual Script Auto-save
        public bool VisualScriptAutoSaveEnabled { get; set; } = true;
        public int VisualScriptAutoSaveIntervalMinutes { get; set; } = 5;
        // Animation Settings
        public AnimationSettings? VisualScriptAnimationSettings { get; set; } = null;
        // Version tracking
        public string? LastSeenVersion { get; set; }
    }
}
