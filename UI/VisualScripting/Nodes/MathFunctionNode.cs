using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for various mathematical functions
    /// </summary>
    public class MathFunctionNode : NodeBase
    {
        public override string NodeType => "MathFunction";
        public override string Category => "Math";
        public override string? Icon => "🧮";

        /// <summary>
        /// The mathematical function to perform
        /// </summary>
        public MathFunctionType Function { get; set; } = MathFunctionType.ABS;

        public MathFunctionNode()
        {
            Label = "Math Function";
            Width = 160;
            Height = 90;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // RND doesn't take an input, others do
            if (Function != MathFunctionType.RND)
            {
                AddInputPin("X", DataType.Number);
            }

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

            if (Function == MathFunctionType.RND)
            {
                return "RND()";
            }
            else
            {
                return $"{funcName}(x)";
            }
        }

        /// <summary>
        /// Get the function name for code generation
        /// </summary>
        private string GetFunctionName(MathFunctionType function)
        {
            return function switch
            {
                MathFunctionType.ABS => "ABS",
                MathFunctionType.SQRT => "SQRT",
                MathFunctionType.CEIL => "CEIL",
                MathFunctionType.FLOOR => "FLOOR",
                MathFunctionType.ROUND => "ROUND",
                MathFunctionType.TRUNC => "TRUNC",
                MathFunctionType.SGN => "SGN",
                MathFunctionType.RND => "RND",
                _ => "ABS"
            };
        }
    }

    /// <summary>
    /// Supported mathematical functions
    /// </summary>
    public enum MathFunctionType
    {
        ABS,    // Absolute value
        SQRT,   // Square root
        CEIL,   // Ceiling (round up)
        FLOOR,  // Floor (round down)
        ROUND,  // Round to nearest
        TRUNC,  // Truncate (remove decimal)
        SGN,    // Sign (-1, 0, 1)
        RND     // Random number
    }
}
