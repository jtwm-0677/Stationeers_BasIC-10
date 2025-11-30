using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasicToMips.UI;

/// <summary>
/// Represents a single editor tab with its associated file and content
/// </summary>
public class EditorTab : INotifyPropertyChanged
{
    private string? _filePath;
    private bool _isModified;
    private string _content = "";

    /// <summary>
    /// Full path to the file, or null for untitled tabs
    /// </summary>
    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(FileName));
            }
        }
    }

    /// <summary>
    /// Whether the tab has unsaved changes
    /// </summary>
    public bool IsModified
    {
        get => _isModified;
        set
        {
            if (_isModified != value)
            {
                _isModified = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    /// <summary>
    /// The text content of the editor
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Display name for tab header (filename or "Untitled")
    /// </summary>
    public string DisplayName
    {
        get
        {
            var name = string.IsNullOrEmpty(FilePath) ? "Untitled" : System.IO.Path.GetFileName(FilePath);
            return IsModified ? name + " *" : name;
        }
    }

    /// <summary>
    /// Just the filename without modified indicator
    /// </summary>
    public string FileName =>
        string.IsNullOrEmpty(FilePath) ? "Untitled" : System.IO.Path.GetFileName(FilePath);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
