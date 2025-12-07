using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for trigonometric functions
    /// </summary>
    public class TrigNode : NodeBase
    {
        public override string NodeType => "Trig";
        public override string Category => "Math";
        public override string? Icon => "📐";

        /// <summary>
        /// The trigonometric function to perform
        /// </summary>
        public TrigFunction Function { get; set; } = TrigFunction.SIN;

        public TrigNode()
        {
            Label = "Trig";
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
            AddInputPin(IsInverse() ? "Value" : "Angle", DataType.Number);

            // Add result output
            AddOutputPin("Result", DataType.Number);

            // Update label based on function
            Label = GetFunctionName(Function);

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
            var funcName = GetFunctionName(Function);
            return $"{funcName}(angle)";
        }

        /// <summary>
        /// Check if this is an inverse trig function
        /// </summary>
        private bool IsInverse()
        {
            return Function == TrigFunction.ASIN ||
                   Function == TrigFunction.ACOS ||
                   Function == TrigFunction.ATAN;
        }

        /// <summary>
        /// Get the function name for code generation
        /// </summary>
        private string GetFunctionName(TrigFunction function)
        {
            return function switch
            {
                TrigFunction.SIN => "SIN",
                TrigFunction.COS => "COS",
                TrigFunction.TAN => "TAN",
                TrigFunction.ASIN => "ASIN",
                TrigFunction.ACOS => "ACOS",
                TrigFunction.ATAN => "ATAN",
                _ => "SIN"
            };
        }
    }

    /// <summary>
    /// Supported trigonometric functions
    /// </summary>
    public enum TrigFunction
    {
        SIN,    // Sine
        COS,    // Cosine
        TAN,    // Tangent
        ASIN,   // Arc sine (inverse sine)
        ACOS,   // Arc cosine (inverse cosine)
        ATAN    // Arc tangent (inverse tangent)
    }
}
