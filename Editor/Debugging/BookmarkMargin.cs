using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor.Debugging;

/// <summary>
/// A margin that displays bookmark indicators in the editor gutter.
/// </summary>
public class BookmarkMargin : AbstractMargin
{
    private readonly BookmarkManager _bookmarkManager;
    private readonly TextEditor _editor;

    private static readonly Brush BookmarkBrush = new SolidColorBrush(Color.FromRgb(30, 144, 255)); // Dodger Blue
    private static readonly Brush BookmarkHoverBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237)); // Cornflower Blue

    private int _hoveredLine = -1;

    static BookmarkMargin()
    {
        BookmarkBrush.Freeze();
        BookmarkHoverBrush.Freeze();
    }

    public BookmarkMargin(TextEditor editor, BookmarkManager bookmarkManager)
    {
        _editor = editor;
        _bookmarkManager = bookmarkManager;
        _bookmarkManager.BookmarksChanged += (s, e) => InvalidateVisual();
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(14, 0); // 14 pixels wide for bookmark markers
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var textView = TextView;
        if (textView == null || !textView.VisualLinesValid) return;

        var renderSize = RenderSize;

        // Transparent background (bookmarks share gutter with breakpoints)
        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            var y = visualLine.VisualTop - textView.VerticalOffset;
            var lineHeight = visualLine.Height;

            // Draw bookmark marker if set (small square/rectangle)
            if (_bookmarkManager.HasBookmark(lineNumber))
            {
                var centerY = y + lineHeight / 2;
                var rectHeight = Math.Min(lineHeight - 4, 10);
                var rectWidth = renderSize.Width - 4;

                var rect = new Rect(2, centerY - rectHeight / 2, rectWidth, rectHeight);
                drawingContext.DrawRectangle(BookmarkBrush, null, rect);
            }
            // Draw hover indicator
            else if (lineNumber == _hoveredLine)
            {
                var centerY = y + lineHeight / 2;
                var rectHeight = Math.Min(lineHeight - 4, 10);
                var rectWidth = renderSize.Width - 4;

                var hoverBrush = new SolidColorBrush(Color.FromArgb(80, 30, 144, 255));
                var rect = new Rect(2, centerY - rectHeight / 2, rectWidth, rectHeight);
                drawingContext.DrawRectangle(hoverBrush, null, rect);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var line = GetLineFromPoint(e.GetPosition(this));
        if (line != _hoveredLine)
        {
            _hoveredLine = line;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredLine = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var line = GetLineFromPoint(e.GetPosition(this));
        if (line > 0)
        {
            _bookmarkManager.ToggleBookmark(line);
            e.Handled = true;
        }
    }

    private int GetLineFromPoint(Point point)
    {
        var textView = TextView;
        if (textView == null) return -1;

        var pos = textView.GetPosition(new Point(0, point.Y + textView.VerticalOffset));
        return pos?.Line ?? -1;
    }

    protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
    {
        if (oldTextView != null)
        {
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
        }
        base.OnTextViewChanged(oldTextView, newTextView);
        if (newTextView != null)
        {
            newTextView.VisualLinesChanged += OnVisualLinesChanged;
        }
    }

    private void OnVisualLinesChanged(object? sender, EventArgs e)
    {
        InvalidateVisual();
    }
}
