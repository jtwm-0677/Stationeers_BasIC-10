using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace BasicToMips.UI.VisualScripting.Canvas;

/// <summary>
/// Represents an item that can be selected on the canvas.
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    bool IsSelected { get; set; }

    /// <summary>
    /// Gets the bounding rectangle of this item.
    /// </summary>
    Rect Bounds { get; }

    /// <summary>
    /// Determines if a point is within this item.
    /// </summary>
    bool HitTest(Point point);
}

/// <summary>
/// Manages selection state for canvas items.
/// Supports single selection, multi-selection (Ctrl+click), and box selection.
/// </summary>
public class SelectionManager
{
    private readonly ObservableCollection<ISelectable> _selectedItems = new();
    private Point? _boxSelectionStart;
    private Rect? _boxSelectionRect;
    private bool _isBoxSelecting;

    /// <summary>
    /// Gets the currently selected items (read-only).
    /// </summary>
    public IReadOnlyList<ISelectable> SelectedItems => _selectedItems;

    /// <summary>
    /// Gets whether any items are selected.
    /// </summary>
    public bool HasSelection => _selectedItems.Count > 0;

    /// <summary>
    /// Gets the current box selection rectangle, if active.
    /// </summary>
    public Rect? BoxSelectionRect => _boxSelectionRect;

    /// <summary>
    /// Gets whether a box selection is in progress.
    /// </summary>
    public bool IsBoxSelecting => _isBoxSelecting;

    /// <summary>
    /// Event raised when the selection changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Event raised when the box selection rectangle changes.
    /// </summary>
    public event EventHandler? BoxSelectionChanged;

    /// <summary>
    /// Selects a single item, clearing all other selections.
    /// </summary>
    public void SelectSingle(ISelectable item)
    {
        var previousSelection = _selectedItems.ToList();

        ClearSelection();
        AddToSelection(item);

        RaiseSelectionChanged(previousSelection, _selectedItems.ToList());
    }

    /// <summary>
    /// Toggles selection of an item (for Ctrl+click).
    /// </summary>
    public void ToggleSelection(ISelectable item)
    {
        var previousSelection = _selectedItems.ToList();

        if (_selectedItems.Contains(item))
        {
            RemoveFromSelection(item);
        }
        else
        {
            AddToSelection(item);
        }

        RaiseSelectionChanged(previousSelection, _selectedItems.ToList());
    }

    /// <summary>
    /// Adds an item to the selection without clearing existing selections.
    /// </summary>
    public void AddToSelection(ISelectable item)
    {
        if (!_selectedItems.Contains(item))
        {
            _selectedItems.Add(item);
            item.IsSelected = true;
        }
    }

    /// <summary>
    /// Removes an item from the selection.
    /// </summary>
    public void RemoveFromSelection(ISelectable item)
    {
        if (_selectedItems.Remove(item))
        {
            item.IsSelected = false;
        }
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    public void ClearSelection()
    {
        var previousSelection = _selectedItems.ToList();

        foreach (var item in _selectedItems)
        {
            item.IsSelected = false;
        }
        _selectedItems.Clear();

        if (previousSelection.Count > 0)
        {
            RaiseSelectionChanged(previousSelection, new List<ISelectable>());
        }
    }

    /// <summary>
    /// Starts a box selection at the specified point.
    /// </summary>
    public void BeginBoxSelection(Point startPoint)
    {
        _boxSelectionStart = startPoint;
        _boxSelectionRect = new Rect(startPoint, new Size(0, 0));
        _isBoxSelecting = true;
        BoxSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the box selection rectangle as the mouse moves.
    /// </summary>
    public void UpdateBoxSelection(Point currentPoint)
    {
        if (!_boxSelectionStart.HasValue)
            return;

        var start = _boxSelectionStart.Value;
        var width = currentPoint.X - start.X;
        var height = currentPoint.Y - start.Y;

        // Create rectangle with positive width/height
        var x = width >= 0 ? start.X : currentPoint.X;
        var y = height >= 0 ? start.Y : currentPoint.Y;

        _boxSelectionRect = new Rect(x, y, Math.Abs(width), Math.Abs(height));
        BoxSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Completes the box selection, selecting all items within the rectangle.
    /// </summary>
    /// <param name="allItems">All items that can be selected.</param>
    /// <param name="addToExisting">If true, adds to existing selection; if false, replaces it.</param>
    public void EndBoxSelection(IEnumerable<ISelectable> allItems, bool addToExisting = false)
    {
        if (!_boxSelectionRect.HasValue)
            return;

        var previousSelection = _selectedItems.ToList();

        if (!addToExisting)
        {
            ClearSelection();
        }

        var selectionRect = _boxSelectionRect.Value;

        // Select all items that intersect with the box
        foreach (var item in allItems)
        {
            if (selectionRect.IntersectsWith(item.Bounds))
            {
                AddToSelection(item);
            }
        }

        _boxSelectionStart = null;
        _boxSelectionRect = null;
        _isBoxSelecting = false;
        BoxSelectionChanged?.Invoke(this, EventArgs.Empty);

        RaiseSelectionChanged(previousSelection, _selectedItems.ToList());
    }

    /// <summary>
    /// Cancels the current box selection without changing the selection.
    /// </summary>
    public void CancelBoxSelection()
    {
        _boxSelectionStart = null;
        _boxSelectionRect = null;
        _isBoxSelecting = false;
        BoxSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Renders the box selection rectangle.
    /// </summary>
    public void RenderBoxSelection(DrawingContext drawingContext)
    {
        if (!_boxSelectionRect.HasValue)
            return;

        var rect = _boxSelectionRect.Value;
        var brush = new SolidColorBrush(Color.FromArgb(40, 0, 122, 204));
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(180, 0, 122, 204)), 1.0);

        brush.Freeze();
        pen.Freeze();

        drawingContext.DrawRectangle(brush, pen, rect);
    }

    /// <summary>
    /// Finds the topmost selectable item at the specified point.
    /// </summary>
    public ISelectable? HitTest(Point point, IEnumerable<ISelectable> allItems)
    {
        // Iterate in reverse order to get topmost items first
        return allItems.Reverse().FirstOrDefault(item => item.HitTest(point));
    }

    private void RaiseSelectionChanged(List<ISelectable> previousSelection, List<ISelectable> currentSelection)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(previousSelection, currentSelection));
    }
}

/// <summary>
/// Event args for selection changes.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public IReadOnlyList<ISelectable> PreviousSelection { get; }
    public IReadOnlyList<ISelectable> CurrentSelection { get; }

    public SelectionChangedEventArgs(IReadOnlyList<ISelectable> previousSelection, IReadOnlyList<ISelectable> currentSelection)
    {
        PreviousSelection = previousSelection;
        CurrentSelection = currentSelection;
    }
}
