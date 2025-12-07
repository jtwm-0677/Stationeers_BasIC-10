using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting.Canvas;

/// <summary>
/// Visual representation of a wire connection with selection, hit testing, and animation support.
/// </summary>
public class WireVisual
{
    // Brushes for different states
    private static readonly Brush NormalBrush = new SolidColorBrush(Colors.Gray);
    private static readonly Brush HoverBrush = new SolidColorBrush(Colors.LightBlue);
    private static readonly Brush SelectedBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));

    static WireVisual()
    {
        NormalBrush.Freeze();
        HoverBrush.Freeze();
        SelectedBrush.Freeze();
    }

    public NodePin SourcePin { get; set; } = null!;
    public NodeBase SourceNode { get; set; } = null!;
    public NodePin TargetPin { get; set; } = null!;
    public NodeBase TargetNode { get; set; } = null!;
    public System.Windows.Shapes.Path Path { get; set; } = null!;

    private bool _isSelected;
    private bool _isHovered;
    private bool _isAnimating;
    private Color _baseColor;
    private double _animationOffset;
    private Storyboard? _pulseStoryboard;

    /// <summary>
    /// Gets or sets whether this wire is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this wire is currently hovered.
    /// </summary>
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered != value)
            {
                _isHovered = value;
                UpdateVisualState();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this wire is currently animating (showing data flow).
    /// </summary>
    public bool IsAnimating
    {
        get => _isAnimating;
        set
        {
            if (_isAnimating != value)
            {
                _isAnimating = value;
                if (_isAnimating)
                    StartAnimation();
                else
                    StopAnimation();
            }
        }
    }

    /// <summary>
    /// Gets the current animation offset (0 to 1) for dash pattern animation.
    /// </summary>
    public double AnimationOffset
    {
        get => _animationOffset;
        set
        {
            _animationOffset = value;
            UpdateAnimationVisual();
        }
    }

    /// <summary>
    /// Starts the data flow animation on this wire.
    /// </summary>
    public void StartAnimation()
    {
        if (Path == null) return;

        _isAnimating = true;

        // Create animated dash pattern for data flow effect
        Path.StrokeDashArray = new DoubleCollection { 4, 2 };
        Path.StrokeDashCap = PenLineCap.Round;

        // Create storyboard for continuous animation
        _pulseStoryboard = new Storyboard();

        var animation = new DoubleAnimation
        {
            From = 0,
            To = 6, // Total dash pattern length (4 + 2)
            Duration = TimeSpan.FromMilliseconds(500),
            RepeatBehavior = RepeatBehavior.Forever
        };

        Storyboard.SetTarget(animation, Path);
        Storyboard.SetTargetProperty(animation, new PropertyPath(System.Windows.Shapes.Shape.StrokeDashOffsetProperty));

        _pulseStoryboard.Children.Add(animation);
        _pulseStoryboard.Begin();

        // Brighten the wire color during animation
        var currentColor = _baseColor != default ? _baseColor : Colors.Gray;
        var brightColor = Color.FromArgb(255,
            (byte)Math.Min(255, currentColor.R + 60),
            (byte)Math.Min(255, currentColor.G + 60),
            (byte)Math.Min(255, currentColor.B + 60));
        Path.Stroke = new SolidColorBrush(brightColor);
        Path.StrokeThickness = 3.0;
    }

    /// <summary>
    /// Stops the data flow animation on this wire.
    /// </summary>
    public void StopAnimation()
    {
        _isAnimating = false;

        _pulseStoryboard?.Stop();
        _pulseStoryboard = null;

        if (Path != null)
        {
            Path.StrokeDashArray = null;
            Path.StrokeDashOffset = 0;
            UpdateVisualState();
        }
    }

    /// <summary>
    /// Triggers a single pulse animation (for one-time data transfer visualization).
    /// </summary>
    public void Pulse(double durationMs = 300)
    {
        if (Path == null) return;

        var originalBrush = GetStrokeBrush();
        var originalThickness = GetStrokeThickness();

        // Create pulse color
        var pulseColor = _baseColor != default ? _baseColor : Colors.Cyan;
        var brightPulse = Color.FromArgb(255,
            (byte)Math.Min(255, pulseColor.R + 80),
            (byte)Math.Min(255, pulseColor.G + 80),
            (byte)Math.Min(255, pulseColor.B + 80));

        // Animate to bright, then back to normal
        var colorAnimation = new ColorAnimation
        {
            From = brightPulse,
            To = _baseColor != default ? _baseColor : Colors.Gray,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        var thicknessAnimation = new DoubleAnimation
        {
            From = 4.0,
            To = originalThickness,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        var brush = new SolidColorBrush(brightPulse);
        Path.Stroke = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        Path.BeginAnimation(System.Windows.Shapes.Shape.StrokeThicknessProperty, thicknessAnimation);
    }

    /// <summary>
    /// Updates the visual during animation.
    /// </summary>
    private void UpdateAnimationVisual()
    {
        if (Path != null && _isAnimating)
        {
            Path.StrokeDashOffset = _animationOffset;
        }
    }

    /// <summary>
    /// Gets the stroke brush based on the current state.
    /// </summary>
    public Brush GetStrokeBrush()
    {
        if (_isSelected)
            return SelectedBrush;
        if (_isHovered)
            return HoverBrush;

        // Use the base color if available (from pin data type)
        if (_baseColor != default)
            return new SolidColorBrush(_baseColor);

        return NormalBrush;
    }

    /// <summary>
    /// Gets the stroke thickness based on the current state.
    /// </summary>
    public double GetStrokeThickness()
    {
        return _isSelected ? 3.0 : 2.0;
    }

    /// <summary>
    /// Sets the base color for this wire (typically from pin data type).
    /// </summary>
    public void SetBaseColor(Color color)
    {
        _baseColor = color;
        UpdateVisualState();
    }

    /// <summary>
    /// Updates the visual appearance based on current state.
    /// </summary>
    public void UpdateVisualState()
    {
        if (Path != null)
        {
            Path.Stroke = GetStrokeBrush();
            Path.StrokeThickness = GetStrokeThickness();
        }
    }

    /// <summary>
    /// Calculates the minimum distance from a point to this wire's curve.
    /// </summary>
    /// <param name="point">The point to test against.</param>
    /// <returns>The minimum distance in pixels.</returns>
    public double GetDistanceToPoint(Point point)
    {
        if (Path?.Data is not PathGeometry pathGeometry)
            return double.MaxValue;

        // Sample the bezier curve at multiple points
        const int sampleCount = 20;
        double minDistance = double.MaxValue;

        for (int i = 0; i <= sampleCount; i++)
        {
            double t = i / (double)sampleCount;
            Point samplePoint = GetPointOnCurve(pathGeometry, t);

            double distance = Math.Sqrt(
                Math.Pow(point.X - samplePoint.X, 2) +
                Math.Pow(point.Y - samplePoint.Y, 2));

            minDistance = Math.Min(minDistance, distance);
        }

        return minDistance;
    }

    /// <summary>
    /// Gets a point on the bezier curve at parameter t (0 to 1).
    /// </summary>
    private Point GetPointOnCurve(PathGeometry geometry, double t)
    {
        if (geometry.Figures.Count == 0)
            return new Point(0, 0);

        var figure = geometry.Figures[0];
        if (figure.Segments.Count == 0)
            return figure.StartPoint;

        if (figure.Segments[0] is BezierSegment bezier)
        {
            // Cubic bezier curve calculation: B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
            var p0 = figure.StartPoint;
            var p1 = bezier.Point1;
            var p2 = bezier.Point2;
            var p3 = bezier.Point3;

            double u = 1 - t;
            double u2 = u * u;
            double u3 = u2 * u;
            double t2 = t * t;
            double t3 = t2 * t;

            double x = u3 * p0.X + 3 * u2 * t * p1.X + 3 * u * t2 * p2.X + t3 * p3.X;
            double y = u3 * p0.Y + 3 * u2 * t * p1.Y + 3 * u * t2 * p2.Y + t3 * p3.Y;

            return new Point(x, y);
        }

        return figure.StartPoint;
    }
}
