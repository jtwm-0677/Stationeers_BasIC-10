using System.Windows;

namespace BasicToMips.UI.VisualScripting.Dialogs
{
    /// <summary>
    /// Dialog result for unsaved changes prompt
    /// </summary>
    public enum UnsavedChangesResult
    {
        Save,
        DontSave,
        Cancel
    }

    public partial class UnsavedChangesDialog : Window
    {
        public UnsavedChangesResult Result { get; private set; } = UnsavedChangesResult.Cancel;

        public UnsavedChangesDialog(string projectName = "Untitled")
        {
            InitializeComponent();
            ProjectNameTextBlock.Text = $"Project: {projectName}";
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UnsavedChangesResult.Save;
            DialogResult = true;
            Close();
        }

        private void DontSaveButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UnsavedChangesResult.DontSave;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UnsavedChangesResult.Cancel;
            DialogResult = false;
            Close();
        }
    }
}
