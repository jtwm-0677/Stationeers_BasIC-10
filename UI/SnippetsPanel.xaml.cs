using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BasicToMips.UI.Services;

namespace BasicToMips.UI;

public partial class SnippetsPanel : System.Windows.Controls.UserControl
{
    public event EventHandler<string>? SnippetSelected;
    public event EventHandler? CloseRequested;

    public SnippetsPanel()
    {
        InitializeComponent();
        PopulateSnippets();
    }

    private void PopulateSnippets()
    {
        var docs = new DocumentationService();
        var snippets = docs.GetSnippets();

        // Group snippets by category
        var categories = new Dictionary<string, List<CodeSnippet>>
        {
            ["Loops"] = new(),
            ["Conditions"] = new(),
            ["Devices"] = new(),
            ["Patterns"] = new(),
            ["Math"] = new()
        };

        // Categorize snippets
        foreach (var snippet in snippets)
        {
            if (snippet.Name.Contains("Loop") || snippet.Name.Contains("While") || snippet.Name.Contains("For"))
                categories["Loops"].Add(snippet);
            else if (snippet.Name.Contains("If") || snippet.Name.Contains("State Machine"))
                categories["Conditions"].Add(snippet);
            else if (snippet.Name.Contains("Device") || snippet.Name.Contains("Alias") || snippet.Name.Contains("Batch"))
                categories["Devices"].Add(snippet);
            else if (snippet.Name.Contains("Hysteresis") || snippet.Name.Contains("Edge") || snippet.Name.Contains("State"))
                categories["Patterns"].Add(snippet);
            else if (snippet.Name.Contains("Temperature") || snippet.Name.Contains("Pressure"))
                categories["Math"].Add(snippet);
            else
                categories["Patterns"].Add(snippet); // Default category
        }

        // Create expandable sections for each category
        foreach (var (categoryName, categorySnippets) in categories)
        {
            if (categorySnippets.Count == 0) continue;

            var expander = new Expander
            {
                Header = categoryName,
                IsExpanded = false,
                Margin = new Thickness(0, 2, 0, 2),
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48))
            };

            var stackPanel = new StackPanel { Margin = new Thickness(4) };

            foreach (var snippet in categorySnippets)
            {
                var button = new Button
                {
                    Content = snippet.Name,
                    Style = (Style)Application.Current.FindResource("ModernButtonStyle"),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(8, 4, 8, 4),
                    Tag = snippet.Code
                };
                button.Click += SnippetButton_Click;
                stackPanel.Children.Add(button);
            }

            expander.Content = stackPanel;
            SnippetsContainer.Children.Add(expander);
        }
    }

    private void SnippetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string code)
        {
            SnippetSelected?.Invoke(this, code);
        }
    }

    private void CloseSnippets_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
