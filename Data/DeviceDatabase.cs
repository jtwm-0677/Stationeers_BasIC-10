using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasicToMips.Data;

public class DeviceDatabase
{
    public static List<DeviceInfo> Devices { get; } = new();
    public static List<LogicType> LogicTypes { get; } = new();
    public static List<SlotLogicType> SlotLogicTypes { get; } = new();
    public static List<BatchMode> BatchModes { get; } = new();
    public static List<ReagentMode> ReagentModes { get; } = new();
    public static List<SortingClass> SortingClasses { get; } = new();

    private static readonly List<string> _loadedFiles = new();
    private static string? _lastLoadError;

    /// <summary>
    /// Gets the list of successfully loaded custom device files.
    /// </summary>
    public static IReadOnlyList<string> LoadedCustomFiles => _loadedFiles.AsReadOnly();

    /// <summary>
    /// Gets the last error message from loading custom devices, if any.
    /// </summary>
    public static string? LastLoadError => _lastLoadError;

    static DeviceDatabase()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var generatedDir = Path.Combine(appDir, "Data", "Generated");

        // Load from generated JSON files if available
        if (Directory.Exists(generatedDir))
        {
            LoadLogicTypesFromJson(Path.Combine(generatedDir, "LogicTypes.json"));
            LoadSlotLogicTypesFromJson(Path.Combine(generatedDir, "SlotLogicTypes.json"));
            LoadBatchModesFromJson(Path.Combine(generatedDir, "BatchModes.json"));
            LoadReagentModesFromJson(Path.Combine(generatedDir, "ReagentModes.json"));
            LoadSortingClassesFromJson(Path.Combine(generatedDir, "SortingClasses.json"));
            LoadDevicesFromJson(Path.Combine(generatedDir, "Devices.json"));
        }

        // Initialize with fallback defaults if no data loaded
        if (LogicTypes.Count == 0)
            InitializeLogicTypesDefault();
        if (SlotLogicTypes.Count == 0)
            InitializeSlotLogicTypesDefault();
        if (BatchModes.Count == 0)
            InitializeBatchModesDefault();
        if (ReagentModes.Count == 0)
            InitializeReagentModesDefault();
        if (Devices.Count == 0)
            InitializeDevicesDefault();

        // Load custom devices from JSON files
        LoadCustomDevicesFromDefaultLocations();
    }

    private static void LoadLogicTypesFromJson(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<JsonLogicType>>(json);
            if (data != null)
            {
                foreach (var item in data)
                {
                    LogicTypes.Add(new LogicType(item.Name, item.Name, "") { Hash = item.Hash, Value = item.Value });
                }
            }
        }
        catch { /* Silently fall back to defaults */ }
    }

    private static void LoadSlotLogicTypesFromJson(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<JsonLogicType>>(json);
            if (data != null)
            {
                foreach (var item in data)
                {
                    SlotLogicTypes.Add(new SlotLogicType(item.Name, item.Name, "") { Hash = item.Hash, Value = item.Value });
                }
            }
        }
        catch { /* Silently fall back to defaults */ }
    }

    private static void LoadBatchModesFromJson(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<JsonLogicType>>(json);
            if (data != null)
            {
                foreach (var item in data)
                {
                    BatchModes.Add(new BatchMode(item.Name, item.Value, ""));
                }
            }
        }
        catch { /* Silently fall back to defaults */ }
    }

    private static void LoadReagentModesFromJson(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<JsonLogicType>>(json);
            if (data != null)
            {
                foreach (var item in data)
                {
                    ReagentModes.Add(new ReagentMode(item.Name, item.Value, ""));
                }
            }
        }
        catch { /* Silently fall back to defaults */ }
    }

    private static void LoadSortingClassesFromJson(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<JsonLogicType>>(json);
            if (data != null)
            {
                foreach (var item in data)
                {
                    SortingClasses.Add(new SortingClass(item.Name, item.Value, item.Hash));
                }
            }
        }
        catch { /* Silently fall back to defaults */ }
    }

    private static void LoadDevicesFromJson(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<JsonDevice>>(json);
            if (data != null)
            {
                foreach (var item in data)
                {
                    Devices.Add(new DeviceInfo(item.PrefabName, item.Category, item.DisplayName, item.Description ?? "")
                    {
                        Hash = item.Hash
                    });
                }
            }
        }
        catch { /* Silently fall back to defaults */ }
    }

    /// <summary>
    /// Loads custom devices from default locations:
    /// 1. CustomDevices.json in application directory
    /// 2. All .json files in CustomDevices/ subdirectory
    /// </summary>
    private static void LoadCustomDevicesFromDefaultLocations()
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;

        // Try loading CustomDevices.json from app directory
        var mainFile = Path.Combine(appDir, "CustomDevices.json");
        if (File.Exists(mainFile))
        {
            LoadCustomDevicesFromFile(mainFile);
        }

        // Try loading all .json files from CustomDevices/ subdirectory
        var customDir = Path.Combine(appDir, "CustomDevices");
        if (Directory.Exists(customDir))
        {
            foreach (var file in Directory.GetFiles(customDir, "*.json"))
            {
                LoadCustomDevicesFromFile(file);
            }
        }

        // Also check user's Documents folder for portability
        var docsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var userCustomDir = Path.Combine(docsDir, "BASIC-IC10", "CustomDevices");
        if (Directory.Exists(userCustomDir))
        {
            foreach (var file in Directory.GetFiles(userCustomDir, "*.json"))
            {
                LoadCustomDevicesFromFile(file);
            }
        }
    }

    /// <summary>
    /// Loads custom devices, logic types, and slot types from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file</param>
    /// <returns>True if loaded successfully, false otherwise</returns>
    public static bool LoadCustomDevicesFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _lastLoadError = $"File not found: {filePath}";
                return false;
            }

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var customData = JsonSerializer.Deserialize<CustomDeviceData>(json, options);
            if (customData == null)
            {
                _lastLoadError = $"Failed to parse JSON: {filePath}";
                return false;
            }

            int addedCount = 0;

            // Add custom devices
            if (customData.Devices != null)
            {
                foreach (var device in customData.Devices)
                {
                    // Skip duplicates (by PrefabName)
                    if (Devices.Any(d => d.PrefabName == device.PrefabName))
                        continue;

                    var info = new DeviceInfo(
                        device.PrefabName,
                        device.Category ?? "Custom",
                        device.DisplayName ?? device.PrefabName,
                        device.Description ?? ""
                    );
                    info.Hash = CalculateHash(device.PrefabName);
                    Devices.Add(info);
                    addedCount++;
                }
            }

            // Add custom logic types
            if (customData.LogicTypes != null)
            {
                foreach (var lt in customData.LogicTypes)
                {
                    if (LogicTypes.Any(l => l.Name == lt.Name))
                        continue;

                    var info = new LogicType(
                        lt.Name,
                        lt.DisplayName ?? lt.Name,
                        lt.Description ?? ""
                    );
                    info.Hash = CalculateHash(lt.Name);
                    LogicTypes.Add(info);
                    addedCount++;
                }
            }

            // Add custom slot logic types
            if (customData.SlotLogicTypes != null)
            {
                foreach (var slt in customData.SlotLogicTypes)
                {
                    if (SlotLogicTypes.Any(s => s.Name == slt.Name))
                        continue;

                    var info = new SlotLogicType(
                        slt.Name,
                        slt.DisplayName ?? slt.Name,
                        slt.Description ?? ""
                    );
                    info.Hash = CalculateHash(slt.Name);
                    SlotLogicTypes.Add(info);
                    addedCount++;
                }
            }

            _loadedFiles.Add(filePath);
            _lastLoadError = null;
            return true;
        }
        catch (JsonException ex)
        {
            _lastLoadError = $"JSON parse error in {filePath}: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            _lastLoadError = $"Error loading {filePath}: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Reloads all custom devices from default locations.
    /// Call this after adding new JSON files at runtime.
    /// </summary>
    public static void ReloadCustomDevices()
    {
        _loadedFiles.Clear();
        LoadCustomDevicesFromDefaultLocations();
    }

    /// <summary>
    /// Exports the current built-in devices to a JSON file as a template.
    /// </summary>
    public static void ExportDevicesToJson(string filePath)
    {
        var data = new CustomDeviceData
        {
            Devices = Devices.Select(d => new CustomDeviceEntry
            {
                PrefabName = d.PrefabName,
                Category = d.Category,
                DisplayName = d.DisplayName,
                Description = d.Description
            }).ToList(),
            LogicTypes = LogicTypes.Select(l => new CustomLogicTypeEntry
            {
                Name = l.Name,
                DisplayName = l.DisplayName,
                Description = l.Description
            }).ToList(),
            SlotLogicTypes = SlotLogicTypes.Select(s => new CustomSlotLogicTypeEntry
            {
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description
            }).ToList()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(filePath, json);
    }

    #region Fallback Initialization Methods

    private static void InitializeDevicesDefault()
    {
        // Basic fallback devices for when JSON files aren't available
        Devices.Add(new DeviceInfo("StructureActiveVent", "Atmospheric", "Active Vent", "Controls atmospheric venting"));
        Devices.Add(new DeviceInfo("StructureGasSensor", "Sensor", "Gas Sensor", "Monitors atmospheric conditions"));
        Devices.Add(new DeviceInfo("StructureLogicMemory", "Logic", "Memory", "Stores a single value"));
        Devices.Add(new DeviceInfo("StructureConsole", "Display", "Console", "Display console"));
        Devices.Add(new DeviceInfo("StructureLEDDisplay", "Display", "LED Display", "Shows numeric values"));
        Devices.Add(new DeviceInfo("StructureSolarPanel", "Power", "Solar Panel", "Generates power from sunlight"));
        Devices.Add(new DeviceInfo("StructureBattery", "Power", "Battery", "Stores electrical power"));

        foreach (var device in Devices)
        {
            device.Hash = CalculateHash(device.PrefabName);
        }
    }

    private static void InitializeLogicTypesDefault()
    {
        // Basic fallback logic types
        var basicTypes = new[] {
            "None", "Power", "Open", "Mode", "Error", "Pressure", "Temperature",
            "Activate", "Lock", "Charge", "Setting", "On", "Ratio", "Quantity",
            "Color", "Horizontal", "Vertical", "Channel0", "Channel1", "Channel2",
            "Channel3", "Channel4", "Channel5", "Channel6", "Channel7"
        };
        for (int i = 0; i < basicTypes.Length; i++)
        {
            LogicTypes.Add(new LogicType(basicTypes[i], basicTypes[i], "")
            {
                Hash = CalculateHash(basicTypes[i]),
                Value = i
            });
        }
    }

    private static void InitializeSlotLogicTypesDefault()
    {
        var basicTypes = new[] {
            "None", "Occupied", "OccupantHash", "Quantity", "Damage",
            "Efficiency", "Health", "Growth", "Pressure", "Temperature"
        };
        for (int i = 0; i < basicTypes.Length; i++)
        {
            SlotLogicTypes.Add(new SlotLogicType(basicTypes[i], basicTypes[i], "")
            {
                Hash = CalculateHash(basicTypes[i]),
                Value = i
            });
        }
    }

    private static void InitializeBatchModesDefault()
    {
        BatchModes.Add(new BatchMode("Average", 0, "Average of all values"));
        BatchModes.Add(new BatchMode("Sum", 1, "Sum of all values"));
        BatchModes.Add(new BatchMode("Minimum", 2, "Minimum value"));
        BatchModes.Add(new BatchMode("Maximum", 3, "Maximum value"));
    }

    private static void InitializeReagentModesDefault()
    {
        ReagentModes.Add(new ReagentMode("Contents", 0, "Contents amount"));
        ReagentModes.Add(new ReagentMode("Required", 1, "Required amount"));
        ReagentModes.Add(new ReagentMode("Recipe", 2, "Recipe amount"));
        ReagentModes.Add(new ReagentMode("TotalContents", 3, "Total contents"));
    }

    #endregion

    public static int CalculateHash(string value)
    {
        // Stationeers uses CRC32 for hashing
        uint crc = 0xFFFFFFFF;
        foreach (char c in value)
        {
            crc ^= c;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ (0xEDB88320 * (crc & 1));
            }
        }
        return (int)~crc;
    }

    public static List<DeviceInfo> SearchDevices(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Devices;

        var lower = query.ToLowerInvariant();
        return Devices.Where(d =>
            d.DisplayName.ToLowerInvariant().Contains(lower) ||
            d.PrefabName.ToLowerInvariant().Contains(lower) ||
            d.Category.ToLowerInvariant().Contains(lower) ||
            d.Description.ToLowerInvariant().Contains(lower) ||
            d.Hash.ToString().Contains(query))
            .ToList();
    }

    public static List<LogicType> SearchLogicTypes(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return LogicTypes;

        var lower = query.ToLowerInvariant();
        return LogicTypes.Where(l =>
            l.Name.ToLowerInvariant().Contains(lower) ||
            l.DisplayName.ToLowerInvariant().Contains(lower) ||
            l.Description.ToLowerInvariant().Contains(lower) ||
            l.Hash.ToString().Contains(query))
            .ToList();
    }

    /// <summary>
    /// Gets the hash for a device type by prefab name.
    /// Returns the calculated hash if device is found, otherwise calculates hash from the name.
    /// </summary>
    public static int GetDeviceHash(string prefabName)
    {
        var device = Devices.FirstOrDefault(d =>
            d.PrefabName.Equals(prefabName, StringComparison.OrdinalIgnoreCase));

        if (device != null)
            return device.Hash;

        // Not found in database - calculate hash directly
        return CalculateHash(prefabName);
    }
}

#region Data Models

public class DeviceInfo
{
    public string PrefabName { get; }
    public string Category { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int Hash { get; set; }

    public DeviceInfo(string prefabName, string category, string displayName, string description)
    {
        PrefabName = prefabName;
        Category = category;
        DisplayName = displayName;
        Description = description;
    }
}

public class LogicType
{
    public string Name { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int Hash { get; set; }
    public int Value { get; set; }

    public LogicType(string name, string displayName, string description)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
    }
}

public class SlotLogicType
{
    public string Name { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int Hash { get; set; }
    public int Value { get; set; }

    public SlotLogicType(string name, string displayName, string description)
    {
        Name = name;
        DisplayName = displayName;
        Description = description;
    }
}

public class BatchMode
{
    public string Name { get; }
    public int Value { get; }
    public string Description { get; }

    public BatchMode(string name, int value, string description)
    {
        Name = name;
        Value = value;
        Description = description;
    }
}

public class ReagentMode
{
    public string Name { get; }
    public int Value { get; }
    public string Description { get; }

    public ReagentMode(string name, int value, string description)
    {
        Name = name;
        Value = value;
        Description = description;
    }
}

public class SortingClass
{
    public string Name { get; }
    public int Value { get; }
    public int Hash { get; }

    public SortingClass(string name, int value, int hash)
    {
        Name = name;
        Value = value;
        Hash = hash;
    }
}

#endregion

#region JSON Models

internal class JsonLogicType
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public int Hash { get; set; }
}

internal class JsonDevice
{
    public string PrefabName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Description { get; set; }
    public int Hash { get; set; }
}

// JSON serialization models for custom device loading
public class CustomDeviceData
{
    public List<CustomDeviceEntry>? Devices { get; set; }
    public List<CustomLogicTypeEntry>? LogicTypes { get; set; }
    public List<CustomSlotLogicTypeEntry>? SlotLogicTypes { get; set; }
}

public class CustomDeviceEntry
{
    public string PrefabName { get; set; } = "";
    public string? Category { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}

public class CustomLogicTypeEntry
{
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}

public class CustomSlotLogicTypeEntry
{
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}

#endregion
