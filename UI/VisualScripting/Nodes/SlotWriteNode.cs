using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for writing a property to a device slot
    /// Generates: device.Slot(index).PropertyName = value
    /// </summary>
    public class SlotWriteNode : NodeBase
    {
        public override string NodeType => "SlotWrite";
        public override string Category => "Devices";
        public override string? Icon => "📝";

        /// <summary>
        /// The slot property to write
        /// </summary>
        public string PropertyName { get; set; } = "Occupied";

        public SlotWriteNode()
        {
            Label = "Write Slot";
            Width = 200;
            Height = 140;
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

            // Add input pins for device, slot index, and value
            AddInputPin("Device", DataType.Device);
            AddInputPin("SlotIndex", DataType.Number);
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

            // Check if slot index is connected
            var slotPin = InputPins.Find(p => p.Name == "SlotIndex");
            if (slotPin != null && !slotPin.IsConnected)
            {
                errorMessage = "SlotIndex input must be connected";
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

        public override string GenerateCode()
        {
            // Note: Actual device name, index, and value would be determined by connected nodes
            return $"device.Slot(index).{PropertyName} = value";
        }

        /// <summary>
        /// Get a list of common writable slot properties for UI dropdown
        /// </summary>
        public static string[] GetCommonWritableSlotProperties()
        {
            return new[]
            {
                "Occupied",
                "Quantity",
                "Damage",
                "Health",
                "Growth"
            };
        }
    }
}
