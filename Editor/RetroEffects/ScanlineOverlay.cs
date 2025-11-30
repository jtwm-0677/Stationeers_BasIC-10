using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.Editor.RetroEffects;

/// <summary>
/// Renders faint CRT-style scanlines over the editor
/// </summary>
public class ScanlineOverlay : IBackgroundRenderer
{
    private readonly TextArea _textArea;
    private readonly Pen _scanlinePen;
    private bool _isEnabled = false;
    private const int ScanlineSpacing = 2; // Every 2 pixels

    public KnownLayer Layer => KnownLayer.Selection; // Render above selection

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            _textArea.TextView.InvalidateVisual();
        }
    }

    public ScanlineOverlay(TextArea textArea, byte opacity = 80)
    {
        _textArea = textArea;
        var brush = new SolidColorBrush(Color.FromArgb(opacity, 0, 0, 0));
        brush.Freeze();
        _scanlinePen = new Pen(brush, 1);
        _scanlinePen.Freeze();
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!_isEnabled)
            return;

        var width = textView.ActualWidth;
        var height = textView.ActualHeight;

        // Draw horizontal scanlines
        for (double y = 0; y < height; y += ScanlineSpacing)
        {
            drawingContext.DrawLine(_scanlinePen, new Point(0, y), new Point(width, y));
        }
    }
}

/// <summary>
/// Manages the scanline overlay for an AvalonEdit TextArea
/// </summary>
public static class ScanlineOverlayManager
{
    private static readonly Dictionary<TextArea, ScanlineOverlay> _overlays = new();

    public static void EnableOverlay(TextArea textArea, byte opacity = 15)
    {
        if (_overlays.ContainsKey(textArea))
        {
            _overlays[textArea].IsEnabled = true;
            return;
        }

        var overlay = new ScanlineOverlay(textArea, opacity);
        overlay.IsEnabled = true;
        textArea.TextView.BackgroundRenderers.Add(overlay);
        _overlays[textArea] = overlay;
    }

    public static void DisableOverlay(TextArea textArea)
    {
        if (_overlays.TryGetValue(textArea, out var overlay))
        {
            overlay.IsEnabled = false;
        }
    }

    public static void SetEnabled(TextArea textArea, bool enabled, byte opacity = 15)
    {
        if (enabled)
        {
            EnableOverlay(textArea, opacity);
        }
        else
        {
            DisableOverlay(textArea);
        }
    }
}
