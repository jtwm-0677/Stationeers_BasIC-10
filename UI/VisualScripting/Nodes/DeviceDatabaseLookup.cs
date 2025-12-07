using System;
using System.Collections.Generic;
using System.Linq;
using BasicToMips.Data;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Helper class for providing device database lookups and autocomplete suggestions
    /// This is not a node itself, but a utility class used by device-related nodes
    /// </summary>
    public static class DeviceDatabaseLookup
    {
        /// <summary>
        /// Cache for autocomplete suggestions to improve performance
        /// </summary>
        private static Dictionary<string, List<string>>? _cachedSuggestions;

        /// <summary>
        /// Get autocomplete suggestions for device prefab names
        /// </summary>
        /// <param name="query">Search query (partial name)</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>List of matching prefab names</returns>
        public static List<string> GetDevicePrefabSuggestions(string query, int maxResults = 20)
        {
            var devices = DeviceDatabase.SearchDevices(query ?? "");
            return devices
                .Take(maxResults)
                .Select(d => d.PrefabName)
                .ToList();
        }

        /// <summary>
        /// Get autocomplete suggestions with display names
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <returns>List of tuples (PrefabName, DisplayName, Category)</returns>
        public static List<(string PrefabName, string DisplayName, string Category)> GetDeviceInfoSuggestions(string query, int maxResults = 20)
        {
            var devices = DeviceDatabase.SearchDevices(query ?? "");
            return devices
                .Take(maxResults)
                .Select(d => (d.PrefabName, d.DisplayName, d.Category))
                .ToList();
        }

        /// <summary>
        /// Get all device categories
        /// </summary>
        public static List<string> GetCategories()
        {
            return DeviceDatabase.Devices
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        /// <summary>
        /// Get all devices in a specific category
        /// </summary>
        public static List<DeviceInfo> GetDevicesByCategory(string category)
        {
            return DeviceDatabase.Devices
                .Where(d => d.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.DisplayName)
                .ToList();
        }

        /// <summary>
        /// Get the hash for a device prefab name
        /// </summary>
        public static int GetDeviceHash(string prefabName)
        {
            return DeviceDatabase.GetDeviceHash(prefabName);
        }

        /// <summary>
        /// Get autocomplete suggestions for logic types (device properties)
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="maxResults">Maximum number of results</param>
        /// <returns>List of matching logic type names</returns>
        public static List<string> GetLogicTypeSuggestions(string query, int maxResults = 30)
        {
            var logicTypes = DeviceDatabase.SearchLogicTypes(query ?? "");
            return logicTypes
                .Take(maxResults)
                .Select(l => l.Name)
                .ToList();
        }

        /// <summary>
        /// Get all available logic types (device properties)
        /// </summary>
        public static List<string> GetAllLogicTypes()
        {
            return DeviceDatabase.LogicTypes
                .Select(l => l.Name)
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Get all available slot logic types
        /// </summary>
        public static List<string> GetAllSlotLogicTypes()
        {
            return DeviceDatabase.SlotLogicTypes
                .Select(l => l.Name)
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>
        /// Get batch modes with their values
        /// </summary>
        public static List<(string Name, int Value, string Description)> GetBatchModes()
        {
            return DeviceDatabase.BatchModes
                .Select(b => (b.Name, b.Value, b.Description))
                .ToList();
        }

        /// <summary>
        /// Get device information by prefab name
        /// </summary>
        public static DeviceInfo? GetDeviceInfo(string prefabName)
        {
            return DeviceDatabase.Devices
                .FirstOrDefault(d => d.PrefabName.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validate if a prefab name exists in the database
        /// </summary>
        public static bool IsValidPrefabName(string prefabName)
        {
            return DeviceDatabase.Devices
                .Any(d => d.PrefabName.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Validate if a logic type name exists
        /// </summary>
        public static bool IsValidLogicType(string logicTypeName)
        {
            return DeviceDatabase.LogicTypes
                .Any(l => l.Name.Equals(logicTypeName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get common properties for all device types
        /// These are the most frequently used properties in visual scripting
        /// </summary>
        public static List<string> GetCommonProperties()
        {
            return new List<string>
            {
                // Universal
                "On", "Power", "Error", "Lock", "PrefabHash", "ReferenceId", "NameHash",

                // Atmospheric
                "Temperature", "Pressure", "RatioOxygen", "RatioCarbonDioxide",
                "RatioNitrogen", "RatioPollutant", "RatioVolatiles", "RatioWater", "TotalMoles",

                // Control
                "Setting", "Mode", "Open", "Ratio", "Activate",

                // Power
                "Charge", "PowerGeneration", "PowerRequired", "PowerActual", "MaxPower",

                // Display
                "Color", "Horizontal", "Vertical",

                // Logic
                "Quantity", "Occupied", "Output", "Input"
            };
        }

        /// <summary>
        /// Get common slot properties
        /// </summary>
        public static List<string> GetCommonSlotProperties()
        {
            return new List<string>
            {
                "Occupied", "OccupantHash", "Quantity", "Damage", "Efficiency",
                "Health", "Growth", "Pressure", "Temperature", "Class",
                "PrefabHash", "MaxQuantity"
            };
        }

        /// <summary>
        /// Clear cached data (call if database is reloaded)
        /// </summary>
        public static void ClearCache()
        {
            _cachedSuggestions?.Clear();
            _cachedSuggestions = null;
        }
    }
}
