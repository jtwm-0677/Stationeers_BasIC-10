using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for logical OR operation
    /// </summary>
    public class OrNode : NodeBase
    {
        public override string NodeType => "Or";
        public override string Category => "Logic";
        public override string? Icon => "∨";

        public OrNode()
        {
            Label = "OR";
            Width = 150;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pins (boolean)
            AddInputPin("A", DataType.Boolean);
            AddInputPin("B", DataType.Boolean);

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
            return "a OR b";
        }
    }
}
