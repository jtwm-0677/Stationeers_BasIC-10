using System;
using System.Collections.Generic;
using System.Globalization;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for declaring a named constant (CONST statement)
    /// </summary>
    public class ConstNode : NodeBase
    {
        public override string NodeType => "Const";
        public override string Category => "Variables";
        public override string? Icon => "🔒";

        /// <summary>
        /// Constant name
        /// </summary>
        public string ConstName { get; set; } = "MY_CONST";

        /// <summary>
        /// Constant value
        /// </summary>
        public double Value { get; set; } = 0.0;

        public ConstNode()
        {
            Label = "CONST";
            Width = 180;
            Height = 60;
        }

        public override void Initialize()
        {
            base.Initialize();

            // CONST declarations don't need pins - they're pure declarations
            InputPins.Clear();
            OutputPins.Clear();

            // Update label
            Label = $"CONST {ConstName}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Name", nameof(ConstName), PropertyType.Text, value =>
                {
                    ConstName = value;
                    Label = $"CONST {ConstName}";
                })
                {
                    Value = ConstName,
                    Placeholder = "e.g., MAX_TEMP",
                    Tooltip = "Constant name (use UPPERCASE by convention)"
                },
                new NodeProperty("Value", nameof(Value), PropertyType.Number, value =>
                {
                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                    {
                        Value = result;
                    }
                })
                {
                    Value = Value.ToString(CultureInfo.InvariantCulture),
                    Placeholder = "0",
                    Tooltip = "Constant value (numeric)"
                }
            };
        }

        public override bool Validate(out string errorMessage)
        {
            // Check constant name
            if (string.IsNullOrWhiteSpace(ConstName))
            {
                errorMessage = "Constant name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(ConstName))
            {
                errorMessage = "Invalid constant name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"CONST {ConstName} = {Value}";
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
}
