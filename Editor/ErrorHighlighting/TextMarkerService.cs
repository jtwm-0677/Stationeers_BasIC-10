using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor.ErrorHighlighting;

public class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer
{
    private readonly TextDocument _document;
    private readonly List<TextMarker> _markers = new();

    public TextMarkerService(TextDocument document)
    {
        _document = document;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public IEnumerable<TextMarker> TextMarkers => _markers;

    public void Add(TextMarker marker)
    {
        _markers.Add(marker);
    }

    public void Remove(TextMarker marker)
    {
        _markers.Remove(marker);
    }

    public void Clear()
    {
        _markers.Clear();
    }

    public TextMarker Create(int startOffset, int length)
    {
        if (startOffset < 0 || startOffset > _document.TextLength)
            return null!;

        int endOffset = Math.Min(startOffset + length, _document.TextLength);
        var marker = new TextMarker(startOffset, endOffset - startOffset);
        _markers.Add(marker);
        return marker;
    }

    public IEnumerable<TextMarker> GetMarkersAtOffset(int offset)
    {
        return _markers.Where(m => m.StartOffset <= offset && offset <= m.EndOffset);
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_markers.Count == 0) return;

        int lineStart = line.Offset;
        int lineEnd = line.EndOffset;

        foreach (var marker in _markers)
        {
            if (marker.EndOffset < lineStart) continue;
            if (marker.StartOffset > lineEnd) continue;

            int startCol = Math.Max(marker.StartOffset, lineStart);
            int endCol = Math.Min(marker.EndOffset, lineEnd);

            if (startCol < endCol)
            {
                ChangeLinePart(startCol, endCol, element =>
                {
                    if (marker.ForegroundColor.HasValue)
                    {
                        element.TextRunProperties.SetForegroundBrush(
                            new SolidColorBrush(marker.ForegroundColor.Value));
                    }
                });
            }
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid) return;

        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0) return;

        int viewStart = visualLines.First().FirstDocumentLine.Offset;
        int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

        foreach (var marker in _markers)
        {
            if (marker.EndOffset < viewStart) continue;
            if (marker.StartOffset > viewEnd) continue;

            int startOffset = Math.Max(marker.StartOffset, viewStart);
            int endOffset = Math.Min(marker.EndOffset, viewEnd);

            var geometry = BackgroundGeometryBuilder.GetRectsForSegment(
                textView,
                new ICSharpCode.AvalonEdit.Document.TextSegment { StartOffset = startOffset, Length = endOffset - startOffset });

            foreach (var rect in geometry)
            {
                if (marker.MarkerType == TextMarkerType.SquigglyUnderline)
                {
                    DrawSquigglyLine(drawingContext, rect, marker.MarkerColor);
                }
                else if (marker.MarkerType == TextMarkerType.Background && marker.BackgroundColor.HasValue)
                {
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(marker.BackgroundColor.Value),
                        null, rect);
                }
            }
        }
    }

    private void DrawSquigglyLine(DrawingContext drawingContext, Rect rect, Color color)
    {
        var pen = new Pen(new SolidColorBrush(color), 1);
        pen.Freeze();

        double offset = 2.0;
        double y = rect.Bottom - 1;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(rect.Left, y), false, false);

            double x = rect.Left;
            bool up = true;
            while (x < rect.Right)
            {
                x = Math.Min(x + offset, rect.Right);
                double yOffset = up ? -offset : 0;
                ctx.LineTo(new Point(x, y + yOffset), true, false);
                up = !up;
            }
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);
    }
}

public class TextMarker
{
    public int StartOffset { get; }
    public int Length { get; }
    public int EndOffset => StartOffset + Length;

    public TextMarkerType MarkerType { get; set; } = TextMarkerType.SquigglyUnderline;
    public Color MarkerColor { get; set; } = Colors.Red;
    public Color? BackgroundColor { get; set; }
    public Color? ForegroundColor { get; set; }
    public string? ToolTip { get; set; }

    public TextMarker(int startOffset, int length)
    {
        StartOffset = startOffset;
        Length = length;
    }
}


public enum TextMarkerType
{
    None,
    SquigglyUnderline,
    Background
}
