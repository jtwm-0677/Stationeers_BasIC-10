using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes.Subroutines
{
    /// <summary>
    /// Call a subroutine defined by SubDefinitionNode
    /// Generates CALL statement and continues execution after subroutine returns
    /// </summary>
    public class CallSubNode : NodeBase
    {
        public override string NodeType => "CallSub";
        public override string Category => "Subroutines";
        public override string? Icon => "📞";

        /// <summary>
        /// Target subroutine name to call
        /// </summary>
        public string TargetSubroutine { get; set; } = "MySubroutine";

        public CallSubNode()
        {
            Label = "CALL SUB";
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

            // Output execution pin - continues after subroutine returns
            AddOutputPin("Exec", DataType.Execution);

            // Update label display
            Label = $"CALL {TargetSubroutine}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check target subroutine name
            if (string.IsNullOrWhiteSpace(TargetSubroutine))
            {
                errorMessage = "Target subroutine name cannot be empty";
                return false;
            }

            // Validate that the subroutine exists (checked by registry)
            if (!SubroutineRegistry.Instance.ValidateCall(TargetSubroutine, false))
            {
                errorMessage = $"Subroutine '{TargetSubroutine}' is not defined";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"CALL {TargetSubroutine}";
        }

        /// <summary>
        /// Get available subroutines for dropdown
        /// </summary>
        public List<string> GetAvailableSubroutines()
        {
            return SubroutineRegistry.Instance.GetDefinedSubroutines();
        }
    }
}
