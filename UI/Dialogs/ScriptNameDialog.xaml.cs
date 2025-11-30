using System.Windows;
using System.Windows.Input;

namespace BasicToMips.UI.Dialogs;

public partial class ScriptNameDialog : Window
{
    public string ScriptName { get; private set; } = "";

    public ScriptNameDialog(string? defaultName = null)
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(defaultName))
        {
            ScriptNameInput.Text = defaultName;
        }
        Loaded += (s, e) =>
        {
            ScriptNameInput.Focus();
            ScriptNameInput.SelectAll();
        };
    }

    private void ScriptNameInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Save_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            Cancel_Click(sender, e);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = ScriptNameInput.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ScriptNameInput.Focus();
            return;
        }

        // Remove invalid characters for folder/file names
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), "");
        }

        ScriptName = name;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
