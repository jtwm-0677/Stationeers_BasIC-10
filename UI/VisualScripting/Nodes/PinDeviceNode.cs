using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for declaring a device alias for a physical pin (d0-d5)
    /// Generates: ALIAS aliasName dN
    /// </summary>
    public class PinDeviceNode : NodeBase
    {
        public override string NodeType => "PinDevice";
        public override string Category => "Devices";
        public override string? Icon => "🔌";

        private string _aliasName = "device";
        private int _pinNumber = 0;

        /// <summary>
        /// The alias name for the device
        /// </summary>
        public string AliasName
        {
            get => _aliasName;
            set
            {
                _aliasName = value;
                Label = $"ALIAS {_aliasName} d{_pinNumber}";
                OnPropertyValueChanged(nameof(AliasName), value);
            }
        }

        /// <summary>
        /// The pin number (0-5)
        /// </summary>
        public int PinNumber
        {
            get => _pinNumber;
            set
            {
                _pinNumber = Math.Clamp(value, 0, 5);
                Label = $"ALIAS {_aliasName} d{_pinNumber}";
                // Use "dN" format to match dropdown items
                OnPropertyValueChanged(nameof(PinNumber), $"d{_pinNumber}");
            }
        }

        public PinDeviceNode()
        {
            Label = "ALIAS device d0";
            Width = 200;
            Height = 130;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Alias Name", nameof(AliasName), PropertyType.Text, value => AliasName = value)
                {
                    Value = AliasName,
                    Placeholder = "Enter device alias...",
                    Tooltip = "Name to reference this device"
                },
                new NodeProperty("Pin Number", nameof(PinNumber), PropertyType.Dropdown, value =>
                {
                    if (int.TryParse(value.Replace("d", ""), out var pin))
                        PinNumber = pin;
                })
                {
                    Value = $"d{PinNumber}",
                    Options = new[] { "d0", "d1", "d2", "d3", "d4", "d5" },
                    Tooltip = "IC housing pin number (d0-d5)"
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add output pin for device reference
            AddOutputPin("Device", DataType.Device);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Validate alias name
            if (string.IsNullOrWhiteSpace(AliasName))
            {
                errorMessage = "Alias name cannot be empty";
                return false;
            }

            // Check for valid BASIC identifier
            if (!IsValidIdentifier(AliasName))
            {
                errorMessage = "Invalid alias name. Must start with a letter and contain only letters, numbers, and underscores.";
                return false;
            }

            // Validate pin number
            if (PinNumber < 0 || PinNumber > 5)
            {
                errorMessage = "Pin number must be between 0 and 5";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            return $"ALIAS {AliasName} d{PinNumber}";
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
