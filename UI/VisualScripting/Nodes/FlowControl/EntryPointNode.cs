using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// Entry point node - marks the start of program execution
    /// Only one per graph allowed
    /// </summary>
    public class EntryPointNode : NodeBase
    {
        public override string NodeType => "EntryPoint";
        public override string Category => "Flow Control";
        public override string? Icon => "▶";

        public EntryPointNode()
        {
            Label = "Program Start";
            Width = 180;
            Height = 80;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Entry point has only an output execution pin
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
            // Entry point just marks the start - generates a comment
            return "' --- Program Start ---";
        }
    }
}
