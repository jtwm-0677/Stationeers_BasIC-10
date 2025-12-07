using System;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// SUB definition node - defines a reusable subroutine block
    /// Creates a SUB...END SUB block that can be called by CallSubNode
    /// </summary>
    public class SubDefinitionNode : NodeBase
    {
        public override string NodeType => "SubDefinition";
        public override string Category => "Subroutines";
        public override string? Icon => "📦";

        /// <summary>
        /// Name of the subroutine
        /// </summary>
        public string SubroutineName { get; set; } = "MySubroutine";

        public SubDefinitionNode()
        {
            Label = "SUB";
            Width = 250;
            Height = 120;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // No input pin - this is a definition node
            // Output execution pin for the body of the subroutine
            AddOutputPin("Body", DataType.Execution);

            // Update label display
            Label = $"SUB {SubroutineName}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check subroutine name
            if (string.IsNullOrWhiteSpace(SubroutineName))
            {
                errorMessage = "Subroutine name cannot be empty";
                return false;
            }

            // Check for valid identifier
            if (!System.Text.RegularExpressions.Regex.IsMatch(SubroutineName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                errorMessage = "Subroutine name must be a valid identifier (letters, numbers, underscore; cannot start with number)";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // The actual code generation is handled by GraphToBasicGenerator
            // This is just for metadata
            return $"SUB {SubroutineName}";
        }

        /// <summary>
        /// Gets the display color for this node (green for SUB definitions)
        /// </summary>
        public string GetHeaderColor()
        {
            return "#27AE60"; // Green
        }

        /// <summary>
        /// Indicates this is a container-style node
        /// </summary>
        public bool IsContainer => true;
    }
}
