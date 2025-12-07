using System;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// FUNCTION definition node - defines a reusable function that returns a value
    /// Creates a FUNCTION...END FUNCTION block that can be called by CallFunctionNode
    /// </summary>
    public class FunctionDefinitionNode : NodeBase
    {
        public override string NodeType => "FunctionDefinition";
        public override string Category => "Subroutines";
        public override string? Icon => "🔧";

        /// <summary>
        /// Name of the function
        /// </summary>
        public string FunctionName { get; set; } = "MyFunction";

        public FunctionDefinitionNode()
        {
            Label = "FUNCTION";
            Width = 250;
            Height = 140;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // No input pin - this is a definition node

            // Output execution pin for the body of the function
            AddOutputPin("Body", DataType.Execution);

            // Output pin for return value - can be connected to set the return value
            AddOutputPin("ReturnValue", DataType.Number);

            // Update label display
            Label = $"FUNCTION {FunctionName}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check function name
            if (string.IsNullOrWhiteSpace(FunctionName))
            {
                errorMessage = "Function name cannot be empty";
                return false;
            }

            // Check for valid identifier
            if (!System.Text.RegularExpressions.Regex.IsMatch(FunctionName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                errorMessage = "Function name must be a valid identifier (letters, numbers, underscore; cannot start with number)";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // The actual code generation is handled by GraphToBasicGenerator
            // This is just for metadata
            return $"FUNCTION {FunctionName}";
        }

        /// <summary>
        /// Gets the display color for this node (green for FUNCTION definitions)
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
