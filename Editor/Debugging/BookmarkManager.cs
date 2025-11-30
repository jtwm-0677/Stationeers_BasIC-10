namespace BasicToMips.Editor.Debugging;

/// <summary>
/// Manages bookmarks in the editor.
/// </summary>
public class BookmarkManager
{
    private readonly SortedSet<int> _bookmarks = new();

    /// <summary>
    /// Event raised when bookmarks change.
    /// </summary>
    public event EventHandler? BookmarksChanged;

    /// <summary>
    /// Get all bookmark line numbers (1-based).
    /// </summary>
    public IReadOnlyCollection<int> Bookmarks => _bookmarks;

    /// <summary>
    /// Check if a line has a bookmark.
    /// </summary>
    public bool HasBookmark(int line) => _bookmarks.Contains(line);

    /// <summary>
    /// Toggle bookmark on a line.
    /// </summary>
    public bool ToggleBookmark(int line)
    {
        if (_bookmarks.Contains(line))
        {
            _bookmarks.Remove(line);
            OnBookmarksChanged();
            return false;
        }
        else
        {
            _bookmarks.Add(line);
            OnBookmarksChanged();
            return true;
        }
    }

    /// <summary>
    /// Set a bookmark on a line.
    /// </summary>
    public void SetBookmark(int line)
    {
        if (_bookmarks.Add(line))
        {
            OnBookmarksChanged();
        }
    }

    /// <summary>
    /// Remove a bookmark from a line.
    /// </summary>
    public void RemoveBookmark(int line)
    {
        if (_bookmarks.Remove(line))
        {
            OnBookmarksChanged();
        }
    }

    /// <summary>
    /// Clear all bookmarks.
    /// </summary>
    public void ClearAll()
    {
        if (_bookmarks.Count > 0)
        {
            _bookmarks.Clear();
            OnBookmarksChanged();
        }
    }

    /// <summary>
    /// Get the next bookmark after the given line.
    /// Returns -1 if no bookmark found (wraps around to start).
    /// </summary>
    public int GetNextBookmark(int currentLine)
    {
        if (_bookmarks.Count == 0) return -1;

        // Find next bookmark after current line
        foreach (var bm in _bookmarks)
        {
            if (bm > currentLine) return bm;
        }

        // Wrap around to first bookmark
        return _bookmarks.Min;
    }

    /// <summary>
    /// Get the previous bookmark before the given line.
    /// Returns -1 if no bookmark found (wraps around to end).
    /// </summary>
    public int GetPreviousBookmark(int currentLine)
    {
        if (_bookmarks.Count == 0) return -1;

        // Find previous bookmark before current line
        int? prev = null;
        foreach (var bm in _bookmarks)
        {
            if (bm >= currentLine) break;
            prev = bm;
        }

        if (prev.HasValue) return prev.Value;

        // Wrap around to last bookmark
        return _bookmarks.Max;
    }

    /// <summary>
    /// Adjust bookmark positions after text changes.
    /// </summary>
    public void AdjustForLineChange(int changedLine, int delta)
    {
        if (delta == 0 || _bookmarks.Count == 0) return;

        var toRemove = _bookmarks.Where(bm => bm >= changedLine).ToList();

        foreach (var bm in toRemove)
        {
            _bookmarks.Remove(bm);
        }

        foreach (var bm in toRemove)
        {
            var newLine = bm + delta;
            if (newLine > 0)
            {
                _bookmarks.Add(newLine);
            }
        }

        OnBookmarksChanged();
    }

    private void OnBookmarksChanged()
    {
        BookmarksChanged?.Invoke(this, EventArgs.Empty);
    }
}
