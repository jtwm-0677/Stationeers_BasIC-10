using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

class Program
{
    static string OutputDir = @"C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired\Data\Generated";

    static void Main(string[] args)
    {
        Directory.CreateDirectory(OutputDir);

        var assemblyPath = @"C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\Managed\Assembly-CSharp.dll";
        var languagePath = @"C:\Program Files (x86)\Steam\steamapps\common\Stationeers\rocketstation_Data\StreamingAssets\Language\english.xml";

        if (!File.Exists(assemblyPath))
        {
            Console.WriteLine($"Assembly not found: {assemblyPath}");
            return;
        }

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            Console.WriteLine("Loaded Assembly-CSharp.dll successfully");

            // Export LogicTypes
            ExportEnumToJson(assembly, "LogicType", "LogicTypes.json");

            // Export SlotLogicTypes
            ExportEnumToJson(assembly, "LogicSlotType", "SlotLogicTypes.json");

            // Export BatchModes
            ExportEnumToJson(assembly, "LogicBatchMethod", "BatchModes.json");

            // Export ReagentModes
            ExportEnumToJson(assembly, "LogicReagentMode", "ReagentModes.json");

            // Export SortingClass
            ExportEnumToJson(assembly, "SortingClass", "SortingClasses.json");

            // Extract device prefabs from language file
            if (File.Exists(languagePath))
            {
                ExtractDevicesFromLanguageFile(languagePath);
            }

            Console.WriteLine($"\nAll files exported to: {OutputDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void ExportEnumToJson(Assembly assembly, string enumName, string fileName)
    {
        try
        {
            var enumType = assembly.GetTypes().FirstOrDefault(t => t.Name == enumName && t.IsEnum);
            if (enumType == null)
            {
                Console.WriteLine($"Enum '{enumName}' not found.");
                return;
            }

            var names = Enum.GetNames(enumType);
            var values = Enum.GetValues(enumType);
            var entries = new List<object>();

            for (int i = 0; i < names.Length; i++)
            {
                var val = Convert.ToInt32(values.GetValue(i));
                var hash = CalculateHash(names[i]);
                entries.Add(new { Name = names[i], Value = val, Hash = hash });
            }

            var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            var path = Path.Combine(OutputDir, fileName);
            File.WriteAllText(path, json);
            Console.WriteLine($"Exported {entries.Count} entries to {fileName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting {enumName}: {ex.Message}");
        }
    }

    static void ExtractDevicesFromLanguageFile(string languagePath)
    {
        try
        {
            var xml = XDocument.Load(languagePath);
            var things = xml.Descendants("Things").FirstOrDefault();
            if (things == null)
            {
                Console.WriteLine("No Things section found in language file");
                return;
            }

            var devices = new List<object>();
            var records = things.Descendants("RecordThing");

            foreach (var record in records)
            {
                var key = record.Element("Key")?.Value;
                var value = record.Element("Value")?.Value;
                var desc = record.Element("Description")?.Value ?? "";

                if (string.IsNullOrEmpty(key)) continue;

                // Determine category
                var category = DetermineCategory(key);
                if (category == null) continue; // Skip non-device items

                var hash = CalculateHash(key);
                devices.Add(new
                {
                    PrefabName = key,
                    DisplayName = value ?? key,
                    Category = category,
                    Description = TruncateDescription(desc),
                    Hash = hash
                });
            }

            var json = JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true });
            var path = Path.Combine(OutputDir, "Devices.json");
            File.WriteAllText(path, json);
            Console.WriteLine($"Exported {devices.Count} devices to Devices.json");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting devices: {ex.Message}");
        }
    }

    static string? DetermineCategory(string prefabName)
    {
        // Logic/Electronics
        if (prefabName.Contains("Logic") || prefabName.Contains("Circuitboard") ||
            prefabName.Contains("Motherboard") || prefabName.Contains("IntegratedCircuit"))
            return "Logic";

        // LED Displays and Consoles
        if (prefabName.Contains("Console") || prefabName.Contains("Display") ||
            prefabName.Contains("LED") || prefabName.Contains("Dial"))
            return "Display";

        // Sensors
        if (prefabName.Contains("Sensor"))
            return "Sensor";

        // Atmospheric
        if (prefabName.Contains("Vent") || prefabName.Contains("Pump") ||
            prefabName.Contains("Filter") || prefabName.Contains("Mixer") ||
            prefabName.Contains("Tank") || prefabName.Contains("Pipe") ||
            prefabName.Contains("Condenser") || prefabName.Contains("Electrolyzer") ||
            prefabName.Contains("Heater") || prefabName.Contains("Cooler") ||
            prefabName.Contains("Regulator") || prefabName.Contains("AirConditioner"))
            return "Atmospheric";

        // Power
        if (prefabName.Contains("Generator") || prefabName.Contains("Battery") ||
            prefabName.Contains("Solar") || prefabName.Contains("Transformer") ||
            prefabName.Contains("Power") || prefabName.Contains("APC") ||
            prefabName.Contains("Turbine") || prefabName.Contains("RTG"))
            return "Power";

        // Manufacturing
        if (prefabName.Contains("Furnace") || prefabName.Contains("Printer") ||
            prefabName.Contains("Autolathe") || prefabName.Contains("Fabricator") ||
            prefabName.Contains("Centrifuge") || prefabName.Contains("Recycler") ||
            prefabName.Contains("Manufactory"))
            return "Manufacturing";

        // Hydroponics
        if (prefabName.Contains("Hydroponics") || prefabName.Contains("Harvie") ||
            prefabName.Contains("GrowLight") || prefabName.Contains("Planter"))
            return "Hydroponics";

        // Storage
        if (prefabName.Contains("Locker") || prefabName.Contains("Crate") ||
            prefabName.Contains("Storage") || prefabName.Contains("Chute") ||
            prefabName.Contains("Stacker") || prefabName.Contains("Sorter") ||
            prefabName.Contains("Vending"))
            return "Storage";

        // Doors/Access
        if (prefabName.Contains("Door") || prefabName.Contains("Airlock") ||
            prefabName.Contains("BlastDoor") || prefabName.Contains("Gate"))
            return "Access";

        // Lighting
        if (prefabName.Contains("Light") && !prefabName.Contains("Daylight"))
            return "Lighting";

        // Mining
        if (prefabName.Contains("Mining") || prefabName.Contains("Drill") ||
            prefabName.Contains("DeepMiner"))
            return "Mining";

        // Structure prefabs
        if (prefabName.StartsWith("Structure"))
            return "Structure";

        // Item prefabs (circuits, tools, etc)
        if (prefabName.StartsWith("Item") || prefabName.StartsWith("Circuitboard"))
            return "Item";

        // Modular devices
        if (prefabName.StartsWith("Modular") || prefabName.StartsWith("Device"))
            return "Modular";

        return null; // Skip items without clear category
    }

    static string TruncateDescription(string desc)
    {
        if (string.IsNullOrEmpty(desc)) return "";
        // Clean up description - remove THING references and truncate
        desc = Regex.Replace(desc, @"\{THING:[^}]+\}", "");
        desc = Regex.Replace(desc, @"\{LINK:[^}]+\}", "");
        desc = Regex.Replace(desc, @"\s+", " ").Trim();
        if (desc.Length > 100) desc = desc.Substring(0, 97) + "...";
        return desc;
    }

    static int CalculateHash(string value)
    {
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
}
