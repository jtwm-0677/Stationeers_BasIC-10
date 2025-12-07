using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// CONTINUE node - skips to the next iteration of the loop
    /// Flow terminates and jumps back to loop start
    /// </summary>
    public class ContinueNode : NodeBase
    {
        public override string NodeType => "Continue";
        public override string Category => "Flow Control";
        public override string? Icon => "⏭";

        public ContinueNode()
        {
            Label = "CONTINUE";
            Width = 150;
            Height = 80;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin only - no output as flow terminates
            AddInputPin("Exec", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // CONTINUE must be inside a loop context
            // This validation will be done by the graph validator
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return "CONTINUE";
        }
    }
}
