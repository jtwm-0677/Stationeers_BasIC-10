using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for assigning a value to an array element
    /// </summary>
    public class ArrayAssignNode : NodeBase
    {
        public override string NodeType => "ArrayAssign";
        public override string Category => "Variables";
        public override string? Icon => "📥";

        /// <summary>
        /// Array name
        /// </summary>
        public string ArrayName { get; set; } = "array";

        public ArrayAssignNode()
        {
            Label = "Array Set";
            Width = 180;
            Height = 120;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Execution flow
            AddInputPin("In", DataType.Execution);
            AddOutputPin("Out", DataType.Execution);

            // Data inputs
            AddInputPin("Index", DataType.Number);
            AddInputPin("Value", DataType.Number);

            // Update label
            Label = $"{ArrayName}[i] = value";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check array name
            if (string.IsNullOrWhiteSpace(ArrayName))
            {
                errorMessage = "Array name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(ArrayName))
            {
                errorMessage = "Invalid array name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // This generates an assignment statement
            // Index and value would come from connected nodes
            return $"{ArrayName}(index) = value";
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
