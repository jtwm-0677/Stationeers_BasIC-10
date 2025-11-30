using System.Windows;

namespace BasicToMips.UI.Dialogs;

public partial class RenameDialog : Window
{
    public string CurrentName
    {
        get => CurrentNameTextBox.Text;
        set => CurrentNameTextBox.Text = value;
    }

    public string NewName
    {
        get => NewNameTextBox.Text;
        set => NewNameTextBox.Text = value;
    }

    public int OccurrenceCount { get; set; }

    public RenameDialog()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            NewNameTextBox.Focus();
            NewNameTextBox.SelectAll();
        };
    }

    public RenameDialog(string currentName, int occurrenceCount) : this()
    {
        CurrentName = currentName;
        NewName = currentName;
        OccurrenceCount = occurrenceCount;
        InfoTextBlock.Text = $"Found {occurrenceCount} occurrence(s) in document";
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            MessageBox.Show("Please enter a new name.", "Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NewName.Equals(CurrentName, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("New name is the same as current name.", "Rename", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }
}
