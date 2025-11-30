using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor.Debugging;

/// <summary>
/// A margin that displays breakpoint indicators in the editor gutter.
/// </summary>
public class BreakpointMargin : AbstractMargin
{
    private readonly BreakpointManager _breakpointManager;
    private readonly TextEditor _editor;

    private static readonly Brush BreakpointBrush = new SolidColorBrush(Color.FromRgb(231, 76, 60)); // Red
    private static readonly Brush BreakpointHoverBrush = new SolidColorBrush(Color.FromRgb(192, 57, 43)); // Darker red
    private static readonly Brush ConditionalBreakpointBrush = new SolidColorBrush(Color.FromRgb(230, 126, 34)); // Orange

    private int _hoveredLine = -1;

    public BreakpointMargin(TextEditor editor, BreakpointManager breakpointManager)
    {
        _editor = editor;
        _breakpointManager = breakpointManager;
        _breakpointManager.BreakpointsChanged += (s, e) => InvalidateVisual();

        // Static brushes
        BreakpointBrush.Freeze();
        BreakpointHoverBrush.Freeze();
        ConditionalBreakpointBrush.Freeze();
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new PointHitTestResult(this, hitTestParameters.HitPoint);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(16, 0); // 16 pixels wide for breakpoint markers
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var textView = TextView;
        if (textView == null || !textView.VisualLinesValid) return;

        var renderSize = RenderSize;
        drawingContext.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(45, 45, 48)), // Dark gutter background
            null,
            new Rect(0, 0, renderSize.Width, renderSize.Height));

        foreach (var visualLine in textView.VisualLines)
        {
            var lineNumber = visualLine.FirstDocumentLine.LineNumber;
            var y = visualLine.VisualTop - textView.VerticalOffset;
            var lineHeight = visualLine.Height;

            // Draw breakpoint marker if set
            if (_breakpointManager.HasBreakpoint(lineNumber))
            {
                var condition = _breakpointManager.GetCondition(lineNumber);
                var brush = !string.IsNullOrEmpty(condition) ? ConditionalBreakpointBrush : BreakpointBrush;

                var centerX = renderSize.Width / 2;
                var centerY = y + lineHeight / 2;
                var radius = Math.Min(lineHeight, renderSize.Width) / 2 - 2;

                drawingContext.DrawEllipse(brush, null, new Point(centerX, centerY), radius, radius);
            }
            // Draw hover indicator
            else if (lineNumber == _hoveredLine)
            {
                var centerX = renderSize.Width / 2;
                var centerY = y + lineHeight / 2;
                var radius = Math.Min(lineHeight, renderSize.Width) / 2 - 2;

                var hoverBrush = new SolidColorBrush(Color.FromArgb(80, 231, 76, 60));
                drawingContext.DrawEllipse(hoverBrush, null, new Point(centerX, centerY), radius, radius);
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
            _breakpointManager.ToggleBreakpoint(line);
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
