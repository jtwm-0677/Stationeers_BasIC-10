using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace BasicToMips.UI;

/// <summary>
/// Tab management functionality for MainWindow
/// </summary>
public partial class MainWindow
{
    // Tab management
    private ObservableCollection<EditorTab> _tabs = new();
    private EditorTab? _currentTab;
    private const int MAX_TABS = 10;

    // Split view mode (Horizontal = top/bottom with horizontal divider, Vertical = side-by-side with vertical divider)
    private enum SplitMode { Horizontal, Vertical, EditorOnly }
    private SplitMode _currentSplitMode = SplitMode.Horizontal;

    /// <summary>
    /// Initialize tab management system
    /// </summary>
    private void InitializeTabManagement()
    {
        // Create initial tab with welcome content
        var initialTab = new EditorTab
        {
            Content = GetWelcomeCode(),
            IsModified = false
        };
        _tabs.Add(initialTab);
        _currentTab = initialTab;

        // Bind tab bar to tabs collection
        TabBar.ItemsSource = _tabs;
        UpdateTabBarSelection();
    }

    /// <summary>
    /// Update visual selection state of tabs in the tab bar
    /// </summary>
    private void UpdateTabBarSelection()
    {
        foreach (var tab in _tabs)
        {
            tab.IsSelected = tab == _currentTab;
        }
    }

    /// <summary>
    /// Handle tab click to switch tabs
    /// </summary>
    private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is EditorTab tab)
        {
            SwitchToTab(tab);
        }
    }

    /// <summary>
    /// Handle middle-click to close tab
    /// </summary>
    private void Tab_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle &&
            sender is FrameworkElement element &&
            element.DataContext is EditorTab tab)
        {
            CloseTab(tab);
        }
    }

    /// <summary>
    /// Handle close button click on tab
    /// </summary>
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is EditorTab tab)
        {
            CloseTab(tab);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handle new tab button click
    /// </summary>
    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        CreateNewTab();
    }

    /// <summary>
    /// Switch to a specific tab
    /// </summary>
    private void SwitchToTab(EditorTab tab)
    {
        if (_currentTab == tab) return;

        // Save current tab content
        SyncEditorToTab();

        // Switch to new tab
        _currentTab = tab;
        SyncTabToEditor();
        UpdateTabBarSelection();
        BasicEditor.Focus();
    }

    /// <summary>
    /// Close a specific tab
    /// </summary>
    private void CloseTab(EditorTab tab) => CloseTab(tab, promptToSave: true);

    /// <summary>
    /// Close a specific tab with option to skip save prompt (for API calls)
    /// </summary>
    private void CloseTab(EditorTab tab, bool promptToSave)
    {
        // Don't close the last tab
        if (_tabs.Count <= 1)
        {
            // Just clear the tab instead
            tab.Content = "";
            tab.FilePath = null;
            tab.IsModified = false;
            SyncTabToEditor();
            return;
        }

        // Check for unsaved changes (only if prompting is enabled)
        if (promptToSave && tab.IsModified)
        {
            var result = MessageBox.Show(
                $"Save changes to {tab.DisplayName.TrimEnd(' ', '*')}?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel) return;
            if (result == MessageBoxResult.Yes)
            {
                // Switch to tab and save
                SwitchToTab(tab);
                SaveFile_Click(null!, null!);
                if (_isModified) return; // Save was cancelled
            }
        }

        // Find index and remove
        var index = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        // Switch to adjacent tab
        if (_currentTab == tab)
        {
            var newIndex = Math.Min(index, _tabs.Count - 1);
            _currentTab = _tabs[newIndex];
            SyncTabToEditor();
        }

        UpdateTabBarSelection();
    }

    /// <summary>
    /// Sync the current tab content with the BasicEditor
    /// </summary>
    private void SyncTabToEditor()
    {
        if (_currentTab == null) return;

        _suppressBasicUpdate = true;
        BasicEditor.Text = _currentTab.Content;
        _isModified = _currentTab.IsModified;
        _currentFilePath = _currentTab.FilePath;
        _workingDirectory = string.IsNullOrEmpty(_currentTab.FilePath) ? null : Path.GetDirectoryName(_currentTab.FilePath);
        UpdateTitle();
        UpdateWorkingDirectoryDisplay();
        _suppressBasicUpdate = false;
    }

    /// <summary>
    /// Sync the BasicEditor content back to the current tab
    /// </summary>
    private void SyncEditorToTab()
    {
        if (_currentTab == null) return;

        _currentTab.Content = BasicEditor.Text;
        _currentTab.IsModified = _isModified;
        _currentTab.FilePath = _currentFilePath;
    }

    /// <summary>
    /// Create a new tab
    /// </summary>
    private void CreateNewTab()
    {
        if (_tabs.Count >= MAX_TABS)
        {
            MessageBox.Show(
                $"Maximum of {MAX_TABS} tabs reached. Please close some tabs before opening more.",
                "Tab Limit Reached",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Save current tab content before switching
        SyncEditorToTab();

        // Create new tab
        var newTab = new EditorTab
        {
            Content = "",
            IsModified = false
        };
        _tabs.Add(newTab);
        _currentTab = newTab;

        // Update editor and reset working directory for new file
        _workingDirectory = null;
        SyncTabToEditor();
        UpdateWorkingDirectoryDisplay();
        UpdateTabBarSelection();
        ClearOutput();
        BasicEditor.Focus();
    }

    /// <summary>
    /// Open a file in a new tab
    /// </summary>
    private void OpenFileInNewTab(string filePath)
    {
        if (_tabs.Count >= MAX_TABS)
        {
            MessageBox.Show(
                $"Maximum of {MAX_TABS} tabs reached. Please close some tabs before opening more.",
                "Tab Limit Reached",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            // Check if file is already open in a tab
            var existingTab = _tabs.FirstOrDefault(t => t.FilePath == filePath);
            if (existingTab != null)
            {
                _currentTab = existingTab;
                SyncTabToEditor();
                UpdateTabBarSelection();
                return;
            }

            // Save current tab content before switching
            SyncEditorToTab();

            // Create new tab with file content
            var fileContent = File.ReadAllText(filePath);
            var newTab = new EditorTab
            {
                FilePath = filePath,
                Content = fileContent,
                IsModified = false
            };
            _tabs.Add(newTab);
            _currentTab = newTab;

            // Update editor
            SyncTabToEditor();
            UpdateTabBarSelection();
            _settings.AddRecentFile(filePath);
            UpdateRecentFilesMenu();
            SetStatus("File opened", true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Close the current tab
    /// </summary>
    private void CloseCurrentTab()
    {
        if (_currentTab == null) return;

        // Check if tab has unsaved changes
        if (_currentTab.IsModified)
        {
            var result = MessageBox.Show(
                $"Do you want to save changes to {_currentTab.FileName}?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }
            else if (result == MessageBoxResult.Yes)
            {
                if (!SaveCurrentTabFile())
                {
                    return; // User cancelled save dialog or save failed
                }
            }
        }

        // Remove tab
        var tabIndex = _tabs.IndexOf(_currentTab);
        _tabs.Remove(_currentTab);

        // If no tabs left, create a new untitled tab
        if (_tabs.Count == 0)
        {
            var newTab = new EditorTab
            {
                Content = "",
                IsModified = false
            };
            _tabs.Add(newTab);
            _currentTab = newTab;
        }
        else
        {
            // Switch to next tab, or previous if we closed the last tab
            var newIndex = tabIndex < _tabs.Count ? tabIndex : _tabs.Count - 1;
            _currentTab = _tabs[newIndex];
        }

        SyncTabToEditor();
    }

    /// <summary>
    /// Close all tabs except the current one
    /// </summary>
    private void CloseOtherTabs()
    {
        if (_currentTab == null) return;

        // Check for unsaved changes in other tabs
        var unsavedTabs = _tabs.Where(t => t != _currentTab && t.IsModified).ToList();
        if (unsavedTabs.Any())
        {
            var result = MessageBox.Show(
                $"There are {unsavedTabs.Count} unsaved tab(s). Close anyway?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        // Keep only current tab
        var currentTab = _currentTab;
        _tabs.Clear();
        _tabs.Add(currentTab);
        _currentTab = currentTab;
    }

    /// <summary>
    /// Close all tabs
    /// </summary>
    private void CloseAllTabs()
    {
        // Check for unsaved changes
        var unsavedTabs = _tabs.Where(t => t.IsModified).ToList();
        if (unsavedTabs.Any())
        {
            var result = MessageBox.Show(
                $"There are {unsavedTabs.Count} unsaved tab(s). Close anyway?",
                "Unsaved Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        // Clear all tabs and create new untitled tab
        _tabs.Clear();
        var newTab = new EditorTab
        {
            Content = "",
            IsModified = false
        };
        _tabs.Add(newTab);
        _currentTab = newTab;
        SyncTabToEditor();
        ClearOutput();
    }

    /// <summary>
    /// Switch to next tab (Ctrl+Tab)
    /// </summary>
    private void NextTab()
    {
        if (_tabs.Count <= 1) return;

        SyncEditorToTab();
        var currentIndex = _tabs.IndexOf(_currentTab!);
        var nextIndex = (currentIndex + 1) % _tabs.Count;
        _currentTab = _tabs[nextIndex];
        SyncTabToEditor();
    }

    /// <summary>
    /// Switch to previous tab (Ctrl+Shift+Tab)
    /// </summary>
    private void PreviousTab()
    {
        if (_tabs.Count <= 1) return;

        SyncEditorToTab();
        var currentIndex = _tabs.IndexOf(_currentTab!);
        var prevIndex = currentIndex == 0 ? _tabs.Count - 1 : currentIndex - 1;
        _currentTab = _tabs[prevIndex];
        SyncTabToEditor();
    }

    /// <summary>
    /// Save the current tab's file
    /// </summary>
    private bool SaveCurrentTabFile()
    {
        if (_currentTab == null) return false;

        // Sync current editor content to tab
        SyncEditorToTab();

        if (string.IsNullOrEmpty(_currentTab.FilePath))
        {
            return SaveCurrentTabFileAs();
        }

        try
        {
            File.WriteAllText(_currentTab.FilePath, _currentTab.Content);
            _currentTab.IsModified = false;
            _isModified = false;
            UpdateTitle();
            _settings.AddRecentFile(_currentTab.FilePath);
            SetStatus("File saved", true);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Save the current tab's file with a new name
    /// </summary>
    private bool SaveCurrentTabFileAs()
    {
        if (_currentTab == null) return false;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "BASIC Files (*.bas)|*.bas|All Files (*.*)|*.*",
            Title = "Save BASIC File",
            DefaultExt = ".bas"
        };

        if (!string.IsNullOrEmpty(_currentTab.FilePath))
        {
            dialog.FileName = Path.GetFileName(_currentTab.FilePath);
            dialog.InitialDirectory = Path.GetDirectoryName(_currentTab.FilePath);
        }

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, _currentTab.Content);
                _currentTab.FilePath = dialog.FileName;
                _currentTab.IsModified = false;
                _currentFilePath = dialog.FileName;
                _isModified = false;
                UpdateTitle();
                _settings.AddRecentFile(dialog.FileName);
                SetStatus("File saved", true);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Copy the file path of the current tab
    /// </summary>
    private void CopyCurrentTabPath()
    {
        if (_currentTab == null || string.IsNullOrEmpty(_currentTab.FilePath)) return;

        Clipboard.SetText(_currentTab.FilePath);
        SetStatus("Path copied to clipboard", true);
    }

    /// <summary>
    /// Open the containing folder of the current tab's file
    /// </summary>
    private void OpenCurrentTabFolder()
    {
        if (_currentTab == null || string.IsNullOrEmpty(_currentTab.FilePath)) return;

        try
        {
            var folder = Path.GetDirectoryName(_currentTab.FilePath);
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening folder:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Setup keyboard shortcuts for tab management
    /// </summary>
    private void SetupTabKeyboardShortcuts()
    {
        // Ctrl+W to close tab
        this.InputBindings.Add(new KeyBinding(
            new RelayCommand(_ => CloseCurrentTab()),
            Key.W,
            ModifierKeys.Control));

        // Ctrl+Tab to next tab
        this.InputBindings.Add(new KeyBinding(
            new RelayCommand(_ => NextTab()),
            Key.Tab,
            ModifierKeys.Control));

        // Ctrl+Shift+Tab to previous tab
        this.InputBindings.Add(new KeyBinding(
            new RelayCommand(_ => PreviousTab()),
            Key.Tab,
            ModifierKeys.Control | ModifierKeys.Shift));

        // Ctrl+Shift+V to cycle split view
        this.InputBindings.Add(new KeyBinding(
            new RelayCommand(_ => CycleSplitView()),
            Key.V,
            ModifierKeys.Control | ModifierKeys.Shift));
    }

    /// <summary>
    /// Cycle through split view modes
    /// </summary>
    private void CycleSplitView()
    {
        _currentSplitMode = _currentSplitMode switch
        {
            SplitMode.Horizontal => SplitMode.Vertical,
            SplitMode.Vertical => SplitMode.EditorOnly,
            SplitMode.EditorOnly => SplitMode.Horizontal,
            _ => SplitMode.Horizontal
        };

        ApplySplitViewMode();
    }

    /// <summary>
    /// Apply the current split view mode by modifying the grid
    /// </summary>
    private void ApplySplitViewMode()
    {
        // Call the actual ApplySplitView implementation and update menu checkmarks
        ApplySplitView(_currentSplitMode.ToString());

        // Update menu checkmarks
        SplitHorizontalMenu.IsChecked = _currentSplitMode == SplitMode.Horizontal;
        SplitVerticalMenu.IsChecked = _currentSplitMode == SplitMode.Vertical;
        SplitEditorOnlyMenu.IsChecked = _currentSplitMode == SplitMode.EditorOnly;

        _settings.SplitViewMode = _currentSplitMode.ToString();
        _settings.Save();
    }
}

/// <summary>
/// Simple relay command for keyboard shortcuts
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }
}
