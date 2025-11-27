using System.Windows;
using System.Windows.Controls;
using BasicToMips.UI.Services;
using BasicToMips.Editor.Highlighting;

namespace BasicToMips.UI;

public partial class ExamplesWindow : Window
{
    private readonly DocumentationService _docs;
    public string? SelectedCode { get; private set; }

    public ExamplesWindow(DocumentationService docs)
    {
        InitializeComponent();
        _docs = docs;

        CodePreview.SyntaxHighlighting = BasicHighlighting.Create();
        LoadExamples();
    }

    private void LoadExamples()
    {
        var examples = _docs.GetExamples();
        ExamplesList.ItemsSource = examples;

        if (examples.Count > 0)
        {
            ExamplesList.SelectedIndex = 0;
        }
    }

    private void ExamplesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ExamplesList.SelectedItem is Example example)
        {
            PreviewTitle.Text = example.Name;
            CodePreview.Text = example.Code;
            LoadButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        if (ExamplesList.SelectedItem is Example example)
        {
            SelectedCode = example.Code;
            DialogResult = true;
            Close();
        }
    }
}
