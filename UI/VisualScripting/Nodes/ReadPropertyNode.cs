using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for reading a property from a device
    /// Generates: device.PropertyName
    /// </summary>
    public class ReadPropertyNode : NodeBase
    {
        public override string NodeType => "ReadProperty";
        public override string Category => "Devices";
        public override string? Icon => "📖";

        /// <summary>
        /// The property to read (e.g., "On", "Temperature", "Pressure")
        /// </summary>
        public string PropertyName { get; set; } = "On";

        public ReadPropertyNode()
        {
            Label = "Read Property";
            Width = 200;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pin for device reference
            AddInputPin("Device", DataType.Device);

            // Add output pin for the value
            AddOutputPin("Value", DataType.Number);

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
            if (InputPins.Count > 0 && !InputPins[0].IsConnected)
            {
                errorMessage = "Device input must be connected";
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
                    Label = $"Read {value}";
                })
                {
                    Value = PropertyName,
                    Options = GetCommonProperties(),
                    Tooltip = "The device property to read (e.g., Temperature, Pressure, On)"
                }
            };
        }

        public override string GenerateCode()
        {
            // Note: Actual device name would be determined by connected node
            return $"device.{PropertyName}";
        }

        /// <summary>
        /// Get a list of common device properties for UI dropdown
        /// </summary>
        public static string[] GetCommonProperties()
        {
            return new[]
            {
                // Universal properties
                "On",
                "Power",
                "Error",
                "Lock",
                "PrefabHash",
                "ReferenceId",
                "NameHash",

                // Atmospheric properties
                "Temperature",
                "Pressure",
                "RatioOxygen",
                "RatioCarbonDioxide",
                "RatioNitrogen",
                "RatioPollutant",
                "RatioVolatiles",
                "RatioWater",
                "TotalMoles",

                // Valve/Pump properties
                "Setting",
                "Mode",
                "Open",
                "Ratio",

                // Power properties
                "Charge",
                "PowerGeneration",
                "PowerRequired",
                "PowerActual",
                "MaxPower",

                // Display properties
                "Color",
                "Horizontal",
                "Vertical",

                // Logic properties
                "Activate",
                "Quantity",
                "Occupied",
                "Output",
                "Input"
            };
        }
    }
}
