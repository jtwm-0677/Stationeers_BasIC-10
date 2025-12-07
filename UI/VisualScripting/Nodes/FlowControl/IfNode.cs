using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// IF/THEN/ELSE branching node
    /// Provides conditional execution flow
    /// </summary>
    public class IfNode : NodeBase
    {
        public override string NodeType => "If";
        public override string Category => "Flow Control";
        public override string? Icon => "🔀";

        public IfNode()
        {
            Label = "IF";
            Width = 200;
            Height = 140;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input pins
            AddInputPin("Exec", DataType.Execution);
            AddInputPin("Condition", DataType.Boolean);

            // Output pins for branching
            AddOutputPin("True", DataType.Execution);
            AddOutputPin("False", DataType.Execution);
            AddOutputPin("Done", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check if condition input is connected
            var conditionPin = InputPins.Find(p => p.Name == "Condition");
            if (conditionPin == null || !conditionPin.IsConnected)
            {
                errorMessage = "Condition input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Code generation is handled by GraphToBasicGenerator
            // which will generate the IF/THEN/ELSE/ENDIF block structure
            return "IF condition THEN";
        }
    }
}
