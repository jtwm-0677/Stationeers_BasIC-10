using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for comparison operations
    /// </summary>
    public class CompareNode : NodeBase
    {
        public override string NodeType => "Compare";
        public override string Category => "Logic";
        public override string? Icon => "⚖️";

        /// <summary>
        /// The comparison operator
        /// </summary>
        public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equal;

        public CompareNode()
        {
            Label = "Compare";
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

            // Add result output (boolean)
            AddOutputPin("Result", DataType.Boolean);

            // Update label based on operator
            Label = GetOperatorSymbol(Operator);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Operator", nameof(Operator), PropertyType.Dropdown, value =>
                {
                    Operator = ParseOperator(value);
                    Label = GetOperatorSymbol(Operator);
                })
                {
                    Value = GetOperatorSymbol(Operator),
                    Options = new[]
                    {
                        "Equal (=)",
                        "Not Equal (<>)",
                        "Less Than (<)",
                        "Greater Than (>)",
                        "Less/Equal (<=)",
                        "Greater/Equal (>=)"
                    },
                    Tooltip = "Comparison operator"
                }
            };
        }

        /// <summary>
        /// Parse operator from display string
        /// </summary>
        private ComparisonOperator ParseOperator(string display)
        {
            return display switch
            {
                "Equal (=)" => ComparisonOperator.Equal,
                "Not Equal (<>)" => ComparisonOperator.NotEqual,
                "Less Than (<)" => ComparisonOperator.LessThan,
                "Greater Than (>)" => ComparisonOperator.GreaterThan,
                "Less/Equal (<=)" => ComparisonOperator.LessThanOrEqual,
                "Greater/Equal (>=)" => ComparisonOperator.GreaterThanOrEqual,
                _ => ComparisonOperator.Equal
            };
        }

        public override string GenerateCode()
        {
            var op = GetOperatorString(Operator);
            return $"a {op} b";
        }

        /// <summary>
        /// Get the operator string for code generation
        /// </summary>
        private string GetOperatorString(ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => "=",
                ComparisonOperator.NotEqual => "<>",
                ComparisonOperator.LessThan => "<",
                ComparisonOperator.GreaterThan => ">",
                ComparisonOperator.LessThanOrEqual => "<=",
                ComparisonOperator.GreaterThanOrEqual => ">=",
                _ => "="
            };
        }

        /// <summary>
        /// Get the operator symbol for display
        /// </summary>
        private string GetOperatorSymbol(ComparisonOperator op)
        {
            return op switch
            {
                ComparisonOperator.Equal => "Equal (=)",
                ComparisonOperator.NotEqual => "Not Equal (<>)",
                ComparisonOperator.LessThan => "Less Than (<)",
                ComparisonOperator.GreaterThan => "Greater Than (>)",
                ComparisonOperator.LessThanOrEqual => "Less/Equal (<=)",
                ComparisonOperator.GreaterThanOrEqual => "Greater/Equal (>=)",
                _ => "Compare"
            };
        }
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,              // =
        NotEqual,           // <>
        LessThan,           // <
        GreaterThan,        // >
        LessThanOrEqual,    // <=
        GreaterThanOrEqual  // >=
    }
}
