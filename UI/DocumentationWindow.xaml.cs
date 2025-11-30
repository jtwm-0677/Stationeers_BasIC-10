using System.Windows;
using System.Windows.Controls;

namespace BasicToMips.UI;

public partial class DocumentationWindow : Window
{
    /// <summary>
    /// Creates a documentation window with formatted StackPanel content.
    /// </summary>
    /// <param name="title">Window title</param>
    /// <param name="populateAction">Action that populates the content panel</param>
    public DocumentationWindow(string title, Action<StackPanel> populateAction)
    {
        InitializeComponent();
        TitleText.Text = title;
        Title = title;
        populateAction(ContentPanel);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
