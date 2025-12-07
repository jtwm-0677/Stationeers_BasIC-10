using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace BasicToMips.UI.Dashboard;

public partial class DashboardWindow : Window
{
    private readonly DashboardLayoutManager _layoutManager;
    private readonly WidgetFactory _widgetFactory;
    private string? _projectPath;
    private bool _isLoaded;

    // Window position and size settings
    private double _savedLeft = 100;
    private double _savedTop = 100;
    private double _savedWidth = 800;
    private double _savedHeight = 600;
    private bool _alwaysOnTop;

    public DashboardWindow()
    {
        InitializeComponent();
        _layoutManager = new DashboardLayoutManager();
        _widgetFactory = new WidgetFactory();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        LoadWindowSettings();
        LoadLayout();
        UpdateGridInfo();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowSettings();
        SaveCurrentLayout();
    }

    private void Window_LocationChanged(object? sender, EventArgs e)
    {
        if (_isLoaded && WindowState == WindowState.Normal)
        {
            _savedLeft = Left;
            _savedTop = Top;
        }
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isLoaded && WindowState == WindowState.Normal)
        {
            _savedWidth = Width;
            _savedHeight = Height;
        }
    }

    public void SetProjectPath(string? projectPath)
    {
        _projectPath = projectPath;
        LoadLayout();
    }

    private void LoadLayout()
    {
        var layout = _layoutManager.LoadLayout(_projectPath);

        // Clear existing widgets
        DashboardGridControl.ClearWidgets();

        // Set grid size
        DashboardGridControl.GridRows = layout.GridRows;
        DashboardGridControl.GridColumns = layout.GridColumns;

        // Create widgets
        foreach (var widgetLayout in layout.Widgets)
        {
            var widget = _widgetFactory.CreateWidget(widgetLayout.Type);
            if (widget != null)
            {
                widget.GridRow = widgetLayout.Row;
                widget.GridColumn = widgetLayout.Column;
                widget.RowSpan = widgetLayout.RowSpan;
                widget.ColumnSpan = widgetLayout.ColumnSpan;

                // Set project path for widgets that need it
                if (widget is Widgets.TaskChecklistWidget taskWidget)
                {
                    taskWidget.SetProjectPath(_projectPath);
                }

                DashboardGridControl.AddWidget(widget);
            }
        }

        UpdateGridInfo();
        StatusText.Text = "Layout loaded";
    }

    private void SaveCurrentLayout()
    {
        var layout = new DashboardLayout
        {
            Version = "1.0",
            GridRows = DashboardGridControl.GridRows,
            GridColumns = DashboardGridControl.GridColumns,
            Widgets = new List<WidgetLayout>()
        };

        foreach (var widget in DashboardGridControl.GetWidgets())
        {
            var widgetLayout = new WidgetLayout
            {
                Type = GetWidgetTypeName(widget),
                Row = widget.GridRow,
                Column = widget.GridColumn,
                RowSpan = widget.RowSpan,
                ColumnSpan = widget.ColumnSpan,
                State = new Dictionary<string, object>()
            };
            layout.Widgets.Add(widgetLayout);
        }

        // Save to project or global
        if (!string.IsNullOrEmpty(_projectPath))
        {
            _layoutManager.SaveProjectLayout(_projectPath, layout);
        }
        else
        {
            _layoutManager.SaveGlobalLayout(layout);
        }
    }

    private string GetWidgetTypeName(WidgetBase widget)
    {
        return widget switch
        {
            Widgets.TaskChecklistWidget => "TaskChecklist",
            _ => "Unknown"
        };
    }

    private void LoadWindowSettings()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var settingsPath = Path.Combine(appData, "BasicToMips", "Dashboard", "window_settings.json");

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<WindowSettings>(json);
                if (settings != null)
                {
                    _savedLeft = settings.Left;
                    _savedTop = settings.Top;
                    _savedWidth = settings.Width;
                    _savedHeight = settings.Height;
                    _alwaysOnTop = settings.AlwaysOnTop;
                }
            }

            // Apply settings
            Left = _savedLeft;
            Top = _savedTop;
            Width = _savedWidth;
            Height = _savedHeight;
            AlwaysOnTopCheckBox.IsChecked = _alwaysOnTop;
            Topmost = _alwaysOnTop;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading window settings: {ex.Message}");
        }
    }

    private void SaveWindowSettings()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dashboardDir = Path.Combine(appData, "BasicToMips", "Dashboard");
            Directory.CreateDirectory(dashboardDir);
            var settingsPath = Path.Combine(dashboardDir, "window_settings.json");

            var settings = new WindowSettings
            {
                Left = _savedLeft,
                Top = _savedTop,
                Width = _savedWidth,
                Height = _savedHeight,
                AlwaysOnTop = _alwaysOnTop
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving window settings: {ex.Message}");
        }
    }

    private void UpdateGridInfo()
    {
        GridInfoText.Text = $"Grid: {DashboardGridControl.GridRows}x{DashboardGridControl.GridColumns} | Widgets: {DashboardGridControl.GetWidgets().Count}";
    }

    private void AlwaysOnTopCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _alwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? false;
        Topmost = _alwaysOnTop;
    }

    private void AddWidget_Click(object sender, RoutedEventArgs e)
    {
        // Show widget selection dialog
        var availableWidgets = _widgetFactory.GetAvailableWidgets();

        var dialog = new Window
        {
            Title = "Add Widget",
            Width = 300,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Foreground = System.Windows.Media.Brushes.White
        };

        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(16) };

        var label = new System.Windows.Controls.TextBlock
        {
            Text = "Select a widget to add:",
            Margin = new Thickness(0, 0, 0, 12),
            FontSize = 13
        };
        stack.Children.Add(label);

        var listBox = new System.Windows.Controls.ListBox
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2D)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3F, 0x3F, 0x46)),
            Margin = new Thickness(0, 0, 0, 12),
            Height = 100
        };

        foreach (var widgetInfo in availableWidgets)
        {
            listBox.Items.Add(widgetInfo.DisplayName);
        }

        if (listBox.Items.Count > 0)
            listBox.SelectedIndex = 0;

        stack.Children.Add(listBox);

        var addButton = new System.Windows.Controls.Button
        {
            Content = "Add Widget",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(24, 8, 24, 8),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x7A, 0xCC)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0, 0, 0, 0)
        };

        addButton.Click += (s, args) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                var selectedWidget = availableWidgets[listBox.SelectedIndex];
                var widget = _widgetFactory.CreateWidget(selectedWidget.TypeName);
                if (widget != null)
                {
                    // Set project path for widgets that need it
                    if (widget is Widgets.TaskChecklistWidget taskWidget)
                    {
                        taskWidget.SetProjectPath(_projectPath);
                    }

                    DashboardGridControl.AddWidget(widget);
                    UpdateGridInfo();
                    StatusText.Text = $"Added {selectedWidget.DisplayName}";
                }
                dialog.Close();
            }
        };

        stack.Children.Add(addButton);
        dialog.Content = stack;
        dialog.ShowDialog();
    }

    private void LayoutMenu_Click(object sender, RoutedEventArgs e)
    {
        LayoutContextMenu.IsOpen = true;
    }

    private void SaveGlobalLayout_Click(object sender, RoutedEventArgs e)
    {
        SaveCurrentLayout();
        _layoutManager.SaveGlobalLayout(GetCurrentLayout());
        StatusText.Text = "Global layout saved";
        MessageBox.Show("Global layout saved successfully!", "Layout Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveProjectLayout_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_projectPath))
        {
            MessageBox.Show("No project is currently open. Global layout will be saved instead.", "No Project", MessageBoxButton.OK, MessageBoxImage.Information);
            SaveGlobalLayout_Click(sender, e);
            return;
        }

        SaveCurrentLayout();
        _layoutManager.SaveProjectLayout(_projectPath, GetCurrentLayout());
        StatusText.Text = "Project layout saved";
        MessageBox.Show("Project layout saved successfully!", "Layout Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportLayout_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "JSON Layout Files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "dashboard_layout.json"
        };

        if (dialog.ShowDialog() == true)
        {
            _layoutManager.ExportLayout(dialog.FileName, GetCurrentLayout());
            StatusText.Text = "Layout exported";
            MessageBox.Show("Layout exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ImportLayout_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "JSON Layout Files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            var layout = _layoutManager.ImportLayout(dialog.FileName);
            if (layout != null)
            {
                ApplyLayout(layout);
                StatusText.Text = "Layout imported";
                MessageBox.Show("Layout imported successfully!", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to import layout.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ResetLayout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Reset to default layout? This will remove all current widgets.", "Reset Layout", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            LoadLayout();
            StatusText.Text = "Layout reset to default";
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private DashboardLayout GetCurrentLayout()
    {
        var layout = new DashboardLayout
        {
            Version = "1.0",
            GridRows = DashboardGridControl.GridRows,
            GridColumns = DashboardGridControl.GridColumns,
            Widgets = new List<WidgetLayout>()
        };

        foreach (var widget in DashboardGridControl.GetWidgets())
        {
            var widgetLayout = new WidgetLayout
            {
                Type = GetWidgetTypeName(widget),
                Row = widget.GridRow,
                Column = widget.GridColumn,
                RowSpan = widget.RowSpan,
                ColumnSpan = widget.ColumnSpan,
                State = new Dictionary<string, object>()
            };
            layout.Widgets.Add(widgetLayout);
        }

        return layout;
    }

    private void ApplyLayout(DashboardLayout layout)
    {
        DashboardGridControl.ClearWidgets();
        DashboardGridControl.GridRows = layout.GridRows;
        DashboardGridControl.GridColumns = layout.GridColumns;

        foreach (var widgetLayout in layout.Widgets)
        {
            var widget = _widgetFactory.CreateWidget(widgetLayout.Type);
            if (widget != null)
            {
                widget.GridRow = widgetLayout.Row;
                widget.GridColumn = widgetLayout.Column;
                widget.RowSpan = widgetLayout.RowSpan;
                widget.ColumnSpan = widgetLayout.ColumnSpan;

                // Set project path for widgets that need it
                if (widget is Widgets.TaskChecklistWidget taskWidget)
                {
                    taskWidget.SetProjectPath(_projectPath);
                }

                DashboardGridControl.AddWidget(widget);
            }
        }

        UpdateGridInfo();
    }

    private class WindowSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool AlwaysOnTop { get; set; }
    }
}
