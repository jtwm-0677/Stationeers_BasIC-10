using System;
using System.Windows;
using System.Windows.Controls;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace BasicToMips.UI.VisualScripting.Animations
{
    /// <summary>
    /// Settings panel for animation preferences
    /// </summary>
    public partial class AnimationSettingsPanel : WpfUserControl
    {
        private AnimationSettings _settings;
        private bool _isUpdating = false;

        public event EventHandler<AnimationSettings>? SettingsChanged;

        public AnimationSettingsPanel()
        {
            InitializeComponent();
            _settings = new AnimationSettings();
            UpdateUIFromSettings();
        }

        /// <summary>
        /// Get current settings
        /// </summary>
        public AnimationSettings GetSettings()
        {
            return _settings.Clone();
        }

        /// <summary>
        /// Load settings into the panel
        /// </summary>
        public void LoadSettings(AnimationSettings settings)
        {
            _settings = settings.Clone();
            UpdateUIFromSettings();
        }

        /// <summary>
        /// Update UI controls from current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            _isUpdating = true;

            EnableAnimationsCheckBox.IsChecked = _settings.EnableAnimations;
            SpeedSlider.Value = _settings.AnimationSpeed;
            SpeedValueText.Text = $"{_settings.AnimationSpeed:F1}x";

            // Particle density
            ParticleCountComboBox.SelectedIndex = _settings.ParticleCount switch
            {
                ParticleDensity.Low => 0,
                ParticleDensity.Medium => 1,
                ParticleDensity.High => 2,
                _ => 1
            };

            // Visual effects
            EnableGlowCheckBox.IsChecked = _settings.EnableGlowEffects;
            EnableValuePopupsCheckBox.IsChecked = _settings.EnableValuePopups;
            EnableExecutionHighlightCheckBox.IsChecked = _settings.EnableExecutionHighlight;
            EnableNodeHoverCheckBox.IsChecked = _settings.EnableNodeHoverEffects;
            EnableCanvasAnimationsCheckBox.IsChecked = _settings.EnableCanvasAnimations;
            EnableErrorAnimationsCheckBox.IsChecked = _settings.EnableErrorAnimations;

            // Performance
            PerformanceModeCheckBox.IsChecked = _settings.PerformanceMode;
            ThresholdSlider.Value = _settings.PerformanceModeThreshold;
            ThresholdValueText.Text = _settings.PerformanceModeThreshold.ToString();

            // Enable/disable settings panel based on master toggle
            AnimationSettingsContainer.IsEnabled = _settings.EnableAnimations;

            _isUpdating = false;
        }

        /// <summary>
        /// Update settings from UI controls
        /// </summary>
        private void UpdateSettingsFromUI()
        {
            if (_isUpdating)
                return;

            _settings.EnableAnimations = EnableAnimationsCheckBox.IsChecked ?? true;
            _settings.AnimationSpeed = SpeedSlider.Value;

            // Particle density
            _settings.ParticleCount = ParticleCountComboBox.SelectedIndex switch
            {
                0 => ParticleDensity.Low,
                1 => ParticleDensity.Medium,
                2 => ParticleDensity.High,
                _ => ParticleDensity.Medium
            };

            // Visual effects
            _settings.EnableGlowEffects = EnableGlowCheckBox.IsChecked ?? true;
            _settings.EnableValuePopups = EnableValuePopupsCheckBox.IsChecked ?? true;
            _settings.EnableExecutionHighlight = EnableExecutionHighlightCheckBox.IsChecked ?? true;
            _settings.EnableNodeHoverEffects = EnableNodeHoverCheckBox.IsChecked ?? true;
            _settings.EnableCanvasAnimations = EnableCanvasAnimationsCheckBox.IsChecked ?? true;
            _settings.EnableErrorAnimations = EnableErrorAnimationsCheckBox.IsChecked ?? true;

            // Performance
            _settings.PerformanceMode = PerformanceModeCheckBox.IsChecked ?? false;
            _settings.PerformanceModeThreshold = (int)ThresholdSlider.Value;

            // Notify listeners
            SettingsChanged?.Invoke(this, _settings);
        }

        private void EnableAnimationsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            AnimationSettingsContainer.IsEnabled = EnableAnimationsCheckBox.IsChecked ?? true;
            UpdateSettingsFromUI();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating)
                return;

            SpeedValueText.Text = $"{SpeedSlider.Value:F1}x";
            UpdateSettingsFromUI();
        }

        private void ParticleCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSettingsFromUI();
        }

        private void EffectCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSettingsFromUI();
        }

        private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdating)
                return;

            ThresholdValueText.Text = ((int)ThresholdSlider.Value).ToString();
            UpdateSettingsFromUI();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset to defaults
            _settings = new AnimationSettings();
            UpdateUIFromSettings();
            UpdateSettingsFromUI();

            MessageBox.Show(
                "Animation settings have been reset to defaults.",
                "Settings Reset",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
