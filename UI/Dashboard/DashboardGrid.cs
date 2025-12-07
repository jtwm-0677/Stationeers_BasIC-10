using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BasicToMips.UI.Dashboard;

/// <summary>
/// Grid layout manager for dashboard widgets
/// </summary>
public class DashboardGrid : Grid
{
    private readonly List<WidgetBase> _widgets = new();
    private int _rows = 4;
    private int _columns = 4;

    public int GridRows
    {
        get => _rows;
        set
        {
            _rows = value;
            RebuildGrid();
        }
    }

    public int GridColumns
    {
        get => _columns;
        set
        {
            _columns = value;
            RebuildGrid();
        }
    }

    public DashboardGrid()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        RebuildGrid();
    }

    private void RebuildGrid()
    {
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();

        // Create row definitions
        for (int i = 0; i < _rows; i++)
        {
            RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        // Create column definitions
        for (int i = 0; i < _columns; i++)
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        // Re-add widgets
        RefreshWidgets();
    }

    public void AddWidget(WidgetBase widget)
    {
        if (_widgets.Contains(widget))
            return;

        // Find empty spot or default to 0,0
        var (row, col) = FindEmptyCell();
        widget.GridRow = row;
        widget.GridColumn = col;

        _widgets.Add(widget);
        PlaceWidget(widget);

        // Subscribe to events
        widget.CloseRequested += OnWidgetCloseRequested;
        widget.DragMoved += OnWidgetDragMoved;
    }

    public void RemoveWidget(WidgetBase widget)
    {
        _widgets.Remove(widget);
        Children.Remove(widget);

        // Unsubscribe from events
        widget.CloseRequested -= OnWidgetCloseRequested;
        widget.DragMoved -= OnWidgetDragMoved;
    }

    public void ClearWidgets()
    {
        foreach (var widget in _widgets.ToList())
        {
            RemoveWidget(widget);
        }
    }

    private void PlaceWidget(WidgetBase widget)
    {
        // Ensure widget is within bounds
        widget.GridRow = Math.Max(0, Math.Min(widget.GridRow, _rows - 1));
        widget.GridColumn = Math.Max(0, Math.Min(widget.GridColumn, _columns - 1));
        widget.RowSpan = Math.Max(1, Math.Min(widget.RowSpan, _rows - widget.GridRow));
        widget.ColumnSpan = Math.Max(1, Math.Min(widget.ColumnSpan, _columns - widget.GridColumn));

        // Check for overlap and adjust if needed
        if (IsOverlapping(widget))
        {
            var (row, col) = FindEmptyCell();
            widget.GridRow = row;
            widget.GridColumn = col;
            widget.RowSpan = 1;
            widget.ColumnSpan = 1;
        }

        Grid.SetRow(widget, widget.GridRow);
        Grid.SetColumn(widget, widget.GridColumn);
        Grid.SetRowSpan(widget, widget.RowSpan);
        Grid.SetColumnSpan(widget, widget.ColumnSpan);

        widget.Margin = new Thickness(4);

        if (!Children.Contains(widget))
        {
            Children.Add(widget);
        }
    }

    private void RefreshWidgets()
    {
        Children.Clear();
        foreach (var widget in _widgets.ToList())
        {
            PlaceWidget(widget);
        }
    }

    private bool IsOverlapping(WidgetBase testWidget)
    {
        foreach (var widget in _widgets)
        {
            if (widget == testWidget)
                continue;

            // Check if rectangles overlap
            var rect1 = new Rect(
                widget.GridColumn,
                widget.GridRow,
                widget.ColumnSpan,
                widget.RowSpan);

            var rect2 = new Rect(
                testWidget.GridColumn,
                testWidget.GridRow,
                testWidget.ColumnSpan,
                testWidget.RowSpan);

            if (rect1.IntersectsWith(rect2))
                return true;
        }
        return false;
    }

    private (int row, int col) FindEmptyCell()
    {
        // Find first empty cell
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _columns; c++)
            {
                if (!IsCellOccupied(r, c))
                    return (r, c);
            }
        }
        return (0, 0); // Default to top-left if all occupied
    }

    private bool IsCellOccupied(int row, int col)
    {
        foreach (var widget in _widgets)
        {
            if (col >= widget.GridColumn &&
                col < widget.GridColumn + widget.ColumnSpan &&
                row >= widget.GridRow &&
                row < widget.GridRow + widget.RowSpan)
            {
                return true;
            }
        }
        return false;
    }

    private void OnWidgetCloseRequested(object? sender, EventArgs e)
    {
        if (sender is WidgetBase widget)
        {
            RemoveWidget(widget);
        }
    }

    private void OnWidgetDragMoved(object? sender, Point position)
    {
        if (sender is not WidgetBase widget)
            return;

        // Calculate which cell the widget was dragged to
        var cellWidth = ActualWidth / _columns;
        var cellHeight = ActualHeight / _rows;

        int newCol = (int)(position.X / cellWidth);
        int newRow = (int)(position.Y / cellHeight);

        // Clamp to grid bounds
        newCol = Math.Max(0, Math.Min(newCol, _columns - widget.ColumnSpan));
        newRow = Math.Max(0, Math.Min(newRow, _rows - widget.RowSpan));

        // Only update if position changed
        if (newRow != widget.GridRow || newCol != widget.GridColumn)
        {
            // Temporarily remove from grid to check overlap
            var oldRow = widget.GridRow;
            var oldCol = widget.GridColumn;

            widget.GridRow = newRow;
            widget.GridColumn = newCol;

            if (IsOverlapping(widget))
            {
                // Revert if overlapping
                widget.GridRow = oldRow;
                widget.GridColumn = oldCol;
            }
            else
            {
                // Update position
                Grid.SetRow(widget, widget.GridRow);
                Grid.SetColumn(widget, widget.GridColumn);
            }
        }
    }

    public List<WidgetBase> GetWidgets()
    {
        return new List<WidgetBase>(_widgets);
    }
}
