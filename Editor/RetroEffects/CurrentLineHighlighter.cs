using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor.RetroEffects;

/// <summary>
/// Highlights the current line being edited with a subtle background color
/// </summary>
public class CurrentLineHighlighter : IBackgroundRenderer
{
    private readonly TextArea _textArea;
    private Brush _highlightBrush = null!;
    private bool _isEnabled = true;

    public KnownLayer Layer => KnownLayer.Background;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            _textArea.TextView.InvalidateLayer(KnownLayer.Background);
        }
    }

    public CurrentLineHighlighter(TextArea textArea, Color highlightColor)
    {
        _textArea = textArea;
        SetHighlightColor(highlightColor);

        // Redraw when caret moves
        _textArea.Caret.PositionChanged += (s, e) =>
            _textArea.TextView.InvalidateLayer(KnownLayer.Background);
    }

    public void SetHighlightColor(Color color)
    {
        _highlightBrush = new SolidColorBrush(color);
        _highlightBrush.Freeze();
        _textArea.TextView.InvalidateLayer(KnownLayer.Background);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!_isEnabled || !_textArea.IsFocused)
            return;

        var currentLine = _textArea.Caret.Line;
        var visualLine = textView.GetVisualLine(currentLine);
        if (visualLine == null)
            return;

        // Get the visual position
        var visualTop = visualLine.VisualTop - textView.ScrollOffset.Y;

        // Draw highlight across the full width
        var rect = new Rect(0, visualTop, textView.ActualWidth, visualLine.Height);
        drawingContext.DrawRectangle(_highlightBrush, null, rect);
    }
}

/// <summary>
/// Manages the current line highlighter for an AvalonEdit TextArea
/// </summary>
public static class CurrentLineHighlighterManager
{
    private static readonly Dictionary<TextArea, CurrentLineHighlighter> _highlighters = new();

    public static void EnableHighlighter(TextArea textArea, Color highlightColor)
    {
        if (_highlighters.ContainsKey(textArea))
        {
            _highlighters[textArea].IsEnabled = true;
            return;
        }

        var highlighter = new CurrentLineHighlighter(textArea, highlightColor);
        textArea.TextView.BackgroundRenderers.Insert(0, highlighter); // Insert at 0 so it's behind text
        _highlighters[textArea] = highlighter;
    }

    public static void DisableHighlighter(TextArea textArea)
    {
        if (_highlighters.TryGetValue(textArea, out var highlighter))
        {
            highlighter.IsEnabled = false;
        }
    }

    public static void SetEnabled(TextArea textArea, bool enabled)
    {
        if (enabled)
        {
            // If not already enabled, enable with default color
            if (!_highlighters.ContainsKey(textArea))
            {
                EnableHighlighter(textArea, Color.FromArgb(30, 255, 255, 255));
            }
            else
            {
                _highlighters[textArea].IsEnabled = true;
            }
        }
        else
        {
            DisableHighlighter(textArea);
        }
    }
}
