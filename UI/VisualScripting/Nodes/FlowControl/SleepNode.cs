using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// SLEEP node - pauses execution for a specified duration
    /// Useful for timing-based operations
    /// </summary>
    public class SleepNode : NodeBase
    {
        public override string NodeType => "Sleep";
        public override string Category => "Flow Control";
        public override string? Icon => "💤";

        public SleepNode()
        {
            Label = "SLEEP";
            Width = 180;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input pins
            AddInputPin("Exec", DataType.Execution);
            AddInputPin("Duration", DataType.Number); // Duration in seconds

            // Output execution pin - continues after sleep
            AddOutputPin("Exec", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check if duration input is connected
            var durationPin = InputPins.Find(p => p.Name == "Duration");
            if (durationPin == null || !durationPin.IsConnected)
            {
                errorMessage = "Duration input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Duration value will be substituted by the code generator
            return "SLEEP duration";
        }
    }
}
