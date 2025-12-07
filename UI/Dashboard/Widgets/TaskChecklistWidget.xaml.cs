using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class TaskChecklistWidget : WidgetBase, INotifyPropertyChanged
{
    private ObservableCollection<TaskItem> _tasks = new();
    private string? _projectPath;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TaskChecklistWidget()
    {
        Title = "Task Checklist";
        InitializeComponent();
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        // The XAML content is already loaded via InitializeComponent
        // Just need to bind the data
        TasksList.ItemsSource = _tasks;
        _tasks.CollectionChanged += (s, e) => UpdateEmptyState();
        UpdateEmptyState();
    }

    public void SetProjectPath(string? projectPath)
    {
        _projectPath = projectPath;
        LoadTasks();
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        AddTask();
    }

    private void NewTaskTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddTask();
            e.Handled = true;
        }
    }

    private void AddTask()
    {
        var text = NewTaskTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            _tasks.Add(new TaskItem { Text = text, IsCompleted = false });
            NewTaskTextBox.Clear();
            NewTaskTextBox.Focus();
            SaveTasks();
        }
    }

    private void TaskCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SaveTasks();
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TaskItem task)
        {
            _tasks.Remove(task);
            SaveTasks();
        }
    }

    private void TaskText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is TextBlock textBlock)
        {
            // Double-click to edit
            if (textBlock.DataContext is TaskItem task)
            {
                EditTask(task, textBlock);
            }
        }
    }

    private void EditTask(TaskItem task, TextBlock textBlock)
    {
        // Create inline editor
        var textBox = new TextBox
        {
            Text = task.Text,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 13,
            CaretBrush = System.Windows.Media.Brushes.White
        };

        // Replace TextBlock with TextBox temporarily
        var parent = textBlock.Parent as Grid;
        if (parent == null)
            return;

        var column = Grid.GetColumn(textBlock);
        parent.Children.Remove(textBlock);
        Grid.SetColumn(textBox, column);
        parent.Children.Add(textBox);

        textBox.Focus();
        textBox.SelectAll();

        // Save on Enter or focus lost
        void SaveEdit()
        {
            var newText = textBox.Text.Trim();
            if (!string.IsNullOrEmpty(newText))
            {
                task.Text = newText;
                SaveTasks();
            }

            parent.Children.Remove(textBox);
            Grid.SetColumn(textBlock, column);
            parent.Children.Add(textBlock);
        }

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SaveEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                parent.Children.Remove(textBox);
                Grid.SetColumn(textBlock, column);
                parent.Children.Add(textBlock);
                e.Handled = true;
            }
        };

        textBox.LostFocus += (s, e) => SaveEdit();
    }

    private void UpdateEmptyState()
    {
        EmptyMessage.Visibility = _tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LoadTasks()
    {
        try
        {
            var tasksPath = GetTasksFilePath();
            if (!string.IsNullOrEmpty(tasksPath) && File.Exists(tasksPath))
            {
                var json = File.ReadAllText(tasksPath);
                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json);
                if (tasks != null)
                {
                    _tasks.Clear();
                    foreach (var task in tasks)
                    {
                        _tasks.Add(task);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tasks: {ex.Message}");
        }
    }

    private void SaveTasks()
    {
        try
        {
            var tasksPath = GetTasksFilePath();
            if (!string.IsNullOrEmpty(tasksPath))
            {
                var json = JsonSerializer.Serialize(_tasks.ToList(), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tasksPath, json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving tasks: {ex.Message}");
        }
    }

    private string? GetTasksFilePath()
    {
        if (string.IsNullOrEmpty(_projectPath))
        {
            // Use global tasks file
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dashboardDir = Path.Combine(appData, "BasicToMips", "Dashboard");
            Directory.CreateDirectory(dashboardDir);
            return Path.Combine(dashboardDir, "tasks.json");
        }
        else
        {
            // Use project-specific tasks file
            var projectDir = Path.GetDirectoryName(_projectPath);
            if (!string.IsNullOrEmpty(projectDir))
            {
                return Path.Combine(projectDir, "tasks.json");
            }
        }
        return null;
    }

    public override object SaveState()
    {
        return new
        {
            Tasks = _tasks.ToList()
        };
    }

    public override void LoadState(object state)
    {
        // State is loaded from tasks.json instead
    }
}

public class TaskItem : INotifyPropertyChanged
{
    private string _text = "";
    private bool _isCompleted;

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted != value)
            {
                _isCompleted = value;
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
