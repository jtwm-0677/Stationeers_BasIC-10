using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for declaring an array (DIM statement)
    /// </summary>
    public class ArrayNode : NodeBase
    {
        public override string NodeType => "Array";
        public override string Category => "Variables";
        public override string? Icon => "📊";

        /// <summary>
        /// Array name
        /// </summary>
        public string ArrayName { get; set; } = "myArray";

        /// <summary>
        /// Array size
        /// </summary>
        public int Size { get; set; } = 10;

        public ArrayNode()
        {
            Label = "Array (DIM)";
            Width = 180;
            Height = 80;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Arrays can be referenced later, so provide an output
            // This is more for visual connection purposes
            AddOutputPin("Array", DataType.Number);

            // Update label
            Label = $"DIM {ArrayName}[{Size}]";

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

            // Check size
            if (Size <= 0)
            {
                errorMessage = "Array size must be greater than 0";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"DIM {ArrayName}({Size})";
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
