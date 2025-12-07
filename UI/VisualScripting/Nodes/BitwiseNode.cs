using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for bitwise operations (AND, OR, XOR)
    /// </summary>
    public class BitwiseNode : NodeBase
    {
        public override string NodeType => "Bitwise";
        public override string Category => "Math";
        public override string? Icon => "🔣";

        /// <summary>
        /// The bitwise operation to perform
        /// </summary>
        public BitwiseOperation Operation { get; set; } = BitwiseOperation.And;

        public BitwiseNode()
        {
            Label = "Bitwise";
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

            // Update label based on operation
            Label = GetOperationName(Operation);

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
            var funcName = GetFunctionName(Operation);
            return $"{funcName}(a, b)";
        }

        /// <summary>
        /// Get the function name for code generation
        /// </summary>
        private string GetFunctionName(BitwiseOperation operation)
        {
            return operation switch
            {
                BitwiseOperation.And => "BAND",
                BitwiseOperation.Or => "BOR",
                BitwiseOperation.Xor => "BXOR",
                _ => "BAND"
            };
        }

        /// <summary>
        /// Get the operation name for display
        /// </summary>
        private string GetOperationName(BitwiseOperation operation)
        {
            return operation switch
            {
                BitwiseOperation.And => "Bitwise AND (&)",
                BitwiseOperation.Or => "Bitwise OR (|)",
                BitwiseOperation.Xor => "Bitwise XOR (^)",
                _ => "Bitwise"
            };
        }
    }

    /// <summary>
    /// Bitwise operations
    /// </summary>
    public enum BitwiseOperation
    {
        And,    // Bitwise AND (&)
        Or,     // Bitwise OR (|)
        Xor     // Bitwise XOR (^)
    }
}
