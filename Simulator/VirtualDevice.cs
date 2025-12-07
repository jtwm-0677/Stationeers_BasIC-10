using System;
using System.Collections.Generic;

namespace BasicToMips.Simulator
{
    /// <summary>
    /// Virtual device for simulator - represents a device referenced by alias name
    /// </summary>
    public class VirtualDevice
    {
        /// <summary>
        /// The alias name for this device (e.g., "sensor", "furnace")
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// The prefab name (e.g., "StructureGasSensor", "StructureFurnace")
        /// </summary>
        public string PrefabName { get; set; }

        /// <summary>
        /// The hash of the prefab name
        /// </summary>
        public int Hash { get; private set; }

        /// <summary>
        /// Device properties (e.g., Temperature, Pressure, On, etc.)
        /// </summary>
        public Dictionary<string, double> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Slot properties (for devices with slots)
        /// </summary>
        public Dictionary<int, Dictionary<string, double>> Slots { get; } = new();

        public VirtualDevice(string alias, string prefabName)
        {
            Alias = alias;
            PrefabName = prefabName;
            Hash = GetPrefabHash(prefabName);
            InitializeDefaultProperties();
        }

        /// <summary>
        /// Initialize default properties based on common Stationeers device properties
        /// </summary>
        private void InitializeDefaultProperties()
        {
            // Common device properties with sensible defaults
            Properties["On"] = 0;
            Properties["Setting"] = 0;
            Properties["Mode"] = 0;
            Properties["Open"] = 0;
            Properties["Lock"] = 0;
            Properties["Error"] = 0;
            Properties["Power"] = 1;
            Properties["Temperature"] = 293.15; // 20C in Kelvin
            Properties["Pressure"] = 101.325;   // 1 atm in kPa
            Properties["Charge"] = 1;
            Properties["Activate"] = 0;
            Properties["ClearMemory"] = 0;
            Properties["Horizontal"] = 0;
            Properties["Vertical"] = 0;
            Properties["Output"] = 0;
            Properties["PrefabHash"] = GetPrefabHash(PrefabName);

            // Device-specific defaults
            if (PrefabName.Contains("Sensor", StringComparison.OrdinalIgnoreCase))
            {
                Properties["Temperature"] = 293.15;
                Properties["Pressure"] = 101.325;
            }
            else if (PrefabName.Contains("Solar", StringComparison.OrdinalIgnoreCase))
            {
                Properties["Horizontal"] = 0;
                Properties["Vertical"] = 45;
                Properties["Power"] = 500;
            }
            else if (PrefabName.Contains("Battery", StringComparison.OrdinalIgnoreCase))
            {
                Properties["Charge"] = 0.5;
                Properties["Power"] = 5000;
            }
            else if (PrefabName.Contains("Furnace", StringComparison.OrdinalIgnoreCase))
            {
                Properties["Temperature"] = 293.15;
                Properties["Pressure"] = 101.325;
                Properties["On"] = 0;
            }
        }

        /// <summary>
        /// Get property value with default fallback
        /// </summary>
        public double GetProperty(string name)
        {
            return Properties.TryGetValue(name, out double value) ? value : 0;
        }

        /// <summary>
        /// Set property value
        /// </summary>
        public void SetProperty(string name, double value)
        {
            Properties[name] = value;
        }

        /// <summary>
        /// Get all properties as a dictionary
        /// </summary>
        public Dictionary<string, double> GetAllProperties()
        {
            return new Dictionary<string, double>(Properties);
        }

        /// <summary>
        /// Get slot property
        /// </summary>
        public double GetSlotProperty(int slot, string name)
        {
            if (Slots.TryGetValue(slot, out var slotProps) &&
                slotProps.TryGetValue(name, out double value))
            {
                return value;
            }
            return 0;
        }

        /// <summary>
        /// Set slot property
        /// </summary>
        public void SetSlotProperty(int slot, string name, double value)
        {
            if (!Slots.ContainsKey(slot))
            {
                Slots[slot] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            }
            Slots[slot][name] = value;
        }

        /// <summary>
        /// Get hash code for a prefab name (simplified version for simulation)
        /// </summary>
        private static int GetPrefabHash(string prefabName)
        {
            // This is a simplified hash - in real Stationeers, these are specific values
            // For simulation purposes, we'll use the string hash
            return prefabName.GetHashCode();
        }
    }
}
