using System.Text.Json;

namespace BasicToMips.Data;

/// <summary>
/// A living hash dictionary that learns string-to-hash mappings from user compilations.
/// Persists across sessions to enable reverse hash lookups during decompilation.
/// </summary>
public static class HashDictionary
{
    private static readonly Dictionary<int, string> _hashToString = new();
    private static readonly Dictionary<string, int> _stringToHash = new(StringComparer.OrdinalIgnoreCase);
    private static string? _dictionaryPath;
    private static bool _isDirty = false;

    /// <summary>
    /// Initializes the hash dictionary, loading from disk if available.
    /// Uses LocalApplicationData to match SettingsService location.
    /// </summary>
    public static void Initialize()
    {
        // Use LocalApplicationData (same as SettingsService) for consistency
        // This ensures both WPF app and in-game mod can share the same dictionary
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var basicTenDir = Path.Combine(appDataPath, "BasicToMips");

        if (!Directory.Exists(basicTenDir))
        {
            Directory.CreateDirectory(basicTenDir);
        }

        _dictionaryPath = Path.Combine(basicTenDir, "hash_dictionary.json");
        Load();

        // Pre-populate with known device types from DeviceDatabase
        foreach (var device in DeviceDatabase.Devices)
        {
            RegisterHash(device.PrefabName, device.Hash);
        }

        // Pre-populate with known logic types (property names)
        foreach (var logicType in DeviceDatabase.LogicTypes)
        {
            RegisterHash(logicType.Name, logicType.Hash);
        }

        // Pre-populate with known slot logic types
        foreach (var slotType in DeviceDatabase.SlotLogicTypes)
        {
            RegisterHash(slotType.Name, slotType.Hash);
        }

        // Pre-populate with batch modes
        foreach (var batchMode in DeviceDatabase.BatchModes)
        {
            RegisterHash(batchMode.Name, batchMode.Value);
        }

        // Pre-populate with reagent modes
        foreach (var reagentMode in DeviceDatabase.ReagentModes)
        {
            RegisterHash(reagentMode.Name, reagentMode.Value);
        }

        // Pre-populate with sorting classes
        foreach (var sortingClass in DeviceDatabase.SortingClasses)
        {
            RegisterHash(sortingClass.Name, sortingClass.Hash);
        }
    }

    /// <summary>
    /// Registers a string-to-hash mapping. Called during compilation.
    /// </summary>
    public static void RegisterHash(string value, int hash)
    {
        if (string.IsNullOrEmpty(value)) return;

        // Only update if this is a new mapping or different value
        if (!_hashToString.TryGetValue(hash, out var existing) || existing != value)
        {
            _hashToString[hash] = value;
            _stringToHash[value] = hash;
            _isDirty = true;
        }
    }

    /// <summary>
    /// Looks up the original string for a hash value.
    /// Returns null if not found.
    /// </summary>
    public static string? LookupHash(int hash)
    {
        return _hashToString.TryGetValue(hash, out var value) ? value : null;
    }

    /// <summary>
    /// Looks up the hash for a string value.
    /// Returns null if not found.
    /// </summary>
    public static int? LookupString(string value)
    {
        return _stringToHash.TryGetValue(value, out var hash) ? hash : null;
    }

    /// <summary>
    /// Gets the number of entries in the dictionary.
    /// </summary>
    public static int Count => _hashToString.Count;

    /// <summary>
    /// Saves the dictionary to disk if there are pending changes.
    /// </summary>
    public static void Save()
    {
        if (!_isDirty || string.IsNullOrEmpty(_dictionaryPath)) return;

        try
        {
            var data = new HashDictionaryData
            {
                Version = 1,
                Entries = _hashToString.Select(kvp => new HashEntry
                {
                    Hash = kvp.Key,
                    Value = kvp.Value
                }).ToList()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_dictionaryPath, json);
            _isDirty = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save hash dictionary: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the dictionary from disk.
    /// </summary>
    private static void Load()
    {
        if (string.IsNullOrEmpty(_dictionaryPath) || !File.Exists(_dictionaryPath)) return;

        try
        {
            var json = File.ReadAllText(_dictionaryPath);
            var data = JsonSerializer.Deserialize<HashDictionaryData>(json);

            if (data?.Entries != null)
            {
                foreach (var entry in data.Entries)
                {
                    _hashToString[entry.Hash] = entry.Value;
                    _stringToHash[entry.Value] = entry.Hash;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load hash dictionary: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all user-learned entries (keeps pre-populated device database entries).
    /// </summary>
    public static void ClearUserEntries()
    {
        _hashToString.Clear();
        _stringToHash.Clear();
        _isDirty = true;

        // Re-populate from device database
        Initialize();
    }

    private class HashDictionaryData
    {
        public int Version { get; set; }
        public List<HashEntry> Entries { get; set; } = new();
    }

    private class HashEntry
    {
        public int Hash { get; set; }
        public string Value { get; set; } = "";
    }
}
