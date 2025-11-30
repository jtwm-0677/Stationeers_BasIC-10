# BASIC-10 Feature Roadmap v1.3.0+

## Overview
This document outlines all planned features organized by phase and priority.

---

## Phase 1: Editor Essentials (High Impact, Medium Effort)

### 1.1 Go to Definition
- **Click on label/SUB/FUNCTION name** → jumps to definition
- **Ctrl+Click** or **F12** keyboard shortcut
- Highlight all references to symbol
- **Files**: `MainWindow.xaml.cs`, new `NavigationService.cs`

### 1.2 Breakpoints
- **Click gutter** to toggle breakpoint (red dot)
- Breakpoint list panel
- Conditional breakpoints: `break when x > 10`
- **Files**: `IC10Simulator.cs`, `MainWindow.xaml`, new `BreakpointManager.cs`

### 1.3 Watch Variables Panel
- Add variables to watch list
- Shows current value during simulation
- Edit values during pause
- **Files**: `SimulatorWindow.xaml`, `IC10Simulator.cs`

### 1.4 Source Maps
- Map each IC10 line back to BASIC source line
- Click IC10 line → highlights BASIC line
- Error messages show BASIC line numbers
- **Files**: `MipsGenerator.cs`, `CompilerService.cs`, new `SourceMap.cs`

### 1.5 Bookmarks
- **Ctrl+B** to toggle bookmark on line
- **F2** to jump to next bookmark
- Bookmark list panel
- Named bookmarks
- **Files**: `MainWindow.xaml.cs`, new `BookmarkService.cs`

---

## Phase 2: Code Intelligence (High Impact, High Effort)

### 2.1 Unused Variable Warnings
- Static analysis pass after parsing
- Highlight unused variables in editor
- "Variable 'x' is declared but never used"
- **Files**: new `StaticAnalyzer.cs`, `ErrorChecker.cs`

### 2.2 Code Formatting / Auto-Indent
- **Ctrl+Shift+F** to format document
- Consistent indentation (configurable: 2/4 spaces or tabs)
- Align = signs in consecutive assignments
- **Files**: new `CodeFormatter.cs`, `MainWindow.xaml.cs`

### 2.3 Refactoring: Rename Symbol
- **F2** on symbol → rename all occurrences
- Preview changes before applying
- Works for variables, labels, SUBs, FUNCTIONs
- **Files**: new `RefactoringService.cs`

### 2.4 Refactoring: Extract Function
- Select code → Extract to SUB/FUNCTION
- Auto-detect parameters needed
- Replace selection with CALL
- **Files**: `RefactoringService.cs`

### 2.5 Step Into/Over/Out
- **F11** Step Into (enter SUB/FUNCTION)
- **F10** Step Over (execute SUB as single step)
- **Shift+F11** Step Out (run to RETURN)
- **Files**: `IC10Simulator.cs`

---

## Phase 3: Language Extensions (Medium Impact, High Effort)

### 3.1 INCLUDE Statement
```basic
INCLUDE "library.bas"
INCLUDE "utils/math.bas"
```
- Preprocessor-style inclusion
- Relative path resolution from current file
- Circular include detection
- **Files**: new `Preprocessor.cs`, `Lexer.cs`, `Parser.cs`

### 3.2 ON GOTO / ON GOSUB
```basic
ON index GOTO label1, label2, label3
ON choice GOSUB handler1, handler2
```
- Computed jumps based on index
- **Files**: `Parser.cs`, `AstNode.cs`, `MipsGenerator.cs`

### 3.3 DATA / READ / RESTORE
```basic
DATA 10, 20, 30, 40, 50
READ x, y, z
RESTORE
```
- Compile-time data tables
- Read pointer management
- **Files**: `Parser.cs`, `AstNode.cs`, `MipsGenerator.cs`

### 3.4 Macros
```basic
#MACRO CLAMP(val, min, max) = MAX(min, MIN(max, val))
result = CLAMP(temp, 0, 100)
```
- Text substitution macros
- Parameterized macros
- **Files**: `Preprocessor.cs`, `Lexer.cs`

### 3.5 Multi-Dimensional Arrays
```basic
DIM grid(10, 10)
grid(5, 3) = 42
```
- 2D array support (row-major storage)
- Bounds checking option
- **Files**: `Parser.cs`, `AstNode.cs`, `MipsGenerator.cs`

### 3.6 String Variables (Limited)
```basic
DIM name AS STRING
name = "Sensor1"
deviceHash = HASH(name)
```
- Compile-time string constants
- HASH() function for device names
- No runtime string manipulation (IC10 limitation)
- **Files**: `Lexer.cs`, `Parser.cs`, `MipsGenerator.cs`

### 3.7 Structs/Records
```basic
TYPE SensorData
    temperature AS FLOAT
    pressure AS FLOAT
    isActive AS BOOLEAN
END TYPE

DIM sensor AS SensorData
sensor.temperature = 293
```
- User-defined composite types
- Stored in contiguous registers
- **Files**: `Parser.cs`, `AstNode.cs`, `MipsGenerator.cs`, `RegisterAllocator.cs`

---

## Phase 4: Compiler Improvements (Medium Impact, Medium Effort)

### 4.1 Dead Code Elimination
- Remove unreachable code after GOTO/RETURN/END
- Remove unused SUBs/FUNCTIONs
- Warning for dead code
- **Files**: new `Optimizer.cs`, `MipsGenerator.cs`

### 4.2 Inline Expansion
- Automatically inline small functions (< 5 lines)
- `INLINE` keyword to force inlining
- Reduces call overhead
- **Files**: `Optimizer.cs`, `MipsGenerator.cs`

### 4.3 Improved Register Allocation
- Graph coloring algorithm
- Register spilling to stack when needed
- Optimal register usage
- **Files**: `RegisterAllocator.cs`

### 4.4 Constant Folding
- Evaluate constant expressions at compile time
- `x = 2 + 3` → `x = 5`
- Propagate constants through code
- **Files**: `Optimizer.cs`

---

## Phase 5: Project & Integration (Medium Impact, Medium Effort)

### 5.1 Project Files
```
myproject.basproj
├── main.bas
├── lib/
│   ├── utils.bas
│   └── devices.bas
└── config.json
```
- Multi-file projects
- Build order management
- Project-wide settings
- **Files**: new `ProjectService.cs`, `MainWindow.xaml.cs`

### 5.2 Git Integration
- Show file status (modified, staged)
- Commit from within editor
- Diff view for changes
- Branch display
- **Files**: new `GitService.cs`, `MainWindow.xaml`

### 5.3 Session Restore
- Remember open tabs on exit
- Restore cursor positions
- Remember split view state
- Recent projects list
- **Files**: `SettingsService.cs`, `MainWindow.xaml.cs`

### 5.4 Deploy Improvements
- Direct copy to IC Housing (via API mod if installed)
- Multiple deploy targets
- Deploy history
- **Files**: `CompilerService.cs`, new `DeployService.cs`

---

## Phase 6: Editor Polish (Lower Impact, Lower Effort)

### 6.1 Undo History Panel
- Visual list of undo/redo actions
- Click to jump to any state
- Branching undo tree
- **Files**: `MainWindow.xaml`, new `UndoHistoryPanel.xaml`

### 6.2 Minimap
- Code overview on right side
- Click to navigate
- Highlight current viewport
- **Files**: `MainWindow.xaml` (AvalonEdit supports this)

### 6.3 Multiple Cursors
- **Ctrl+D** to select next occurrence
- **Alt+Click** to add cursor
- Type at multiple locations
- **Files**: Requires AvalonEdit extension or custom implementation

### 6.4 Diff View
- Compare two files side-by-side
- Highlight differences
- Merge changes
- **Files**: new `DiffWindow.xaml`, `DiffService.cs`

### 6.5 Custom Snippets
- User-defined code snippets
- Snippet editor UI
- Import/export snippets
- **Files**: `SettingsService.cs`, new `SnippetEditor.xaml`

### 6.6 Keyboard Shortcuts Config
- Customizable key bindings
- Import/export configurations
- Conflict detection
- **Files**: new `KeyBindingsWindow.xaml`, `SettingsService.cs`

### 6.7 Plugin System
- Load external DLLs
- Plugin API for extensions
- Plugin marketplace concept
- **Files**: new `PluginManager.cs`, `IPlugin.cs`

---

## Implementation Priority Matrix

| Priority | Feature | Effort | Impact |
|----------|---------|--------|--------|
| 1 | Go to Definition | Low | High |
| 2 | Source Maps | Medium | High |
| 3 | Breakpoints | Medium | High |
| 4 | Watch Variables | Medium | High |
| 5 | Unused Variable Warnings | Medium | High |
| 6 | Code Formatting | Low | Medium |
| 7 | Bookmarks | Low | Medium |
| 8 | INCLUDE Statement | Medium | High |
| 9 | Step Into/Over/Out | Medium | Medium |
| 10 | Rename Symbol | Medium | Medium |
| 11 | ON GOTO/GOSUB | Low | Low |
| 12 | DATA/READ/RESTORE | Medium | Low |
| 13 | Dead Code Elimination | Medium | Medium |
| 14 | Project Files | High | Medium |
| 15 | Session Restore | Low | Medium |
| 16 | Custom Snippets | Low | Low |
| 17 | Macros | Medium | Medium |
| 18 | Multi-Dim Arrays | Medium | Low |
| 19 | String Variables | High | Low |
| 20 | Structs/Records | High | Low |
| 21 | Git Integration | High | Low |
| 22 | Minimap | Low | Low |
| 23 | Multiple Cursors | High | Low |
| 24 | Plugin System | Very High | Low |

---

## Suggested Implementation Order

### Sprint 1: Navigation & Debugging Foundation
1. Go to Definition
2. Source Maps
3. Breakpoints
4. Watch Variables

### Sprint 2: Code Quality
5. Unused Variable Warnings
6. Code Formatting
7. Bookmarks
8. Step Into/Over/Out

### Sprint 3: Language Power
9. INCLUDE Statement
10. Rename Symbol
11. ON GOTO/GOSUB
12. DATA/READ/RESTORE

### Sprint 4: Optimization
13. Dead Code Elimination
14. Constant Folding
15. Inline Expansion

### Sprint 5: Project Management
16. Project Files
17. Session Restore
18. Custom Snippets

### Sprint 6: Advanced Language
19. Macros
20. Multi-Dim Arrays
21. String Variables (limited)
22. Structs/Records

### Sprint 7: Polish
23. Git Integration
24. Minimap
25. Diff View
26. Keyboard Shortcuts Config
27. Plugin System

---

## Version Targets

- **v1.3.0**: Sprint 1 (Navigation & Debugging)
- **v1.4.0**: Sprint 2 (Code Quality)
- **v1.5.0**: Sprint 3 (Language Power)
- **v1.6.0**: Sprint 4 (Optimization)
- **v1.7.0**: Sprint 5 (Project Management)
- **v1.8.0**: Sprint 6 (Advanced Language)
- **v2.0.0**: Sprint 7 (Polish) + stability

---

*Last updated: 2025-11-28*
