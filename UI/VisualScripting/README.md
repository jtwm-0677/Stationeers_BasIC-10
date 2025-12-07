# Visual Scripting Infrastructure - Phase 1A

This folder contains the foundational infrastructure for Basic-10's visual scripting feature (v3.0).

## Overview

Phase 1A implements the core canvas infrastructure that will support node-based visual scripting. The canvas provides smooth pan and zoom, selection management, and a robust undo/redo system.

## Architecture

### Canvas/ - Core Canvas Components

#### VisualCanvas (VisualCanvas.xaml/xaml.cs)
The main visual scripting canvas control. This is a WPF UserControl that can be embedded in any window.

**Features:**
- **Pan Navigation:**
  - Middle-mouse drag
  - Space + left-drag
  - Smooth, responsive panning

- **Zoom:**
  - Mouse wheel zooming
  - Range: 10% to 400%
  - Zooms centered on cursor position
  - Real-time zoom indicator

- **Selection:**
  - Click to select single items
  - Ctrl+click to toggle selection
  - Drag to box select multiple items
  - Visual feedback with selection rectangle

- **Performance:**
  - Throttled rendering at 60fps
  - Uses DrawingVisual for efficient rendering
  - Supports large canvases with minimal overhead

**Events:**
- `SelectionChanged` - Fired when selection changes
- `CanvasClicked` - Fired when canvas is clicked
- `ZoomChanged` - Fired when zoom level changes

**Properties:**
- `Zoom` - Current zoom level (0.1 to 4.0)
- `PanOffset` - Current pan offset
- `Grid` - Access to grid manager
- `Selection` - Access to selection manager
- `UndoRedo` - Access to undo/redo manager

#### CanvasGrid (CanvasGrid.cs)
Renders and manages the grid background.

**Features:**
- Adjustable grid size (default: 20px)
- Minor grid lines (light)
- Major grid lines (darker, every 5 lines)
- Toggle visibility
- Snap-to-grid support (for future use)
- Scales correctly with zoom

**Properties:**
- `IsVisible` - Toggle grid visibility
- `GridSize` - Size of grid squares in pixels
- `MajorLineInterval` - How often to draw major lines
- `MinorLineColor` - Color for minor grid lines
- `MajorLineColor` - Color for major grid lines

**Methods:**
- `Render()` - Renders the grid to a DrawingContext
- `SnapToGrid()` - Snaps a point to nearest grid intersection

#### SelectionManager (SelectionManager.cs)
Manages selection state for canvas items.

**Features:**
- Single selection (click)
- Multi-selection (Ctrl+click)
- Box selection (drag rectangle)
- Hit testing
- Selection change events

**Interface: ISelectable**
Items that can be selected must implement:
- `IsSelected` - Get/set selection state
- `Bounds` - Bounding rectangle
- `HitTest(Point)` - Test if point is within item

**Properties:**
- `SelectedItems` - Read-only list of selected items
- `HasSelection` - Whether any items are selected
- `BoxSelectionRect` - Current box selection rectangle
- `IsBoxSelecting` - Whether box selection is active

**Events:**
- `SelectionChanged` - Fired when selection changes
- `BoxSelectionChanged` - Fired when box selection rectangle changes

**Methods:**
- `SelectSingle(item)` - Select one item, clear others
- `ToggleSelection(item)` - Toggle selection (for Ctrl+click)
- `ClearSelection()` - Clear all selections
- `BeginBoxSelection(point)` - Start box selection
- `UpdateBoxSelection(point)` - Update box during drag
- `EndBoxSelection(items)` - Complete box selection
- `HitTest(point, items)` - Find topmost item at point

#### UndoRedoManager (UndoRedoManager.cs)
Stack-based undo/redo system with keyboard shortcuts.

**Features:**
- Stack-based history (max 100 actions by default)
- Ctrl+Z for undo
- Ctrl+Y for redo
- Action descriptions for UI
- State change events

**Interface: IUndoableAction**
Actions must implement:
- `Description` - Human-readable description
- `Execute()` - Perform the action
- `Undo()` - Reverse the action

**Properties:**
- `CanUndo` - Whether undo is available
- `CanRedo` - Whether redo is available
- `NextUndoDescription` - Description of next undo
- `NextRedoDescription` - Description of next redo
- `MaxHistorySize` - Maximum actions to keep

**Events:**
- `StateChanged` - Fired when undo/redo state changes

**Methods:**
- `ExecuteAction(action)` - Execute and add to history
- `AddAction(action)` - Add already-executed action
- `Undo()` - Undo last action
- `Redo()` - Redo last undone action
- `Clear()` - Clear all history

**Built-in Actions:**
- `AddNodeAction` - For adding nodes
- `RemoveNodeAction` - For removing nodes
- `MoveNodeAction` - For moving nodes
- `CompositeAction` - Combine multiple actions

### Test Window

**VisualScriptingWindow.xaml/xaml.cs**
A test/demo window showing the canvas in action.

**Features:**
- Reset view button
- Zoom to fit button
- Toggle grid visibility
- Undo/redo buttons
- Status indicators (selection count, zoom level, undo/redo state)
- Interactive help text

## Usage Example

```xaml
<Window xmlns:canvas="clr-namespace:BasicToMips.UI.VisualScripting.Canvas">
    <canvas:VisualCanvas x:Name="MyCanvas"
                         SelectionChanged="Canvas_SelectionChanged"
                         ZoomChanged="Canvas_ZoomChanged"/>
</Window>
```

```csharp
// Access managers
var grid = MyCanvas.Grid;
var selection = MyCanvas.Selection;
var undoRedo = MyCanvas.UndoRedo;

// Toggle grid
grid.IsVisible = true;
grid.GridSize = 30;

// Handle selection
MyCanvas.SelectionChanged += (s, e) => {
    Console.WriteLine($"Selected: {e.CurrentSelection.Count} items");
};

// Use undo/redo
var action = new AddNodeAction(node, AddNode, RemoveNode);
undoRedo.ExecuteAction(action);
```

## Keyboard Shortcuts

- **Space** - Hold to enable pan mode (cursor changes to hand)
- **Space + Drag** - Pan the canvas
- **Ctrl + Z** - Undo last action
- **Ctrl + Y** - Redo last undone action
- **Delete** - Delete selected items (to be implemented with nodes)

## Mouse Controls

- **Middle-mouse drag** - Pan the canvas
- **Mouse wheel** - Zoom in/out (centered on cursor)
- **Left click** - Select item (or click canvas to deselect)
- **Ctrl + Left click** - Toggle item selection
- **Left drag** - Box select multiple items

## Performance Characteristics

- **Rendering:** Throttled to 60fps using DispatcherTimer
- **Memory:** Minimal overhead, uses WPF's DrawingVisual
- **Scalability:** Supports large canvases with thousands of items
- **Responsiveness:** All operations are smooth and immediate

## Future Phases

### Phase 1B - Node System (Next)
- Base node class
- Node visual representation
- Node properties panel
- Node library/palette

### Phase 2 - Connection System
- Wire drawing and routing
- Connection validation
- Input/output ports
- Data flow visualization

### Phase 3 - BASIC Integration
- Node types for BASIC statements
- Code generation from node graph
- Bidirectional sync with text editor
- Debugging integration

## Architecture Notes

### Why DrawingVisual?
We use WPF's DrawingVisual for the grid and selection rendering because:
1. It's much faster than creating UIElements
2. Supports 60fps rendering even with complex grids
3. Minimal memory overhead
4. Full control over rendering pipeline

### Why Separate Managers?
The canvas delegates to specialized managers (Grid, Selection, UndoRedo) because:
1. Single Responsibility Principle
2. Easier testing of individual components
3. Managers can be reused in other contexts
4. Clear separation of concerns

### Transform Strategy
We use WPF's TransformGroup (ScaleTransform + TranslateTransform) because:
1. Hardware-accelerated transformations
2. Automatic coordinate system handling
3. Mouse events get transformed coordinates
4. Works seamlessly with WPF's rendering pipeline

## Testing

To test the canvas:

1. Build the project:
   ```bash
   dotnet build -c Release
   ```

2. The VisualScriptingWindow can be opened from code:
   ```csharp
   var window = new VisualScriptingWindow();
   window.Show();
   ```

3. Test pan, zoom, selection, and undo/redo functionality.

## Version History

- **v3.0.0** - Initial visual scripting infrastructure (Phase 1A)
  - Canvas with pan/zoom
  - Selection manager
  - Undo/redo system
  - Grid rendering
