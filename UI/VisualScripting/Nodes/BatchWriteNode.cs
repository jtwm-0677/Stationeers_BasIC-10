using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for batch writing a property to all devices of a type
    /// Generates: BATCHWRITE(hash, Property, value)
    /// </summary>
    public class BatchWriteNode : NodeBase
    {
        public override string NodeType => "BatchWrite";
        public override string Category => "Devices";
        public override string? Icon => "📢";

        /// <summary>
        /// The property to write (e.g., "On", "Setting", "Mode")
        /// </summary>
        public string PropertyName { get; set; } = "On";

        public BatchWriteNode()
        {
            Label = "Batch Write";
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

            // Add input pins for device hash and value
            AddInputPin("DeviceHash", DataType.Number);
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

            // Check if device hash input is connected
            var hashPin = InputPins.Find(p => p.Name == "DeviceHash");
            if (hashPin != null && !hashPin.IsConnected)
            {
                errorMessage = "DeviceHash input must be connected";
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
            // Note: Actual hash and value would be determined by connected nodes
            return $"BATCHWRITE(hash, {PropertyName}, value)";
        }
    }
}
