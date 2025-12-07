using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for mathematical operations
    /// </summary>
    public class MathNode : NodeBase
    {
        public override string NodeType => "Math";
        public override string Category => "Math";
        public override string? Icon => "➕";

        /// <summary>
        /// Mathematical operation to perform
        /// </summary>
        public MathOperation Operation { get; set; } = MathOperation.Add;

        public MathNode()
        {
            Label = "Math";
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

            // Add value inputs based on operation
            if (IsUnaryOperation(Operation))
            {
                AddInputPin("Value", DataType.Number);
            }
            else
            {
                AddInputPin("A", DataType.Number);
                AddInputPin("B", DataType.Number);
            }

            // Add result output
            AddOutputPin("Result", DataType.Number);

            // Update label based on operation
            Label = GetOperationSymbol(Operation);

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
            // This would be part of an expression, not a standalone statement
            // The actual code generation would happen when building the full expression tree
            var op = GetOperationSymbol(Operation);

            if (IsUnaryOperation(Operation))
            {
                return $"{op}(value)";
            }
            else
            {
                return $"a {op} b";
            }
        }

        /// <summary>
        /// Check if an operation is unary (single operand)
        /// </summary>
        private bool IsUnaryOperation(MathOperation op)
        {
            return op == MathOperation.Negate ||
                   op == MathOperation.Abs ||
                   op == MathOperation.Sqrt;
        }

        /// <summary>
        /// Get the symbol/name for an operation
        /// </summary>
        private string GetOperationSymbol(MathOperation op)
        {
            return op switch
            {
                MathOperation.Add => "Add (+)",
                MathOperation.Subtract => "Subtract (-)",
                MathOperation.Multiply => "Multiply (*)",
                MathOperation.Divide => "Divide (/)",
                MathOperation.Modulo => "Modulo (%)",
                MathOperation.Power => "Power (^)",
                MathOperation.Negate => "Negate (-)",
                MathOperation.Abs => "Abs",
                MathOperation.Sqrt => "Sqrt",
                MathOperation.Min => "Min",
                MathOperation.Max => "Max",
                _ => "Math"
            };
        }
    }

    /// <summary>
    /// Mathematical operations supported
    /// </summary>
    public enum MathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Power,
        Negate,
        Abs,
        Sqrt,
        Min,
        Max
    }
}
