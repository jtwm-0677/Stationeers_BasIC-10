using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for multiplication operation
    /// </summary>
    public class MultiplyNode : NodeBase
    {
        public override string NodeType => "Multiply";
        public override string Category => "Math";
        public override string? Icon => "✖️";

        public MultiplyNode()
        {
            Label = "Multiply (*)";
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
            AddInputPin("A", DataType.Number);
            AddInputPin("B", DataType.Number);

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
            return "a * b";
        }
    }
}
