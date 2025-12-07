using System;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// FOR/NEXT loop node
    /// Iterates from start to end with optional step
    /// </summary>
    public class ForNode : NodeBase
    {
        public override string NodeType => "For";
        public override string Category => "Flow Control";
        public override string? Icon => "🔢";

        /// <summary>
        /// Loop variable name
        /// </summary>
        public string VariableName { get; set; } = "i";

        /// <summary>
        /// Step value (default 1)
        /// </summary>
        public double Step { get; set; } = 1.0;

        /// <summary>
        /// Whether to automatically insert YIELD in the loop
        /// </summary>
        public bool AutoYield { get; set; } = true;

        public ForNode()
        {
            Label = "FOR";
            Width = 200;
            Height = 140;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input pins
            AddInputPin("Exec", DataType.Execution);
            AddInputPin("Start", DataType.Number);
            AddInputPin("End", DataType.Number);

            // Output pins
            AddOutputPin("LoopBody", DataType.Execution);
            AddOutputPin("Done", DataType.Execution);
            AddOutputPin("Index", DataType.Number); // Current loop value

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check variable name
            if (string.IsNullOrWhiteSpace(VariableName))
            {
                errorMessage = "Loop variable name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(VariableName))
            {
                errorMessage = "Invalid variable name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            // Check if start/end inputs are connected
            var startPin = InputPins.Find(p => p.Name == "Start");
            var endPin = InputPins.Find(p => p.Name == "End");

            if (startPin == null || !startPin.IsConnected)
            {
                errorMessage = "Start input must be connected";
                return false;
            }

            if (endPin == null || !endPin.IsConnected)
            {
                errorMessage = "End input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Code generation is handled by GraphToBasicGenerator
            // which will generate the FOR/NEXT block structure
            return $"FOR {VariableName} = start TO end STEP {Step}";
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
