using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// GOSUB node - calls a subroutine at a label
    /// Flow continues after RETURN is executed
    /// </summary>
    public class GosubNode : NodeBase
    {
        public override string NodeType => "Gosub";
        public override string Category => "Flow Control";
        public override string? Icon => "📞";

        /// <summary>
        /// Target subroutine label name
        /// </summary>
        public string TargetLabel { get; set; } = "MySubroutine";

        public GosubNode()
        {
            Label = "GOSUB";
            Width = 180;
            Height = 90;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin
            AddInputPin("Exec", DataType.Execution);

            // Output execution pin - continues after RETURN
            AddOutputPin("Exec", DataType.Execution);

            // Update label display
            Label = $"GOSUB {TargetLabel}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check target label name
            if (string.IsNullOrWhiteSpace(TargetLabel))
            {
                errorMessage = "Target label name cannot be empty";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"GOSUB {TargetLabel}";
        }
    }
}
