using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for bit shifting operations (left/right)
    /// </summary>
    public class ShiftNode : NodeBase
    {
        public override string NodeType => "Shift";
        public override string Category => "Math";
        public override string? Icon => "⇄";

        /// <summary>
        /// The shift direction
        /// </summary>
        public ShiftDirection Direction { get; set; } = ShiftDirection.Left;

        public ShiftNode()
        {
            Label = "Bit Shift";
            Width = 150;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pins
            AddInputPin("Value", DataType.Number);
            AddInputPin("Bits", DataType.Number);

            // Add result output
            AddOutputPin("Result", DataType.Number);

            // Update label based on direction
            Label = Direction == ShiftDirection.Left ? "Shift Left (<<)" : "Shift Right (>>)";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            var funcName = Direction == ShiftDirection.Left ? "SHL" : "SHR";
            return $"{funcName}(value, bits)";
        }
    }

    /// <summary>
    /// Bit shift direction
    /// </summary>
    public enum ShiftDirection
    {
        Left,   // << (shift left)
        Right   // >> (shift right)
    }
}
