using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for ATAN2 function (two-argument arctangent)
    /// </summary>
    public class Atan2Node : NodeBase
    {
        public override string NodeType => "Atan2";
        public override string Category => "Math";
        public override string? Icon => "📐";

        public Atan2Node()
        {
            Label = "ATAN2";
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
            AddInputPin("Y", DataType.Number);
            AddInputPin("X", DataType.Number);

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
            return "ATAN2(y, x)";
        }
    }
}
