# Visual Scripting Canvas - Quick Start Guide

## Opening the Test Window

```csharp
using BasicToMips.UI.VisualScripting;

// Create and show the test window
var window = new VisualScriptingWindow();
window.Show();
```

## Using the Canvas in Your Own Window

### 1. Add the XAML namespace
```xaml
<Window xmlns:canvas="clr-namespace:BasicToMips.UI.VisualScripting.Canvas">
```

### 2. Add the canvas control
```xaml
<canvas:VisualCanvas x:Name="MyCanvas"
                     SelectionChanged="Canvas_SelectionChanged"
                     CanvasClicked="Canvas_CanvasClicked"
                     ZoomChanged="Canvas_ZoomChanged"/>
```

### 3. Access the managers in code-behind
```csharp
// Grid control
MyCanvas.Grid.IsVisible = true;
MyCanvas.Grid.GridSize = 30;

// Selection management
MyCanvas.Selection.ClearSelection();

// Undo/Redo
MyCanvas.UndoRedo.Undo();
MyCanvas.UndoRedo.Redo();

// Pan and zoom
MyCanvas.Zoom = 1.5;
MyCanvas.PanOffset = new Point(100, 100);
MyCanvas.ResetView();
```

## Mouse Controls

| Action | Control |
|--------|---------|
| Pan | Middle-mouse drag |
| Pan (alt) | Space + Left-drag |
| Zoom | Mouse wheel |
| Select | Left click |
| Multi-select | Ctrl + Left click |
| Box select | Left drag |

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Space | Enable pan mode |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Delete | Delete selected items |

## Creating Selectable Items

Implement the `ISelectable` interface:

```csharp
public class MyNode : ISelectable
{
    public bool IsSelected { get; set; }

    public Rect Bounds => new Rect(X, Y, Width, Height);

    public bool HitTest(Point point)
    {
        return Bounds.Contains(point);
    }
}
```

## Using Undo/Redo

Create custom actions:

```csharp
public class MyAction : IUndoableAction
{
    public string Description => "My Custom Action";

    public void Execute()
    {
        // Do the action
    }

    public void Undo()
    {
        // Reverse the action
    }
}

// Use it
var action = new MyAction();
MyCanvas.UndoRedo.ExecuteAction(action);
```

Or use built-in actions:

```csharp
var action = new AddNodeAction(
    node,
    addAction: n => nodes.Add(n),
    removeAction: n => nodes.Remove(n),
    description: "Add Node"
);
MyCanvas.UndoRedo.ExecuteAction(action);
```

## Events

```csharp
// Selection changed
MyCanvas.SelectionChanged += (s, e) => {
    Console.WriteLine($"Selected {e.CurrentSelection.Count} items");
};

// Canvas clicked
MyCanvas.CanvasClicked += (s, e) => {
    Console.WriteLine($"Clicked at {e.Position}");
    if (e.IsControlPressed)
        Console.WriteLine("Ctrl was held");
};

// Zoom changed
MyCanvas.ZoomChanged += (s, e) => {
    Console.WriteLine($"Zoom: {MyCanvas.Zoom * 100}%");
};
```

## Common Patterns

### Clear Selection on Canvas Click
```csharp
MyCanvas.CanvasClicked += (s, e) => {
    if (!e.IsControlPressed)
        MyCanvas.Selection.ClearSelection();
};
```

### Zoom to Fit Content
```csharp
public void ZoomToFitItems(IEnumerable<ISelectable> items)
{
    if (!items.Any()) return;

    var bounds = items
        .Select(i => i.Bounds)
        .Aggregate((a, b) => Rect.Union(a, b));

    var scale = Math.Min(
        MyCanvas.ActualWidth / bounds.Width,
        MyCanvas.ActualHeight / bounds.Height
    ) * 0.9; // 90% to leave margin

    MyCanvas.Zoom = Math.Clamp(scale, 0.1, 4.0);

    // Center on bounds
    var centerX = bounds.Left + bounds.Width / 2;
    var centerY = bounds.Top + bounds.Height / 2;
    MyCanvas.PanOffset = new Point(
        MyCanvas.ActualWidth / 2 - centerX * MyCanvas.Zoom,
        MyCanvas.ActualHeight / 2 - centerY * MyCanvas.Zoom
    );
}
```

### Snap Item to Grid
```csharp
var snappedPos = MyCanvas.Grid.SnapToGrid(new Point(x, y));
item.X = snappedPos.X;
item.Y = snappedPos.Y;
```

### Batch Operations with Undo
```csharp
var actions = new List<IUndoableAction>();
foreach (var item in itemsToDelete)
{
    actions.Add(new RemoveNodeAction(
        item,
        addAction: n => items.Add(n),
        removeAction: n => items.Remove(n)
    ));
}

var compositeAction = new CompositeAction(
    actions,
    description: $"Delete {actions.Count} items"
);

MyCanvas.UndoRedo.ExecuteAction(compositeAction);
```

## Customization

### Change Grid Appearance
```csharp
MyCanvas.Grid.GridSize = 25;
MyCanvas.Grid.MajorLineInterval = 4;
MyCanvas.Grid.MinorLineColor = Color.FromArgb(20, 255, 255, 255);
MyCanvas.Grid.MajorLineColor = Color.FromArgb(40, 255, 255, 255);
```

### Adjust Undo History Size
```csharp
MyCanvas.UndoRedo.MaxHistorySize = 200;
```

### Custom Zoom Limits
You can modify the constants in VisualCanvas.xaml.cs:
```csharp
private const double MinZoom = 0.1;  // 10%
private const double MaxZoom = 4.0;  // 400%
private const double ZoomStep = 0.1; // 10% per scroll
```

## Tips

1. **Performance:** The canvas uses throttled rendering at 60fps. For large numbers of items, consider implementing culling (only render visible items).

2. **Coordinate Systems:** Mouse events give you coordinates in canvas space (already transformed). Use `e.GetPosition(DrawingCanvas)` to get canvas coordinates.

3. **Grid Snapping:** Call `Grid.SnapToGrid()` when placing or moving items if you want them aligned to the grid.

4. **Selection Management:** Always use the SelectionManager methods rather than directly modifying `IsSelected` to ensure events fire correctly.

5. **Undo/Redo:** Use `AddAction()` for already-executed actions, `ExecuteAction()` for actions that haven't been performed yet.

## Troubleshooting

### Grid not visible
- Check `Grid.IsVisible` is true
- Verify grid colors have sufficient alpha/contrast
- Ensure zoom level isn't too high/low

### Selection not working
- Ensure items implement `ISelectable` correctly
- Check `HitTest()` implementation
- Verify items are being added to the canvas

### Undo/Redo not working
- Ensure actions implement both `Execute()` and `Undo()`
- Check that `Undo()` properly reverses `Execute()`
- Use `StateChanged` event to update UI

## Next Steps

Once Phase 1B is complete, you'll be able to:
- Add visual nodes to the canvas
- Connect nodes with wires
- Edit node properties
- Generate BASIC code from the node graph
