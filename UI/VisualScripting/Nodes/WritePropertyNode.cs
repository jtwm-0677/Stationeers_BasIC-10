using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for writing a property to a device
    /// Generates: device.PropertyName = value
    /// </summary>
    public class WritePropertyNode : NodeBase
    {
        public override string NodeType => "WriteProperty";
        public override string Category => "Devices";
        public override string? Icon => "✏️";

        /// <summary>
        /// The property to write (e.g., "On", "Setting", "Mode")
        /// </summary>
        public string PropertyName { get; set; } = "On";

        public WritePropertyNode()
        {
            Label = "Write Property";
            Width = 200;
            Height = 120;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add execution pins
            AddInputPin("In", DataType.Execution);
            AddOutputPin("Out", DataType.Execution);

            // Add input pins for device and value
            AddInputPin("Device", DataType.Device);
            AddInputPin("Value", DataType.Number);

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Validate property name
            if (string.IsNullOrWhiteSpace(PropertyName))
            {
                errorMessage = "Property name cannot be empty";
                return false;
            }

            // Check if device input is connected
            var devicePin = InputPins.Find(p => p.DataType == DataType.Device);
            if (devicePin != null && !devicePin.IsConnected)
            {
                errorMessage = "Device input must be connected";
                return false;
            }

            // Check if value input is connected
            var valuePin = InputPins.Find(p => p.Name == "Value");
            if (valuePin != null && !valuePin.IsConnected)
            {
                errorMessage = "Value input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Property", nameof(PropertyName), PropertyType.Dropdown, value =>
                {
                    PropertyName = value;
                    Label = $"Write {value}";
                })
                {
                    Value = PropertyName,
                    Options = GetCommonWritableProperties(),
                    Tooltip = "The device property to write (e.g., On, Setting, Mode)"
                }
            };
        }

        public override string GenerateCode()
        {
            // Note: Actual device name and value would be determined by connected nodes
            return $"device.{PropertyName} = value";
        }

        /// <summary>
        /// Get a list of common writable device properties for UI dropdown
        /// </summary>
        public static string[] GetCommonWritableProperties()
        {
            return new[]
            {
                // Universal properties
                "On",
                "Lock",

                // Control properties
                "Setting",
                "Mode",
                "Open",
                "Ratio",
                "Activate",

                // Display properties
                "Color",
                "Horizontal",
                "Vertical",

                // Logic properties
                "Output"
            };
        }
    }
}
