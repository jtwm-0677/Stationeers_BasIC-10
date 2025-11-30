using System.IO;
using System.Windows.Threading;

namespace BasicToMips.UI.Services;

/// <summary>
/// Provides automatic backup saves to protect user work in case of crash.
/// </summary>
public class AutoSaveService
{
    private readonly string _autoSaveDir;
    private readonly string _autoSavePath;
    private readonly string _metadataPath;
    private DispatcherTimer? _autoSaveTimer;
    private string _lastSavedContent = "";
    private Func<string>? _getContentFunc;
    private Func<string?>? _getCurrentFilePathFunc;

    /// <summary>
    /// Auto-save interval in seconds. Default: 60 seconds.
    /// </summary>
    public int IntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Whether auto-save is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Event raised when auto-save occurs.
    /// </summary>
    public event EventHandler<AutoSaveEventArgs>? AutoSaved;

    public AutoSaveService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _autoSaveDir = Path.Combine(appData, "BasicToMips", "AutoSave");
        Directory.CreateDirectory(_autoSaveDir);
        _autoSavePath = Path.Combine(_autoSaveDir, "autosave.basic");
        _metadataPath = Path.Combine(_autoSaveDir, "autosave.meta");
    }

    /// <summary>
    /// Initialize auto-save with content provider functions.
    /// </summary>
    public void Initialize(Func<string> getContent, Func<string?> getCurrentFilePath)
    {
        _getContentFunc = getContent;
        _getCurrentFilePathFunc = getCurrentFilePath;
    }

    /// <summary>
    /// Start the auto-save timer.
    /// </summary>
    public void Start()
    {
        if (_autoSaveTimer != null)
        {
            _autoSaveTimer.Stop();
        }

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(IntervalSeconds)
        };
        _autoSaveTimer.Tick += AutoSaveTimer_Tick;
        _autoSaveTimer.Start();
    }

    /// <summary>
    /// Stop the auto-save timer.
    /// </summary>
    public void Stop()
    {
        _autoSaveTimer?.Stop();
    }

    /// <summary>
    /// Update the timer interval without restarting.
    /// </summary>
    public void UpdateInterval(int seconds)
    {
        IntervalSeconds = seconds;
        if (_autoSaveTimer != null)
        {
            _autoSaveTimer.Interval = TimeSpan.FromSeconds(seconds);
        }
    }

    private void AutoSaveTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsEnabled || _getContentFunc == null) return;

        try
        {
            var content = _getContentFunc();

            // Only save if content has changed
            if (content == _lastSavedContent) return;
            if (string.IsNullOrWhiteSpace(content)) return;

            // Save the content
            File.WriteAllText(_autoSavePath, content);

            // Save metadata (original file path, timestamp)
            var metadata = new AutoSaveMetadata
            {
                OriginalFilePath = _getCurrentFilePathFunc?.Invoke(),
                Timestamp = DateTime.Now,
                ContentHash = content.GetHashCode()
            };
            var metaJson = System.Text.Json.JsonSerializer.Serialize(metadata);
            File.WriteAllText(_metadataPath, metaJson);

            _lastSavedContent = content;

            AutoSaved?.Invoke(this, new AutoSaveEventArgs { FilePath = _autoSavePath, Timestamp = metadata.Timestamp });
        }
        catch
        {
            // Silently ignore auto-save failures
        }
    }

    /// <summary>
    /// Force an immediate auto-save.
    /// </summary>
    public void SaveNow()
    {
        AutoSaveTimer_Tick(null, EventArgs.Empty);
    }

    /// <summary>
    /// Check if there's a recovery file available.
    /// </summary>
    public bool HasRecoveryFile()
    {
        return File.Exists(_autoSavePath) && File.Exists(_metadataPath);
    }

    /// <summary>
    /// Get recovery file information.
    /// </summary>
    public AutoSaveMetadata? GetRecoveryInfo()
    {
        if (!HasRecoveryFile()) return null;

        try
        {
            var json = File.ReadAllText(_metadataPath);
            return System.Text.Json.JsonSerializer.Deserialize<AutoSaveMetadata>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Recover the auto-saved content.
    /// </summary>
    public string? RecoverContent()
    {
        if (!HasRecoveryFile()) return null;

        try
        {
            return File.ReadAllText(_autoSavePath);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Delete the recovery files (called after successful recovery or user dismissal).
    /// </summary>
    public void ClearRecoveryFiles()
    {
        try
        {
            if (File.Exists(_autoSavePath)) File.Delete(_autoSavePath);
            if (File.Exists(_metadataPath)) File.Delete(_metadataPath);
            _lastSavedContent = "";
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    /// <summary>
    /// Called when user successfully saves their file - clears recovery.
    /// </summary>
    public void OnUserSaved()
    {
        ClearRecoveryFiles();
    }

    /// <summary>
    /// Reset the last saved content tracker (call when loading new file).
    /// </summary>
    public void ResetContentTracker()
    {
        _lastSavedContent = "";
    }
}

public class AutoSaveMetadata
{
    public string? OriginalFilePath { get; set; }
    public DateTime Timestamp { get; set; }
    public int ContentHash { get; set; }
}

public class AutoSaveEventArgs : EventArgs
{
    public string FilePath { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
