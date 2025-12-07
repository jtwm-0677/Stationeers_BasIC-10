using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for accessing an array element (reading)
    /// </summary>
    public class ArrayAccessNode : NodeBase
    {
        public override string NodeType => "ArrayAccess";
        public override string Category => "Variables";
        public override string? Icon => "🔍";

        /// <summary>
        /// Array name (for display purposes, actual array comes from connection)
        /// </summary>
        public string ArrayName { get; set; } = "array";

        public ArrayAccessNode()
        {
            Label = "Array Get";
            Width = 180;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input: Array reference (visual only, actual name used in code gen)
            // Input: Index
            AddInputPin("Index", DataType.Number);

            // Output: Element value
            AddOutputPin("Value", DataType.Number);

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
            // This is used as part of an expression
            // The index would come from a connected node
            return $"{ArrayName}(index)";
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
