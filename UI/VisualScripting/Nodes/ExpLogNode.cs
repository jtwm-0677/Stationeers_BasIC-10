using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for exponential and logarithm functions
    /// </summary>
    public class ExpLogNode : NodeBase
    {
        public override string NodeType => "ExpLog";
        public override string Category => "Math";
        public override string? Icon => "📈";

        /// <summary>
        /// The function to perform (EXP or LOG)
        /// </summary>
        public ExpLogType Type { get; set; } = ExpLogType.EXP;

        public ExpLogNode()
        {
            Label = "EXP/LOG";
            Width = 150;
            Height = 90;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pin
            AddInputPin("X", DataType.Number);

            // Add result output
            AddOutputPin("Result", DataType.Number);

            // Update label based on type
            Label = Type == ExpLogType.EXP ? "EXP" : "LOG";

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
            var funcName = Type == ExpLogType.EXP ? "EXP" : "LOG";
            return $"{funcName}(x)";
        }
    }

    /// <summary>
    /// EXP or LOG function
    /// </summary>
    public enum ExpLogType
    {
        EXP,    // e^x (exponential)
        LOG     // Natural logarithm (ln)
    }
}
