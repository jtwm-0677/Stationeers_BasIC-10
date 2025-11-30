# Feature Expansion Plan - v1.2.0

## Overview
Comprehensive feature expansion to make Basic-10 the definitive BASIC to IC10 compiler for Stationeers.

## Version Update
- Update version from 1.1.6 → 1.2.0 in:
  - BasicToMips.csproj (`<Version>1.2.0</Version>`)
  - MainWindow.xaml (status bar)
  - MainWindow.xaml.cs (About dialog)

## UI Enhancements

### 1. Line Budget Indicator
- **Location**: Status bar (bottom right area)
- **Display**: "Lines: 47/128" with color coding
  - Green: < 100 lines
  - Yellow: 100-120 lines
  - Red: > 120 lines
- **Update**: After each compilation

### 2. Copy IC10 to Clipboard Button
- **Location**: Bottom action bar, next to Compile button
- **Icon**: Clipboard icon (&#xE8C8;)
- **Behavior**: Copy compiled IC10 output to clipboard, show confirmation in status

### 3. Recent Files Menu
- **Location**: File menu, after "Open"
- **Capacity**: Last 10 files
- **Storage**: UserSettings.cs
- **Display**: File name with full path tooltip

### 4. Studio Logo Resize
- **Change**: Increase from 42px to 52px height

## Editor Features

### 5. Code Folding
- **Implementation**: AvalonEdit FoldingStrategy
- **Fold regions**: IF/ENDIF, WHILE/WEND, FOR/NEXT, SUB/END SUB, FUNCTION/END FUNCTION
- **Visual**: +/- icons in margin

### 6. Find & Replace Dialog
- **Shortcut**: Ctrl+F (Find), Ctrl+H (Replace)
- **Features**:
  - Case sensitive toggle
  - Whole word toggle
  - Regex support toggle
  - Find Next/Previous
  - Replace/Replace All
- **UI**: Floating panel at top of editor

### 7. Split View Toggle
- **Location**: View menu + toolbar button
- **Modes**: Vertical (current), Horizontal, Editor Only
- **Shortcut**: Ctrl+Shift+V to cycle

## Documentation & Navigation

### 8. Contextual F1 Help
- **Behavior**: Press F1 with cursor on keyword → open Docs panel to relevant section
- **Keywords to handle**: All BASIC keywords, functions, device properties
- **Implementation**: Map keywords to documentation tab + scroll position

### 9. Variable/Label List Panel
- **Location**: Collapsible panel on left side of editor (or tab in docs panel)
- **Sections**:
  - Variables (VAR declarations)
  - Constants (CONST/DEFINE)
  - Labels (label:)
  - Aliases (ALIAS declarations)
- **Click action**: Jump to definition in code

## Advanced Features

### 10. Multiple Tabs
- **Implementation**: TabControl wrapping editor
- **Features**:
  - New tab button (+)
  - Close button on each tab (x)
  - Tab shows filename (or "Untitled")
  - Modified indicator (*)
  - Right-click context menu (Close, Close Others, Close All)
- **Limit**: Configurable, default 10 tabs

### 11. Import IC10 (Decompiler Integration)
- **Location**: File menu → "Import IC10..."
- **Behavior**:
  - Open file dialog for .ic10 files
  - Use existing IC10Decompiler to convert to BASIC
  - Open in new tab
- **Also**: Add "Decompile from Clipboard" option

### 12. Code Snippets Palette
- **Location**: View menu toggle, floating panel
- **Categories**: Loops, Conditionals, Device, Math, Patterns
- **Insert**: Double-click or drag to insert at cursor
- **Custom**: Allow user-defined snippets (saved to Data folder)

## Simulation

### 13. IC10 Simulator Panel
- **Location**: Toggleable panel (View menu)
- **Features**:
  - Step/Run/Pause/Reset buttons
  - Register view (r0-r15, sp, ra)
  - Stack view (last 16 values)
  - Device mockup (d0-d5 with editable properties)
  - Current line highlight in IC10 output
  - Execution speed slider
- **Integration**: Use existing Simulator/IC10Simulator.cs

## Settings & Optimization

### 14. Optimization Modes
- **Location**: Build menu → "Output Mode" submenu
- **Modes**:
  - Readable (default): Include comments, spacing
  - Compact: Minimize lines, strip comments
  - Debug: Include source line numbers (existing feature)
- **Storage**: UserSettings

### 15. Customizable Keyboard Shortcuts (Future consideration)
- Defer to future version

## Implementation Groups (For Parallel Agents)

### Agent 1: Core UI Updates
- Version bump (1.1.6 → 1.2.0)
- Logo resize (42px → 52px)
- Line budget indicator
- Copy to clipboard button

### Agent 2: File Management
- Recent files menu
- Import IC10 decompiler integration

### Agent 3: Editor Enhancements
- Code folding
- Find & Replace dialog

### Agent 4: Navigation & Help
- Contextual F1 help
- Variable/Label list panel

### Agent 5: Advanced UI
- Multiple tabs system
- Split view toggle

### Agent 6: Tools & Simulation
- Code snippets palette
- Optimization modes
- IC10 Simulator panel integration

## Testing Checklist
- [ ] All version numbers updated to 1.2.0
- [ ] Logo displays correctly at new size
- [ ] Line count updates after compile
- [ ] Copy to clipboard works and shows confirmation
- [ ] Recent files persist across sessions
- [ ] Code folding works for all block types
- [ ] Find/Replace works with all options
- [ ] F1 opens correct documentation
- [ ] Variable list updates on code change
- [ ] Multiple tabs can be opened/closed
- [ ] Import IC10 successfully decompiles
- [ ] Snippets can be inserted
- [ ] Simulator steps through code correctly
- [ ] Optimization modes affect output
