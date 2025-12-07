using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for increment/decrement operations (++/--)
    /// </summary>
    public class IncrementNode : NodeBase
    {
        public override string NodeType => "Increment";
        public override string Category => "Variables";
        public override string? Icon => "🔼";

        /// <summary>
        /// The operation type (increment or decrement)
        /// </summary>
        public IncrementType Type { get; set; } = IncrementType.Increment;

        /// <summary>
        /// Whether this is prefix or postfix
        /// </summary>
        public IncrementPosition Position { get; set; } = IncrementPosition.Prefix;

        /// <summary>
        /// Variable name
        /// </summary>
        public string VariableName { get; set; } = "myVar";

        public IncrementNode()
        {
            Label = "Increment";
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

            // Add value output (for expression use)
            AddOutputPin("Value", DataType.Number);

            // Update label based on type and position
            Label = GetDisplayLabel();

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
                    Label = GetDisplayLabel();
                })
                {
                    Value = VariableName,
                    Placeholder = "e.g., counter",
                    Tooltip = "The variable to increment or decrement"
                },
                new NodeProperty("Type", nameof(Type), PropertyType.Dropdown, value =>
                {
                    Type = value == "Decrement (--)" ? IncrementType.Decrement : IncrementType.Increment;
                    Label = GetDisplayLabel();
                })
                {
                    Value = Type == IncrementType.Increment ? "Increment (++)" : "Decrement (--)",
                    Options = new[] { "Increment (++)", "Decrement (--)" },
                    Tooltip = "Increment or decrement the variable"
                }
            };
        }

        public override string GenerateCode()
        {
            var op = Type == IncrementType.Increment ? "++" : "--";

            if (Position == IncrementPosition.Prefix)
            {
                return $"{op}{VariableName}";
            }
            else
            {
                return $"{VariableName}{op}";
            }
        }

        /// <summary>
        /// Get the display label
        /// </summary>
        private string GetDisplayLabel()
        {
            var op = Type == IncrementType.Increment ? "++" : "--";

            if (Position == IncrementPosition.Prefix)
            {
                return $"{op}{VariableName}";
            }
            else
            {
                return $"{VariableName}{op}";
            }
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
    /// Increment or decrement
    /// </summary>
    public enum IncrementType
    {
        Increment,  // ++
        Decrement   // --
    }

    /// <summary>
    /// Prefix or postfix position
    /// </summary>
    public enum IncrementPosition
    {
        Prefix,     // ++x
        Postfix     // x++
    }
}
