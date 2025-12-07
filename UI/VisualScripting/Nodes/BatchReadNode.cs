using System;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for batch reading a property from all devices of a type
    /// Generates: BATCHREAD(hash, Property, mode)
    /// </summary>
    public class BatchReadNode : NodeBase
    {
        public override string NodeType => "BatchRead";
        public override string Category => "Devices";
        public override string? Icon => "📊";

        /// <summary>
        /// The property to read (e.g., "On", "Temperature", "Pressure")
        /// </summary>
        public string PropertyName { get; set; } = "Temperature";

        /// <summary>
        /// The batch mode: Average=0, Sum=1, Min=2, Max=3
        /// </summary>
        public BatchMode Mode { get; set; } = BatchMode.Average;

        public BatchReadNode()
        {
            Label = "Batch Read";
            Width = 200;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add input pin for device hash
            AddInputPin("DeviceHash", DataType.Number);

            // Add output pin for the result value
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

            // Check if device hash input is connected
            if (InputPins.Count > 0 && !InputPins[0].IsConnected)
            {
                errorMessage = "DeviceHash input must be connected";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Note: Actual hash value would be determined by connected node
            int modeValue = (int)Mode;
            return $"BATCHREAD(hash, {PropertyName}, {modeValue})";
        }
    }

    /// <summary>
    /// Batch read operation modes
    /// </summary>
    public enum BatchMode
    {
        Average = 0,
        Sum = 1,
        Minimum = 2,
        Maximum = 3
    }
}
