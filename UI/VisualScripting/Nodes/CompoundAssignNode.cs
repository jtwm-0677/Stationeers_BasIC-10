using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for compound assignment operations (+=, -=, *=, /=)
    /// </summary>
    public class CompoundAssignNode : NodeBase
    {
        public override string NodeType => "CompoundAssign";
        public override string Category => "Variables";
        public override string? Icon => "⚡";

        /// <summary>
        /// The compound assignment operator
        /// </summary>
        public CompoundOperator Operator { get; set; } = CompoundOperator.AddAssign;

        /// <summary>
        /// Variable name
        /// </summary>
        public string VariableName { get; set; } = "myVar";

        public CompoundAssignNode()
        {
            Label = "Compound Assign";
            Width = 180;
            Height = 110;
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

            // Add value input
            AddInputPin("Value", DataType.Number);

            // Update label based on operator
            Label = GetOperatorSymbol(Operator);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check variable name
            if (string.IsNullOrWhiteSpace(VariableName))
            {
                errorMessage = "Variable name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(VariableName))
            {
                errorMessage = "Invalid variable name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Variable", nameof(VariableName), PropertyType.Text, value =>
                {
                    VariableName = value;
                    Label = GetOperatorSymbol(Operator);
                })
                {
                    Value = VariableName,
                    Placeholder = "e.g., counter",
                    Tooltip = "The variable to modify"
                },
                new NodeProperty("Operator", nameof(Operator), PropertyType.Dropdown, value =>
                {
                    Operator = ParseOperator(value);
                    Label = GetOperatorSymbol(Operator);
                })
                {
                    Value = GetOperatorDisplayName(Operator),
                    Options = new[] { "Add (+=)", "Subtract (-=)", "Multiply (*=)", "Divide (/=)" },
                    Tooltip = "The compound assignment operator"
                }
            };
        }

        /// <summary>
        /// Parse operator from display string
        /// </summary>
        private CompoundOperator ParseOperator(string display)
        {
            return display switch
            {
                "Add (+=)" => CompoundOperator.AddAssign,
                "Subtract (-=)" => CompoundOperator.SubtractAssign,
                "Multiply (*=)" => CompoundOperator.MultiplyAssign,
                "Divide (/=)" => CompoundOperator.DivideAssign,
                _ => CompoundOperator.AddAssign
            };
        }

        /// <summary>
        /// Get display name for operator
        /// </summary>
        private string GetOperatorDisplayName(CompoundOperator op)
        {
            return op switch
            {
                CompoundOperator.AddAssign => "Add (+=)",
                CompoundOperator.SubtractAssign => "Subtract (-=)",
                CompoundOperator.MultiplyAssign => "Multiply (*=)",
                CompoundOperator.DivideAssign => "Divide (/=)",
                _ => "Add (+=)"
            };
        }

        public override string GenerateCode()
        {
            var op = GetOperatorString(Operator);
            return $"{VariableName} {op} value";
        }

        /// <summary>
        /// Get the operator string for code generation
        /// </summary>
        private string GetOperatorString(CompoundOperator op)
        {
            return op switch
            {
                CompoundOperator.AddAssign => "+=",
                CompoundOperator.SubtractAssign => "-=",
                CompoundOperator.MultiplyAssign => "*=",
                CompoundOperator.DivideAssign => "/=",
                _ => "+="
            };
        }

        /// <summary>
        /// Get the operator symbol for display
        /// </summary>
        private string GetOperatorSymbol(CompoundOperator op)
        {
            return op switch
            {
                CompoundOperator.AddAssign => $"{VariableName} +=",
                CompoundOperator.SubtractAssign => $"{VariableName} -=",
                CompoundOperator.MultiplyAssign => $"{VariableName} *=",
                CompoundOperator.DivideAssign => $"{VariableName} /=",
                _ => $"{VariableName} +="
            };
        }

        /// <summary>
        /// Check if a string is a valid BASIC identifier
        /// </summary>
        private bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Must start with a letter
            if (!char.IsLetter(name[0]))
                return false;

            // Rest must be letters, digits, or underscores
            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Compound assignment operators
    /// </summary>
    public enum CompoundOperator
    {
        AddAssign,      // +=
        SubtractAssign, // -=
        MultiplyAssign, // *=
        DivideAssign    // /=
    }
}
