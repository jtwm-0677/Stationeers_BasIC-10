using System;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// Exit from a subroutine early (before reaching END SUB)
    /// Generates EXIT SUB statement
    /// </summary>
    public class ExitSubNode : NodeBase
    {
        public override string NodeType => "ExitSub";
        public override string Category => "Subroutines";
        public override string? Icon => "🚪";

        public ExitSubNode()
        {
            Label = "EXIT SUB";
            Width = 180;
            Height = 80;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin only - no output as flow exits the subroutine
            AddInputPin("Exec", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // EXIT SUB must be inside a subroutine definition
            // This validation will be done by the graph validator
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return "EXIT SUB";
        }
    }
}
