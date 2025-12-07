using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Services;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace BasicToMips.UI.VisualScripting.Canvas;

/// <summary>
/// Main visual scripting canvas with pan, zoom, selection, and undo/redo support.
/// </summary>
public partial class VisualCanvas : System.Windows.Controls.UserControl
{
    // Canvas state
    private double _zoom = 1.0;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 0.1;

    // Pan state
    private bool _isPanning;
    private bool _isSpacePressed;
    private Point _panStartPoint;
    private Point _panStartOffset;

    // Selection state
    private Point? _selectionStartPoint;
    private WpfRectangle? _selectionRect;

    // Managers
    private readonly CanvasGrid _grid;
    private readonly SelectionManager _selectionManager;
    private readonly UndoRedoManager _undoRedoManager;

    // Node tracking
    private readonly Dictionary<NodeBase, NodeControl> _nodeControls = new();
    private readonly HashSet<NodeBase> _selectedNodes = new();

    // Wire creation state
    private NodePin? _wireStartPin;
    private System.Windows.Shapes.Line? _tempWireLine;
    private readonly List<WireVisual> _wires = new();

    // Wire selection state
    private WireVisual? _selectedWire;
    private WireVisual? _hoveredWire;

    // Wire drag-to-disconnect state
    private WireVisual? _draggingWire;
    private NodePin? _dragSourcePin;
    private bool _isDraggingFromPin;

    /// <summary>
    /// Event raised when nodes are deleted.
    /// </summary>
    public event EventHandler<NodesDeletedEventArgs>? NodesDeleted;

    /// <summary>
    /// Event raised when a wire is created.
    /// </summary>
    public event EventHandler<WireCreatedEventArgs>? WireCreated;

    /// <summary>
    /// Event raised when a wire is deleted.
    /// </summary>
    public event EventHandler<WireDeletedEventArgs>? WireDeleted;

    /// <summary>
    /// Gets the grid manager.
    /// </summary>
    public CanvasGrid Grid => _grid;

    /// <summary>
    /// Gets the selection manager.
    /// </summary>
    public SelectionManager Selection => _selectionManager;

    /// <summary>
    /// Gets the undo/redo manager.
    /// </summary>
    public UndoRedoManager UndoRedo => _undoRedoManager;

    /// <summary>
    /// Gets or sets the current zoom level (0.1 to 4.0).
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            var newZoom = Math.Clamp(value, MinZoom, MaxZoom);
            if (Math.Abs(_zoom - newZoom) > 0.001)
            {
                _zoom = newZoom;
                ScaleTransform.ScaleX = _zoom;
                ScaleTransform.ScaleY = _zoom;
                UpdateZoomIndicator();
                UpdateGridBackground();
                ZoomChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the pan offset.
    /// </summary>
    public Point PanOffset
    {
        get => new Point(TranslateTransform.X, TranslateTransform.Y);
        set
        {
            TranslateTransform.X = value.X;
            TranslateTransform.Y = value.Y;
            UpdateGridBackground();
        }
    }

    /// <summary>
    /// Event raised when the selection changes.
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Event raised when the canvas is clicked.
    /// </summary>
    public event EventHandler<CanvasClickedEventArgs>? CanvasClicked;

    /// <summary>
    /// Event raised when the zoom level changes.
    /// </summary>
    public event EventHandler? ZoomChanged;

    /// <summary>
    /// Event raised when a node's property is changed (e.g., via TextBox edit).
    /// </summary>
    public event EventHandler<NodePropertyChangedEventArgs>? NodePropertyChanged;

    public VisualCanvas()
    {
        InitializeComponent();

        _grid = new CanvasGrid();
        _selectionManager = new SelectionManager();
        _undoRedoManager = new UndoRedoManager();

        // Subscribe to events
        _grid.VisibilityChanged += (s, e) => UpdateGridBackground();
        _grid.GridSizeChanged += (s, e) => UpdateGridBackground();
        _selectionManager.SelectionChanged += OnSelectionChanged;
        _selectionManager.BoxSelectionChanged += OnBoxSelectionChanged;
        _undoRedoManager.StateChanged += (s, e) => OnUndoRedoStateChanged();

        // Setup keyboard handling
        Loaded += OnLoaded;
        UpdateZoomIndicator();

        // Subscribe to color theme changes for accessibility
        NodeColorProvider.ColorsChanged += OnNodeColorsChanged;

        // Initial grid background
        Dispatcher.BeginInvoke(new Action(UpdateGridBackground), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Update all node header colors when the syntax color theme changes.
    /// </summary>
    private void OnNodeColorsChanged(object? sender, EventArgs e)
    {
        foreach (var nodeControl in _nodeControls.Values)
        {
            nodeControl.UpdateHeaderColor();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Focus the control to receive keyboard input
        Focus();

        // Setup keyboard shortcuts
        var window = Window.GetWindow(this);
        if (window != null)
        {
            window.KeyDown += Window_KeyDown;
            window.KeyUp += Window_KeyUp;
        }

        UpdateGridBackground();
    }

    private void UpdateGridBackground()
    {
        if (!IsLoaded || RootGrid == null)
            return;

        if (!_grid.IsVisible)
        {
            RootGrid.Background = (Brush)FindResource("EditorBackgroundBrush");
            return;
        }

        // Create a tiled grid pattern using DrawingBrush
        var gridSize = _grid.GridSize * _zoom;
        var majorInterval = _grid.MajorLineInterval;

        var drawingGroup = new DrawingGroup();

        // Minor grid lines
        var minorPen = new Pen(new SolidColorBrush(_grid.MinorLineColor), 1);
        minorPen.Freeze();

        // Draw minor grid cell
        var minorGeometry = new GeometryGroup();
        minorGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(gridSize, 0)));
        minorGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, gridSize)));

        var minorDrawing = new GeometryDrawing(null, minorPen, minorGeometry);
        drawingGroup.Children.Add(minorDrawing);

        // Major grid lines (draw every majorInterval cells)
        var majorPen = new Pen(new SolidColorBrush(_grid.MajorLineColor), 1.5);
        majorPen.Freeze();

        var majorSize = gridSize * majorInterval;
        var majorGeometry = new GeometryGroup();
        majorGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(majorSize, 0)));
        majorGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, majorSize)));

        var majorDrawing = new GeometryDrawing(null, majorPen, majorGeometry);

        // Create the minor grid brush
        var minorBrush = new DrawingBrush
        {
            Drawing = minorDrawing,
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, gridSize, gridSize),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, gridSize, gridSize),
            ViewboxUnits = BrushMappingMode.Absolute
        };

        // Offset for pan
        var offsetX = TranslateTransform.X % gridSize;
        var offsetY = TranslateTransform.Y % gridSize;
        minorBrush.Transform = new TranslateTransform(offsetX, offsetY);
        if (minorBrush.CanFreeze)
            minorBrush.Freeze();

        // Create a combined visual brush
        var combinedDrawing = new DrawingGroup();

        // Background - clone the brush so it can be frozen
        var resourceBrush = FindResource("EditorBackgroundBrush") as Brush;
        Brush bgBrush;
        if (resourceBrush != null && resourceBrush.CanFreeze)
        {
            bgBrush = resourceBrush.Clone();
            bgBrush.Freeze();
        }
        else if (resourceBrush is SolidColorBrush scb)
        {
            bgBrush = new SolidColorBrush(scb.Color);
            bgBrush.Freeze();
        }
        else
        {
            bgBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            bgBrush.Freeze();
        }
        combinedDrawing.Children.Add(new GeometryDrawing(bgBrush, null,
            new RectangleGeometry(new Rect(0, 0, majorSize, majorSize))));

        // Minor lines tiled within major cell
        for (int i = 0; i <= majorInterval; i++)
        {
            var x = i * gridSize;
            var y = i * gridSize;

            if (i < majorInterval)
            {
                // Vertical minor line
                combinedDrawing.Children.Add(new GeometryDrawing(null, minorPen,
                    new LineGeometry(new Point(x, 0), new Point(x, majorSize))));
                // Horizontal minor line
                combinedDrawing.Children.Add(new GeometryDrawing(null, minorPen,
                    new LineGeometry(new Point(0, y), new Point(majorSize, y))));
            }
        }

        // Major lines at edges
        combinedDrawing.Children.Add(new GeometryDrawing(null, majorPen,
            new LineGeometry(new Point(0, 0), new Point(majorSize, 0))));
        combinedDrawing.Children.Add(new GeometryDrawing(null, majorPen,
            new LineGeometry(new Point(0, 0), new Point(0, majorSize))));

        var gridBrush = new DrawingBrush
        {
            Drawing = combinedDrawing,
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, majorSize, majorSize),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, majorSize, majorSize),
            ViewboxUnits = BrushMappingMode.Absolute
        };

        // Apply pan offset
        var majorOffsetX = TranslateTransform.X % majorSize;
        var majorOffsetY = TranslateTransform.Y % majorSize;
        gridBrush.Transform = new TranslateTransform(majorOffsetX, majorOffsetY);

        // Only freeze if possible
        if (gridBrush.CanFreeze)
            gridBrush.Freeze();

        RootGrid.Background = gridBrush;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Space key for pan mode
        if (e.Key == Key.Space && !_isSpacePressed && !_isPanning)
        {
            _isSpacePressed = true;
            PanHint.Visibility = Visibility.Visible;
            Cursor = System.Windows.Input.Cursors.Hand;
            e.Handled = true;
        }

        // Ctrl+Z for undo
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _undoRedoManager.Undo();
            e.Handled = true;
        }

        // Ctrl+Y for redo
        if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _undoRedoManager.Redo();
            e.Handled = true;
        }

        // Delete key to delete selected nodes or wires
        if (e.Key == Key.Delete)
        {
            if (_selectedWire != null)
            {
                DeleteWire(_selectedWire);
                e.Handled = true;
            }
            else if (_selectedNodes.Count > 0)
            {
                DeleteSelectedNodes();
                e.Handled = true;
            }
        }
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && _isSpacePressed)
        {
            _isSpacePressed = false;
            if (!_isPanning)
            {
                PanHint.Visibility = Visibility.Collapsed;
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
            e.Handled = true;
        }
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Only focus canvas if the click was not on an input control
        // This allows TextBox, ComboBox, etc. to receive focus when clicked
        var originalSource = e.OriginalSource as DependencyObject;
        if (!IsInputControl(originalSource))
        {
            Focus(); // Ensure canvas has focus for keyboard events
        }
        else
        {
            // Click was on an input control - don't interfere
            return;
        }

        var position = e.GetPosition(DrawingCanvas);

        // Middle mouse button or Space+Left click for panning
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && _isSpacePressed))
        {
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartOffset = PanOffset;
            Cursor = System.Windows.Input.Cursors.SizeAll;
            DrawingCanvas.CaptureMouse();
            e.Handled = true;
            return;
        }

        // Left click for selection
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            // Try to hit test for a wire first
            var hitWire = HitTestWire(position);
            if (hitWire != null)
            {
                // Wire was clicked - select it
                SelectWire(hitWire);
                e.Handled = true;
                return;
            }

            // No wire hit - clear wire selection and proceed with node/box selection
            if (_selectedWire != null)
            {
                DeselectWire();
            }

            if (!isCtrlPressed)
            {
                _selectionManager.ClearSelection();
            }

            // Start box selection
            _selectionStartPoint = position;
            _selectionManager.BeginBoxSelection(position);
            DrawingCanvas.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        // Handle panning
        if (_isPanning)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _panStartPoint;
            PanOffset = new Point(_panStartOffset.X + delta.X, _panStartOffset.Y + delta.Y);
            e.Handled = true;
            return;
        }

        // Handle box selection
        if (_selectionStartPoint.HasValue && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPoint = e.GetPosition(DrawingCanvas);
            _selectionManager.UpdateBoxSelection(currentPoint);
            e.Handled = true;
            return;
        }

        // Handle wire hover detection
        var position = e.GetPosition(DrawingCanvas);
        var hitWire = HitTestWire(position);

        if (hitWire != _hoveredWire)
        {
            // Clear previous hover
            if (_hoveredWire != null)
            {
                _hoveredWire.IsHovered = false;
            }

            // Set new hover
            _hoveredWire = hitWire;
            if (_hoveredWire != null)
            {
                _hoveredWire.IsHovered = true;
            }
        }
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // End panning
        if (_isPanning)
        {
            _isPanning = false;
            DrawingCanvas.ReleaseMouseCapture();
            Cursor = _isSpacePressed ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
            e.Handled = true;
            return;
        }

        // End box selection
        if (_selectionStartPoint.HasValue)
        {
            var currentPoint = e.GetPosition(DrawingCanvas);
            var isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            // Complete box selection (will select items when we add nodes)
            _selectionManager.EndBoxSelection(new List<ISelectable>(), isCtrlPressed);

            var startPoint = _selectionStartPoint.Value;
            _selectionStartPoint = null;
            DrawingCanvas.ReleaseMouseCapture();

            // Raise canvas clicked event if this was just a click (no drag)
            var distance = (currentPoint - startPoint).Length;
            if (distance < 3.0)
            {
                CanvasClicked?.Invoke(this, new CanvasClickedEventArgs(currentPoint, isCtrlPressed));
            }

            e.Handled = true;
        }
    }

    private void Canvas_MouseLeave(object sender, MouseEventArgs e)
    {
        // Don't cancel operations if mouse is captured
        if (DrawingCanvas.IsMouseCaptured)
            return;

        // Update cursor when leaving
        if (_isSpacePressed && !_isPanning)
        {
            Cursor = System.Windows.Input.Cursors.Hand;
        }
    }

    #region UserControl-level Mouse Handlers (for zoom/pan anywhere in control)

    private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Handle panning from anywhere in the UserControl
        if (e.MiddleButton == MouseButtonState.Pressed ||
            (e.LeftButton == MouseButtonState.Pressed && _isSpacePressed))
        {
            Focus();
            _isPanning = true;
            _panStartPoint = e.GetPosition(this);
            _panStartOffset = PanOffset;
            Cursor = System.Windows.Input.Cursors.SizeAll;
            CaptureMouse();
            e.Handled = true;
        }
    }

    private void UserControl_MouseMove(object sender, MouseEventArgs e)
    {
        // Handle panning
        if (_isPanning && IsMouseCaptured)
        {
            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _panStartPoint;
            PanOffset = new Point(_panStartOffset.X + delta.X, _panStartOffset.Y + delta.Y);
            e.Handled = true;
        }
    }

    private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // End panning
        if (_isPanning && IsMouseCaptured)
        {
            _isPanning = false;
            ReleaseMouseCapture();
            Cursor = _isSpacePressed ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
            e.Handled = true;
        }
    }

    private void UserControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Zoom from anywhere in the UserControl
        // Use center of UserControl if mouse is outside DrawingCanvas
        var mousePos = e.GetPosition(DrawingCanvas);
        var oldZoom = _zoom;
        var newZoom = _zoom + (e.Delta > 0 ? ZoomStep : -ZoomStep);
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        if (Math.Abs(oldZoom - newZoom) > 0.001)
        {
            // Calculate the point in canvas space before zoom
            var canvasPoint = new Point(
                (mousePos.X - TranslateTransform.X) / oldZoom,
                (mousePos.Y - TranslateTransform.Y) / oldZoom
            );

            // Apply zoom
            Zoom = newZoom;

            // Adjust pan to keep the same canvas point under the cursor
            var newMousePos = new Point(
                canvasPoint.X * newZoom + TranslateTransform.X,
                canvasPoint.Y * newZoom + TranslateTransform.Y
            );

            var offset = mousePos - newMousePos;
            PanOffset = new Point(
                TranslateTransform.X + offset.X,
                TranslateTransform.Y + offset.Y
            );
        }

        e.Handled = true;
    }

    #endregion

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Zoom centered on mouse cursor
        var mousePos = e.GetPosition(DrawingCanvas);
        var oldZoom = _zoom;
        var newZoom = _zoom + (e.Delta > 0 ? ZoomStep : -ZoomStep);
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        if (Math.Abs(oldZoom - newZoom) > 0.001)
        {
            // Calculate the point in canvas space before zoom
            var canvasPoint = new Point(
                (mousePos.X - TranslateTransform.X) / oldZoom,
                (mousePos.Y - TranslateTransform.Y) / oldZoom
            );

            // Apply zoom
            Zoom = newZoom;

            // Adjust pan to keep the same canvas point under the cursor
            var newMousePos = new Point(
                canvasPoint.X * newZoom + TranslateTransform.X,
                canvasPoint.Y * newZoom + TranslateTransform.Y
            );

            var offset = mousePos - newMousePos;
            PanOffset = new Point(
                TranslateTransform.X + offset.X,
                TranslateTransform.Y + offset.Y
            );
        }

        e.Handled = true;
    }

    private void UpdateZoomIndicator()
    {
        ZoomText.Text = $"{_zoom * 100:F0}%";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        SelectionChanged?.Invoke(this, e);
    }

    private void OnBoxSelectionChanged(object? sender, EventArgs e)
    {
        UpdateSelectionRectangle(_selectionManager.BoxSelectionRect);
    }

    private void UpdateSelectionRectangle(Rect? bounds)
    {
        if (!bounds.HasValue || bounds.Value.IsEmpty)
        {
            // Remove selection rectangle
            if (_selectionRect != null && OverlayCanvas.Children.Contains(_selectionRect))
            {
                OverlayCanvas.Children.Remove(_selectionRect);
            }
            return;
        }

        // Create or update selection rectangle
        if (_selectionRect == null)
        {
            _selectionRect = new WpfRectangle
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(50, 0, 120, 215)),
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
        }

        if (!OverlayCanvas.Children.Contains(_selectionRect))
        {
            OverlayCanvas.Children.Add(_selectionRect);
        }

        // Transform bounds from canvas space to screen space
        var rect = bounds.Value;
        var topLeft = DrawingCanvas.TranslatePoint(new Point(rect.X, rect.Y), OverlayCanvas);
        var bottomRight = DrawingCanvas.TranslatePoint(new Point(rect.Right, rect.Bottom), OverlayCanvas);

        System.Windows.Controls.Canvas.SetLeft(_selectionRect, Math.Min(topLeft.X, bottomRight.X));
        System.Windows.Controls.Canvas.SetTop(_selectionRect, Math.Min(topLeft.Y, bottomRight.Y));
        _selectionRect.Width = Math.Abs(bottomRight.X - topLeft.X);
        _selectionRect.Height = Math.Abs(bottomRight.Y - topLeft.Y);
    }

    private void OnUndoRedoStateChanged()
    {
        // Update UI based on undo/redo state
        // This can be used to enable/disable menu items
    }

    /// <summary>
    /// Resets the view to default zoom and position.
    /// </summary>
    public void ResetView()
    {
        Zoom = 1.0;
        PanOffset = new Point(0, 0);
    }

    /// <summary>
    /// Zooms to fit all content in the viewport.
    /// </summary>
    public void ZoomToFit()
    {
        // This will be implemented when we have nodes
        ResetView();
    }

    /// <summary>
    /// Add a node to the canvas.
    /// </summary>
    public void AddNode(NodeBase node)
    {
        // Create the visual control
        var nodeControl = new NodeControl { Node = node };

        // Position it on the canvas
        System.Windows.Controls.Canvas.SetLeft(nodeControl, node.X);
        System.Windows.Controls.Canvas.SetTop(nodeControl, node.Y);

        // Subscribe to drag events
        nodeControl.NodeDragStarted += NodeControl_DragStarted;
        nodeControl.NodeDragging += NodeControl_Dragging;
        nodeControl.NodeDragEnded += NodeControl_DragEnded;

        // Subscribe to pin click events for wire creation
        nodeControl.PinClicked += NodeControl_PinClicked;

        // Subscribe to property changes (triggers code regeneration)
        nodeControl.PropertyChanged += (s, e) =>
        {
            NodePropertyChanged?.Invoke(this, new NodePropertyChangedEventArgs(node));
        };

        // Handle selection on click, but NOT if clicking on an input control
        nodeControl.PreviewMouseLeftButtonDown += (s, e) =>
        {
            // Don't interfere with input controls (TextBox, ComboBox, CheckBox)
            var originalSource = e.OriginalSource as DependencyObject;
            if (IsInputControl(originalSource))
            {
                // Let the input control handle the click - don't select node
                return;
            }

            var isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            SelectNode(node, !isCtrl);
            // Don't set e.Handled - let NodeControl handle dragging
        };

        // Add context menu for right-click delete
        var contextMenu = new ContextMenu();
        var deleteItem = new MenuItem { Header = "Delete", InputGestureText = "Del" };
        deleteItem.Click += (s, e) =>
        {
            if (!_selectedNodes.Contains(node))
                SelectNode(node, true);
            DeleteSelectedNodes();
        };
        contextMenu.Items.Add(deleteItem);
        nodeControl.ContextMenu = contextMenu;

        // Add to canvas and tracking dictionary
        DrawingCanvas.Children.Add(nodeControl);
        _nodeControls[node] = nodeControl;
    }

    private void NodeControl_PinClicked(object? sender, PinClickEventArgs e)
    {
        if (_wireStartPin == null)
        {
            // Check if this pin is already connected (drag-to-disconnect)
            if (e.Pin.IsConnected)
            {
                // Find the wire connected to this pin
                var connectedWire = _wires.FirstOrDefault(w =>
                    (w.SourcePin == e.Pin) || (w.TargetPin == e.Pin));

                if (connectedWire != null)
                {
                    // Start drag-to-disconnect
                    StartWireDragFromPin(e.Pin, e.ParentNode, connectedWire);
                    return;
                }
            }

            // Start normal wire creation
            StartWireCreation(e.Pin, e.ParentNode);
        }
        else
        {
            // Complete wire creation
            CompleteWireCreation(e.Pin, e.ParentNode);
        }
    }

    private void StartWireCreation(NodePin pin, NodeBase node)
    {
        _wireStartPin = pin;

        // Create temporary line for visual feedback
        var pinPos = GetPinWorldPosition(pin, node);

        _tempWireLine = new System.Windows.Shapes.Line
        {
            X1 = pinPos.X,
            Y1 = pinPos.Y,
            X2 = pinPos.X,
            Y2 = pinPos.Y,
            Stroke = new SolidColorBrush(PinColors.GetColor(pin.DataType)),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            IsHitTestVisible = false
        };

        DrawingCanvas.Children.Add(_tempWireLine);

        // Track mouse movement for wire preview
        DrawingCanvas.MouseMove += TempWire_MouseMove;
        DrawingCanvas.MouseRightButtonDown += TempWire_Cancel;
    }

    private void TempWire_MouseMove(object sender, MouseEventArgs e)
    {
        if (_tempWireLine != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            _tempWireLine.X2 = pos.X;
            _tempWireLine.Y2 = pos.Y;
        }
    }

    private void TempWire_Cancel(object sender, MouseButtonEventArgs e)
    {
        CancelWireCreation();
    }

    private void CancelWireCreation()
    {
        if (_tempWireLine != null)
        {
            DrawingCanvas.Children.Remove(_tempWireLine);
            _tempWireLine = null;
        }

        _wireStartPin = null;
        DrawingCanvas.MouseMove -= TempWire_MouseMove;
        DrawingCanvas.MouseRightButtonDown -= TempWire_Cancel;
    }

    /// <summary>
    /// Start dragging a wire from a connected pin to disconnect/reconnect it.
    /// </summary>
    private void StartWireDragFromPin(NodePin pin, NodeBase node, WireVisual wire)
    {
        _draggingWire = wire;
        _dragSourcePin = pin;
        _isDraggingFromPin = true;

        // Determine which pin to keep connected (the other end of the wire)
        NodePin otherPin;
        NodeBase otherNode;

        if (wire.SourcePin == pin)
        {
            otherPin = wire.TargetPin;
            otherNode = wire.TargetNode;
        }
        else
        {
            otherPin = wire.SourcePin;
            otherNode = wire.SourceNode;
        }

        // Remove the wire visually but keep track of it
        DrawingCanvas.Children.Remove(wire.Path);

        // Create temporary line from the other pin to mouse cursor
        var otherPinPos = GetPinWorldPosition(otherPin, otherNode);

        _tempWireLine = new System.Windows.Shapes.Line
        {
            X1 = otherPinPos.X,
            Y1 = otherPinPos.Y,
            X2 = otherPinPos.X,
            Y2 = otherPinPos.Y,
            Stroke = wire.Path.Stroke,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            IsHitTestVisible = false
        };

        DrawingCanvas.Children.Add(_tempWireLine);

        // Track mouse movement
        DrawingCanvas.MouseMove += WireDrag_MouseMove;
        DrawingCanvas.MouseLeftButtonUp += WireDrag_MouseUp;
        DrawingCanvas.MouseRightButtonDown += WireDrag_Cancel;
    }

    private void WireDrag_MouseMove(object sender, MouseEventArgs e)
    {
        if (_tempWireLine != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            _tempWireLine.X2 = pos.X;
            _tempWireLine.Y2 = pos.Y;
        }
    }

    private void WireDrag_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingWire != null && _dragSourcePin != null)
        {
            var mousePos = e.GetPosition(DrawingCanvas);

            // Check if mouse is over a pin (look for pin at mouse position)
            NodePin? targetPin = null;
            NodeBase? targetNode = null;

            foreach (var kvp in _nodeControls)
            {
                var node = kvp.Key;
                var control = kvp.Value;

                // Check all pins on this node
                var allPins = node.InputPins.Concat(node.OutputPins);
                foreach (var pin in allPins)
                {
                    var pinPos = GetPinWorldPosition(pin, node);
                    var distance = (mousePos - pinPos).Length;

                    if (distance <= 15) // Pin hit test radius
                    {
                        targetPin = pin;
                        targetNode = node;
                        break;
                    }
                }

                if (targetPin != null) break;
            }

            // Determine the original other end of the wire
            NodePin otherPin;
            NodeBase otherNode;

            if (_draggingWire.SourcePin == _dragSourcePin)
            {
                otherPin = _draggingWire.TargetPin;
                otherNode = _draggingWire.TargetNode;
            }
            else
            {
                otherPin = _draggingWire.SourcePin;
                otherNode = _draggingWire.SourceNode;
            }

            // Remove the original wire completely
            _draggingWire.SourcePin.Connections.Remove(_draggingWire.TargetPin.Id);
            _draggingWire.TargetPin.Connections.Remove(_draggingWire.SourcePin.Id);
            _wires.Remove(_draggingWire);

            // If dropped on a valid pin, create new wire
            if (targetPin != null && targetNode != null &&
                targetPin != otherPin && // Not the same pin
                targetNode != otherNode && // Not the same node
                targetPin.PinType != otherPin.PinType) // Different pin types
            {
                // Reconnect to new pin
                CompleteWireCreation(targetPin, targetNode, otherPin, otherNode);
            }
            // Otherwise, wire is deleted (already removed above)
        }

        CancelWireDrag();
    }

    private void WireDrag_Cancel(object sender, MouseButtonEventArgs e)
    {
        // Cancel drag and restore original wire
        if (_draggingWire != null)
        {
            // Re-add the wire to canvas
            DrawingCanvas.Children.Insert(0, _draggingWire.Path);
        }

        CancelWireDrag();
    }

    private void CancelWireDrag()
    {
        if (_tempWireLine != null)
        {
            DrawingCanvas.Children.Remove(_tempWireLine);
            _tempWireLine = null;
        }

        _draggingWire = null;
        _dragSourcePin = null;
        _isDraggingFromPin = false;

        DrawingCanvas.MouseMove -= WireDrag_MouseMove;
        DrawingCanvas.MouseLeftButtonUp -= WireDrag_MouseUp;
        DrawingCanvas.MouseRightButtonDown -= WireDrag_Cancel;
    }

    private void CompleteWireCreation(NodePin endPin, NodeBase endNode)
    {
        if (_wireStartPin == null) return;

        var startPin = _wireStartPin;
        var startNode = startPin.ParentNode;

        // Clean up temp wire
        CancelWireCreation();

        // Validate connection
        if (startNode == endNode)
            return; // Can't connect to self

        if (startPin.PinType == endPin.PinType)
            return; // Can't connect input to input or output to output

        // Ensure we have output -> input direction
        NodePin sourcePin, targetPin;
        NodeBase sourceNode, targetNode;

        if (startPin.PinType == PinType.Output)
        {
            sourcePin = startPin;
            sourceNode = startNode!;
            targetPin = endPin;
            targetNode = endNode;
        }
        else
        {
            sourcePin = endPin;
            sourceNode = endNode;
            targetPin = startPin;
            targetNode = startNode!;
        }

        // Create the wire visual
        CreateWireVisual(sourcePin, sourceNode, targetPin, targetNode);

        // Mark pins as connected by adding to their connection lists
        sourcePin.Connections.Add(targetPin.Id);
        targetPin.Connections.Add(sourcePin.Id);

        // Raise event
        WireCreated?.Invoke(this, new WireCreatedEventArgs(sourcePin, targetPin));
    }

    /// <summary>
    /// Overload for drag-to-disconnect: create wire between two specific pins.
    /// </summary>
    private void CompleteWireCreation(NodePin pin1, NodeBase node1, NodePin pin2, NodeBase node2)
    {
        // Validate connection
        if (node1 == node2)
            return; // Can't connect to self

        if (pin1.PinType == pin2.PinType)
            return; // Can't connect input to input or output to output

        // Ensure we have output -> input direction
        NodePin sourcePin, targetPin;
        NodeBase sourceNode, targetNode;

        if (pin1.PinType == PinType.Output)
        {
            sourcePin = pin1;
            sourceNode = node1;
            targetPin = pin2;
            targetNode = node2;
        }
        else
        {
            sourcePin = pin2;
            sourceNode = node2;
            targetPin = pin1;
            targetNode = node1;
        }

        // Create the wire visual
        CreateWireVisual(sourcePin, sourceNode, targetPin, targetNode);

        // Mark pins as connected by adding to their connection lists
        sourcePin.Connections.Add(targetPin.Id);
        targetPin.Connections.Add(sourcePin.Id);

        // Raise event
        WireCreated?.Invoke(this, new WireCreatedEventArgs(sourcePin, targetPin));
    }

    private void CreateWireVisual(NodePin sourcePin, NodeBase sourceNode, NodePin targetPin, NodeBase targetNode)
    {
        var startPos = GetPinWorldPosition(sourcePin, sourceNode);
        var endPos = GetPinWorldPosition(targetPin, targetNode);

        var pinColor = PinColors.GetColor(sourcePin.DataType);
        var wirePath = new System.Windows.Shapes.Path
        {
            Stroke = new SolidColorBrush(pinColor),
            StrokeThickness = 2,
            IsHitTestVisible = true  // Enable hit testing for right-click menu
        };

        UpdateWirePath(wirePath, startPos, endPos);

        var wireVisual = new WireVisual
        {
            SourcePin = sourcePin,
            SourceNode = sourceNode,
            TargetPin = targetPin,
            TargetNode = targetNode,
            Path = wirePath
        };

        // Set the base color for state changes
        wireVisual.SetBaseColor(pinColor);

        // Add right-click context menu handler
        wirePath.MouseRightButtonDown += (s, e) =>
        {
            var wire = _wires.FirstOrDefault(w => w.Path == wirePath);
            if (wire != null)
            {
                var position = e.GetPosition(DrawingCanvas);
                OnWireRightClick(wire, position);
                e.Handled = true;
            }
        };

        _wires.Add(wireVisual);
        DrawingCanvas.Children.Insert(0, wirePath); // Insert at back so nodes appear on top
    }

    private void UpdateWirePath(System.Windows.Shapes.Path path, Point start, Point end)
    {
        // Create a bezier curve for smooth wire appearance
        var midX = (start.X + end.X) / 2;
        var controlOffset = Math.Abs(end.X - start.X) * 0.5;
        controlOffset = Math.Max(controlOffset, 50);

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = start };

        var bezier = new BezierSegment(
            new Point(start.X + controlOffset, start.Y),
            new Point(end.X - controlOffset, end.Y),
            end,
            true);

        figure.Segments.Add(bezier);
        geometry.Figures.Add(figure);
        path.Data = geometry;
    }

    private Point GetPinWorldPosition(NodePin pin, NodeBase node)
    {
        // Get pin position relative to node
        int pinIndex = pin.PinType == PinType.Input
            ? node.InputPins.IndexOf(pin)
            : node.OutputPins.IndexOf(pin);

        // Calculate position
        double x = pin.PinType == PinType.Input ? node.X : node.X + node.Width;
        double y = node.Y + 32 + 20 + (pinIndex * 24); // Header height + offset + pin spacing

        return new Point(x, y);
    }

    /// <summary>
    /// Update all wire positions (call after nodes move).
    /// </summary>
    public void UpdateWirePositions()
    {
        foreach (var wire in _wires)
        {
            var startPos = GetPinWorldPosition(wire.SourcePin, wire.SourceNode);
            var endPos = GetPinWorldPosition(wire.TargetPin, wire.TargetNode);
            UpdateWirePath(wire.Path, startPos, endPos);
        }
    }

    /// <summary>
    /// Select a node.
    /// </summary>
    public void SelectNode(NodeBase node, bool clearOthers = true)
    {
        if (clearOthers)
        {
            ClearNodeSelection();
        }

        _selectedNodes.Add(node);
        node.IsSelected = true;

        if (_nodeControls.TryGetValue(node, out var control))
        {
            control.UpdateVisualState();
        }
    }

    /// <summary>
    /// Deselect a node.
    /// </summary>
    public void DeselectNode(NodeBase node)
    {
        _selectedNodes.Remove(node);
        node.IsSelected = false;

        if (_nodeControls.TryGetValue(node, out var control))
        {
            control.UpdateVisualState();
        }
    }

    /// <summary>
    /// Clear all node selections.
    /// </summary>
    public void ClearNodeSelection()
    {
        foreach (var node in _selectedNodes.ToList())
        {
            node.IsSelected = false;
            if (_nodeControls.TryGetValue(node, out var control))
            {
                control.UpdateVisualState();
            }
        }
        _selectedNodes.Clear();
    }

    /// <summary>
    /// Delete all selected nodes.
    /// </summary>
    public void DeleteSelectedNodes()
    {
        if (_selectedNodes.Count == 0) return;

        var deletedNodes = _selectedNodes.ToList();

        foreach (var node in deletedNodes)
        {
            RemoveNode(node);
        }

        _selectedNodes.Clear();

        // Raise event so the window can update its node list
        NodesDeleted?.Invoke(this, new NodesDeletedEventArgs(deletedNodes));
    }

    /// <summary>
    /// Gets the currently selected nodes.
    /// </summary>
    public IReadOnlyCollection<NodeBase> SelectedNodes => _selectedNodes;

    /// <summary>
    /// Remove a node from the canvas.
    /// </summary>
    public void RemoveNode(NodeBase node)
    {
        // First, remove all wires connected to this node
        RemoveWiresForNode(node);

        // Then remove the node control
        if (_nodeControls.TryGetValue(node, out var control))
        {
            DrawingCanvas.Children.Remove(control);
            _nodeControls.Remove(node);
        }
    }

    /// <summary>
    /// Remove all wires connected to a node.
    /// </summary>
    private void RemoveWiresForNode(NodeBase node)
    {
        // Find all wires connected to this node
        var wiresToRemove = _wires.Where(w =>
            w.SourceNode == node || w.TargetNode == node).ToList();

        foreach (var wire in wiresToRemove)
        {
            // Remove from canvas
            DrawingCanvas.Children.Remove(wire.Path);

            // Clear connection references on the pins
            wire.SourcePin.Connections.Remove(wire.TargetPin.Id);
            wire.TargetPin.Connections.Remove(wire.SourcePin.Id);

            // Remove from our list
            _wires.Remove(wire);
        }
    }

    /// <summary>
    /// Remove a specific wire.
    /// </summary>
    public void RemoveWire(WireVisual wire)
    {
        if (_wires.Contains(wire))
        {
            DrawingCanvas.Children.Remove(wire.Path);
            wire.SourcePin.Connections.Remove(wire.TargetPin.Id);
            wire.TargetPin.Connections.Remove(wire.SourcePin.Id);
            _wires.Remove(wire);
        }
    }

    /// <summary>
    /// Delete a wire and raise the appropriate event.
    /// </summary>
    public void DeleteWire(WireVisual wire)
    {
        if (!_wires.Contains(wire))
            return;

        // Clear selection if this wire is selected
        if (_selectedWire == wire)
        {
            _selectedWire = null;
        }

        // Clear hover if this wire is hovered
        if (_hoveredWire == wire)
        {
            _hoveredWire = null;
        }

        // Remove from canvas
        DrawingCanvas.Children.Remove(wire.Path);

        // Disconnect pins
        wire.SourcePin.Connections.Remove(wire.TargetPin.Id);
        wire.TargetPin.Connections.Remove(wire.SourcePin.Id);

        // Remove from collection
        _wires.Remove(wire);

        // Raise event
        WireDeleted?.Invoke(this, new WireDeletedEventArgs(wire));
    }

    /// <summary>
    /// Select a wire.
    /// </summary>
    private void SelectWire(WireVisual wire)
    {
        // Deselect previous wire
        if (_selectedWire != null && _selectedWire != wire)
        {
            _selectedWire.IsSelected = false;
        }

        // Deselect all nodes when selecting a wire
        ClearNodeSelection();

        // Select the wire
        _selectedWire = wire;
        wire.IsSelected = true;
    }

    /// <summary>
    /// Deselect the currently selected wire.
    /// </summary>
    private void DeselectWire()
    {
        if (_selectedWire != null)
        {
            _selectedWire.IsSelected = false;
            _selectedWire = null;
        }
    }

    /// <summary>
    /// Show context menu for a wire at the specified position.
    /// </summary>
    private void OnWireRightClick(WireVisual wire, Point position)
    {
        var menu = new ContextMenu();
        var deleteItem = new MenuItem { Header = "Delete Wire", InputGestureText = "Del" };
        deleteItem.Click += (s, args) => DeleteWire(wire);
        menu.Items.Add(deleteItem);

        // Position the menu at the click position
        menu.PlacementTarget = DrawingCanvas;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
        menu.HorizontalOffset = position.X;
        menu.VerticalOffset = position.Y;
        menu.IsOpen = true;
    }

    /// <summary>
    /// Hit test to find a wire near the given position.
    /// </summary>
    /// <param name="position">The position to test in canvas coordinates.</param>
    /// <returns>The wire within 5 pixels of the position, or null if none found.</returns>
    private WireVisual? HitTestWire(Point position)
    {
        const double hitTestRadius = 5.0;

        foreach (var wire in _wires)
        {
            double distance = wire.GetDistanceToPoint(position);
            if (distance <= hitTestRadius)
            {
                return wire;
            }
        }

        return null;
    }

    /// <summary>
    /// Clear all nodes from the canvas.
    /// </summary>
    public void ClearNodes()
    {
        // Remove all wires first
        foreach (var wire in _wires)
        {
            DrawingCanvas.Children.Remove(wire.Path);
        }
        _wires.Clear();

        // Remove all node controls
        foreach (var control in _nodeControls.Values)
        {
            DrawingCanvas.Children.Remove(control);
        }
        _nodeControls.Clear();
    }

    /// <summary>
    /// Gets all wires on the canvas.
    /// </summary>
    public IReadOnlyList<WireVisual> Wires => _wires;

    #region API Methods for MCP Integration

    /// <summary>
    /// Get all wires for API.
    /// </summary>
    public IReadOnlyList<WireVisual> ApiGetWires() => _wires;

    /// <summary>
    /// Connect two pins programmatically.
    /// </summary>
    public bool ApiConnectPins(NodeBase sourceNode, NodePin sourcePin, NodeBase targetNode, NodePin targetPin)
    {
        // Validate they're not the same node
        if (sourceNode == targetNode)
            return false;

        // Ensure we have output -> input
        NodePin actualSource, actualTarget;
        NodeBase actualSourceNode, actualTargetNode;

        if (sourcePin.PinType == PinType.Output && targetPin.PinType == PinType.Input)
        {
            actualSource = sourcePin;
            actualSourceNode = sourceNode;
            actualTarget = targetPin;
            actualTargetNode = targetNode;
        }
        else if (sourcePin.PinType == PinType.Input && targetPin.PinType == PinType.Output)
        {
            actualSource = targetPin;
            actualSourceNode = targetNode;
            actualTarget = sourcePin;
            actualTargetNode = sourceNode;
        }
        else
        {
            return false; // Can't connect same types
        }

        // Check if already connected
        if (actualSource.Connections.Contains(actualTarget.Id))
            return true; // Already connected

        // Create the wire
        CreateWireVisual(actualSource, actualSourceNode, actualTarget, actualTargetNode);

        // Mark pins as connected
        actualSource.Connections.Add(actualTarget.Id);
        actualTarget.Connections.Add(actualSource.Id);

        return true;
    }

    /// <summary>
    /// Disconnect two pins programmatically.
    /// </summary>
    public bool ApiDisconnectPins(string sourceNodeId, string sourcePinId, string targetNodeId, string targetPinId)
    {
        var wire = _wires.FirstOrDefault(w =>
            (w.SourceNode.Id.ToString() == sourceNodeId && w.SourcePin.Id.ToString() == sourcePinId &&
             w.TargetNode.Id.ToString() == targetNodeId && w.TargetPin.Id.ToString() == targetPinId) ||
            (w.SourceNode.Id.ToString() == targetNodeId && w.SourcePin.Id.ToString() == targetPinId &&
             w.TargetNode.Id.ToString() == sourceNodeId && w.TargetPin.Id.ToString() == sourcePinId));

        if (wire == null)
            return false;

        RemoveWire(wire);
        return true;
    }

    /// <summary>
    /// Update a node's visual position on the canvas.
    /// </summary>
    public void UpdateNodePosition(NodeBase node)
    {
        if (_nodeControls.TryGetValue(node, out var control))
        {
            System.Windows.Controls.Canvas.SetLeft(control, node.X);
            System.Windows.Controls.Canvas.SetTop(control, node.Y);
            UpdateWirePositions();
        }
    }

    #endregion

    private void NodeControl_DragStarted(object? sender, NodeDragEventArgs e)
    {
        // Could add undo action here
    }

    private void NodeControl_Dragging(object? sender, NodeDragEventArgs e)
    {
        if (_nodeControls.TryGetValue(e.Node, out var control))
        {
            // Update node position
            e.Node.X += e.Delta.X;
            e.Node.Y += e.Delta.Y;

            // Update visual position
            System.Windows.Controls.Canvas.SetLeft(control, e.Node.X);
            System.Windows.Controls.Canvas.SetTop(control, e.Node.Y);

            // Update connected wires
            UpdateWirePositions();
        }
    }

    private void NodeControl_DragEnded(object? sender, NodeDragEventArgs e)
    {
        // Could commit undo action here
    }

    /// <summary>
    /// Check if the element or any of its ancestors is an input control.
    /// This prevents the canvas from stealing focus from text input fields.
    /// </summary>
    private bool IsInputControl(DependencyObject? element)
    {
        while (element != null)
        {
            if (element is System.Windows.Controls.TextBox ||
                element is System.Windows.Controls.ComboBox ||
                element is System.Windows.Controls.CheckBox ||
                element is System.Windows.Controls.Primitives.TextBoxBase ||
                element is System.Windows.Controls.Primitives.Selector)
            {
                return true;
            }
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }
}

/// <summary>
/// Event args for canvas click events.
/// </summary>
public class CanvasClickedEventArgs : EventArgs
{
    public Point Position { get; }
    public bool IsControlPressed { get; }

    public CanvasClickedEventArgs(Point position, bool isControlPressed)
    {
        Position = position;
        IsControlPressed = isControlPressed;
    }
}

/// <summary>
/// Event args for node deletion events.
/// </summary>
public class NodesDeletedEventArgs : EventArgs
{
    public IReadOnlyList<NodeBase> DeletedNodes { get; }

    public NodesDeletedEventArgs(IReadOnlyList<NodeBase> deletedNodes)
    {
        DeletedNodes = deletedNodes;
    }
}

/// <summary>
/// Event args for wire creation events.
/// </summary>
public class WireCreatedEventArgs : EventArgs
{
    public NodePin SourcePin { get; }
    public NodePin TargetPin { get; }

    public WireCreatedEventArgs(NodePin sourcePin, NodePin targetPin)
    {
        SourcePin = sourcePin;
        TargetPin = targetPin;
    }
}

/// <summary>
/// Event args for node property change events (for code regeneration).
/// </summary>
public class NodePropertyChangedEventArgs : EventArgs
{
    public NodeBase Node { get; }

    public NodePropertyChangedEventArgs(NodeBase node)
    {
        Node = node;
    }
}

/// <summary>
/// Event args for wire deletion events.
/// </summary>
public class WireDeletedEventArgs : EventArgs
{
    public WireVisual DeletedWire { get; }

    public WireDeletedEventArgs(WireVisual deletedWire)
    {
        DeletedWire = deletedWire;
    }
}
