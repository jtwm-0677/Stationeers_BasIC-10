using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor.RetroEffects;

/// <summary>
/// Highlights multiple lines in the IC10/MIPS output panel to show which lines
/// correspond to the current BASIC source line. This helps users understand
/// what their BASIC code compiles to.
/// </summary>
public class MipsLineHighlighter : IBackgroundRenderer
{
    private readonly TextArea _textArea;
    private Brush _highlightBrush = null!;
    private bool _isEnabled = true;
    private HashSet<int> _highlightedLines = new(); // 1-based line numbers

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

    public MipsLineHighlighter(TextArea textArea, Color highlightColor)
    {
        _textArea = textArea;
        SetHighlightColor(highlightColor);
    }

    public void SetHighlightColor(Color color)
    {
        _highlightBrush = new SolidColorBrush(color);
        _highlightBrush.Freeze();
        _textArea.TextView.InvalidateLayer(KnownLayer.Background);
    }

    /// <summary>
    /// Set which lines should be highlighted (0-based IC10 line numbers).
    /// </summary>
    public void SetHighlightedLines(IEnumerable<int> ic10Lines)
    {
        _highlightedLines.Clear();
        foreach (var line in ic10Lines)
        {
            _highlightedLines.Add(line + 1); // Convert to 1-based for AvalonEdit
        }
        _textArea.TextView.InvalidateLayer(KnownLayer.Background);
    }

    /// <summary>
    /// Clear all highlighted lines.
    /// </summary>
    public void ClearHighlights()
    {
        _highlightedLines.Clear();
        _textArea.TextView.InvalidateLayer(KnownLayer.Background);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!_isEnabled || _highlightedLines.Count == 0)
            return;

        foreach (var lineNumber in _highlightedLines)
        {
            var visualLine = textView.GetVisualLine(lineNumber);
            if (visualLine == null)
                continue;

            // Get the visual position
            var visualTop = visualLine.VisualTop - textView.ScrollOffset.Y;

            // Draw highlight across the full width
            var rect = new Rect(0, visualTop, textView.ActualWidth, visualLine.Height);
            drawingContext.DrawRectangle(_highlightBrush, null, rect);
        }
    }
}
