using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BasicToMips.UI.Dialogs;

public partial class ScriptSelectDialog : Window
{
    public string? SelectedScriptPath { get; private set; }
    public bool BrowseRequested { get; private set; }

    public ScriptSelectDialog(string scriptsFolder)
    {
        InitializeComponent();
        LoadScripts(scriptsFolder);
    }

    private void LoadScripts(string scriptsFolder)
    {
        var scripts = new List<ScriptItem>();

        if (Directory.Exists(scriptsFolder))
        {
            foreach (var dir in Directory.GetDirectories(scriptsFolder))
            {
                var dirName = Path.GetFileName(dir);

                // Look for .bas files in the folder
                var basFiles = Directory.GetFiles(dir, "*.bas");
                if (basFiles.Length > 0)
                {
                    var filePath = basFiles[0];
                    scripts.Add(new ScriptItem
                    {
                        Name = dirName,
                        Path = filePath,
                        LastModified = File.GetLastWriteTime(filePath)
                    });
                }
                else
                {
                    // Check for .basic files too
                    var basicFiles = Directory.GetFiles(dir, "*.basic");
                    if (basicFiles.Length > 0)
                    {
                        var filePath = basicFiles[0];
                        scripts.Add(new ScriptItem
                        {
                            Name = dirName,
                            Path = filePath,
                            LastModified = File.GetLastWriteTime(filePath)
                        });
                    }
                }
            }
        }

        if (scripts.Count == 0)
        {
            EmptyMessage.Visibility = Visibility.Visible;
        }
        else
        {
            ScriptList.ItemsSource = scripts.OrderByDescending(s => s.LastModified).ToList();
        }
    }

    private void ScriptButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            SelectedScriptPath = path;
            DialogResult = true;
            Close();
        }
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        BrowseRequested = true;
        DialogResult = false;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private class ScriptItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public DateTime LastModified { get; set; }
    }
}
