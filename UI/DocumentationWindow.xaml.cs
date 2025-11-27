using System.Windows;

namespace BasicToMips.UI;

public partial class DocumentationWindow : Window
{
    public DocumentationWindow(string title, string content)
    {
        InitializeComponent();
        TitleText.Text = title;
        Title = title;
        ContentText.Text = content;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
