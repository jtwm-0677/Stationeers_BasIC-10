using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for popping a value from the stack
    /// Generates: POP variableName
    /// </summary>
    public class PopNode : NodeBase
    {
        public override string NodeType => "Pop";
        public override string Category => "Devices";
        public override string? Icon => "⬇️";

        /// <summary>
        /// Variable name to store the popped value
        /// </summary>
        public string VariableName { get; set; } = "result";

        public PopNode()
        {
            Label = "Pop";
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

            // Add output pin for popped value
            AddOutputPin("Value", DataType.Number);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Validate variable name
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

        public override string GenerateCode()
        {
            return $"POP {VariableName}";
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
