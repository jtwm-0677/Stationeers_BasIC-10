using System;
using BasicToMips.Data;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Node for generating a hash value from a string (for device/prefab names)
    /// Can either generate at compile-time or create a DEFINE statement
    /// </summary>
    public class HashNode : NodeBase
    {
        public override string NodeType => "Hash";
        public override string Category => "Devices";
        public override string? Icon => "#️⃣";

        /// <summary>
        /// The string value to hash (device name, prefab name, etc.)
        /// </summary>
        public string StringValue { get; set; } = "StructureActiveVent";

        /// <summary>
        /// Whether to create a DEFINE constant or just output the hash value
        /// </summary>
        public bool CreateDefine { get; set; } = false;

        /// <summary>
        /// The name for the DEFINE constant (if CreateDefine is true)
        /// </summary>
        public string DefineName { get; set; } = "DEVICE_HASH";

        /// <summary>
        /// Cached hash value
        /// </summary>
        private int _cachedHash = 0;

        public HashNode()
        {
            Label = "Hash";
            Width = 220;
            Height = 100;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Clear existing pins
            InputPins.Clear();
            OutputPins.Clear();

            // Add output pin for hash value
            AddOutputPin("Hash", DataType.Number);

            // Calculate hash if we have a value
            if (!string.IsNullOrWhiteSpace(StringValue))
            {
                _cachedHash = DeviceDatabase.CalculateHash(StringValue);
            }

            // Calculate height
            Height = CalculateMinHeight();
        }

        public override bool Validate(out string errorMessage)
        {
            // Validate string value
            if (string.IsNullOrWhiteSpace(StringValue))
            {
                errorMessage = "String value cannot be empty";
                return false;
            }

            // If creating a DEFINE, validate the name
            if (CreateDefine)
            {
                if (string.IsNullOrWhiteSpace(DefineName))
                {
                    errorMessage = "DEFINE name cannot be empty";
                    return false;
                }

                // Check for valid BASIC identifier
                if (!IsValidIdentifier(DefineName))
                {
                    errorMessage = "Invalid DEFINE name. Must start with a letter and contain only letters, numbers, and underscores.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public override string GenerateCode()
        {
            // Calculate hash
            int hash = DeviceDatabase.CalculateHash(StringValue);

            if (CreateDefine)
            {
                // Generate DEFINE statement
                return $"DEFINE {DefineName} {hash}";
            }
            else
            {
                // Just output the hash value as a comment for reference
                return $"# Hash of '{StringValue}': {hash}";
            }
        }

        /// <summary>
        /// Get the computed hash value for this node
        /// </summary>
        public int GetHashValue()
        {
            if (_cachedHash == 0 && !string.IsNullOrWhiteSpace(StringValue))
            {
                _cachedHash = DeviceDatabase.CalculateHash(StringValue);
            }
            return _cachedHash;
        }

        /// <summary>
        /// Try to lookup a device by prefab name and get its hash
        /// </summary>
        public bool TryGetDeviceHash(out int hash)
        {
            hash = DeviceDatabase.GetDeviceHash(StringValue);
            return true;
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
