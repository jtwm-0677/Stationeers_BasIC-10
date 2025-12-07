using System.Windows;
using System.Windows.Media;

namespace BasicToMips.UI.VisualScripting.Canvas;

/// <summary>
/// Renders a grid background on the visual scripting canvas.
/// Supports adjustable grid size, visibility toggle, and major/minor grid lines.
/// </summary>
public class CanvasGrid
{
    private bool _isVisible = true;
    private double _gridSize = 20.0;
    private int _majorLineInterval = 5;
    private Color _minorLineColor = Color.FromArgb(30, 255, 255, 255);
    private Color _majorLineColor = Color.FromArgb(60, 255, 255, 255);

    /// <summary>
    /// Gets or sets whether the grid is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the grid size in pixels (default: 20).
    /// </summary>
    public double GridSize
    {
        get => _gridSize;
        set
        {
            if (value > 0)
            {
                _gridSize = value;
                GridSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the interval for major grid lines (default: every 5 minor lines).
    /// </summary>
    public int MajorLineInterval
    {
        get => _majorLineInterval;
        set
        {
            if (value > 0)
            {
                _majorLineInterval = value;
                GridSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the color for minor grid lines.
    /// </summary>
    public Color MinorLineColor
    {
        get => _minorLineColor;
        set
        {
            _minorLineColor = value;
            GridSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets the color for major grid lines.
    /// </summary>
    public Color MajorLineColor
    {
        get => _majorLineColor;
        set
        {
            _majorLineColor = value;
            GridSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Event raised when grid visibility changes.
    /// </summary>
    public event EventHandler? VisibilityChanged;

    /// <summary>
    /// Event raised when grid size or appearance changes.
    /// </summary>
    public event EventHandler? GridSizeChanged;

    /// <summary>
    /// Renders the grid to the specified DrawingContext.
    /// </summary>
    /// <param name="drawingContext">The DrawingContext to render to.</param>
    /// <param name="viewportBounds">The visible area of the canvas.</param>
    /// <param name="transform">The current transform applied to the canvas.</param>
    public void Render(DrawingContext drawingContext, Rect viewportBounds, Transform transform)
    {
        if (!_isVisible)
            return;

        // Get inverse transform to convert viewport coordinates to canvas coordinates
        var inverseTransform = transform.Inverse;
        if (inverseTransform == null)
            return;

        // Transform viewport bounds to canvas space
        var canvasBounds = inverseTransform.TransformBounds(viewportBounds);

        // Calculate grid line positions
        var startX = Math.Floor(canvasBounds.Left / _gridSize) * _gridSize;
        var startY = Math.Floor(canvasBounds.Top / _gridSize) * _gridSize;
        var endX = Math.Ceiling(canvasBounds.Right / _gridSize) * _gridSize;
        var endY = Math.Ceiling(canvasBounds.Bottom / _gridSize) * _gridSize;

        // Create pens for drawing
        var minorPen = new Pen(new SolidColorBrush(_minorLineColor), 1.0 / GetScale(transform));
        var majorPen = new Pen(new SolidColorBrush(_majorLineColor), 1.5 / GetScale(transform));

        minorPen.Freeze();
        majorPen.Freeze();

        // Draw vertical lines
        for (var x = startX; x <= endX; x += _gridSize)
        {
            var isMajorLine = Math.Abs(x % (_gridSize * _majorLineInterval)) < 0.01;
            var pen = isMajorLine ? majorPen : minorPen;

            var start = transform.Transform(new Point(x, canvasBounds.Top));
            var end = transform.Transform(new Point(x, canvasBounds.Bottom));

            drawingContext.DrawLine(pen, start, end);
        }

        // Draw horizontal lines
        for (var y = startY; y <= endY; y += _gridSize)
        {
            var isMajorLine = Math.Abs(y % (_gridSize * _majorLineInterval)) < 0.01;
            var pen = isMajorLine ? majorPen : minorPen;

            var start = transform.Transform(new Point(canvasBounds.Left, y));
            var end = transform.Transform(new Point(canvasBounds.Right, y));

            drawingContext.DrawLine(pen, start, end);
        }
    }

    /// <summary>
    /// Snaps a point to the nearest grid intersection.
    /// </summary>
    /// <param name="point">The point to snap.</param>
    /// <returns>The snapped point.</returns>
    public Point SnapToGrid(Point point)
    {
        return new Point(
            Math.Round(point.X / _gridSize) * _gridSize,
            Math.Round(point.Y / _gridSize) * _gridSize
        );
    }

    /// <summary>
    /// Gets the scale factor from a transform.
    /// </summary>
    private static double GetScale(Transform transform)
    {
        var matrix = transform.Value;
        return Math.Sqrt(matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12);
    }
}
