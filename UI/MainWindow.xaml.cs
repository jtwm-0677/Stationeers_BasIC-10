using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;
using BasicToMips.Editor.Highlighting;
using BasicToMips.Editor.Completion;
using BasicToMips.UI.Services;

namespace BasicToMips.UI;

public partial class MainWindow : Window
{
    private string? _currentFilePath;
    private bool _isModified;
    private CompletionWindow? _completionWindow;
    private readonly CompilerService _compiler;
    private readonly SettingsService _settings;
    private readonly DocumentationService _docs;
    private int _optimizationLevel = 1; // 0=None, 1=Basic, 2=Aggressive

    public MainWindow()
    {
        InitializeComponent();

        _compiler = new CompilerService();
        _settings = new SettingsService();
        _docs = new DocumentationService();

        SetupEditors();
        SetupAutoComplete();
        LoadSettings();
        PopulateDocumentation();
        PopulateSnippetsMenu();
        UpdateRecentFilesMenu();
    }

    private void SetupEditors()
    {
        // Apply BASIC syntax highlighting
        BasicEditor.SyntaxHighlighting = BasicHighlighting.Create();
        MipsOutput.SyntaxHighlighting = MipsHighlighting.Create();

        // Editor events
        BasicEditor.TextArea.Caret.PositionChanged += (s, e) => UpdateCursorPosition();
        BasicEditor.TextArea.TextEntering += TextArea_TextEntering;
        BasicEditor.TextArea.TextEntered += TextArea_TextEntered;

        // Set initial content
        BasicEditor.Text = GetWelcomeCode();
        UpdateLineCount();
    }

    private void SetupAutoComplete()
    {
        BasicEditor.TextArea.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowCompletionWindow();
                e.Handled = true;
            }
        };
    }

    private void LoadSettings()
    {
        _settings.Load();

        if (!string.IsNullOrEmpty(_settings.StationeersPath))
        {
            StationeersPathText.Text = $"Stationeers: {_settings.StationeersPath}";
        }

        ShowDocsMenu.IsChecked = _settings.ShowDocumentation;
        if (!_settings.ShowDocumentation)
        {
            DocsPanelColumn.Width = new GridLength(0);
            DocsSplitter.Visibility = Visibility.Collapsed;
            DocsPanel.Visibility = Visibility.Collapsed;
        }

        AutoCompileMenu.IsChecked = _settings.AutoCompile;
    }

    private string GetWelcomeCode()
    {
        return @"' Welcome to BASIC-IC10 Compiler for Stationeers!
' This compiler transforms BASIC code into IC10 MIPS assembly.
'
' Quick Start:
' 1. Write your BASIC code in this editor
' 2. Press F5 or click Compile to generate IC10 code
' 3. Use Save & Deploy to automatically save to Stationeers
'
' Press Ctrl+Space for auto-complete suggestions
' Press F1 for documentation

' Example: Simple temperature controller
ALIAS sensor d0
ALIAS heater d1
DEFINE TARGET 20

main:
    VAR temp = sensor.Temperature

    IF temp < TARGET THEN
        heater.On = 1
    ELSE
        heater.On = 0
    ENDIF

    YIELD
    GOTO main
END
";
    }

    #region File Operations

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckSaveChanges()) return;

        BasicEditor.Text = "";
        _currentFilePath = null;
        _isModified = false;
        UpdateTitle();
        ClearOutput();
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckSaveChanges()) return;

        var dialog = new OpenFileDialog
        {
            Filter = "BASIC Files (*.bas;*.basic)|*.bas;*.basic|All Files (*.*)|*.*",
            Title = "Open BASIC File"
        };

        if (dialog.ShowDialog() == true)
        {
            OpenFile(dialog.FileName);
        }
    }

    private void OpenFile(string path)
    {
        try
        {
            BasicEditor.Text = File.ReadAllText(path);
            _currentFilePath = path;
            _isModified = false;
            _settings.AddRecentFile(path);
            UpdateTitle();
            UpdateRecentFilesMenu();
            SetStatus("File opened", true);

            if (AutoCompileMenu.IsChecked)
            {
                Compile();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        SaveFile();
    }

    private bool SaveFile()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            return SaveFileAs();
        }

        return SaveToFile(_currentFilePath);
    }

    private void SaveFileAs_Click(object sender, RoutedEventArgs e)
    {
        SaveFileAs();
    }

    private bool SaveFileAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "BASIC Files (*.bas)|*.bas|All Files (*.*)|*.*",
            Title = "Save BASIC File",
            DefaultExt = ".bas"
        };

        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            dialog.FileName = Path.GetFileName(_currentFilePath);
            dialog.InitialDirectory = Path.GetDirectoryName(_currentFilePath);
        }

        if (dialog.ShowDialog() == true)
        {
            return SaveToFile(dialog.FileName);
        }

        return false;
    }

    private bool SaveToFile(string path)
    {
        try
        {
            File.WriteAllText(path, BasicEditor.Text);
            _currentFilePath = path;
            _isModified = false;
            _settings.AddRecentFile(path);
            UpdateTitle();
            UpdateRecentFilesMenu();

            // Auto-compile if enabled
            if (AutoCompileMenu.IsChecked)
            {
                Compile();

                // Auto-deploy to Stationeers
                if (!string.IsNullOrEmpty(_settings.StationeersPath) && !string.IsNullOrEmpty(MipsOutput.Text))
                {
                    DeployToStationeers();
                }
            }

            SetStatus("File saved", true);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void ExportIC10_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MipsOutput.Text))
        {
            MessageBox.Show("No IC10 code to export. Please compile first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "IC10 Files (*.ic10)|*.ic10|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Export IC10 Code",
            DefaultExt = ".ic10"
        };

        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            dialog.FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + ".ic10";
        }

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, MipsOutput.Text);
                SetStatus("IC10 code exported", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool CheckSaveChanges()
    {
        if (!_isModified) return true;

        var result = MessageBox.Show(
            "Do you want to save changes?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => SaveFile(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();

        if (_settings.RecentFiles.Count == 0)
        {
            var emptyItem = new MenuItem { Header = "(No recent files)", IsEnabled = false };
            RecentFilesMenu.Items.Add(emptyItem);
            return;
        }

        foreach (var file in _settings.RecentFiles.Take(10))
        {
            var item = new MenuItem { Header = file, Tag = file };
            item.Click += (s, e) =>
            {
                if (CheckSaveChanges())
                {
                    OpenFile((string)((MenuItem)s).Tag);
                }
            };
            RecentFilesMenu.Items.Add(item);
        }

        RecentFilesMenu.Items.Add(new Separator());
        var clearItem = new MenuItem { Header = "Clear Recent Files" };
        clearItem.Click += (s, e) =>
        {
            _settings.RecentFiles.Clear();
            _settings.Save();
            UpdateRecentFilesMenu();
        };
        RecentFilesMenu.Items.Add(clearItem);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Edit Operations

    private void Undo_Click(object sender, RoutedEventArgs e) => BasicEditor.Undo();
    private void Redo_Click(object sender, RoutedEventArgs e) => BasicEditor.Redo();
    private void Cut_Click(object sender, RoutedEventArgs e) => BasicEditor.Cut();
    private void Copy_Click(object sender, RoutedEventArgs e) => BasicEditor.Copy();
    private void Paste_Click(object sender, RoutedEventArgs e) => BasicEditor.Paste();

    private void Find_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement find dialog
        SetStatus("Find feature coming soon", false);
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement replace dialog
        SetStatus("Replace feature coming soon", false);
    }

    #endregion

    #region Compilation

    private void Compile_Click(object sender, RoutedEventArgs e)
    {
        Compile();
    }

    private void CompileAndCopy_Click(object sender, RoutedEventArgs e)
    {
        if (Compile())
        {
            Clipboard.SetText(MipsOutput.Text);
            SetStatus("Compiled and copied to clipboard", true);
        }
    }

    private void CopyIC10_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MipsOutput.Text))
        {
            Clipboard.SetText(MipsOutput.Text);
            SetStatus("IC10 code copied to clipboard", true);
        }
    }

    private bool Compile()
    {
        try
        {
            ClearError();
            SetStatus("Compiling...", false);

            var result = _compiler.Compile(BasicEditor.Text, _optimizationLevel);

            if (result.Success)
            {
                MipsOutput.Text = result.Output;
                UpdateLineCount();
                SetStatus($"Compiled successfully ({result.LineCount} lines)", true);
                return true;
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Unknown error", result.ErrorLine);
                SetStatus("Compilation failed", false);
                return false;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, null);
            SetStatus("Compilation failed", false);
            return false;
        }
    }

    private void OptLevel_Click(object sender, RoutedEventArgs e)
    {
        OptNone.IsChecked = sender == OptNone;
        OptBasic.IsChecked = sender == OptBasic;
        OptAggressive.IsChecked = sender == OptAggressive;

        _optimizationLevel = sender == OptNone ? 0 : sender == OptBasic ? 1 : 2;
    }

    private void Optimize_Click(object sender, RoutedEventArgs e)
    {
        var previousLevel = _optimizationLevel;
        _optimizationLevel = 2; // Aggressive
        Compile();
        _optimizationLevel = previousLevel;
    }

    #endregion

    #region Stationeers Integration

    private void SetStationeersDir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Stationeers installation directory",
            ShowNewFolderButton = false
        };

        // Try to find default Stationeers path
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
            "Rocketwerkz", "rocketstation");

        if (Directory.Exists(defaultPath))
        {
            dialog.SelectedPath = defaultPath;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _settings.StationeersPath = dialog.SelectedPath;
            _settings.Save();
            StationeersPathText.Text = $"Stationeers: {dialog.SelectedPath}";
            SetStatus("Stationeers path configured", true);
        }
    }

    private void OpenScriptsFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_settings.StationeersPath))
        {
            MessageBox.Show("Please configure the Stationeers directory first.", "Not Configured", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var scriptsPath = Path.Combine(_settings.StationeersPath, "scripts");
        if (!Directory.Exists(scriptsPath))
        {
            Directory.CreateDirectory(scriptsPath);
        }

        System.Diagnostics.Process.Start("explorer.exe", scriptsPath);
    }

    private void SaveAndDeploy_Click(object sender, RoutedEventArgs e)
    {
        if (SaveFile() && Compile())
        {
            DeployToStationeers();
        }
    }

    private void DeployToStationeers()
    {
        if (string.IsNullOrEmpty(_settings.StationeersPath))
        {
            var result = MessageBox.Show(
                "Stationeers directory not configured. Would you like to configure it now?",
                "Configuration Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SetStationeersDir_Click(this, new RoutedEventArgs());
            }
            return;
        }

        try
        {
            var scriptsPath = Path.Combine(_settings.StationeersPath, "scripts");
            if (!Directory.Exists(scriptsPath))
            {
                Directory.CreateDirectory(scriptsPath);
            }

            var fileName = !string.IsNullOrEmpty(_currentFilePath)
                ? Path.GetFileNameWithoutExtension(_currentFilePath) + ".ic10"
                : "compiled.ic10";

            var outputPath = Path.Combine(scriptsPath, fileName);
            File.WriteAllText(outputPath, MipsOutput.Text);

            SetStatus($"Deployed to Stationeers: {fileName}", true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deploying to Stationeers:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region View Operations

    private void ToggleDocs_Click(object sender, RoutedEventArgs e)
    {
        var show = ShowDocsMenu.IsChecked;
        _settings.ShowDocumentation = show;
        _settings.Save();

        if (show)
        {
            DocsPanelColumn.Width = new GridLength(300);
            DocsSplitter.Visibility = Visibility.Visible;
            DocsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            DocsPanelColumn.Width = new GridLength(0);
            DocsSplitter.Visibility = Visibility.Collapsed;
            DocsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseDocs_Click(object sender, RoutedEventArgs e)
    {
        ShowDocsMenu.IsChecked = false;
        ToggleDocs_Click(sender, e);
    }

    private void ToggleLineNumbers_Click(object sender, RoutedEventArgs e)
    {
        var show = ShowLineNumbersMenu.IsChecked;
        BasicEditor.ShowLineNumbers = show;
        MipsOutput.ShowLineNumbers = show;
    }

    private void ToggleWordWrap_Click(object sender, RoutedEventArgs e)
    {
        var wrap = WordWrapMenu.IsChecked;
        BasicEditor.WordWrap = wrap;
        MipsOutput.WordWrap = wrap;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        BasicEditor.FontSize = Math.Min(BasicEditor.FontSize + 2, 32);
        MipsOutput.FontSize = BasicEditor.FontSize;
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        BasicEditor.FontSize = Math.Max(BasicEditor.FontSize - 2, 8);
        MipsOutput.FontSize = BasicEditor.FontSize;
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        BasicEditor.FontSize = 14;
        MipsOutput.FontSize = 14;
    }

    #endregion

    #region Help and Documentation

    private void ShowDocs_Click(object sender, RoutedEventArgs e)
    {
        ShowDocsMenu.IsChecked = true;
        ToggleDocs_Click(sender, e);
    }

    private void QuickStart_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DocumentationWindow("Quick Start Guide", _docs.GetQuickStartGuide());
        dialog.Owner = this;
        dialog.Show();
    }

    private void LangRef_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DocumentationWindow("Language Reference", _docs.GetLanguageReference());
        dialog.Owner = this;
        dialog.Show();
    }

    private void Examples_Click(object sender, RoutedEventArgs e)
    {
        LoadExample_Click(sender, e);
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("You are using the latest version.", "Updates", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "BASIC to IC10 Compiler for Stationeers\n\n" +
            "Version 1.0.0\n\n" +
            "A professional BASIC compiler that generates optimized\n" +
            "IC10 MIPS assembly code for Stationeers.\n\n" +
            "Features:\n" +
            "- Syntax highlighting and auto-complete\n" +
            "- Automatic code optimization\n" +
            "- Direct deployment to Stationeers\n" +
            "- Comprehensive documentation",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            LoadSettings();
        }
    }

    private void PopulateDocumentation()
    {
        _docs.PopulateQuickReference(QuickRefPanel);
        _docs.PopulateFunctions(FunctionsPanel);
        _docs.PopulateExamples(ExamplesPanel, LoadExampleCode);
    }

    private void LoadExampleCode(string code)
    {
        if (!CheckSaveChanges()) return;
        BasicEditor.Text = code;
        _currentFilePath = null;
        _isModified = false;
        UpdateTitle();
        if (AutoCompileMenu.IsChecked) Compile();
    }

    private void LoadExample_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ExamplesWindow(_docs);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedCode))
        {
            LoadExampleCode(dialog.SelectedCode);
        }
    }

    private void PopulateSnippetsMenu()
    {
        var snippets = _docs.GetSnippets();
        foreach (var snippet in snippets)
        {
            var item = new MenuItem { Header = snippet.Name, Tag = snippet.Code };
            item.Click += (s, e) =>
            {
                var code = (string)((MenuItem)s).Tag;
                BasicEditor.Document.Insert(BasicEditor.CaretOffset, code);
            };
            SnippetsMenu.Items.Add(item);
        }
    }

    #endregion

    #region Auto-Complete

    private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (_completionWindow != null && e.Text.Length > 0)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
            {
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
    }

    private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        // Trigger auto-complete on certain characters
        if (char.IsLetter(e.Text[0]) || e.Text[0] == '.')
        {
            var word = GetWordBeforeCaret();
            if (word.Length >= 2 || e.Text[0] == '.')
            {
                ShowCompletionWindow();
            }
        }
    }

    private void ShowCompletionWindow()
    {
        var completionData = BasicCompletionData.GetCompletionData(BasicEditor, GetWordBeforeCaret());

        if (completionData.Count == 0) return;

        _completionWindow = new CompletionWindow(BasicEditor.TextArea);
        var data = _completionWindow.CompletionList.CompletionData;

        foreach (var item in completionData)
        {
            data.Add(item);
        }

        _completionWindow.Show();
        _completionWindow.Closed += (s, e) => _completionWindow = null;
    }

    private string GetWordBeforeCaret()
    {
        var offset = BasicEditor.CaretOffset;
        var document = BasicEditor.Document;

        int start = offset;
        while (start > 0)
        {
            var c = document.GetCharAt(start - 1);
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                break;
            start--;
        }

        return document.GetText(start, offset - start);
    }

    #endregion

    #region UI Updates

    private void BasicEditor_TextChanged(object sender, EventArgs e)
    {
        _isModified = true;
        UpdateTitle();
        UpdateLineCount();
        ModifiedIndicator.Visibility = Visibility.Visible;
    }

    private void UpdateTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentFilePath)
            ? "Untitled"
            : Path.GetFileName(_currentFilePath);

        var modified = _isModified ? "*" : "";
        Title = $"{fileName}{modified} - BASIC to IC10 Compiler";
        BasicFileNameText.Text = string.IsNullOrEmpty(_currentFilePath) ? "" : $"- {fileName}";
    }

    private void UpdateLineCount()
    {
        var basicLines = BasicEditor.LineCount;
        var mipsLines = string.IsNullOrEmpty(MipsOutput.Text) ? 0 : MipsOutput.LineCount;

        BasicLineCountText.Text = $"Lines: {basicLines}";
        MipsLineCountText.Text = $"Lines: {mipsLines} / 128";

        // Update warning badges
        LineWarningBadge.Visibility = mipsLines >= 100 && mipsLines <= 128 ? Visibility.Visible : Visibility.Collapsed;
        LineErrorBadge.Visibility = mipsLines > 128 ? Visibility.Visible : Visibility.Collapsed;

        if (mipsLines > 128)
        {
            MipsLineCountText.Foreground = (Brush)FindResource("ErrorBrush");
        }
        else if (mipsLines >= 100)
        {
            MipsLineCountText.Foreground = (Brush)FindResource("WarningBrush");
        }
        else
        {
            MipsLineCountText.Foreground = (Brush)FindResource("SecondaryTextBrush");
        }
    }

    private void UpdateCursorPosition()
    {
        var line = BasicEditor.TextArea.Caret.Line;
        var col = BasicEditor.TextArea.Caret.Column;
        CursorPositionText.Text = $"Ln {line}, Col {col}";
    }

    private void SetStatus(string message, bool success)
    {
        StatusText.Text = message;
        StatusIcon.Text = success ? "\uE73E" : "\uE7BA";
    }

    private void ShowError(string message, int? line)
    {
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorText.Text = line.HasValue ? $"Line {line}: {message}" : message;
        ErrorIcon.Foreground = (Brush)FindResource("ErrorBrush");
    }

    private void ClearError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
    }

    private void ClearOutput()
    {
        MipsOutput.Text = "";
        UpdateLineCount();
        ClearError();
    }

    #endregion

    #region Window Events

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BasicEditor.Focus();

        // Try to auto-detect Stationeers
        if (string.IsNullOrEmpty(_settings.StationeersPath))
        {
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
                "Rocketwerkz", "rocketstation");

            if (Directory.Exists(defaultPath))
            {
                _settings.StationeersPath = defaultPath;
                _settings.Save();
                StationeersPathText.Text = $"Stationeers: {defaultPath}";
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!CheckSaveChanges())
        {
            e.Cancel = true;
        }
        else
        {
            _settings.Save();
        }
    }

    #endregion
}
