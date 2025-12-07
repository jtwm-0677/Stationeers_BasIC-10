# Phase 6A: Experience Mode System - Complete File List

## New Files Created (11)

### Core System Files (3)
1. **ExperienceMode.cs** (1,816 bytes)
   - Location: `UI/VisualScripting/ExperienceMode.cs`
   - Contains: ExperienceLevel, NodeLabelStyle, ErrorMessageStyle enums

2. **ExperienceModeSettings.cs** (6,994 bytes)
   - Location: `UI/VisualScripting/ExperienceModeSettings.cs`
   - Contains: Configuration class with preset factory methods

3. **ExperienceModeManager.cs** (8,142 bytes)
   - Location: `UI/VisualScripting/ExperienceModeManager.cs`
   - Contains: Singleton manager with mode switching and events

### Helper Classes (2)
4. **NodeLabelProvider.cs** (9,738 bytes)
   - Location: `UI/VisualScripting/NodeLabelProvider.cs`
   - Contains: Label generation for all node types (Friendly/Mixed/Technical)

5. **NodePaletteFilter.cs** (7,221 bytes)
   - Location: `UI/VisualScripting/NodePaletteFilter.cs`
   - Contains: Node filtering by experience level

### UI Components (4)
6. **ExperienceModeSelector.xaml** (6,947 bytes)
   - Location: `UI/VisualScripting/ExperienceModeSelector.xaml`
   - Contains: Toolbar mode selector control (XAML)

7. **ExperienceModeSelector.xaml.cs** (3,184 bytes)
   - Location: `UI/VisualScripting/ExperienceModeSelector.xaml.cs`
   - Contains: Mode selector logic

8. **CustomModeDialog.xaml** (8,889 bytes)
   - Location: `UI/VisualScripting/CustomModeDialog.xaml`
   - Contains: Custom settings dialog (XAML)

9. **CustomModeDialog.xaml.cs** (9,047 bytes)
   - Location: `UI/VisualScripting/CustomModeDialog.xaml.cs`
   - Contains: Custom dialog logic

### Documentation (3)
10. **PHASE_6A_COMPLETE.md**
    - Location: `UI/VisualScripting/PHASE_6A_COMPLETE.md`
    - Contains: Complete implementation documentation

11. **PHASE_6A_QUICK_REFERENCE.md**
    - Location: `UI/VisualScripting/PHASE_6A_QUICK_REFERENCE.md`
    - Contains: Quick reference and code examples

12. **PHASE_6A_FILE_LIST.md** (this file)
    - Location: `UI/VisualScripting/PHASE_6A_FILE_LIST.md`
    - Contains: Complete file listing

## Modified Files (5)

### Settings Integration (1)
1. **SettingsService.cs**
   - Location: `UI/Services/SettingsService.cs`
   - Changes:
     - Added `using BasicToMips.UI.VisualScripting;`
     - Added `ExperienceLevel ExperienceMode` property
     - Added `ExperienceModeSettings? CustomModeSettings` property
     - Updated `Load()` method to load experience mode settings
     - Updated `Save()` method to save experience mode settings
     - Updated `SettingsData` private class with new properties

### Node System Integration (1)
2. **NodeFactory.cs**
   - Location: `UI/VisualScripting/Nodes/NodeFactory.cs`
   - Changes:
     - Added `GetFilteredNodes(ExperienceModeSettings)` method
     - Returns filtered nodes based on experience mode settings

### Visual Scripting Window Integration (2)
3. **VisualScriptingWindow.xaml**
   - Location: `UI/VisualScripting/VisualScriptingWindow.xaml`
   - Changes:
     - Added `ExperienceModeSelector` control to toolbar
     - Added separator bars around selector
     - Added `ExperienceMode_Changed` event handler

4. **VisualScriptingWindow.xaml.cs**
   - Location: `UI/VisualScripting/VisualScriptingWindow.xaml.cs`
   - Changes:
     - Constructor: Subscribe to `ExperienceModeManager.ModeChanged`
     - Constructor: Apply initial mode settings
     - Added `ExperienceMode_Changed()` handler
     - Added `OnExperienceModeChanged()` handler
     - Added `ApplyExperienceMode()` method
     - Updated `OnClosed()` to unsubscribe from events

### Code Panel Integration (1)
5. **CodePanel.xaml.cs**
   - Location: `UI/VisualScripting/CodePanel.xaml.cs`
   - Changes:
     - Added `ShowIC10Toggle` property
     - Added `ShowLineNumbers` property

## File Size Summary

```
New Files:        61,976 bytes (12 files)
Modified Files:   ~2 KB changes (5 files)
Total Impact:     ~64 KB
```

## Directory Structure

```
UI/
├── Services/
│   └── SettingsService.cs              [MODIFIED]
│
└── VisualScripting/
    ├── ExperienceMode.cs                [NEW]
    ├── ExperienceModeSettings.cs        [NEW]
    ├── ExperienceModeManager.cs         [NEW]
    ├── NodeLabelProvider.cs             [NEW]
    ├── NodePaletteFilter.cs             [NEW]
    ├── ExperienceModeSelector.xaml      [NEW]
    ├── ExperienceModeSelector.xaml.cs   [NEW]
    ├── CustomModeDialog.xaml            [NEW]
    ├── CustomModeDialog.xaml.cs         [NEW]
    ├── VisualScriptingWindow.xaml       [MODIFIED]
    ├── VisualScriptingWindow.xaml.cs    [MODIFIED]
    ├── CodePanel.xaml.cs                [MODIFIED]
    ├── PHASE_6A_COMPLETE.md             [NEW]
    ├── PHASE_6A_QUICK_REFERENCE.md      [NEW]
    ├── PHASE_6A_FILE_LIST.md            [NEW]
    │
    └── Nodes/
        └── NodeFactory.cs               [MODIFIED]
```

## Build Requirements

### New Dependencies
None - All code uses existing dependencies:
- System.Windows (WPF)
- System.Text.Json (SettingsService)
- System.Collections.Generic
- System.Linq

### XAML Namespaces
The following namespace is used in VisualScriptingWindow.xaml:
```xaml
xmlns:local="clr-namespace:BasicToMips.UI.VisualScripting"
```

This is already defined, so no changes to XAML namespaces needed.

## Compilation Notes

1. All files should compile without errors
2. No external dependencies required
3. All XAML files have matching .xaml.cs code-behind
4. All classes are properly namespaced
5. ExperienceModeManager is a singleton (thread-safe)
6. All enums are in the BasicToMips.UI.VisualScripting namespace

## Integration Checklist

- [x] Core enums and settings defined
- [x] Manager singleton implemented
- [x] Label provider for all node types
- [x] Palette filter with node lists
- [x] UI selector control
- [x] Custom settings dialog
- [x] SettingsService integration
- [x] NodeFactory integration
- [x] VisualScriptingWindow integration
- [x] CodePanel properties added
- [x] Documentation complete

## Next Phase Dependencies

**Phase 6B (Node Palette UI) will need:**
- `NodeFactory.GetFilteredNodes()` ✅ Ready
- `NodeLabelProvider.GetLabel()` ✅ Ready
- `ExperienceModeManager.ModeChanged` event ✅ Ready

**Phase 6C (Properties Panel) will need:**
- `ExperienceModeSettings.ShowAdvancedProperties` ✅ Ready
- `ExperienceModeManager.CurrentSettings` ✅ Ready

**Phase 3 (Node Rendering) will need:**
- `NodeLabelProvider` for dynamic labels ✅ Ready
- `ShowExecutionPins` setting ✅ Ready
- `ShowDataTypes` setting ✅ Ready

**Phase 5 (Error System) will need:**
- `ErrorMessageStyle` enum ✅ Ready
- `ExperienceModeSettings.ErrorMessageStyle` ✅ Ready

## Testing Files Created

No unit test files were created as part of this phase. The system is designed to be tested through:
1. Visual testing via the VisualScriptingWindow
2. Mode switching behavior
3. CustomModeDialog functionality
4. Settings persistence

## Version Control

All files are ready to commit. Suggested commit message:

```
Add Experience Mode System (Phase 6A)

- Implement four experience modes (Beginner, Intermediate, Expert, Custom)
- Add dynamic node label generation (Friendly/Mixed/Technical)
- Add node filtering by experience level
- Add mode selector UI control
- Add custom mode configuration dialog
- Integrate with SettingsService for persistence
- Update VisualScriptingWindow with mode switching
- Add comprehensive documentation

New files: 12
Modified files: 5
Total: 17 files changed
```

## Deployment Notes

1. **No database changes required**
2. **No API changes required**
3. **Settings file format updated** - but backwards compatible (new fields optional)
4. **First launch**: Will default to Beginner mode
5. **Existing users**: Will default to Beginner mode (can switch immediately)
6. **Custom settings**: Only created when user opens CustomModeDialog

## File Checksums (for verification)

Run this command to verify all files are present:

```bash
ls -la "C:\Development\Stationeers Stuff\BASICtoMIPS_ByDogTired\UI\VisualScripting" | grep -E "(Experience|Custom|NodeLabel|NodePalette)"
```

Expected output should show all 9 new .cs and .xaml files.

## End of Phase 6A

All components successfully implemented and documented.
Ready for integration with Phase 2 (Node Palette) and Phase 3 (Node Rendering).
