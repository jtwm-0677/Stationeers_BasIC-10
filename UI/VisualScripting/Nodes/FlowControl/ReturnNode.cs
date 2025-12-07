using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// RETURN node - returns from a subroutine called by GOSUB
    /// Flow returns to the statement after the GOSUB
    /// </summary>
    public class ReturnNode : NodeBase
    {
        public override string NodeType => "Return";
        public override string Category => "Flow Control";
        public override string? Icon => "↩";

        public ReturnNode()
        {
            Label = "RETURN";
            Width = 150;
            Height = 80;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin only - no output as flow returns to caller
            AddInputPin("Exec", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // RETURN must be in a subroutine context (after a label called by GOSUB)
            // This validation will be done by the graph validator
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return "RETURN";
        }
    }
}
