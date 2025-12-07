using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for logical NOT operation
    /// </summary>
    public class NotNode : NodeBase
    {
        public override string NodeType => "Not";
        public override string Category => "Logic";
        public override string? Icon => "¬";

        public NotNode()
        {
            Label = "NOT";
            Width = 150;
            Height = 90;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pin (boolean)
            AddInputPin("Value", DataType.Boolean);

            // Add result output (boolean)
            AddOutputPin("Result", DataType.Boolean);

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
            return "NOT value";
        }
    }
}
