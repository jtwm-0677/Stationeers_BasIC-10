using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// Call a function defined by FunctionDefinitionNode
    /// Generates function call and provides result as output
    /// </summary>
    public class CallFunctionNode : NodeBase
    {
        public override string NodeType => "CallFunction";
        public override string Category => "Subroutines";
        public override string? Icon => "📱";

        /// <summary>
        /// Target function name to call
        /// </summary>
        public string TargetFunction { get; set; } = "MyFunction";

        public CallFunctionNode()
        {
            Label = "CALL FUNCTION";
            Width = 220;
            Height = 110;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin
            AddInputPin("Exec", DataType.Execution);

            // Output execution pin - continues after function returns
            AddOutputPin("Exec", DataType.Execution);

            // Output pin for the function's return value
            AddOutputPin("Result", DataType.Number);

            // Update label display
            Label = $"CALL {TargetFunction}()";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check target function name
            if (string.IsNullOrWhiteSpace(TargetFunction))
            {
                errorMessage = "Target function name cannot be empty";
                return false;
            }

            // Validate that the function exists (checked by registry)
            if (!SubroutineRegistry.Instance.ValidateCall(TargetFunction, true))
            {
                errorMessage = $"Function '{TargetFunction}' is not defined";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // This will be used inline in expressions or as assignment
            return $"{TargetFunction}()";
        }

        /// <summary>
        /// Get available functions for dropdown
        /// </summary>
        public List<string> GetAvailableFunctions()
        {
            return SubroutineRegistry.Instance.GetDefinedFunctions();
        }
    }
}
