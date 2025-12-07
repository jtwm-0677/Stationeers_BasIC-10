using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// LABEL node - defines a jump target for GOTO/GOSUB
    /// Can be placed inline in execution flow
    /// </summary>
    public class LabelNode : NodeBase
    {
        public override string NodeType => "Label";
        public override string Category => "Flow Control";
        public override string? Icon => "🏷";

        private string _labelName = "MyLabel";

        /// <summary>
        /// Label name
        /// </summary>
        public string LabelName
        {
            get => _labelName;
            set
            {
                _labelName = value;
                Label = $"Label: {_labelName}";
                OnPropertyValueChanged(nameof(LabelName), value);
            }
        }

        public LabelNode()
        {
            Label = "Label";
            Width = 180;
            Height = 110;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Label Name", nameof(LabelName), PropertyType.Text, value => LabelName = value)
                {
                    Value = LabelName,
                    Placeholder = "Enter label name...",
                    Tooltip = "The name of this label (used with GOTO/GOSUB)"
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Optional execution input for inline placement
            AddInputPin("Exec", DataType.Execution);

            // Execution output to continue flow after label
            AddOutputPin("Exec", DataType.Execution);

            // Update label display
            Label = $"Label: {LabelName}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check label name
            if (string.IsNullOrWhiteSpace(LabelName))
            {
                errorMessage = "Label name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(LabelName))
            {
                errorMessage = "Invalid label name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"{LabelName}:";
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
