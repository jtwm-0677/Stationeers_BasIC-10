using System;
using System.Collections.Generic;
using System.Linq;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// SELECT CASE node - switch/case statement
    /// Provides multi-way branching based on a value
    /// </summary>
    public class SelectCaseNode : NodeBase
    {
        public override string NodeType => "SelectCase";
        public override string Category => "Flow Control";
        public override string? Icon => "🔀";

        /// <summary>
        /// List of case values
        /// </summary>
        public List<int> CaseValues { get; set; } = new List<int> { 0, 1, 2 };

        public SelectCaseNode()
        {
            Label = "SELECT CASE";
            Width = 220;
            Height = 160;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input pins
            AddInputPin("Exec", DataType.Execution);
            AddInputPin("Value", DataType.Number);

            // Add output pin for each case
            foreach (var caseValue in CaseValues)
            {
                AddOutputPin($"Case {caseValue}", DataType.Execution);
            }

            // Default and Done output pins
            AddOutputPin("Default", DataType.Execution);
            AddOutputPin("Done", DataType.Execution);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check if value input is connected
            var valuePin = InputPins.Find(p => p.Name == "Value");
            if (valuePin == null || !valuePin.IsConnected)
            {
                errorMessage = "Value input must be connected";
                return false;
            }

            // Check for duplicate case values
            if (CaseValues.Distinct().Count() != CaseValues.Count)
            {
                errorMessage = "Duplicate case values are not allowed";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Code generation is handled by GraphToBasicGenerator
            // which will generate the SELECT CASE/END SELECT block structure
            return "SELECT CASE value";
        }

        /// <summary>
        /// Add a new case value
        /// </summary>
        public void AddCase(int value)
        {
            if (!CaseValues.Contains(value))
            {
                CaseValues.Add(value);
                Initialize(); // Rebuild pins
            }
        }

        /// <summary>
        /// Remove a case value
        /// </summary>
        public void RemoveCase(int value)
        {
            if (CaseValues.Remove(value))
            {
                Initialize(); // Rebuild pins
            }
        }
    }
}
