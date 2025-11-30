using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using BasicToMips.Editor.Highlighting;
using BasicToMips.UI.Services;

namespace BasicToMips.UI;

public partial class SyntaxColorsWindow : Window
{
    private readonly SettingsService _settings;
    private SyntaxColorSettings _currentColors;
    private bool _suppressPresetChange = false;

    public bool ColorsChanged { get; private set; } = false;

    public SyntaxColorsWindow(SettingsService settings)
    {
        InitializeComponent();
        _settings = settings;
        _currentColors = settings.SyntaxColors.Clone();

        LoadPresets();
        UpdateColorBoxes();
        UpdatePreview();
    }

    private void LoadPresets()
    {
        PresetCombo.Items.Clear();
        foreach (var preset in SyntaxColorSettings.GetPresetNames())
        {
            PresetCombo.Items.Add(preset);
        }

        // Select current preset
        var currentPreset = _currentColors.PresetName;
        if (PresetCombo.Items.Contains(currentPreset))
        {
            _suppressPresetChange = true;
            PresetCombo.SelectedItem = currentPreset;
            _suppressPresetChange = false;
        }
        else
        {
            _suppressPresetChange = true;
            PresetCombo.SelectedItem = "Custom";
            _suppressPresetChange = false;
        }
    }

    private void PresetCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPresetChange) return;

        var selectedPreset = PresetCombo.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selectedPreset) || selectedPreset == "Custom") return;

        _currentColors = SyntaxColorSettings.GetPreset(selectedPreset);
        UpdateColorBoxes();
        UpdatePreview();
    }

    private void UpdateColorBoxes()
    {
        KeywordColorBox.Background = new SolidColorBrush(_currentColors.GetKeywordsColor());
        DeclarationColorBox.Background = new SolidColorBrush(_currentColors.GetDeclarationsColor());
        DeviceRefColorBox.Background = new SolidColorBrush(_currentColors.GetDeviceRefsColor());
        PropertyColorBox.Background = new SolidColorBrush(_currentColors.GetPropertiesColor());
        FunctionColorBox.Background = new SolidColorBrush(_currentColors.GetFunctionsColor());
        LabelColorBox.Background = new SolidColorBrush(_currentColors.GetLabelsColor());
        StringColorBox.Background = new SolidColorBrush(_currentColors.GetStringsColor());
        NumberColorBox.Background = new SolidColorBrush(_currentColors.GetNumbersColor());
        CommentColorBox.Background = new SolidColorBrush(_currentColors.GetCommentsColor());
        BooleanColorBox.Background = new SolidColorBrush(_currentColors.GetBooleansColor());
        OperatorColorBox.Background = new SolidColorBrush(_currentColors.GetOperatorsColor());
        BracketColorBox.Background = new SolidColorBrush(_currentColors.GetBracketsColor());
        EditorBackgroundColorBox.Background = new SolidColorBrush(_currentColors.GetEditorBackgroundColor());
    }

    private void UpdatePreview()
    {
        PreviewText.Inlines.Clear();

        // Build preview with colored text
        AddColoredText("' Temperature Controller\n", _currentColors.GetCommentsColor());
        AddColoredText("ALIAS ", _currentColors.GetDeclarationsColor());
        AddColoredText("sensor ", _currentColors.GetOperatorsColor());
        AddColoredText("d0\n", _currentColors.GetDeviceRefsColor());
        AddColoredText("VAR ", _currentColors.GetDeclarationsColor());
        AddColoredText("temp ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("0\n\n", _currentColors.GetNumbersColor());
        AddColoredText("main:\n", _currentColors.GetLabelsColor());
        AddColoredText("    temp ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("sensor", _currentColors.GetOperatorsColor());
        AddColoredText(".Temperature\n", _currentColors.GetPropertiesColor());
        AddColoredText("    IF ", _currentColors.GetKeywordsColor());
        AddColoredText("temp ", _currentColors.GetOperatorsColor());
        AddColoredText("> ", _currentColors.GetOperatorsColor());
        AddColoredText("300 ", _currentColors.GetNumbersColor());
        AddColoredText("THEN\n", _currentColors.GetKeywordsColor());
        AddColoredText("        ", _currentColors.GetOperatorsColor());
        AddColoredText("PRINT ", _currentColors.GetKeywordsColor());
        AddColoredText("\"Hot!\"\n", _currentColors.GetStringsColor());
        AddColoredText("    ENDIF\n", _currentColors.GetKeywordsColor());
        AddColoredText("    x ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("ABS", _currentColors.GetFunctionsColor());
        AddColoredText("(", _currentColors.GetOperatorsColor());
        AddColoredText("temp", _currentColors.GetOperatorsColor());
        AddColoredText(")\n", _currentColors.GetOperatorsColor());
        AddColoredText("    isOn ", _currentColors.GetOperatorsColor());
        AddColoredText("= ", _currentColors.GetOperatorsColor());
        AddColoredText("TRUE\n", _currentColors.GetBooleansColor());
        AddColoredText("    YIELD\n", _currentColors.GetKeywordsColor());
        AddColoredText("    GOTO ", _currentColors.GetKeywordsColor());
        AddColoredText("main\n", _currentColors.GetLabelsColor());
    }

    private void AddColoredText(string text, Color color)
    {
        PreviewText.Inlines.Add(new Run(text) { Foreground = new SolidColorBrush(color) });
    }

    private void ColorBox_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string colorName) return;

        var currentColor = colorName switch
        {
            "Keywords" => _currentColors.GetKeywordsColor(),
            "Declarations" => _currentColors.GetDeclarationsColor(),
            "DeviceRefs" => _currentColors.GetDeviceRefsColor(),
            "Properties" => _currentColors.GetPropertiesColor(),
            "Functions" => _currentColors.GetFunctionsColor(),
            "Labels" => _currentColors.GetLabelsColor(),
            "Strings" => _currentColors.GetStringsColor(),
            "Numbers" => _currentColors.GetNumbersColor(),
            "Comments" => _currentColors.GetCommentsColor(),
            "Booleans" => _currentColors.GetBooleansColor(),
            "Operators" => _currentColors.GetOperatorsColor(),
            "Brackets" => _currentColors.GetBracketsColor(),
            "EditorBackground" => _currentColors.GetEditorBackgroundColor(),
            _ => Colors.White
        };

        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
            var hexColor = SyntaxColorSettings.ColorToHex(newColor);

            switch (colorName)
            {
                case "Keywords": _currentColors.Keywords = hexColor; break;
                case "Declarations": _currentColors.Declarations = hexColor; break;
                case "DeviceRefs": _currentColors.DeviceRefs = hexColor; break;
                case "Properties": _currentColors.Properties = hexColor; break;
                case "Functions": _currentColors.Functions = hexColor; break;
                case "Labels": _currentColors.Labels = hexColor; break;
                case "Strings": _currentColors.Strings = hexColor; break;
                case "Numbers": _currentColors.Numbers = hexColor; break;
                case "Comments": _currentColors.Comments = hexColor; break;
                case "Booleans": _currentColors.Booleans = hexColor; break;
                case "Operators": _currentColors.Operators = hexColor; break;
                case "Brackets": _currentColors.Brackets = hexColor; break;
                case "EditorBackground": _currentColors.EditorBackground = hexColor; break;
            }

            _currentColors.PresetName = "Custom";
            _suppressPresetChange = true;
            PresetCombo.SelectedItem = "Custom";
            _suppressPresetChange = false;

            UpdateColorBoxes();
            UpdatePreview();
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _currentColors = SyntaxColorSettings.GetPreset("Default");
        _suppressPresetChange = true;
        PresetCombo.SelectedItem = "Default";
        _suppressPresetChange = false;
        UpdateColorBoxes();
        UpdatePreview();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        _settings.SyntaxColors = _currentColors;
        _settings.Save();
        ColorsChanged = true;
        DialogResult = true;
        Close();
    }
}
