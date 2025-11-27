using System.Windows;
using BasicToMips.UI.Services;

namespace BasicToMips.UI;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        LoadSettings();

        FontSizeSlider.ValueChanged += (s, e) =>
        {
            FontSizeText.Text = ((int)FontSizeSlider.Value).ToString();
        };
    }

    private void LoadSettings()
    {
        AutoCompileCheck.IsChecked = _settings.AutoCompile;
        ShowDocsCheck.IsChecked = _settings.ShowDocumentation;
        WordWrapCheck.IsChecked = _settings.WordWrap;
        FontSizeSlider.Value = _settings.FontSize;
        FontSizeText.Text = ((int)_settings.FontSize).ToString();
        StationeersPathText.Text = _settings.StationeersPath ?? "";
        OptLevelCombo.SelectedIndex = _settings.OptimizationLevel;
    }

    private void BrowseStationeers_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Stationeers installation directory",
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(_settings.StationeersPath))
        {
            dialog.SelectedPath = _settings.StationeersPath;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            StationeersPathText.Text = dialog.SelectedPath;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoCompile = AutoCompileCheck.IsChecked ?? false;
        _settings.ShowDocumentation = ShowDocsCheck.IsChecked ?? false;
        _settings.WordWrap = WordWrapCheck.IsChecked ?? false;
        _settings.FontSize = (int)FontSizeSlider.Value;
        _settings.StationeersPath = StationeersPathText.Text;
        _settings.OptimizationLevel = OptLevelCombo.SelectedIndex;
        _settings.Save();

        DialogResult = true;
        Close();
    }
}
