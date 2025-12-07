using System;
using System.Windows;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting.Wires
{
    /// <summary>
    /// Renders wires as bezier curves with anti-aliasing and enhanced visual effects
    /// </summary>
    public class WireRenderer
    {
        // Wire thickness based on data type
        private const double ExecutionWireThickness = 3.0;
        private const double DataWireThickness = 2.0;
        private const double HoverThicknessIncrease = 1.0;
        private const double SelectedThicknessIncrease = 1.0;

        /// <summary>
        /// Render a wire to a drawing context
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <param name="wire">Wire to render</param>
        /// <param name="validDropTarget">Whether this wire's target is a valid drop target</param>
        public static void RenderWire(DrawingContext context, Wire wire, bool validDropTarget = false)
        {
            if (wire.SourceNode == null || wire.TargetNode == null)
                return;

            var (startX, startY, endX, endY) = wire.GetPoints();
            var (cp1X, cp1Y, cp2X, cp2Y) = wire.GetControlPoints();

            // Get color for wire based on data type
            Color wireColor = PinColors.GetColor(wire.DataType);

            // Base thickness depends on data type
            double baseThickness = wire.DataType == DataType.Execution ? ExecutionWireThickness : DataWireThickness;
            double thickness = baseThickness;
            double opacity = 1.0;

            // Adjust for state
            if (wire.IsSelected)
            {
                thickness += SelectedThicknessIncrease;
                // Brighten color for selected wires
                wireColor = Color.FromRgb(
                    (byte)Math.Min(255, wireColor.R + 40),
                    (byte)Math.Min(255, wireColor.G + 40),
                    (byte)Math.Min(255, wireColor.B + 40)
                );
            }
            else if (wire.IsHovered)
            {
                thickness += HoverThicknessIncrease;
            }

            // Create bezier curve geometry
            var pathGeometry = CreateBezierPath(startX, startY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);

            // Draw glow for valid drop targets
            if (validDropTarget)
            {
                var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(120, 0x44, 0xFF, 0x44)), thickness + 6)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                context.DrawGeometry(null, glowPen, pathGeometry);
            }

            // Draw shadow for selected wires
            if (wire.IsSelected)
            {
                var shadowPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), thickness + 4)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                context.DrawGeometry(null, shadowPen, pathGeometry);
            }

            // Create pen with anti-aliasing
            var pen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), wireColor.R, wireColor.G, wireColor.B)), thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };

            // Draw the main wire
            context.DrawGeometry(null, pen, pathGeometry);

            // Draw arrow indicator at target
            DrawArrowIndicator(context, endX, endY, cp2X, cp2Y, wireColor, thickness);
        }

        /// <summary>
        /// Render a temporary wire being dragged (dashed line until connected)
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <param name="startX">Start X coordinate</param>
        /// <param name="startY">Start Y coordinate</param>
        /// <param name="endX">End X coordinate</param>
        /// <param name="endY">End Y coordinate</param>
        /// <param name="dataType">Data type for color</param>
        /// <param name="isValid">Whether the current target is valid</param>
        public static void RenderTemporaryWire(DrawingContext context, double startX, double startY, double endX, double endY, DataType dataType, bool isValid = true)
        {
            // Calculate control points
            double dx = endX - startX;
            double distance = Math.Abs(dx);
            double offset = Math.Max(50, Math.Min(distance * 0.5, 150));

            double cp1X = startX + offset;
            double cp1Y = startY;
            double cp2X = endX - offset;
            double cp2Y = endY;

            // Get color for wire based on data type
            Color wireColor = PinColors.GetColor(dataType);

            // Choose color based on validity
            if (!isValid)
            {
                wireColor = Color.FromRgb(0xFF, 0x44, 0x44); // Red for invalid
            }
            else if (isValid)
            {
                // Add subtle green tint for valid connections
                wireColor = Color.FromRgb(
                    (byte)Math.Min(255, wireColor.R + 20),
                    (byte)Math.Min(255, wireColor.G + 40),
                    (byte)Math.Min(255, wireColor.B + 20)
                );
            }

            // Base thickness depends on data type
            double thickness = dataType == DataType.Execution ? ExecutionWireThickness : DataWireThickness;

            // Create dashed pen for temporary wire
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, wireColor.R, wireColor.G, wireColor.B)), thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                DashStyle = new DashStyle(new double[] { 6, 3 }, 0)
            };

            // Create bezier curve geometry
            var pathGeometry = CreateBezierPath(startX, startY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);

            // Draw the temporary wire
            context.DrawGeometry(null, pen, pathGeometry);

            // Draw endpoint indicator
            var endpointBrush = new SolidColorBrush(wireColor);
            context.DrawEllipse(endpointBrush, null, new Point(endX, endY), 4, 4);
        }

        /// <summary>
        /// Get a StreamGeometry for a wire (useful for hit testing)
        /// </summary>
        /// <param name="wire">Wire to create geometry for</param>
        /// <returns>StreamGeometry representing the wire path</returns>
        public static StreamGeometry GetWireGeometry(Wire wire)
        {
            var (startX, startY, endX, endY) = wire.GetPoints();
            var (cp1X, cp1Y, cp2X, cp2Y) = wire.GetControlPoints();

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(new Point(startX, startY), false, false);
                context.BezierTo(
                    new Point(cp1X, cp1Y),
                    new Point(cp2X, cp2Y),
                    new Point(endX, endY),
                    true,
                    false
                );
            }

            geometry.Freeze();
            return geometry;
        }

        /// <summary>
        /// Create a pen for wire rendering
        /// </summary>
        /// <param name="dataType">Data type for color</param>
        /// <param name="thickness">Stroke thickness (optional, uses type-based default)</param>
        /// <param name="opacity">Opacity (0-1)</param>
        /// <returns>Configured pen</returns>
        public static Pen CreateWirePen(DataType dataType, double? thickness = null, double opacity = 1.0)
        {
            double actualThickness = thickness ?? (dataType == DataType.Execution ? ExecutionWireThickness : DataWireThickness);
            Color wireColor = PinColors.GetColor(dataType);
            return new Pen(new SolidColorBrush(Color.FromArgb((byte)(opacity * 255), wireColor.R, wireColor.G, wireColor.B)), actualThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
        }

        /// <summary>
        /// Create a bezier path geometry
        /// </summary>
        private static PathGeometry CreateBezierPath(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            var figure = new PathFigure
            {
                StartPoint = new Point(x0, y0),
                IsClosed = false
            };

            figure.Segments.Add(new BezierSegment(
                new Point(x1, y1),
                new Point(x2, y2),
                new Point(x3, y3),
                true
            ));

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(figure);
            return pathGeometry;
        }

        /// <summary>
        /// Draw an arrow indicator at the wire target
        /// </summary>
        private static void DrawArrowIndicator(DrawingContext context, double endX, double endY, double cp2X, double cp2Y, Color wireColor, double wireThickness)
        {
            // Calculate direction vector from second control point to end
            double dx = endX - cp2X;
            double dy = endY - cp2Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length < 0.01)
                return;

            // Normalize direction
            dx /= length;
            dy /= length;

            // Arrow size based on wire thickness
            double arrowLength = 6 + wireThickness;
            double arrowWidth = 4 + wireThickness * 0.5;

            // Calculate arrow points (small triangle at the end)
            double baseX = endX - dx * arrowLength * 0.3;
            double baseY = endY - dy * arrowLength * 0.3;

            // Perpendicular vector
            double perpX = -dy;
            double perpY = dx;

            var arrowPoints = new[]
            {
                new Point(endX, endY),
                new Point(baseX + perpX * arrowWidth, baseY + perpY * arrowWidth),
                new Point(baseX - perpX * arrowWidth, baseY - perpY * arrowWidth)
            };

            var arrowGeometry = new PathGeometry();
            var arrowFigure = new PathFigure { StartPoint = arrowPoints[0], IsClosed = true };
            arrowFigure.Segments.Add(new LineSegment(arrowPoints[1], true));
            arrowFigure.Segments.Add(new LineSegment(arrowPoints[2], true));
            arrowGeometry.Figures.Add(arrowFigure);

            var arrowBrush = new SolidColorBrush(wireColor);
            context.DrawGeometry(arrowBrush, null, arrowGeometry);
        }
    }
}
