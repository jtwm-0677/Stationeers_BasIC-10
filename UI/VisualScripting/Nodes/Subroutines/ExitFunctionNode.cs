using System;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// Exit from a function early with a return value
    /// Generates EXIT FUNCTION statement (with RETURN before it)
    /// </summary>
    public class ExitFunctionNode : NodeBase
    {
        public override string NodeType => "ExitFunction";
        public override string Category => "Subroutines";
        public override string? Icon => "🚪";

        public ExitFunctionNode()
        {
            Label = "EXIT FUNCTION";
            Width = 200;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin
            AddInputPin("Exec", DataType.Execution);

            // Input pin for return value
            AddInputPin("ReturnValue", DataType.Number);

            // No output pins - flow exits the function

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // EXIT FUNCTION must be inside a function definition
            // This validation will be done by the graph validator
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // The code generator will handle getting the return value from the input pin
            return "EXIT FUNCTION";
        }
    }
}
