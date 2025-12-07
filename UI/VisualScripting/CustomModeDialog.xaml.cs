using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Dialog for customizing experience mode settings
    /// </summary>
    public partial class CustomModeDialog : Window
    {
        public CustomModeDialog()
        {
            InitializeComponent();

            // Load current custom settings
            LoadSettings(ExperienceModeManager.Instance.GetCustomSettings());
        }

        private void LoadSettings(ExperienceModeSettings settings)
        {
            // UI Display
            ShowCodePanelCheck.IsChecked = settings.ShowCodePanel;
            ShowIC10ToggleCheck.IsChecked = settings.ShowIC10Toggle;
            ShowRegisterInfoCheck.IsChecked = settings.ShowRegisterInfo;
            ShowLineNumbersCheck.IsChecked = settings.ShowLineNumbers;
            ShowGridSnapCheck.IsChecked = settings.ShowGridSnap;
            ShowAdvancedPropertiesCheck.IsChecked = settings.ShowAdvancedProperties;

            // Label Style
            switch (settings.NodeLabelStyle)
            {
                case NodeLabelStyle.Friendly:
                    FriendlyLabelRadio.IsChecked = true;
                    break;
                case NodeLabelStyle.Mixed:
                    MixedLabelRadio.IsChecked = true;
                    break;
                case NodeLabelStyle.Technical:
                    TechnicalLabelRadio.IsChecked = true;
                    break;
            }

            // Display Options
            ShowExecutionPinsCheck.IsChecked = settings.ShowExecutionPins;
            ShowDataTypesCheck.IsChecked = settings.ShowDataTypes;
            ShowOptimizationHintsCheck.IsChecked = settings.ShowOptimizationHints;

            // Error Style
            switch (settings.ErrorMessageStyle)
            {
                case ErrorMessageStyle.Simple:
                    SimpleErrorsRadio.IsChecked = true;
                    break;
                case ErrorMessageStyle.Detailed:
                    DetailedErrorsRadio.IsChecked = true;
                    break;
                case ErrorMessageStyle.Technical:
                    TechnicalErrorsRadio.IsChecked = true;
                    break;
            }

            // Behavior
            AutoCompileCheck.IsChecked = settings.AutoCompile;

            // Categories (if empty, all are selected)
            bool allSelected = settings.AvailableNodeCategories.Count == 0;
            CatVariablesCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Variables");
            CatDevicesCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Devices");
            CatBasicMathCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Basic Math");
            CatFlowControlCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Flow Control");
            CatMathFunctionsCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Math Functions");
            CatLogicCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Logic");
            CatArraysCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Arrays");
            CatBitwiseCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Bitwise");
            CatAdvancedCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Advanced");
            CatStackCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Stack");
            CatTrigonometryCheck.IsChecked = allSelected || settings.AvailableNodeCategories.Contains("Trigonometry");
        }

        private ExperienceModeSettings SaveSettings()
        {
            var settings = new ExperienceModeSettings
            {
                ShowCodePanel = ShowCodePanelCheck.IsChecked == true,
                ShowIC10Toggle = ShowIC10ToggleCheck.IsChecked == true,
                ShowRegisterInfo = ShowRegisterInfoCheck.IsChecked == true,
                ShowLineNumbers = ShowLineNumbersCheck.IsChecked == true,
                ShowGridSnap = ShowGridSnapCheck.IsChecked == true,
                ShowAdvancedProperties = ShowAdvancedPropertiesCheck.IsChecked == true,
                ShowExecutionPins = ShowExecutionPinsCheck.IsChecked == true,
                ShowDataTypes = ShowDataTypesCheck.IsChecked == true,
                ShowOptimizationHints = ShowOptimizationHintsCheck.IsChecked == true,
                AutoCompile = AutoCompileCheck.IsChecked == true
            };

            // Label Style
            if (FriendlyLabelRadio.IsChecked == true)
                settings.NodeLabelStyle = NodeLabelStyle.Friendly;
            else if (MixedLabelRadio.IsChecked == true)
                settings.NodeLabelStyle = NodeLabelStyle.Mixed;
            else if (TechnicalLabelRadio.IsChecked == true)
                settings.NodeLabelStyle = NodeLabelStyle.Technical;

            // Error Style
            if (SimpleErrorsRadio.IsChecked == true)
                settings.ErrorMessageStyle = ErrorMessageStyle.Simple;
            else if (DetailedErrorsRadio.IsChecked == true)
                settings.ErrorMessageStyle = ErrorMessageStyle.Detailed;
            else if (TechnicalErrorsRadio.IsChecked == true)
                settings.ErrorMessageStyle = ErrorMessageStyle.Technical;

            // Categories
            var categories = new List<string>();
            if (CatVariablesCheck.IsChecked == true) categories.Add("Variables");
            if (CatDevicesCheck.IsChecked == true) categories.Add("Devices");
            if (CatBasicMathCheck.IsChecked == true) categories.Add("Basic Math");
            if (CatFlowControlCheck.IsChecked == true) categories.Add("Flow Control");
            if (CatMathFunctionsCheck.IsChecked == true) categories.Add("Math Functions");
            if (CatLogicCheck.IsChecked == true) categories.Add("Logic");
            if (CatArraysCheck.IsChecked == true) categories.Add("Arrays");
            if (CatBitwiseCheck.IsChecked == true) categories.Add("Bitwise");
            if (CatAdvancedCheck.IsChecked == true) categories.Add("Advanced");
            if (CatStackCheck.IsChecked == true) categories.Add("Stack");
            if (CatTrigonometryCheck.IsChecked == true) categories.Add("Trigonometry");

            // If all categories selected, use empty list (means "all")
            if (categories.Count == 11)
                categories.Clear();

            settings.AvailableNodeCategories = categories;

            return settings;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = SaveSettings();
            ExperienceModeManager.Instance.SetCustomSettings(settings);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelectAllCategories_Click(object sender, RoutedEventArgs e)
        {
            CatVariablesCheck.IsChecked = true;
            CatDevicesCheck.IsChecked = true;
            CatBasicMathCheck.IsChecked = true;
            CatFlowControlCheck.IsChecked = true;
            CatMathFunctionsCheck.IsChecked = true;
            CatLogicCheck.IsChecked = true;
            CatArraysCheck.IsChecked = true;
            CatBitwiseCheck.IsChecked = true;
            CatAdvancedCheck.IsChecked = true;
            CatStackCheck.IsChecked = true;
            CatTrigonometryCheck.IsChecked = true;
        }

        private void DeselectAllCategories_Click(object sender, RoutedEventArgs e)
        {
            CatVariablesCheck.IsChecked = false;
            CatDevicesCheck.IsChecked = false;
            CatBasicMathCheck.IsChecked = false;
            CatFlowControlCheck.IsChecked = false;
            CatMathFunctionsCheck.IsChecked = false;
            CatLogicCheck.IsChecked = false;
            CatArraysCheck.IsChecked = false;
            CatBitwiseCheck.IsChecked = false;
            CatAdvancedCheck.IsChecked = false;
            CatStackCheck.IsChecked = false;
            CatTrigonometryCheck.IsChecked = false;
        }

        private void LoadBeginnerPreset_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings(ExperienceModeSettings.CreateBeginnerSettings());
        }

        private void LoadIntermediatePreset_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings(ExperienceModeSettings.CreateIntermediateSettings());
        }

        private void LoadExpertPreset_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings(ExperienceModeSettings.CreateExpertSettings());
        }
    }
}
