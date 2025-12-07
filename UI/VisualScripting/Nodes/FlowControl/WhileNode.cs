using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// WHILE loop node
    /// Repeats body while condition is true
    /// </summary>
    public class WhileNode : NodeBase
    {
        public override string NodeType => "While";
        public override string Category => "Flow Control";
        public override string? Icon => "🔁";

        /// <summary>
        /// Whether to automatically insert YIELD in the loop
        /// </summary>
        public bool AutoYield { get; set; } = true;

        public WhileNode()
        {
            Label = "WHILE";
            Width = 200;
            Height = 120;
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

            // Output pins
            AddOutputPin("LoopBody", DataType.Execution);
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
            // which will generate the WHILE/WEND block structure
            return "WHILE condition";
        }
    }
}
