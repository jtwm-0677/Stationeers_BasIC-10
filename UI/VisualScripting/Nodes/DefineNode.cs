using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for DEFINE preprocessor directive
    /// </summary>
    public class DefineNode : NodeBase
    {
        public override string NodeType => "Define";
        public override string Category => "Variables";
        public override string? Icon => "📝";

        /// <summary>
        /// Define name
        /// </summary>
        public string DefineName { get; set; } = "MY_DEFINE";

        /// <summary>
        /// Define value
        /// </summary>
        public double Value { get; set; } = 0.0;

        public DefineNode()
        {
            Label = "DEFINE";
            Width = 180;
            Height = 60;
        }

        public override void Initialize()
        {
            base.Initialize();

            // DEFINE directives don't need pins - they're preprocessor directives
            InputPins.Clear();
            OutputPins.Clear();

            // Update label
            Label = $"DEFINE {DefineName}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check define name
            if (string.IsNullOrWhiteSpace(DefineName))
            {
                errorMessage = "Define name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(DefineName))
            {
                errorMessage = "Invalid define name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"DEFINE {DefineName} {Value}";
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
