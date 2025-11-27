namespace BasicToMips.Data;

public class DeviceDatabase
{
    public static List<DeviceInfo> Devices { get; } = new();
    public static List<LogicType> LogicTypes { get; } = new();
    public static List<SlotLogicType> SlotLogicTypes { get; } = new();
    public static List<BatchMode> BatchModes { get; } = new();
    public static List<ReagentMode> ReagentModes { get; } = new();

    static DeviceDatabase()
    {
        InitializeDevices();
        InitializeLogicTypes();
        InitializeSlotLogicTypes();
        InitializeBatchModes();
        InitializeReagentModes();
    }

    private static void InitializeDevices()
    {
        // Structures
        Devices.Add(new DeviceInfo("ItemStructureActiveVent", "Structure", "Active Vent", "Controls atmospheric venting"));
        Devices.Add(new DeviceInfo("ItemStructureAdvancedFurnace", "Structure", "Advanced Furnace", "High-temperature smelting furnace"));
        Devices.Add(new DeviceInfo("ItemStructureAdvancedPackagingMachine", "Structure", "Advanced Packager", "Packages items with reagent handling"));
        Devices.Add(new DeviceInfo("ItemStructureAirConditioner", "Structure", "Air Conditioner", "Cools or heats atmosphere"));
        Devices.Add(new DeviceInfo("ItemStructureArcFurnace", "Structure", "Arc Furnace", "Electric arc smelting furnace"));
        Devices.Add(new DeviceInfo("ItemStructureAutolathe", "Structure", "Autolathe", "Automated manufacturing"));
        Devices.Add(new DeviceInfo("ItemStructureElectronicsPrinter", "Structure", "Electronics Printer", "Prints circuit boards"));
        Devices.Add(new DeviceInfo("ItemStructureHydraulicPipeBender", "Structure", "Hydraulic Pipe Bender", "Bends pipes"));
        Devices.Add(new DeviceInfo("ItemStructureToolManufactory", "Structure", "Tool Manufactory", "Creates tools"));
        Devices.Add(new DeviceInfo("ItemStructureSecurityPrinter", "Structure", "Security Printer", "Prints security items"));
        Devices.Add(new DeviceInfo("ItemStructureFabricator", "Structure", "Fabricator", "General fabrication machine"));
        Devices.Add(new DeviceInfo("ItemStructurePaintMixer", "Structure", "Paint Mixer", "Mixes paint colors"));
        Devices.Add(new DeviceInfo("ItemStructureOrganicsPrinter", "Structure", "Organics Printer", "Prints organic materials"));
        Devices.Add(new DeviceInfo("ItemStructureReagentProcessor", "Structure", "Reagent Processor", "Processes reagents"));
        Devices.Add(new DeviceInfo("ItemStructureCentrifuge", "Structure", "Centrifuge", "Separates materials"));
        Devices.Add(new DeviceInfo("ItemStructureRecycler", "Structure", "Recycler", "Recycles items into materials"));

        // Sensors
        Devices.Add(new DeviceInfo("ItemStructureGasSensor", "Sensor", "Gas Sensor", "Monitors atmospheric conditions"));
        Devices.Add(new DeviceInfo("ItemStructureDaylightSensor", "Sensor", "Daylight Sensor", "Detects sunlight"));
        Devices.Add(new DeviceInfo("ItemStructureMotionSensor", "Sensor", "Motion Sensor", "Detects entity movement"));
        Devices.Add(new DeviceInfo("ItemStructureOccupancySensor", "Sensor", "Occupancy Sensor", "Detects presence"));
        Devices.Add(new DeviceInfo("ItemStructureQuantitySensor", "Sensor", "Quantity Sensor", "Counts items in storage"));

        // Logic
        Devices.Add(new DeviceInfo("ItemCircuitboardProgrammableChip", "Logic", "IC Housing", "Programmable IC10 chip holder"));
        Devices.Add(new DeviceInfo("ItemIntegratedCircuit10", "Logic", "IC10 Chip", "Programmable integrated circuit"));
        Devices.Add(new DeviceInfo("ItemStructureLogicMemory", "Logic", "Memory", "Stores a single value"));
        Devices.Add(new DeviceInfo("ItemStructureLogicReader", "Logic", "Logic Reader", "Reads value from slot"));
        Devices.Add(new DeviceInfo("ItemStructureLogicWriter", "Logic", "Logic Writer", "Writes value to slot"));
        Devices.Add(new DeviceInfo("ItemStructureLogicBatchReader", "Logic", "Batch Reader", "Reads from multiple devices"));
        Devices.Add(new DeviceInfo("ItemStructureLogicBatchWriter", "Logic", "Batch Writer", "Writes to multiple devices"));
        Devices.Add(new DeviceInfo("ItemStructureLogicDialVariant", "Logic", "Dial", "User input dial"));
        Devices.Add(new DeviceInfo("ItemStructureLogicSwitch", "Logic", "Logic Switch", "Binary switch"));
        Devices.Add(new DeviceInfo("ItemStructureLogicButton", "Logic", "Logic Button", "Momentary button"));
        Devices.Add(new DeviceInfo("ItemStructureConsole", "Logic", "Console", "Display console"));
        Devices.Add(new DeviceInfo("ItemStructureLEDDisplay", "Logic", "LED Display", "Shows numeric values"));

        // Power
        Devices.Add(new DeviceInfo("ItemStructureSolidFuelGenerator", "Power", "Solid Generator", "Burns solid fuel for power"));
        Devices.Add(new DeviceInfo("ItemStructureGasGenerator", "Power", "Gas Generator", "Burns gas for power"));
        Devices.Add(new DeviceInfo("ItemStructureTurbineGenerator", "Power", "Turbine Generator", "Steam/gas turbine power"));
        Devices.Add(new DeviceInfo("ItemStructureSolarPanel", "Power", "Solar Panel", "Generates power from sunlight"));
        Devices.Add(new DeviceInfo("ItemStructureBattery", "Power", "Battery", "Stores electrical power"));
        Devices.Add(new DeviceInfo("ItemStructureBatteryLarge", "Power", "Large Battery", "Large power storage"));
        Devices.Add(new DeviceInfo("ItemStructureAreaPowerController", "Power", "APC", "Area Power Controller"));
        Devices.Add(new DeviceInfo("ItemStructureTransformer", "Power", "Transformer", "Power transformation"));

        // Atmospheric
        Devices.Add(new DeviceInfo("ItemStructurePumpVolume", "Atmospheric", "Volume Pump", "Moves gas by volume"));
        Devices.Add(new DeviceInfo("ItemStructurePumpPressure", "Atmospheric", "Pressure Pump", "Moves gas by pressure"));
        Devices.Add(new DeviceInfo("ItemStructureTurboVolumePump", "Atmospheric", "Turbo Volume Pump", "High-speed volume pump"));
        Devices.Add(new DeviceInfo("ItemStructureGasFilter", "Atmospheric", "Gas Filter", "Filters specific gases"));
        Devices.Add(new DeviceInfo("ItemStructureGasMixer", "Atmospheric", "Gas Mixer", "Mixes gases"));
        Devices.Add(new DeviceInfo("ItemStructureTank", "Atmospheric", "Tank", "Stores gases"));
        Devices.Add(new DeviceInfo("ItemStructureElectrolyzer", "Atmospheric", "Electrolyzer", "Splits water into H2/O2"));
        Devices.Add(new DeviceInfo("ItemStructureCondenser", "Atmospheric", "Condenser", "Condenses liquids from gas"));
        Devices.Add(new DeviceInfo("ItemStructureAtmosphericRegulator", "Atmospheric", "Atmos Regulator", "Regulates atmosphere"));
        Devices.Add(new DeviceInfo("ItemStructureWallHeater", "Atmospheric", "Wall Heater", "Heats atmosphere"));
        Devices.Add(new DeviceInfo("ItemStructureWallCooler", "Atmospheric", "Wall Cooler", "Cools atmosphere"));

        // Hydroponics
        Devices.Add(new DeviceInfo("ItemStructureHarvie", "Hydroponics", "Harvie", "Automated harvester"));
        Devices.Add(new DeviceInfo("ItemStructureHydroponicsTray", "Hydroponics", "Hydroponics Tray", "Grows plants"));
        Devices.Add(new DeviceInfo("ItemStructureGrowLight", "Hydroponics", "Grow Light", "Provides light for plants"));

        // Storage
        Devices.Add(new DeviceInfo("ItemStructureLocker", "Storage", "Locker", "Personal storage"));
        Devices.Add(new DeviceInfo("ItemStructureStorageLarge", "Storage", "Large Crate", "Large item storage"));
        Devices.Add(new DeviceInfo("ItemStructureVendingMachine", "Storage", "Vending Machine", "Dispenses items"));
        Devices.Add(new DeviceInfo("ItemStructureChute", "Storage", "Chute", "Item transfer"));
        Devices.Add(new DeviceInfo("ItemStructureStackerOutput", "Storage", "Stacker", "Stacks items"));
        Devices.Add(new DeviceInfo("ItemStructureSorter", "Storage", "Sorter", "Sorts items by type"));

        // Mining
        Devices.Add(new DeviceInfo("ItemStructureMiningDrill", "Mining", "Mining Drill", "Extracts ore"));
        Devices.Add(new DeviceInfo("ItemStructureDeepMiner", "Mining", "Deep Miner", "Deep ore extraction"));

        // Doors and Access
        Devices.Add(new DeviceInfo("ItemStructureDoorSingle", "Access", "Door", "Single door"));
        Devices.Add(new DeviceInfo("ItemStructureAirlock", "Access", "Airlock", "Sealed airlock door"));
        Devices.Add(new DeviceInfo("ItemStructureBlastDoor", "Access", "Blast Door", "Heavy blast door"));

        // Lighting
        Devices.Add(new DeviceInfo("ItemStructureWallLight", "Lighting", "Wall Light", "Wall-mounted light"));
        Devices.Add(new DeviceInfo("ItemStructureCeilingLight", "Lighting", "Ceiling Light", "Overhead lighting"));
        Devices.Add(new DeviceInfo("ItemStructureFloorLight", "Lighting", "Floor Light", "Floor-mounted light"));
        Devices.Add(new DeviceInfo("ItemStructureSpotLight", "Lighting", "Spot Light", "Directional light"));

        // Calculate hashes
        foreach (var device in Devices)
        {
            device.Hash = CalculateHash(device.PrefabName);
        }
    }

    private static void InitializeLogicTypes()
    {
        // Basic Properties
        LogicTypes.Add(new LogicType("On", "Power state", "0 = off, 1 = on"));
        LogicTypes.Add(new LogicType("Open", "Door/vent state", "0 = closed, 1 = open"));
        LogicTypes.Add(new LogicType("Lock", "Lock state", "0 = unlocked, 1 = locked"));
        LogicTypes.Add(new LogicType("Mode", "Operating mode", "Device-specific mode value"));
        LogicTypes.Add(new LogicType("Error", "Error state", "0 = no error, 1 = error"));
        LogicTypes.Add(new LogicType("Setting", "Target setting", "Device-specific setting value"));
        LogicTypes.Add(new LogicType("Activate", "Activation trigger", "1 to activate"));
        LogicTypes.Add(new LogicType("Idle", "Idle state", "1 when idle"));

        // Power Properties
        LogicTypes.Add(new LogicType("Power", "Power state", "Current power status"));
        LogicTypes.Add(new LogicType("PowerRequired", "Power demand", "Watts required"));
        LogicTypes.Add(new LogicType("PowerActual", "Power draw", "Actual watts being used"));
        LogicTypes.Add(new LogicType("PowerGeneration", "Power output", "Watts being generated"));
        LogicTypes.Add(new LogicType("PowerPotential", "Power capacity", "Maximum watts possible"));
        LogicTypes.Add(new LogicType("Charge", "Battery charge", "Current charge level 0-1"));
        LogicTypes.Add(new LogicType("ChargeRatio", "Charge percentage", "Charge as ratio 0-1"));

        // Atmospheric Properties
        LogicTypes.Add(new LogicType("Pressure", "Atmosphere pressure", "Pressure in kPa"));
        LogicTypes.Add(new LogicType("PressureInput", "Input pressure", "Input side pressure"));
        LogicTypes.Add(new LogicType("PressureOutput", "Output pressure", "Output side pressure"));
        LogicTypes.Add(new LogicType("PressureInternal", "Internal pressure", "Internal pressure"));
        LogicTypes.Add(new LogicType("PressureExternal", "External pressure", "External pressure"));
        LogicTypes.Add(new LogicType("PressureSetting", "Target pressure", "Desired pressure"));
        LogicTypes.Add(new LogicType("Temperature", "Temperature", "Temperature in Kelvin"));
        LogicTypes.Add(new LogicType("TemperatureInput", "Input temperature", "Input side temp"));
        LogicTypes.Add(new LogicType("TemperatureOutput", "Output temperature", "Output side temp"));
        LogicTypes.Add(new LogicType("TemperatureInternal", "Internal temperature", "Internal temp"));
        LogicTypes.Add(new LogicType("TemperatureExternal", "External temperature", "External temp"));
        LogicTypes.Add(new LogicType("TemperatureSetting", "Target temperature", "Desired temp"));
        LogicTypes.Add(new LogicType("TotalMoles", "Total gas moles", "Total mol in atmosphere"));
        LogicTypes.Add(new LogicType("TotalMolesInput", "Input moles", "Mol on input side"));
        LogicTypes.Add(new LogicType("TotalMolesOutput", "Output moles", "Mol on output side"));

        // Gas Ratios
        LogicTypes.Add(new LogicType("RatioOxygen", "Oxygen ratio", "O2 ratio 0-1"));
        LogicTypes.Add(new LogicType("RatioCarbonDioxide", "CO2 ratio", "CO2 ratio 0-1"));
        LogicTypes.Add(new LogicType("RatioNitrogen", "Nitrogen ratio", "N2 ratio 0-1"));
        LogicTypes.Add(new LogicType("RatioNitrousOxide", "N2O ratio", "N2O ratio 0-1"));
        LogicTypes.Add(new LogicType("RatioPollutant", "Pollutant ratio", "X ratio 0-1"));
        LogicTypes.Add(new LogicType("RatioVolatiles", "Volatiles ratio", "H2 ratio 0-1"));
        LogicTypes.Add(new LogicType("RatioWater", "Steam ratio", "H2O ratio 0-1"));

        // Storage/Inventory
        LogicTypes.Add(new LogicType("Quantity", "Item count", "Number of items"));
        LogicTypes.Add(new LogicType("MaxQuantity", "Max capacity", "Maximum items"));
        LogicTypes.Add(new LogicType("Ratio", "Fill ratio", "Quantity/MaxQuantity"));
        LogicTypes.Add(new LogicType("PrefabHash", "Item type hash", "Hash of item prefab"));
        LogicTypes.Add(new LogicType("OccupantHash", "Occupant hash", "Hash of occupant"));

        // Machines
        LogicTypes.Add(new LogicType("ImportCount", "Import count", "Items imported"));
        LogicTypes.Add(new LogicType("ExportCount", "Export count", "Items exported"));
        LogicTypes.Add(new LogicType("RecipeHash", "Recipe selection", "Hash of selected recipe"));
        LogicTypes.Add(new LogicType("ClearMemory", "Clear memory", "Clears stored recipe"));
        LogicTypes.Add(new LogicType("Completions", "Completions", "Number of completions"));
        LogicTypes.Add(new LogicType("Efficiency", "Efficiency", "Current efficiency 0-1"));
        LogicTypes.Add(new LogicType("Growth", "Plant growth", "Growth progress 0-1"));
        LogicTypes.Add(new LogicType("Health", "Plant health", "Health value 0-1"));
        LogicTypes.Add(new LogicType("Mature", "Mature state", "1 when mature"));
        LogicTypes.Add(new LogicType("Seeding", "Seeding state", "1 when seeding"));

        // Solar/Light
        LogicTypes.Add(new LogicType("Horizontal", "Horizontal angle", "Solar panel angle"));
        LogicTypes.Add(new LogicType("Vertical", "Vertical angle", "Solar panel angle"));
        LogicTypes.Add(new LogicType("SolarAngle", "Sun angle", "Current sun angle"));
        LogicTypes.Add(new LogicType("SolarIrradiance", "Irradiance", "Sunlight intensity"));
        LogicTypes.Add(new LogicType("Color", "Light color", "Color value"));
        LogicTypes.Add(new LogicType("ColorBlue", "Blue", "Blue 0-255"));
        LogicTypes.Add(new LogicType("ColorGreen", "Green", "Green 0-255"));
        LogicTypes.Add(new LogicType("ColorRed", "Red", "Red 0-255"));

        // Communication
        LogicTypes.Add(new LogicType("Channel0", "Channel 0", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel1", "Channel 1", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel2", "Channel 2", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel3", "Channel 3", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel4", "Channel 4", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel5", "Channel 5", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel6", "Channel 6", "Transmit/receive channel"));
        LogicTypes.Add(new LogicType("Channel7", "Channel 7", "Transmit/receive channel"));

        // Misc
        LogicTypes.Add(new LogicType("Volume", "Volume", "Volume setting"));
        LogicTypes.Add(new LogicType("Combustion", "Combustion state", "Fuel burn state"));
        LogicTypes.Add(new LogicType("Fuel", "Fuel level", "Remaining fuel"));
        LogicTypes.Add(new LogicType("FuelRatio", "Fuel ratio", "Fuel level 0-1"));
        LogicTypes.Add(new LogicType("Time", "Time", "Timer value"));
        LogicTypes.Add(new LogicType("LineNumber", "Line number", "Current IC line"));
        LogicTypes.Add(new LogicType("ReferenceId", "Reference ID", "Device reference ID"));
        LogicTypes.Add(new LogicType("NameHash", "Name hash", "Hash of device name"));

        // Calculate hashes
        foreach (var lt in LogicTypes)
        {
            lt.Hash = CalculateHash(lt.Name);
        }
    }

    private static void InitializeSlotLogicTypes()
    {
        SlotLogicTypes.Add(new SlotLogicType("Occupied", "Slot occupied", "1 if slot has item"));
        SlotLogicTypes.Add(new SlotLogicType("OccupantHash", "Occupant hash", "Hash of item in slot"));
        SlotLogicTypes.Add(new SlotLogicType("Quantity", "Item quantity", "Stack size in slot"));
        SlotLogicTypes.Add(new SlotLogicType("MaxQuantity", "Max quantity", "Max stack size"));
        SlotLogicTypes.Add(new SlotLogicType("Damage", "Item damage", "Damage level 0-1"));
        SlotLogicTypes.Add(new SlotLogicType("Charge", "Item charge", "Battery charge 0-1"));
        SlotLogicTypes.Add(new SlotLogicType("ChargeRatio", "Charge ratio", "Charge percentage"));
        SlotLogicTypes.Add(new SlotLogicType("PrefabHash", "Prefab hash", "Item type hash"));
        SlotLogicTypes.Add(new SlotLogicType("Class", "Item class", "Item class type"));
        SlotLogicTypes.Add(new SlotLogicType("SortingClass", "Sorting class", "For sorter machines"));
        SlotLogicTypes.Add(new SlotLogicType("Growth", "Growth", "Plant growth"));
        SlotLogicTypes.Add(new SlotLogicType("Health", "Health", "Plant/item health"));
        SlotLogicTypes.Add(new SlotLogicType("Mature", "Mature", "Plant maturity"));
        SlotLogicTypes.Add(new SlotLogicType("Seeding", "Seeding", "Seeding state"));

        foreach (var slt in SlotLogicTypes)
        {
            slt.Hash = CalculateHash(slt.Name);
        }
    }

    private static void InitializeBatchModes()
    {
        BatchModes.Add(new BatchMode("Average", 0, "Average of all values"));
        BatchModes.Add(new BatchMode("Sum", 1, "Sum of all values"));
        BatchModes.Add(new BatchMode("Minimum", 2, "Minimum value"));
        BatchModes.Add(new BatchMode("Maximum", 3, "Maximum value"));
    }

    private static void InitializeReagentModes()
    {
        ReagentModes.Add(new ReagentMode("Contents", 0, "Contents amount"));
        ReagentModes.Add(new ReagentMode("Required", 1, "Required amount"));
        ReagentModes.Add(new ReagentMode("Recipe", 2, "Recipe amount"));
    }

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
}

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
