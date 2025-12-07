using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes.FlowControl
{
    /// <summary>
    /// GOTO node - unconditional jump to a label
    /// Flow terminates and jumps to the target label
    /// </summary>
    public class GotoNode : NodeBase
    {
        public override string NodeType => "Goto";
        public override string Category => "Flow Control";
        public override string? Icon => "➡";

        private string _targetLabel = "MyLabel";

        /// <summary>
        /// Target label name
        /// </summary>
        public string TargetLabel
        {
            get => _targetLabel;
            set
            {
                _targetLabel = value;
                Label = $"GOTO {_targetLabel}";
                OnPropertyValueChanged(nameof(TargetLabel), value);
            }
        }

        public GotoNode()
        {
            Label = "GOTO MyLabel";
            Width = 180;
            Height = 100;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Target Label", nameof(TargetLabel), PropertyType.Text, value => TargetLabel = value)
                {
                    Value = TargetLabel,
                    Placeholder = "Enter target label...",
                    Tooltip = "The label to jump to"
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Input execution pin only - no output as flow jumps elsewhere
            AddInputPin("Exec", DataType.Execution);

            // Update label display
            Label = $"GOTO {TargetLabel}";

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check target label name
            if (string.IsNullOrWhiteSpace(TargetLabel))
            {
                errorMessage = "Target label name cannot be empty";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"GOTO {TargetLabel}";
        }
    }
}
