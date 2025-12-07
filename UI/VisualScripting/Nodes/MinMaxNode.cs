using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for MIN and MAX functions
    /// </summary>
    public class MinMaxNode : NodeBase
    {
        public override string NodeType => "MinMax";
        public override string Category => "Math";
        public override string? Icon => "⬌";

        /// <summary>
        /// Whether to use MIN or MAX
        /// </summary>
        public MinMaxType Type { get; set; } = MinMaxType.MIN;

        public MinMaxNode()
        {
            Label = "MIN/MAX";
            Width = 150;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pins
            AddInputPin("A", DataType.Number);
            AddInputPin("B", DataType.Number);

            // Add result output
            AddOutputPin("Result", DataType.Number);

            // Update label based on type
            Label = Type == MinMaxType.MIN ? "MIN" : "MAX";

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
            var funcName = Type == MinMaxType.MIN ? "MIN" : "MAX";
            return $"{funcName}(a, b)";
        }
    }

    /// <summary>
    /// MIN or MAX operation
    /// </summary>
    public enum MinMaxType
    {
        MIN,
        MAX
    }
}
