using System;
using System.Windows;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting.Wires
{
    /// <summary>
    /// Represents a connection wire between two node pins
    /// </summary>
    public class Wire
    {
        #region Properties

        /// <summary>
        /// Unique identifier for this wire
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Source node ID
        /// </summary>
        public Guid SourceNodeId { get; set; }

        /// <summary>
        /// Source pin ID
        /// </summary>
        public Guid SourcePinId { get; set; }

        /// <summary>
        /// Target node ID
        /// </summary>
        public Guid TargetNodeId { get; set; }

        /// <summary>
        /// Target pin ID
        /// </summary>
        public Guid TargetPinId { get; set; }

        /// <summary>
        /// Reference to source node (not serialized)
        /// </summary>
        public NodeBase? SourceNode { get; set; }

        /// <summary>
        /// Reference to source pin (not serialized)
        /// </summary>
        public NodePin? SourcePin { get; set; }

        /// <summary>
        /// Reference to target node (not serialized)
        /// </summary>
        public NodeBase? TargetNode { get; set; }

        /// <summary>
        /// Reference to target pin (not serialized)
        /// </summary>
        public NodePin? TargetPin { get; set; }

        /// <summary>
        /// Data type of this wire (inherited from connected pins)
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Whether this wire is currently selected
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Whether this wire is currently being hovered
        /// </summary>
        public bool IsHovered { get; set; }

        #endregion

        #region Constructor

        public Wire()
        {
            Id = Guid.NewGuid();
        }

        public Wire(NodePin sourcePin, NodePin targetPin)
        {
            Id = Guid.NewGuid();

            SourcePin = sourcePin;
            SourceNode = sourcePin.ParentNode;
            SourcePinId = sourcePin.Id;
            SourceNodeId = sourcePin.ParentNode?.Id ?? Guid.Empty;

            TargetPin = targetPin;
            TargetNode = targetPin.ParentNode;
            TargetPinId = targetPin.Id;
            TargetNodeId = targetPin.ParentNode?.Id ?? Guid.Empty;

            DataType = sourcePin.DataType;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get the start and end points for rendering this wire
        /// </summary>
        /// <returns>Tuple of (startX, startY, endX, endY)</returns>
        public (double startX, double startY, double endX, double endY) GetPoints()
        {
            if (SourceNode == null || SourcePin == null || TargetNode == null || TargetPin == null)
                return (0, 0, 0, 0);

            // Get source pin position
            int sourceIndex = SourceNode.OutputPins.IndexOf(SourcePin);
            var (sourcePinX, sourcePinY) = SourcePin.GetPosition(sourceIndex);
            double startX = SourceNode.X + sourcePinX;
            double startY = SourceNode.Y + sourcePinY;

            // Get target pin position
            int targetIndex = TargetNode.InputPins.IndexOf(TargetPin);
            var (targetPinX, targetPinY) = TargetPin.GetPosition(targetIndex);
            double endX = TargetNode.X + targetPinX;
            double endY = TargetNode.Y + targetPinY;

            return (startX, startY, endX, endY);
        }

        /// <summary>
        /// Get bezier control points for smooth curve rendering
        /// </summary>
        /// <returns>Tuple of (cp1X, cp1Y, cp2X, cp2Y)</returns>
        public (double cp1X, double cp1Y, double cp2X, double cp2Y) GetControlPoints()
        {
            var (startX, startY, endX, endY) = GetPoints();

            // Calculate horizontal offset based on distance
            double dx = endX - startX;
            double distance = Math.Abs(dx);

            // Offset is at least 50px, up to half the horizontal distance
            double offset = Math.Max(50, Math.Min(distance * 0.5, 150));

            // Control points extend horizontally from start/end
            double cp1X = startX + offset;
            double cp1Y = startY;
            double cp2X = endX - offset;
            double cp2Y = endY;

            return (cp1X, cp1Y, cp2X, cp2Y);
        }

        /// <summary>
        /// Validate this wire connection
        /// </summary>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate(out string errorMessage)
        {
            // Check that all references are set
            if (SourceNode == null || SourcePin == null)
            {
                errorMessage = "Source node or pin is null";
                return false;
            }

            if (TargetNode == null || TargetPin == null)
            {
                errorMessage = "Target node or pin is null";
                return false;
            }

            // Check pin directions
            if (SourcePin.PinType != PinType.Output)
            {
                errorMessage = "Source pin must be an output pin";
                return false;
            }

            if (TargetPin.PinType != PinType.Input)
            {
                errorMessage = "Target pin must be an input pin";
                return false;
            }

            // Check type compatibility
            if (!IsTypeCompatible(SourcePin.DataType, TargetPin.DataType))
            {
                errorMessage = $"Incompatible types: {SourcePin.DataType} cannot connect to {TargetPin.DataType}";
                return false;
            }

            // Check not connecting to self
            if (SourceNode.Id == TargetNode.Id)
            {
                errorMessage = "Cannot connect a node to itself";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Test if a point is near this wire for hit detection
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="tolerance">Hit tolerance in pixels</param>
        /// <returns>True if the point is near the wire</returns>
        public bool HitTest(double x, double y, double tolerance = 5.0)
        {
            var (startX, startY, endX, endY) = GetPoints();
            var (cp1X, cp1Y, cp2X, cp2Y) = GetControlPoints();

            // Sample points along the bezier curve
            const int sampleCount = 20;
            double minDistance = double.MaxValue;

            for (int i = 0; i <= sampleCount; i++)
            {
                double t = i / (double)sampleCount;
                var (px, py) = GetBezierPoint(t, startX, startY, cp1X, cp1Y, cp2X, cp2Y, endX, endY);

                double dx = x - px;
                double dy = y - py;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                minDistance = Math.Min(minDistance, distance);
            }

            return minDistance <= tolerance;
        }

        /// <summary>
        /// Get a point along the bezier curve
        /// </summary>
        /// <param name="t">Parameter from 0 to 1</param>
        private (double x, double y) GetBezierPoint(double t, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            double x = uuu * x0 + 3 * uu * t * x1 + 3 * u * tt * x2 + ttt * x3;
            double y = uuu * y0 + 3 * uu * t * y1 + 3 * u * tt * y2 + ttt * y3;

            return (x, y);
        }

        /// <summary>
        /// Check if two data types are compatible for connection
        /// </summary>
        public static bool IsTypeCompatible(DataType sourceType, DataType targetType)
        {
            // Exact match always works
            if (sourceType == targetType)
                return true;

            // Execution can only connect to execution
            if (sourceType == DataType.Execution || targetType == DataType.Execution)
                return false;

            // Number can accept Boolean (as 0/1)
            if (sourceType == DataType.Boolean && targetType == DataType.Number)
                return true;

            // Boolean can accept Number (0 = false, non-zero = true)
            if (sourceType == DataType.Number && targetType == DataType.Boolean)
                return true;

            // All other combinations are incompatible
            return false;
        }

        #endregion
    }
}
