using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// BREAK node - exits the current loop early
    /// Flow terminates and jumps to parent loop's Done pin
    /// </summary>
    public class BreakNode : NodeBase
    {
        public override string NodeType => "Break";
        public override string Category => "Flow Control";
        public override string? Icon => "⏸";

        public BreakNode()
        {
            Label = "BREAK";
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
            // BREAK must be inside a loop context
            // This validation will be done by the graph validator
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return "BREAK";
        }
    }
}
