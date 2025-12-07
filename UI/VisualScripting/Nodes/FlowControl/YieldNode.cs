using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// YIELD node - yields execution to allow other scripts to run
    /// Essential for preventing infinite loop lockups in Stationeers
    /// </summary>
    public class YieldNode : NodeBase
    {
        public override string NodeType => "Yield";
        public override string Category => "Flow Control";
        public override string? Icon => "⏸";

        public YieldNode()
        {
            Label = "YIELD";
            Width = 150;
            Height = 90;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input and output execution pins - flow continues
            AddInputPin("Exec", DataType.Execution);
            AddOutputPin("Exec", DataType.Execution);

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
            return "YIELD";
        }
    }
}
