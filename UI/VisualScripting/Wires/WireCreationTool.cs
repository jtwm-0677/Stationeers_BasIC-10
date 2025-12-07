using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Canvas;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting.Wires
{
    /// <summary>
    /// Interactive tool for creating wire connections
    /// </summary>
    public class WireCreationTool
    {
        private readonly ConnectionManager _connectionManager;
        private readonly UndoRedoManager? _undoRedoManager;

        private bool _isDragging = false;
        private NodePin? _sourcePin = null;
        private NodePin? _targetPin = null;
        private Point _currentMousePosition;
        private Point _startPosition;

        #region Properties

        /// <summary>
        /// Gets whether a wire is currently being dragged
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// Gets the source pin of the wire being created
        /// </summary>
        public NodePin? SourcePin => _sourcePin;

        /// <summary>
        /// Gets the current mouse position during drag
        /// </summary>
        public Point CurrentMousePosition => _currentMousePosition;

        /// <summary>
        /// Gets whether the current target is valid
        /// </summary>
        public bool IsCurrentTargetValid { get; private set; } = true;

        /// <summary>
        /// Event raised when wire creation starts
        /// </summary>
        public event EventHandler? WireCreationStarted;

        /// <summary>
        /// Event raised when wire creation ends
        /// </summary>
        public event EventHandler? WireCreationEnded;

        /// <summary>
        /// Event raised when the temporary wire should be redrawn
        /// </summary>
        public event EventHandler? TemporaryWireChanged;

        #endregion

        #region Constructor

        public WireCreationTool(ConnectionManager connectionManager, UndoRedoManager? undoRedoManager = null)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _undoRedoManager = undoRedoManager;
        }

        #endregion

        #region Wire Creation

        /// <summary>
        /// Start creating a wire from a pin
        /// </summary>
        /// <param name="sourcePin">Source pin to start from</param>
        /// <param name="startPosition">Starting position in canvas coordinates</param>
        public void StartWireCreation(NodePin sourcePin, Point startPosition)
        {
            if (sourcePin.PinType != PinType.Output)
            {
                // Can only start wire creation from output pins
                return;
            }

            _isDragging = true;
            _sourcePin = sourcePin;
            _targetPin = null;
            _startPosition = startPosition;
            _currentMousePosition = startPosition;
            IsCurrentTargetValid = true;

            WireCreationStarted?.Invoke(this, EventArgs.Empty);
            TemporaryWireChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Update the wire creation with current mouse position
        /// </summary>
        /// <param name="mousePosition">Current mouse position in canvas coordinates</param>
        /// <param name="hoveredPin">Pin currently being hovered, if any</param>
        public void UpdateWireCreation(Point mousePosition, NodePin? hoveredPin)
        {
            if (!_isDragging)
                return;

            _currentMousePosition = mousePosition;
            _targetPin = hoveredPin;

            // Validate the connection if hovering over a pin
            if (hoveredPin != null && _sourcePin != null)
            {
                IsCurrentTargetValid = _connectionManager.ValidateConnection(_sourcePin, hoveredPin, out _);
            }
            else
            {
                IsCurrentTargetValid = true; // No target pin, so just show the dragging wire
            }

            TemporaryWireChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Complete the wire creation
        /// </summary>
        /// <param name="targetPin">Target pin to connect to</param>
        /// <returns>True if wire was created successfully</returns>
        public bool CompleteWireCreation(NodePin? targetPin = null)
        {
            if (!_isDragging || _sourcePin == null)
                return false;

            bool success = false;

            // Use provided target pin or the one from UpdateWireCreation
            NodePin? actualTargetPin = targetPin ?? _targetPin;

            if (actualTargetPin != null)
            {
                // Try to create the connection
                var wire = _connectionManager.CreateConnection(_sourcePin, actualTargetPin, out string errorMessage);

                if (wire != null)
                {
                    // Add to undo/redo if available
                    if (_undoRedoManager != null)
                    {
                        var action = new CreateConnectionAction(
                            wire,
                            _connectionManager,
                            "Create Connection"
                        );
                        _undoRedoManager.AddAction(action);
                    }

                    success = true;
                }
            }

            // Reset state
            _isDragging = false;
            _sourcePin = null;
            _targetPin = null;
            IsCurrentTargetValid = true;

            WireCreationEnded?.Invoke(this, EventArgs.Empty);

            return success;
        }

        /// <summary>
        /// Cancel the wire creation
        /// </summary>
        public void CancelWireCreation()
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            _sourcePin = null;
            _targetPin = null;
            IsCurrentTargetValid = true;

            WireCreationEnded?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Handle mouse down event
        /// </summary>
        /// <param name="mousePosition">Mouse position in canvas coordinates</param>
        /// <param name="hitPin">Pin that was clicked, if any</param>
        /// <returns>True if event was handled</returns>
        public bool HandleMouseDown(Point mousePosition, NodePin? hitPin)
        {
            if (hitPin != null && hitPin.PinType == PinType.Output)
            {
                // Calculate the actual position of the pin
                if (hitPin.ParentNode != null)
                {
                    int pinIndex = hitPin.ParentNode.OutputPins.IndexOf(hitPin);
                    var (pinX, pinY) = hitPin.GetPosition(pinIndex);
                    Point pinPosition = new Point(hitPin.ParentNode.X + pinX, hitPin.ParentNode.Y + pinY);
                    StartWireCreation(hitPin, pinPosition);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handle mouse move event
        /// </summary>
        /// <param name="mousePosition">Mouse position in canvas coordinates</param>
        /// <param name="hoveredPin">Pin being hovered, if any</param>
        /// <returns>True if event was handled</returns>
        public bool HandleMouseMove(Point mousePosition, NodePin? hoveredPin)
        {
            if (_isDragging)
            {
                UpdateWireCreation(mousePosition, hoveredPin);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle mouse up event
        /// </summary>
        /// <param name="mousePosition">Mouse position in canvas coordinates</param>
        /// <param name="hitPin">Pin that was released on, if any</param>
        /// <returns>True if event was handled</returns>
        public bool HandleMouseUp(Point mousePosition, NodePin? hitPin)
        {
            if (_isDragging)
            {
                // Only complete if releasing on a valid input pin
                if (hitPin != null && hitPin.PinType == PinType.Input)
                {
                    CompleteWireCreation(hitPin);
                }
                else
                {
                    CancelWireCreation();
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Handle key down event
        /// </summary>
        /// <param name="key">Key that was pressed</param>
        /// <returns>True if event was handled</returns>
        public bool HandleKeyDown(Key key)
        {
            if (_isDragging)
            {
                if (key == Key.Escape)
                {
                    CancelWireCreation();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handle right mouse button down event
        /// </summary>
        /// <returns>True if event was handled</returns>
        public bool HandleRightMouseDown()
        {
            if (_isDragging)
            {
                CancelWireCreation();
                return true;
            }

            return false;
        }

        #endregion

        #region Rendering

        /// <summary>
        /// Render the temporary wire being created
        /// </summary>
        /// <param name="context">Drawing context</param>
        public void RenderTemporaryWire(DrawingContext context)
        {
            if (!_isDragging || _sourcePin == null)
                return;

            // Get start position (from source pin)
            double startX = _startPosition.X;
            double startY = _startPosition.Y;

            // Get end position (mouse cursor or target pin)
            double endX, endY;

            if (_targetPin != null && _targetPin.ParentNode != null)
            {
                // Snap to target pin position
                int pinIndex = _targetPin.ParentNode.InputPins.IndexOf(_targetPin);
                var (pinX, pinY) = _targetPin.GetPosition(pinIndex);
                endX = _targetPin.ParentNode.X + pinX;
                endY = _targetPin.ParentNode.Y + pinY;
            }
            else
            {
                // Follow mouse cursor
                endX = _currentMousePosition.X;
                endY = _currentMousePosition.Y;
            }

            // Render the temporary wire
            WireRenderer.RenderTemporaryWire(
                context,
                startX, startY,
                endX, endY,
                _sourcePin.DataType,
                IsCurrentTargetValid
            );
        }

        /// <summary>
        /// Render visual feedback on pins during wire creation
        /// </summary>
        /// <param name="context">Drawing context</param>
        /// <param name="pin">Pin to render feedback for</param>
        /// <param name="pinCenter">Center position of the pin</param>
        public void RenderPinFeedback(DrawingContext context, NodePin pin, Point pinCenter)
        {
            if (!_isDragging || _sourcePin == null)
                return;

            // Only show feedback on input pins
            if (pin.PinType != PinType.Input)
                return;

            // Check if this pin is compatible
            bool isValid = _connectionManager.ValidateConnection(_sourcePin, pin, out _);

            if (isValid)
            {
                // Draw green glow for valid target
                var glowBrush = new SolidColorBrush(Color.FromArgb(128, 0x44, 0xFF, 0x44));
                context.DrawEllipse(glowBrush, null, pinCenter, 12, 12);
            }
            else
            {
                // Draw red X for invalid target
                var redPen = new Pen(new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44)), 2);
                context.DrawLine(redPen, new Point(pinCenter.X - 6, pinCenter.Y - 6), new Point(pinCenter.X + 6, pinCenter.Y + 6));
                context.DrawLine(redPen, new Point(pinCenter.X - 6, pinCenter.Y + 6), new Point(pinCenter.X + 6, pinCenter.Y - 6));
            }
        }

        #endregion
    }

    #region Undo/Redo Actions

    /// <summary>
    /// Undoable action for creating a connection
    /// </summary>
    public class CreateConnectionAction : IUndoableAction
    {
        private readonly Wire _wire;
        private readonly ConnectionManager _connectionManager;

        public string Description { get; }

        public CreateConnectionAction(Wire wire, ConnectionManager connectionManager, string? description = null)
        {
            _wire = wire;
            _connectionManager = connectionManager;
            Description = description ?? "Create Connection";
        }

        public void Execute()
        {
            // Wire already created, just add it back
            if (_wire.SourcePin != null && _wire.TargetPin != null)
            {
                _connectionManager.CreateConnection(_wire.SourcePin, _wire.TargetPin, out _);
            }
        }

        public void Undo()
        {
            _connectionManager.RemoveConnection(_wire.Id);
        }
    }

    /// <summary>
    /// Undoable action for removing a connection
    /// </summary>
    public class RemoveConnectionAction : IUndoableAction
    {
        private readonly Wire _wire;
        private readonly ConnectionManager _connectionManager;

        public string Description { get; }

        public RemoveConnectionAction(Wire wire, ConnectionManager connectionManager, string? description = null)
        {
            _wire = wire;
            _connectionManager = connectionManager;
            Description = description ?? "Remove Connection";
        }

        public void Execute()
        {
            _connectionManager.RemoveConnection(_wire.Id);
        }

        public void Undo()
        {
            if (_wire.SourcePin != null && _wire.TargetPin != null)
            {
                _connectionManager.CreateConnection(_wire.SourcePin, _wire.TargetPin, out _);
            }
        }
    }

    #endregion
}
