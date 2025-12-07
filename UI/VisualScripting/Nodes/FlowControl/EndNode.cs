using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// END node - terminates program execution
    /// No output pins as program stops
    /// </summary>
    public class EndNode : NodeBase
    {
        public override string NodeType => "End";
        public override string Category => "Flow Control";
        public override string? Icon => "⏹";

        public EndNode()
        {
            Label = "END";
            Width = 150;
            Height = 80;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin only - no output as program ends
            AddInputPin("Exec", DataType.Execution);

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
            return "END";
        }
    }
}
