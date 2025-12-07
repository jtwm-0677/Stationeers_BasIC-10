using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using BasicToMips.UI.VisualScripting.Project;

namespace BasicToMips.UI.VisualScripting.Dialogs
{
    public partial class NewProjectDialog : Window
    {
        public string ProjectName { get; private set; } = "";
        public string ProjectLocation { get; private set; } = "";
        public ExperienceLevel ExperienceMode { get; private set; } = ExperienceLevel.Beginner;
        public ProjectTemplate SelectedTemplate { get; private set; } = ProjectTemplate.Blank;

        public NewProjectDialog()
        {
            InitializeComponent();

            // Set default location to Documents/BasicToMips/Visual Scripts
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var defaultLocation = Path.Combine(documentsPath, "BasicToMips", "Visual Scripts");
            Directory.CreateDirectory(defaultLocation);
            LocationTextBox.Text = defaultLocation;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select project location",
                SelectedPath = LocationTextBox.Text,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LocationTextBox.Text = dialog.SelectedPath;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate project name
            ProjectName = ProjectNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Please enter a project name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return;
            }

            // Validate location
            ProjectLocation = LocationTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ProjectLocation))
            {
                MessageBox.Show("Please select a project location.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for invalid characters in project name
            if (ProjectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Project name contains invalid characters.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return;
            }

            // Get full project path
            var fullPath = Path.Combine(ProjectLocation, ProjectName);

            // Check if project already exists
            if (Directory.Exists(fullPath) && Directory.GetFiles(fullPath).Length > 0)
            {
                var result = MessageBox.Show(
                    $"A folder named '{ProjectName}' already exists at this location. Do you want to overwrite it?",
                    "Confirm Overwrite",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // Get experience mode
            ExperienceMode = ExperienceModeComboBox.SelectedIndex switch
            {
                0 => ExperienceLevel.Beginner,
                1 => ExperienceLevel.Intermediate,
                2 => ExperienceLevel.Expert,
                _ => ExperienceLevel.Beginner
            };

            // Get template
            SelectedTemplate = TemplateListBox.SelectedIndex switch
            {
                0 => ProjectTemplate.Blank,
                1 => ProjectTemplate.HelloWorld,
                2 => ProjectTemplate.SensorMonitor,
                3 => ProjectTemplate.DeviceController,
                _ => ProjectTemplate.Blank
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    /// <summary>
    /// Available project templates
    /// </summary>
    public enum ProjectTemplate
    {
        Blank,
        HelloWorld,
        SensorMonitor,
        DeviceController
    }
}
