using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for referencing the IC chip itself (db/THIS)
    /// Generates: ALIAS aliasName db (or just uses db directly)
    /// </summary>
    public class ThisDeviceNode : NodeBase
    {
        public override string NodeType => "ThisDevice";
        public override string Category => "Devices";
        public override string? Icon => "💾";

        /// <summary>
        /// The alias name for this device (optional, defaults to "chip")
        /// If empty, will use "db" directly in generated code
        /// </summary>
        public string AliasName { get; set; } = "chip";

        /// <summary>
        /// Whether to create an alias or use db directly
        /// </summary>
        public bool UseDirectReference { get; set; } = false;

        public ThisDeviceNode()
        {
            Label = "This Device";
            Width = 200;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add output pin for device reference
            AddOutputPin("Device", DataType.Device);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // If not using direct reference, validate alias name
            if (!UseDirectReference)
            {
                if (string.IsNullOrWhiteSpace(AliasName))
                {
                    errorMessage = "Alias name cannot be empty (or enable UseDirectReference)";
                    return false;
                }

                // Check for valid BASIC identifier
                if (!IsValidIdentifier(AliasName))
                {
                    errorMessage = "Invalid alias name. Must start with a letter and contain only letters, numbers, and underscores.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            if (UseDirectReference || string.IsNullOrWhiteSpace(AliasName))
            {
                // Use db directly - no code generation needed
                return "# Using db directly";
            }
            else
            {
                return $"ALIAS {AliasName} db";
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
}
