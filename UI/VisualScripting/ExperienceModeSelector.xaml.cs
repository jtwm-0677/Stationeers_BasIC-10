using System.Windows;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfRadioButton = System.Windows.Controls.RadioButton;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// UI control for selecting experience mode
    /// </summary>
    public partial class ExperienceModeSelector : WpfUserControl
    {
        public event EventHandler<ExperienceLevel>? ModeChanged;

        public ExperienceModeSelector()
        {
            InitializeComponent();

            // Set initial mode (with error handling)
            try
            {
                UpdateButtonsFromMode(ExperienceModeManager.Instance.CurrentMode);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set initial mode: {ex.Message}");
                // Default to Beginner if there's an error
            }
        }

        private void ModeButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfRadioButton button)
                return;

            ExperienceLevel newMode;

            if (ReferenceEquals(button, BeginnerButton))
                newMode = ExperienceLevel.Beginner;
            else if (ReferenceEquals(button, IntermediateButton))
                newMode = ExperienceLevel.Intermediate;
            else if (ReferenceEquals(button, ExpertButton))
                newMode = ExperienceLevel.Expert;
            else if (ReferenceEquals(button, CustomButton))
                newMode = ExperienceLevel.Custom;
            else
                return;

            // Update manager
            ExperienceModeManager.Instance.SetMode(newMode);

            // Notify listeners
            ModeChanged?.Invoke(this, newMode);
        }

        private void CustomizeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomModeDialog
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                // Switch to custom mode if settings were saved
                CustomButton.IsChecked = true;
            }
        }

        /// <summary>
        /// Update UI to reflect current mode
        /// </summary>
        public void UpdateButtonsFromMode(ExperienceLevel mode)
        {
            switch (mode)
            {
                case ExperienceLevel.Beginner:
                    BeginnerButton.IsChecked = true;
                    break;
                case ExperienceLevel.Intermediate:
                    IntermediateButton.IsChecked = true;
                    break;
                case ExperienceLevel.Expert:
                    ExpertButton.IsChecked = true;
                    break;
                case ExperienceLevel.Custom:
                    CustomButton.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// Get currently selected mode
        /// </summary>
        public ExperienceLevel GetSelectedMode()
        {
            if (BeginnerButton.IsChecked == true)
                return ExperienceLevel.Beginner;
            if (IntermediateButton.IsChecked == true)
                return ExperienceLevel.Intermediate;
            if (ExpertButton.IsChecked == true)
                return ExperienceLevel.Expert;
            if (CustomButton.IsChecked == true)
                return ExperienceLevel.Custom;

            return ExperienceLevel.Beginner;
        }
    }
}
