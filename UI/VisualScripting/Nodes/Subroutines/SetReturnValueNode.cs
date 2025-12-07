using System;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// Set the return value in a function
    /// Used within a FUNCTION block to set the value that will be returned
    /// </summary>
    public class SetReturnValueNode : NodeBase
    {
        public override string NodeType => "SetReturnValue";
        public override string Category => "Subroutines";
        public override string? Icon => "↩️";

        public SetReturnValueNode()
        {
            Label = "SET RETURN";
            Width = 190;
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

            // Input pin for the value to return
            AddInputPin("Value", DataType.Number);

            // Output execution pin - continues execution within the function
            AddOutputPin("Exec", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // SET RETURN must be inside a function definition
            // This validation will be done by the graph validator
            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // The code generator will handle getting the value from the input pin
            // and generating the appropriate RETURN statement
            return "' Set return value";
        }
    }
}
