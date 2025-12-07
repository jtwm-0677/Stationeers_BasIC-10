using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for pushing a value onto the stack
    /// Generates: PUSH value
    /// </summary>
    public class PushNode : NodeBase
    {
        public override string NodeType => "Push";
        public override string Category => "Devices";
        public override string? Icon => "⬆️";

        public PushNode()
        {
            Label = "Push";
            Width = 180;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add execution pins
            AddInputPin("In", DataType.Execution);
            AddOutputPin("Out", DataType.Execution);

            // Add input pin for value to push
            AddInputPin("Value", DataType.Number);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check if value input is connected
            var valuePin = InputPins.Find(p => p.Name == "Value");
            if (valuePin != null && !valuePin.IsConnected)
            {
                errorMessage = "Value input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Note: Actual value would be determined by connected node
            return "PUSH value";
        }
    }
}
