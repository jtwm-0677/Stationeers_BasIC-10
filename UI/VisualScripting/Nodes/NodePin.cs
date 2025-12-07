using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Represents a connection pin on a node (input or output)
    /// </summary>
    public class NodePin
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public PinType PinType { get; set; }
        public DataType DataType { get; set; }
        public NodeBase? ParentNode { get; set; }

        /// <summary>
        /// Gets whether this pin is connected to another pin
        /// </summary>
        public bool IsConnected => Connections.Count > 0;

        /// <summary>
        /// List of pin IDs this pin is connected to
        /// </summary>
        public List<Guid> Connections { get; set; } = new();

        public NodePin()
        {
            Id = Guid.NewGuid();
            Name = string.Empty;
        }

        public NodePin(string name, PinType pinType, DataType dataType)
        {
            Id = Guid.NewGuid();
            Name = name;
            PinType = pinType;
            DataType = dataType;
        }

        /// <summary>
        /// Calculate the position of this pin relative to its parent node
        /// Pins are 12px diameter, 8px from edge, 24px vertical spacing
        /// </summary>
        public (double X, double Y) GetPosition(int index)
        {
            if (ParentNode == null)
                return (0, 0);

            const double pinRadius = 6.0; // 12px diameter = 6px radius
            const double edgeOffset = 8.0;
            const double headerHeight = 32.0;
            const double pinSpacing = 24.0;
            const double firstPinY = headerHeight + pinSpacing;

            double x = PinType == PinType.Input
                ? edgeOffset + pinRadius
                : ParentNode.Width - edgeOffset - pinRadius;

            double y = firstPinY + (index * pinSpacing);

            return (x, y);
        }
    }

    /// <summary>
    /// Pin direction type
    /// </summary>
    public enum PinType
    {
        Input,
        Output
    }

    /// <summary>
    /// Pin data type for type-safe connections
    /// </summary>
    public enum DataType
    {
        Execution,  // White - flow control
        Number,     // Blue - numeric values
        Boolean,    // Green/Red - true/false
        Device,     // Orange - device references
        String      // Purple - text values
    }
}
