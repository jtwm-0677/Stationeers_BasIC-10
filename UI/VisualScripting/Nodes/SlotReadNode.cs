using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for reading a property from a device slot
    /// Generates: device.Slot(index).PropertyName
    /// </summary>
    public class SlotReadNode : NodeBase
    {
        public override string NodeType => "SlotRead";
        public override string Category => "Devices";
        public override string? Icon => "📦";

        /// <summary>
        /// The slot property to read
        /// </summary>
        public string PropertyName { get; set; } = "Occupied";

        public SlotReadNode()
        {
            Label = "Read Slot";
            Width = 200;
            Height = 120;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pins for device and slot index
            AddInputPin("Device", DataType.Device);
            AddInputPin("SlotIndex", DataType.Number);

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
            var devicePin = InputPins.Find(p => p.DataType == DataType.Device);
            if (devicePin != null && !devicePin.IsConnected)
            {
                errorMessage = "Device input must be connected";
                return false;
            }

            // Check if slot index is connected
            var slotPin = InputPins.Find(p => p.Name == "SlotIndex");
            if (slotPin != null && !slotPin.IsConnected)
            {
                errorMessage = "SlotIndex input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Note: Actual device name and index would be determined by connected nodes
            return $"device.Slot(index).{PropertyName}";
        }

        /// <summary>
        /// Get a list of common slot properties for UI dropdown
        /// </summary>
        public static string[] GetCommonSlotProperties()
        {
            return new[]
            {
                "Occupied",
                "OccupantHash",
                "Quantity",
                "Damage",
                "Efficiency",
                "Health",
                "Growth",
                "Pressure",
                "Temperature",
                "Class",
                "PrefabHash",
                "MaxQuantity"
            };
        }
    }
}
