using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for declaring a device by prefab name and labeler name (bypasses 6-pin limit)
    /// Generates: ALIAS aliasName = IC.Device[PrefabName].Name["DeviceName"]
    /// </summary>
    public class NamedDeviceNode : NodeBase
    {
        public override string NodeType => "NamedDevice";
        public override string Category => "Devices";
        public override string? Icon => "📡";

        /// <summary>
        /// The alias name for the device (variable name used in code)
        /// </summary>
        public string AliasName { get; set; } = "device";

        /// <summary>
        /// The prefab name of the device (e.g., "StructureActiveVent")
        /// </summary>
        public string PrefabName { get; set; } = "StructureActiveVent";

        /// <summary>
        /// The labeler name of the device (in-game label set by Labeler tool)
        /// </summary>
        public string DeviceName { get; set; } = "My Device";

        public NamedDeviceNode()
        {
            Label = "Named Device";
            Width = 240;
            Height = 130;
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

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Alias Name", nameof(AliasName), PropertyType.Text, value => AliasName = value)
                {
                    Value = AliasName,
                    Placeholder = "e.g., myVent",
                    Tooltip = "Variable name to reference this device in code"
                },
                new NodeProperty("Device Type", nameof(PrefabName), PropertyType.Text, value => PrefabName = value)
                {
                    Value = PrefabName,
                    Placeholder = "e.g., StructureActiveVent",
                    Tooltip = "Stationeers prefab name (e.g., StructureActiveVent, StructureSolarPanel)"
                },
                new NodeProperty("Device Label", nameof(DeviceName), PropertyType.Text, value => DeviceName = value)
                {
                    Value = DeviceName,
                    Placeholder = "e.g., Main Airlock Vent",
                    Tooltip = "In-game label set by the Labeler tool"
                }
            };
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

            // Validate prefab name
            if (string.IsNullOrWhiteSpace(PrefabName))
            {
                errorMessage = "Device type cannot be empty";
                return false;
            }

            // Validate device label
            if (string.IsNullOrWhiteSpace(DeviceName))
            {
                errorMessage = "Device label cannot be empty";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Generate: ALIAS aliasName = IC.Device[PrefabName].Name["DeviceName"]
            return $"ALIAS {AliasName} = IC.Device[{PrefabName}].Name[\"{DeviceName}\"]";
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
