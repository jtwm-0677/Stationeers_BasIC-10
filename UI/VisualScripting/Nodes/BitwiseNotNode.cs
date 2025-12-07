using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for bitwise NOT operation
    /// </summary>
    public class BitwiseNotNode : NodeBase
    {
        public override string NodeType => "BitwiseNot";
        public override string Category => "Math";
        public override string? Icon => "🔀";

        public BitwiseNotNode()
        {
            Label = "Bitwise NOT (~)";
            Width = 150;
            Height = 90;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pin
            AddInputPin("Value", DataType.Number);

            // Add result output
            AddOutputPin("Result", DataType.Number);

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
            return "BNOT(value)";
        }
    }
}
