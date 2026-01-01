# Basic-10 Multi-Platform Architecture Design

**Date:** 2025-12-17
**Version:** 1.0
**Author:** Dog Tired Studios + Claude

---

## Executive Summary

This document outlines the architecture for supporting three parallel development tracks:

1. **Standalone Release** - Current text-based compiler (GitHub releases)
2. **Standalone v3** - Text + Visual Programming (future desktop release)
3. **In-Game Mod** - BepInEx plugin with auto device detection (no visual programming)

The core compiler is extracted into a shared library, enabling code reuse while maintaining platform-specific features.

---

## Project Architecture

### Solution Structure

```
BASICtoMIPS_ByDogTired/
├── BasicToMips.sln
│
├── Basic10.Core/                    ← Shared library (netstandard2.0)
│   ├── Basic10.Core.csproj
│   ├── Lexer/
│   ├── Parser/
│   ├── AST/
│   ├── CodeGen/
│   ├── Analysis/
│   ├── IC10/
│   ├── Data/
│   │   ├── DeviceDatabase.cs
│   │   └── HashDictionary.cs
│   └── Compiler.cs                  ← Simple facade API
│
├── Basic10.Desktop/                 ← WPF standalone (net8.0-windows)
│   ├── Basic10.Desktop.csproj       ← References Basic10.Core
│   ├── App.xaml
│   ├── UI/
│   ├── Editor/
│   └── Services/
│
└── Basic10.GameMod/                 ← BepInEx plugin (net472)
    ├── Basic10.GameMod.csproj       ← References Basic10.Core
    ├── Plugin.cs                    ← BepInEx entry point
    ├── DeviceReflector.cs           ← ILogicable scanner
    ├── EditorPatch.cs               ← Harmony patch for IC editor
    └── ChipStorage.cs               ← BASIC source persistence
```

### Target Frameworks

| Project | Framework | Reason |
|---------|-----------|--------|
| Basic10.Core | netstandard2.0 | Compatible with both net8.0 and net472 |
| Basic10.Desktop | net8.0-windows | Modern WPF with latest .NET features |
| Basic10.GameMod | net472 | Unity/BepInEx compatibility for Stationeers |

### Branch Strategy

```
main ────●────●────●────●────●────►  (all development)
          \
           └─► release/standalone-v2.x  (current stable, text-only)
          \
           └─► release/standalone-v3.x  (future, with visual programming)
          \
           └─► release/gamemod-v1.x     (BepInEx plugin releases)
```

---

## Shared Core Library (Basic10.Core)

### Public API

```csharp
namespace Basic10.Core;

public class Compiler
{
    // Main compilation entry point
    public CompileResult Compile(string basicSource, CompilerOptions? options = null);

    // Decompile IC10 back to BASIC
    public string Decompile(string ic10Source);

    // Check syntax without full compilation
    public List<CompilerError> Validate(string basicSource);
}

public class CompileResult
{
    public bool Success { get; }
    public string IC10Output { get; }
    public List<CompilerError> Errors { get; }
    public List<CompilerWarning> Warnings { get; }
    public SourceMap SourceMap { get; }  // BASIC line → IC10 line mapping
}

public class CompilerError
{
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    public string Code { get; }  // e.g., "B10-001"
}
```

### Device Database API

```csharp
public static class DeviceDatabase
{
    // Existing methods
    public static List<DeviceInfo> Devices { get; }
    public static int CalculateHash(string value);

    // New: Runtime registration (for GameMod reflection)
    public static void RegisterDevice(DeviceInfo device);
    public static void RegisterDevices(IEnumerable<DeviceInfo> devices);

    // New: Export/import for sync between apps
    public static void ExportToJson(string path);
    public static void ImportFromJson(string path);
}
```

### Dependencies

Only `System.Text.Json` - no WPF, no Unity, no external packages.

---

## In-Game Mod (Basic10.GameMod)

### Plugin Entry Point

```csharp
[BepInPlugin("com.dogtired.basic10", "Basic-10 Compiler", "1.0.0")]
[BepInDependency("StationeersLaunchPad")]
public class Plugin : BaseUnityPlugin
{
    void Awake()
    {
        // 1. Scan all ILogicable devices via reflection
        DeviceReflector.ScanAndRegister();

        // 2. Export detected devices to JSON for standalone sync
        var exportPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BasicToMips", "detected_devices.json");
        DeviceDatabase.ExportToJson(exportPath);

        // 3. Apply Harmony patches to IC editor
        Harmony harmony = new Harmony("com.dogtired.basic10");
        harmony.PatchAll();

        Logger.LogInfo($"Basic-10 loaded. Detected {DeviceDatabase.Devices.Count} devices.");
    }
}
```

### Device Reflection Scanner

```csharp
public static class DeviceReflector
{
    public static void ScanAndRegister()
    {
        var logicableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => typeof(ILogicable).IsAssignableFrom(t)
                     && !t.IsInterface
                     && !t.IsAbstract);

        foreach (var type in logicableTypes)
        {
            var prefabName = type.Name;
            var hash = DeviceDatabase.CalculateHash(prefabName);
            var category = DetermineCategory(type);

            DeviceDatabase.RegisterDevice(new DeviceInfo(
                prefabName, category, prefabName,
                $"Detected from {type.Assembly.GetName().Name}"
            ) { Hash = hash });
        }
    }

    private static Type[] SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null).ToArray()!;
        }
    }
}
```

### Harmony Patches

**Compile on Confirm:**
```csharp
[HarmonyPatch(typeof(ProgrammableChipEditor), "OnConfirm")]
public static class EditorConfirmPatch
{
    static bool Prefix(ProgrammableChipEditor __instance, ref string ___currentCode)
    {
        if (!IsBasicCode(___currentCode))
            return true;  // Let original IC10 handling proceed

        var compiler = new Compiler();
        var result = compiler.Compile(___currentCode);

        if (!result.Success)
        {
            ShowErrorMarkers(__instance, result.Errors);
            return false;  // Block confirm, keep editor open
        }

        ChipStorage.SaveBasicSource(__instance.Chip, ___currentCode);
        ___currentCode = result.IC10Output;
        return true;
    }
}
```

**Restore BASIC on Open:**
```csharp
[HarmonyPatch(typeof(ProgrammableChipEditor), "OnOpen")]
public static class EditorOpenPatch
{
    static void Postfix(ProgrammableChipEditor __instance, ref string ___currentCode)
    {
        var basicSource = ChipStorage.LoadBasicSource(__instance.Chip);
        if (basicSource != null)
        {
            ___currentCode = basicSource;
            __instance.RefreshDisplay();
        }
    }
}
```

### BASIC Source Persistence

```csharp
public static class ChipStorage
{
    private const int CHARS_PER_SLOT = 8;
    private const int HEADER_SLOT = 0;
    private const int MAGIC_NUMBER = 0xBA510;  // "BASIC" identifier

    public static void SaveBasicSource(ProgrammableChip chip, string basicCode)
    {
        var encoded = EncodeString(basicCode);
        chip.SetMemory(HEADER_SLOT, MAGIC_NUMBER + (encoded.Length << 16));
        for (int i = 0; i < encoded.Length; i++)
            chip.SetMemory(i + 1, encoded[i]);
    }

    public static string? LoadBasicSource(ProgrammableChip chip)
    {
        var header = (int)chip.GetMemory(HEADER_SLOT);
        if ((header & 0xFFFF) != MAGIC_NUMBER)
            return null;

        var length = header >> 16;
        var encoded = new double[length];
        for (int i = 0; i < length; i++)
            encoded[i] = chip.GetMemory(i + 1);

        return DecodeString(encoded);
    }
}
```

### Error Display

- Inline markers highlighting error lines in red
- Hover tooltips showing error message
- Summary output to game console

---

## Standalone Desktop Changes

### Auto-Load Detected Devices

```csharp
// In App.xaml.cs OnStartup
var detectedPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "BasicToMips", "detected_devices.json");

if (File.Exists(detectedPath))
{
    DeviceDatabase.ImportFromJson(detectedPath);
}
```

### Visual Programming

- Stays in `Basic10.Desktop/` only
- Uses `Basic10.Core.Compiler` for code generation
- No changes to visual programming architecture

---

## Device Sync Flow

```
┌─────────────────────┐
│   Stationeers       │
│   (with mods)       │
└──────────┬──────────┘
           │ Game loads
           ▼
┌─────────────────────┐
│  Basic10.GameMod    │
│  DeviceReflector    │
│  scans ILogicable   │
└──────────┬──────────┘
           │ Exports to
           ▼
┌─────────────────────┐
│  %LocalAppData%/    │
│  BasicToMips/       │
│  detected_devices   │
│  .json              │
└──────────┬──────────┘
           │ Imports from
           ▼
┌─────────────────────┐
│  Basic10.Desktop    │
│  DeviceDatabase     │
│  has all devices    │
└─────────────────────┘
```

---

## Migration Plan

### Phase 1: Extract Core
1. Create `Basic10.Core/` project (netstandard2.0)
2. Move `src/` contents to Core
3. Move `Data/DeviceDatabase.cs`, `Data/HashDictionary.cs` to Core
4. Add `Compiler.cs` facade API
5. Update Desktop to reference Core
6. Build and verify standalone works

### Phase 2: Create GameMod
1. Create `Basic10.GameMod/` project (net472)
2. Reference Basic10.Core
3. Add BepInEx/Harmony packages
4. Implement Plugin.cs, DeviceReflector.cs
5. Implement Harmony patches
6. Implement ChipStorage.cs
7. Test in-game

### Phase 3: Device Sync
1. Add ExportToJson/ImportFromJson to DeviceDatabase
2. GameMod exports on load
3. Desktop imports on startup
4. Test sync between apps

---

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Code sharing | Shared project in solution | Free, simple, single repo |
| Core target | netstandard2.0 | Compatible with both platforms |
| GameMod target | net472 | Stationeers/Unity requirement |
| IC editor integration | Compile on confirm | Minimal changes, like Slang |
| Device detection | On game load | Mods loaded at startup anyway |
| Device sync | Export to JSON | Both apps can read |
| Sync location | %LocalAppData%/BasicToMips/ | Consistent with existing data |
| Error display | Inline markers + hover | Clean UX, modern IDE feel |
| BASIC persistence | Chip memory slots | Can retrieve/edit later |
| Visual programming | Desktop only | WPF-specific, not for Unity |

---

*Last updated: 2025-12-17*
