using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor;

/// <summary>
/// Custom caret layer that renders a classic BASIC-style block cursor
/// with inverted character display
/// </summary>
public class BlockCaretLayer : IBackgroundRenderer
{
    private readonly TextArea _textArea;
    private readonly Brush _caretBrush;
    private readonly Brush _textBrush;
    private readonly Color _caretColor;
    private bool _isVisible = true;
    private readonly DispatcherTimer _blinkTimer;

    public KnownLayer Layer => KnownLayer.Caret;

    public BlockCaretLayer(TextArea textArea, Color caretColor, Color textColor)
    {
        _textArea = textArea;
        _caretColor = caretColor;
        _caretBrush = new SolidColorBrush(caretColor);
        _caretBrush.Freeze();
        _textBrush = new SolidColorBrush(textColor);
        _textBrush.Freeze();

        // Setup blink timer (classic 530ms interval)
        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _blinkTimer.Tick += (s, e) =>
        {
            _isVisible = !_isVisible;
            _textArea.TextView.InvalidateLayer(KnownLayer.Caret);
        };
        _blinkTimer.Start();

        // Reset blink on caret move (cursor should be visible after moving)
        _textArea.Caret.PositionChanged += (s, e) =>
        {
            _isVisible = true;
            _blinkTimer.Stop();
            _blinkTimer.Start();
            _textArea.TextView.InvalidateLayer(KnownLayer.Caret);
        };
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!_isVisible || !_textArea.IsFocused)
            return;

        var caretPosition = _textArea.Caret.Position;
        var visualLine = textView.GetVisualLine(caretPosition.Line);
        if (visualLine == null)
            return;

        // Get character width (use wide space width for monospace)
        double charWidth = textView.WideSpaceWidth;
        if (charWidth <= 0)
            charWidth = 8; // fallback

        // Get the visual position of the caret
        var visualPos = visualLine.GetVisualPosition(caretPosition.VisualColumn, VisualYPosition.LineTop);
        var point = new Point(visualPos.X - textView.ScrollOffset.X,
                              visualPos.Y - textView.ScrollOffset.Y);

        // Draw block cursor background
        double height = visualLine.Height;
        var rect = new Rect(point.X, point.Y, charWidth, height);
        drawingContext.DrawRectangle(_caretBrush, null, rect);

        // Get the character under the cursor and draw it in contrasting color
        var doc = _textArea.Document;
        if (doc != null)
        {
            int offset = _textArea.Caret.Offset;
            if (offset < doc.TextLength)
            {
                char ch = doc.GetCharAt(offset);
                if (ch != '\n' && ch != '\r' && ch != '\t')
                {
                    // Draw the character in contrasting color
                    var typeface = new Typeface(
                        _textArea.FontFamily,
                        _textArea.FontStyle,
                        _textArea.FontWeight,
                        _textArea.FontStretch);

                    var formattedText = new FormattedText(
                        ch.ToString(),
                        CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        typeface,
                        _textArea.FontSize,
                        _textBrush,
                        VisualTreeHelper.GetDpi(textView).PixelsPerDip);

                    // Center the character in the block
                    double textX = point.X + (charWidth - formattedText.Width) / 2;
                    double textY = point.Y + (height - formattedText.Height) / 2;

                    drawingContext.DrawText(formattedText, new Point(textX, textY));
                }
            }
        }
    }
}

/// <summary>
/// Manages the block caret for an AvalonEdit TextArea
/// </summary>
public static class BlockCaretManager
{
    public static void EnableBlockCaret(TextArea textArea, Color caretColor, Color? textColor = null)
    {
        // Default to black text for contrast if not specified
        var actualTextColor = textColor ?? Colors.Black;

        // Hide the default caret by making it transparent
        textArea.Caret.CaretBrush = Brushes.Transparent;

        // Add our custom block caret layer
        var blockCaret = new BlockCaretLayer(textArea, caretColor, actualTextColor);
        textArea.TextView.BackgroundRenderers.Add(blockCaret);
    }
}
