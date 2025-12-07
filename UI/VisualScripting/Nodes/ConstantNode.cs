using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node that outputs a constant numeric value
    /// </summary>
    public class ConstantNode : NodeBase
    {
        public override string NodeType => "Constant";
        public override string Category => "Variables";
        public override string? Icon => "🔢";

        private double _value = 0.0;

        /// <summary>
        /// The constant value
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                _value = value;
                Label = $"Constant: {_value}";
                OnPropertyValueChanged(nameof(Value), value.ToString());
            }
        }

        public ConstantNode()
        {
            Label = "Constant: 0";
            Width = 150;
            Height = 100;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Value", nameof(Value), PropertyType.Number, value =>
                {
                    if (double.TryParse(value, out var num))
                        Value = num;
                })
                {
                    Value = Value.ToString(),
                    Placeholder = "Enter numeric value...",
                    Tooltip = "The constant numeric value to output"
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add value output
            AddOutputPin("Value", DataType.Number);

            // Update label to show value
            Label = $"Constant: {Value}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Constant values are always valid
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Constant values are used inline, not as standalone statements
            return Value.ToString();
        }
    }
}
