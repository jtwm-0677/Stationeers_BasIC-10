using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A node for declaring and setting variables
    /// </summary>
    public class VariableNode : NodeBase
    {
        public override string NodeType => "Variable";
        public override string Category => "Variables";
        public override string? Icon => "📦";

        private string _variableName = "myVar";
        private string _initialValue = "0";
        private bool _isDeclaration = true;

        /// <summary>
        /// Variable name
        /// </summary>
        public string VariableName
        {
            get => _variableName;
            set
            {
                _variableName = value;
                Label = _isDeclaration ? $"VAR {_variableName}" : $"LET {_variableName}";
                OnPropertyValueChanged(nameof(VariableName), value);
            }
        }

        /// <summary>
        /// Variable type (Number, String, etc.)
        /// </summary>
        public string VariableType { get; set; } = "Number";

        /// <summary>
        /// Initial value (optional)
        /// </summary>
        public string InitialValue
        {
            get => _initialValue;
            set
            {
                _initialValue = value;
                OnPropertyValueChanged(nameof(InitialValue), value);
            }
        }

        /// <summary>
        /// Whether this is a declaration (VAR) or assignment (LET)
        /// </summary>
        public bool IsDeclaration
        {
            get => _isDeclaration;
            set
            {
                _isDeclaration = value;
                Label = _isDeclaration ? $"VAR {_variableName}" : $"LET {_variableName}";
                OnPropertyValueChanged(nameof(IsDeclaration), value.ToString());
            }
        }

        public VariableNode()
        {
            Label = "VAR myVar";
            Width = 200;
            Height = 150;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Variable Name", nameof(VariableName), PropertyType.Text, value => VariableName = value)
                {
                    Value = VariableName,
                    Placeholder = "Enter variable name...",
                    Tooltip = "Name of the variable"
                },
                new NodeProperty("Initial Value", nameof(InitialValue), PropertyType.Text, value => InitialValue = value)
                {
                    Value = InitialValue,
                    Placeholder = "Enter initial value...",
                    Tooltip = "Initial value for the variable"
                },
                new NodeProperty("Is Declaration", nameof(IsDeclaration), PropertyType.Boolean, value => IsDeclaration = value.ToLower() == "true")
                {
                    Value = IsDeclaration.ToString(),
                    Tooltip = "Check for VAR (declaration), uncheck for LET (assignment)"
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear any existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add execution pins
            AddInputPin("In", DataType.Execution);
            AddOutputPin("Out", DataType.Execution);

            // If this is an assignment, add value input
            if (!IsDeclaration)
            {
                AddInputPin("Value", DataType.Number);
            }

            // Add variable output (for reading the variable)
            AddOutputPin(VariableName, DataType.Number);

            // Calculate height based on pins
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Check variable name
            if (string.IsNullOrWhiteSpace(VariableName))
            {
                errorMessage = "Variable name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(VariableName))
            {
                errorMessage = "Invalid variable name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            // Check initial value if declaration
            if (IsDeclaration && VariableType == "Number" && !string.IsNullOrWhiteSpace(InitialValue))
            {
                if (!double.TryParse(InitialValue, out _))
                {
                    errorMessage = "Initial value must be a valid number";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            if (IsDeclaration)
            {
                // Generate VAR statement
                if (!string.IsNullOrWhiteSpace(InitialValue))
                {
                    return $"VAR {VariableName} = {InitialValue}";
                }
                else
                {
                    return $"VAR {VariableName}";
                }
            }
            else
            {
                // Generate LET statement
                // Note: Actual value would come from connected node
                return $"LET {VariableName} = {InitialValue}";
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
