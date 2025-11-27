using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;

namespace BasicToMips.UI;

public partial class FindReplaceWindow : Window
{
    private readonly TextEditor _editor;
    private int _lastSearchIndex = -1;
    private string _lastSearchText = "";

    public FindReplaceWindow(TextEditor editor)
    {
        InitializeComponent();
        _editor = editor;

        // If there's selected text, use it as the search term
        if (!string.IsNullOrEmpty(_editor.SelectedText) && !_editor.SelectedText.Contains('\n'))
        {
            FindTextBox.Text = _editor.SelectedText;
        }

        FindTextBox.Focus();
        FindTextBox.SelectAll();
    }

    private void FindTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Reset search position when text changes
        if (FindTextBox.Text != _lastSearchText)
        {
            _lastSearchIndex = -1;
            _lastSearchText = FindTextBox.Text;
        }
        StatusText.Text = "";
    }

    private void FindTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            FindNext_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void RegexCheck_Changed(object sender, RoutedEventArgs e)
    {
        // Disable whole word when regex is enabled
        if (RegexCheck.IsChecked == true)
        {
            WholeWordCheck.IsEnabled = false;
            WholeWordCheck.IsChecked = false;
        }
        else
        {
            WholeWordCheck.IsEnabled = true;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindTextBox.Text))
        {
            StatusText.Text = "Please enter search text";
            return;
        }

        var result = FindNext();
        if (!result.Found)
        {
            if (result.WrappedAround)
            {
                StatusText.Text = "No matches found";
            }
            else
            {
                // Try from beginning
                _lastSearchIndex = -1;
                result = FindNext();
                if (result.Found)
                {
                    StatusText.Text = "Wrapped to beginning of document";
                }
                else
                {
                    StatusText.Text = "No matches found";
                }
            }
        }
        else
        {
            StatusText.Text = "";
        }
    }

    private (bool Found, bool WrappedAround) FindNext()
    {
        string text = _editor.Text;
        string searchText = FindTextBox.Text;
        int startIndex = _lastSearchIndex + 1;

        if (startIndex >= text.Length)
        {
            return (false, true);
        }

        int foundIndex = -1;
        int foundLength = searchText.Length;

        if (RegexCheck.IsChecked == true)
        {
            try
            {
                var options = CaseSensitiveCheck.IsChecked == true
                    ? RegexOptions.None
                    : RegexOptions.IgnoreCase;
                var regex = new Regex(searchText, options);
                var match = regex.Match(text, startIndex);
                if (match.Success)
                {
                    foundIndex = match.Index;
                    foundLength = match.Length;
                }
            }
            catch (RegexParseException ex)
            {
                StatusText.Text = $"Invalid regex: {ex.Message}";
                return (false, false);
            }
        }
        else
        {
            var comparison = CaseSensitiveCheck.IsChecked == true
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (WholeWordCheck.IsChecked == true)
            {
                foundIndex = FindWholeWord(text, searchText, startIndex, comparison);
            }
            else
            {
                foundIndex = text.IndexOf(searchText, startIndex, comparison);
            }
        }

        if (foundIndex >= 0)
        {
            _lastSearchIndex = foundIndex;
            _editor.Select(foundIndex, foundLength);
            _editor.ScrollTo(_editor.Document.GetLineByOffset(foundIndex).LineNumber, 0);
            _editor.Focus();
            return (true, false);
        }

        return (false, false);
    }

    private int FindWholeWord(string text, string searchText, int startIndex, StringComparison comparison)
    {
        int index = startIndex;
        while (index < text.Length)
        {
            int found = text.IndexOf(searchText, index, comparison);
            if (found < 0) return -1;

            bool startOk = found == 0 || !char.IsLetterOrDigit(text[found - 1]);
            bool endOk = found + searchText.Length >= text.Length ||
                         !char.IsLetterOrDigit(text[found + searchText.Length]);

            if (startOk && endOk)
            {
                return found;
            }

            index = found + 1;
        }
        return -1;
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindTextBox.Text))
        {
            StatusText.Text = "Please enter search text";
            return;
        }

        // If current selection matches search, replace it
        if (!string.IsNullOrEmpty(_editor.SelectedText))
        {
            bool matches = false;
            if (RegexCheck.IsChecked == true)
            {
                try
                {
                    var options = CaseSensitiveCheck.IsChecked == true
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase;
                    var regex = new Regex($"^{FindTextBox.Text}$", options);
                    matches = regex.IsMatch(_editor.SelectedText);
                }
                catch { }
            }
            else
            {
                var comparison = CaseSensitiveCheck.IsChecked == true
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                matches = _editor.SelectedText.Equals(FindTextBox.Text, comparison);
            }

            if (matches)
            {
                string replacement = ReplaceTextBox.Text;
                if (RegexCheck.IsChecked == true)
                {
                    try
                    {
                        var options = CaseSensitiveCheck.IsChecked == true
                            ? RegexOptions.None
                            : RegexOptions.IgnoreCase;
                        var regex = new Regex(FindTextBox.Text, options);
                        replacement = regex.Replace(_editor.SelectedText, ReplaceTextBox.Text);
                    }
                    catch { }
                }

                int offset = _editor.SelectionStart;
                _editor.Document.Replace(_editor.SelectionStart, _editor.SelectionLength, replacement);
                _lastSearchIndex = offset + replacement.Length - 1;
                StatusText.Text = "Replaced 1 occurrence";
            }
        }

        // Find next occurrence
        FindNext_Click(sender, e);
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindTextBox.Text))
        {
            StatusText.Text = "Please enter search text";
            return;
        }

        string text = _editor.Text;
        string searchText = FindTextBox.Text;
        string replaceText = ReplaceTextBox.Text;
        int count = 0;

        _editor.Document.BeginUpdate();
        try
        {
            if (RegexCheck.IsChecked == true)
            {
                try
                {
                    var options = CaseSensitiveCheck.IsChecked == true
                        ? RegexOptions.None
                        : RegexOptions.IgnoreCase;
                    var regex = new Regex(searchText, options);
                    count = regex.Matches(text).Count;
                    string newText = regex.Replace(text, replaceText);
                    _editor.Document.Text = newText;
                }
                catch (RegexParseException ex)
                {
                    StatusText.Text = $"Invalid regex: {ex.Message}";
                    return;
                }
            }
            else
            {
                var comparison = CaseSensitiveCheck.IsChecked == true
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                // Count and replace
                int index = 0;
                var result = new System.Text.StringBuilder();

                while (index < text.Length)
                {
                    int found;
                    if (WholeWordCheck.IsChecked == true)
                    {
                        found = FindWholeWord(text, searchText, index, comparison);
                    }
                    else
                    {
                        found = text.IndexOf(searchText, index, comparison);
                    }

                    if (found < 0)
                    {
                        result.Append(text.Substring(index));
                        break;
                    }

                    result.Append(text.Substring(index, found - index));
                    result.Append(replaceText);
                    index = found + searchText.Length;
                    count++;
                }

                if (count > 0)
                {
                    _editor.Document.Text = result.ToString();
                }
            }
        }
        finally
        {
            _editor.Document.EndUpdate();
        }

        StatusText.Text = count > 0
            ? $"Replaced {count} occurrence{(count != 1 ? "s" : "")}"
            : "No matches found";
        _lastSearchIndex = -1;
    }

    private void Count_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(FindTextBox.Text))
        {
            StatusText.Text = "Please enter search text";
            return;
        }

        string text = _editor.Text;
        string searchText = FindTextBox.Text;
        int count = 0;

        if (RegexCheck.IsChecked == true)
        {
            try
            {
                var options = CaseSensitiveCheck.IsChecked == true
                    ? RegexOptions.None
                    : RegexOptions.IgnoreCase;
                var regex = new Regex(searchText, options);
                count = regex.Matches(text).Count;
            }
            catch (RegexParseException ex)
            {
                StatusText.Text = $"Invalid regex: {ex.Message}";
                return;
            }
        }
        else
        {
            var comparison = CaseSensitiveCheck.IsChecked == true
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            int index = 0;
            while (index < text.Length)
            {
                int found;
                if (WholeWordCheck.IsChecked == true)
                {
                    found = FindWholeWord(text, searchText, index, comparison);
                }
                else
                {
                    found = text.IndexOf(searchText, index, comparison);
                }

                if (found < 0) break;
                count++;
                index = found + searchText.Length;
            }
        }

        StatusText.Text = $"Found {count} occurrence{(count != 1 ? "s" : "")}";
    }
}
