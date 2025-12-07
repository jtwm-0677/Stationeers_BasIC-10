using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for power/exponentiation operation
    /// </summary>
    public class PowerNode : NodeBase
    {
        public override string NodeType => "Power";
        public override string Category => "Math";
        public override string? Icon => "🔺";

        public PowerNode()
        {
            Label = "Power (^)";
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
            AddInputPin("Base", DataType.Number);
            AddInputPin("Exponent", DataType.Number);

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
            // This is part of an expression
            return "base ^ exponent";
        }
    }
}
