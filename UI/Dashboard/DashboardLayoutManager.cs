using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasicToMips.UI.Dashboard;

/// <summary>
/// Manages saving and loading dashboard layouts
/// </summary>
public class DashboardLayoutManager
{
    private readonly string _globalLayoutPath;

    public DashboardLayoutManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dashboardDir = Path.Combine(appData, "BasicToMips", "Dashboard");
        Directory.CreateDirectory(dashboardDir);
        _globalLayoutPath = Path.Combine(dashboardDir, "layout.json");
    }

    /// <summary>
    /// Save layout to global default
    /// </summary>
    public void SaveGlobalLayout(DashboardLayout layout)
    {
        try
        {
            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_globalLayoutPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving global layout: {ex.Message}");
        }
    }

    /// <summary>
    /// Load layout from global default
    /// </summary>
    public DashboardLayout? LoadGlobalLayout()
    {
        try
        {
            if (File.Exists(_globalLayoutPath))
            {
                var json = File.ReadAllText(_globalLayoutPath);
                return JsonSerializer.Deserialize<DashboardLayout>(json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading global layout: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Save layout to project folder
    /// </summary>
    public void SaveProjectLayout(string projectPath, DashboardLayout layout)
    {
        try
        {
            var projectDir = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrEmpty(projectDir))
                return;

            var layoutPath = Path.Combine(projectDir, "dashboard_layout.json");
            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(layoutPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving project layout: {ex.Message}");
        }
    }

    /// <summary>
    /// Load layout from project folder, with fallback to global
    /// </summary>
    public DashboardLayout LoadLayout(string? projectPath)
    {
        DashboardLayout? layout = null;

        // Try project-specific layout first
        if (!string.IsNullOrEmpty(projectPath))
        {
            try
            {
                var projectDir = Path.GetDirectoryName(projectPath);
                if (!string.IsNullOrEmpty(projectDir))
                {
                    var layoutPath = Path.Combine(projectDir, "dashboard_layout.json");
                    if (File.Exists(layoutPath))
                    {
                        var json = File.ReadAllText(layoutPath);
                        layout = JsonSerializer.Deserialize<DashboardLayout>(json);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading project layout: {ex.Message}");
            }
        }

        // Fall back to global layout
        if (layout == null)
        {
            layout = LoadGlobalLayout();
        }

        // Fall back to default layout
        if (layout == null)
        {
            layout = CreateDefaultLayout();
        }

        return layout;
    }

    /// <summary>
    /// Export layout to a file
    /// </summary>
    public void ExportLayout(string filePath, DashboardLayout layout)
    {
        try
        {
            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error exporting layout: {ex.Message}");
        }
    }

    /// <summary>
    /// Import layout from a file
    /// </summary>
    public DashboardLayout? ImportLayout(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<DashboardLayout>(json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error importing layout: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Create default layout with TaskChecklist widget
    /// </summary>
    private DashboardLayout CreateDefaultLayout()
    {
        return new DashboardLayout
        {
            Version = "1.0",
            GridRows = 4,
            GridColumns = 4,
            Widgets = new List<WidgetLayout>
            {
                new WidgetLayout
                {
                    Type = "TaskChecklist",
                    Row = 0,
                    Column = 0,
                    RowSpan = 2,
                    ColumnSpan = 2,
                    State = new Dictionary<string, object>()
                }
            }
        };
    }
}

/// <summary>
/// Dashboard layout data structure
/// </summary>
public class DashboardLayout
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("gridRows")]
    public int GridRows { get; set; } = 4;

    [JsonPropertyName("gridColumns")]
    public int GridColumns { get; set; } = 4;

    [JsonPropertyName("widgets")]
    public List<WidgetLayout> Widgets { get; set; } = new();
}

/// <summary>
/// Widget layout data structure
/// </summary>
public class WidgetLayout
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("row")]
    public int Row { get; set; }

    [JsonPropertyName("column")]
    public int Column { get; set; }

    [JsonPropertyName("rowSpan")]
    public int RowSpan { get; set; } = 1;

    [JsonPropertyName("columnSpan")]
    public int ColumnSpan { get; set; } = 1;

    [JsonPropertyName("state")]
    public Dictionary<string, object> State { get; set; } = new();
}
