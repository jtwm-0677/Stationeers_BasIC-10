using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BasicToMips.UI.VisualScripting.Canvas;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Nodes.FlowControl;
using BasicToMips.UI.VisualScripting.Wires;
using BasicToMips.UI.VisualScripting.Services;

namespace BasicToMips.UI.VisualScripting;

/// <summary>
/// Visual scripting editor window with side-by-side code panel.
/// Features live code generation, bidirectional highlighting, and IC10 compilation.
/// </summary>
public partial class VisualScriptingWindow : Window
{
    private LiveCodeGenerator? _liveCodeGenerator;
    private HighlightSyncService? _highlightSyncService;
    private bool _codePanelVisible = true;

    // Temporary collections for nodes and wires (will be replaced with actual canvas data)
    private readonly List<NodeBase> _nodes = new();
    private readonly List<Wire> _wires = new();

    /// <summary>
    /// Event raised when BASIC code is generated from the visual graph.
    /// Subscribe to this to sync VS code with the main editor.
    /// </summary>
    public event EventHandler<BasicCodeGeneratedEventArgs>? BasicCodeGenerated;

    /// <summary>
    /// Gets the most recently generated BASIC code.
    /// </summary>
    public string GeneratedBasicCode => CodePanel.BasicCode;

    /// <summary>
    /// Gets the most recently generated IC10 code.
    /// </summary>
    public string GeneratedIC10Code => CodePanel.Ic10Code;

    public VisualScriptingWindow()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            // Get the innermost exception for the real error
            var innerEx = ex;
            while (innerEx.InnerException != null)
                innerEx = innerEx.InnerException;

            MessageBox.Show($"Failed to initialize Visual Scripting Window components:\n\nRoot cause: {innerEx.Message}\n\nType: {innerEx.GetType().Name}\n\nStack trace:\n{innerEx.StackTrace}",
                "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            InitializeServices();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize services:\n{ex.Message}\n\n{ex.StackTrace}",
                "Service Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            // Continue without services
        }

        try
        {
            UpdateUndoRedoStatus();
            SetupKeyboardShortcuts();

            // Subscribe to experience mode changes
            ExperienceModeManager.Instance.ModeChanged += OnExperienceModeChanged;

            // Subscribe to node deletion events
            Canvas.NodesDeleted += Canvas_NodesDeleted;

            // Subscribe to node property changes (for code regeneration)
            Canvas.NodePropertyChanged += Canvas_NodePropertyChanged;

            // Apply initial mode settings
            ApplyExperienceMode(ExperienceModeManager.Instance.CurrentSettings);

            // Load example script
            LoadExampleScript();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to configure window:\n{ex.Message}\n\n{ex.StackTrace}",
                "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Load an example script to demonstrate visual scripting features.
    /// </summary>
    private void LoadExampleScript()
    {
        // Add a comment node with instructions
        var commentNode = new CommentNode
        {
            X = 50,
            Y = 30,
            CommentText = "VISUAL SCRIPTING TUTORIAL\n" +
                "1. Add nodes from the palette (left)\n" +
                "2. Click a pin to start a wire\n" +
                "3. Click another pin to connect\n" +
                "4. Drag nodes to move them\n" +
                "5. Delete key removes selected\n" +
                "6. Drag corner to resize nodes"
        };
        commentNode.Initialize();
        commentNode.Width = 220;
        commentNode.Height = 140;
        _nodes.Add(commentNode);
        Canvas.AddNode(commentNode);

        // Add entry point
        var entryNode = new EntryPointNode
        {
            X = 50,
            Y = 200
        };
        entryNode.Initialize();
        _nodes.Add(entryNode);
        Canvas.AddNode(entryNode);

        // Add a device node
        var deviceNode = new PinDeviceNode
        {
            X = 50,
            Y = 310,
            AliasName = "sensor",
            PinNumber = 0
        };
        deviceNode.Initialize();
        _nodes.Add(deviceNode);
        Canvas.AddNode(deviceNode);

        // Add a variable node
        var varNode = new VariableNode
        {
            X = 280,
            Y = 200,
            VariableName = "temperature",
            InitialValue = "0",
            IsDeclaration = true
        };
        varNode.Initialize();
        _nodes.Add(varNode);
        Canvas.AddNode(varNode);

        // Add a constant node
        var constNode = new ConstantNode
        {
            X = 280,
            Y = 340,
            Value = 100
        };
        constNode.Initialize();
        _nodes.Add(constNode);
        Canvas.AddNode(constNode);

        // Add a label node
        var labelNode = new LabelNode
        {
            X = 500,
            Y = 200,
            LabelName = "MainLoop"
        };
        labelNode.Initialize();
        _nodes.Add(labelNode);
        Canvas.AddNode(labelNode);

        // Add a goto node
        var gotoNode = new GotoNode
        {
            X = 500,
            Y = 340,
            TargetLabel = "MainLoop"
        };
        gotoNode.Initialize();
        _nodes.Add(gotoNode);
        Canvas.AddNode(gotoNode);

        // Update next placement position
        _nextNodePosition = new Point(720, 200);

        // Notify code generator
        NotifyGraphChanged();

        StatusText.Text = "Example loaded - Click nodes in the palette to add more, or modify the example!";
    }

    private void InitializeServices()
    {
        // Initialize code generation and highlighting services
        _liveCodeGenerator = new LiveCodeGenerator(CodePanel);
        _highlightSyncService = new HighlightSyncService(CodePanel, Canvas);

        // Subscribe to code generation events
        _liveCodeGenerator.CodeGenerated += LiveCodeGenerator_CodeGenerated;
        _liveCodeGenerator.GenerationFailed += LiveCodeGenerator_GenerationFailed;

        // Trigger initial code generation (empty graph)
        _liveCodeGenerator.NotifyGraphChanged(_nodes, _wires);
    }

    private void SetupKeyboardShortcuts()
    {
        // F5 - Generate code now
        CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Find,
            (s, e) => GenerateCode_Click(s, e)));

        InputBindings.Add(new KeyBinding(ApplicationCommands.Find, Key.F5, ModifierKeys.None));
    }

    private void ResetView_Click(object sender, RoutedEventArgs e)
    {
        Canvas.ResetView();
    }

    private void ZoomToFit_Click(object sender, RoutedEventArgs e)
    {
        Canvas.ZoomToFit();
    }

    private void ShowGrid_Changed(object sender, RoutedEventArgs e)
    {
        // Guard against null during XAML initialization
        if (Canvas?.Grid != null)
        {
            Canvas.Grid.IsVisible = ShowGridCheckBox.IsChecked ?? true;
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        Canvas.UndoRedo.Undo();
        UpdateUndoRedoStatus();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        Canvas.UndoRedo.Redo();
        UpdateUndoRedoStatus();
    }

    private void Canvas_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var count = e.CurrentSelection.Count;
        SelectionText.Text = count == 0
            ? "No selection"
            : $"{count} item{(count == 1 ? "" : "s")} selected";
    }

    // Track position for next node placement
    private Point _nextNodePosition = new Point(100, 100);

    private void Canvas_CanvasClicked(object? sender, CanvasClickedEventArgs e)
    {
        // Store click position for next node placement
        _nextNodePosition = e.Position;
        StatusText.Text = $"Click to place node at ({e.Position.X:F0}, {e.Position.Y:F0})";
    }

    private void NodePalette_NodeTypeSelected(object? sender, NodeTypeSelectedEventArgs e)
    {
        // Create a new node of the selected type
        var node = NodePalette.CreateNode(e.TypeName);
        if (node != null)
        {
            // Set position
            node.X = _nextNodePosition.X;
            node.Y = _nextNodePosition.Y;

            // Add to our collection
            _nodes.Add(node);

            // Offset next placement position
            _nextNodePosition = new Point(_nextNodePosition.X + 20, _nextNodePosition.Y + 20);

            // Add visual representation to canvas
            Canvas.AddNode(node);

            // Notify code generator
            NotifyGraphChanged();

            StatusText.Text = $"Added {e.DisplayName} node at ({node.X:F0}, {node.Y:F0})";
        }
        else
        {
            StatusText.Text = $"Failed to create node: {e.TypeName}";
        }
    }

    private void Canvas_ZoomChanged(object? sender, EventArgs e)
    {
        ZoomText.Text = $"Zoom: {Canvas.Zoom * 100:F0}%";
    }

    private void Canvas_NodesDeleted(object? sender, NodesDeletedEventArgs e)
    {
        // Remove deleted nodes from our tracking list
        foreach (var node in e.DeletedNodes)
        {
            _nodes.Remove(node);
        }

        // Notify code generator
        NotifyGraphChanged();

        StatusText.Text = $"Deleted {e.DeletedNodes.Count} node{(e.DeletedNodes.Count == 1 ? "" : "s")}";
    }

    private void Canvas_NodePropertyChanged(object? sender, Canvas.NodePropertyChangedEventArgs e)
    {
        // Regenerate code when node properties change
        NotifyGraphChanged();
    }

    private void UpdateUndoRedoStatus()
    {
        var undoCount = Canvas.UndoRedo.CanUndo ? "Available" : "None";
        var redoCount = Canvas.UndoRedo.CanRedo ? "Available" : "None";
        UndoRedoText.Text = $"Undo: {undoCount} | Redo: {redoCount}";
    }

    private void ToggleCodePanel_Click(object sender, RoutedEventArgs e)
    {
        _codePanelVisible = !_codePanelVisible;

        if (_codePanelVisible)
        {
            CodePanelColumn.Width = new GridLength(0.6, GridUnitType.Star);
            CodePanelColumn.MinWidth = 200;
            CodePanelSplitter.Visibility = Visibility.Visible;
        }
        else
        {
            CodePanelColumn.Width = new GridLength(0);
            CodePanelColumn.MinWidth = 0;
            CodePanelSplitter.Visibility = Visibility.Collapsed;
        }
    }

    private void GenerateCode_Click(object sender, RoutedEventArgs e)
    {
        // Force immediate code generation
        _liveCodeGenerator?.RegenerateNow();
        StatusText.Text = "Code regenerated";
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        var helpText = @"VISUAL SCRIPTING HELP

ADDING NODES:
• Click on a node in the left palette to add it to the canvas
• The node appears at the last clicked position

CONNECTING NODES:
• Click on a pin (circle) to start a wire
• Click on another pin to complete the connection
• Output pins (right side) connect to Input pins (left side)
• Right-click to cancel wire creation

EDITING NODES:
• Most nodes have editable fields (text, numbers, dropdowns)
• Changes update the generated code automatically
• Drag the corner grip to resize nodes

MOVING & SELECTING:
• Drag a node's header to move it
• Click to select, Ctrl+Click for multi-select
• Delete key removes selected nodes

NAVIGATION:
• Mouse wheel to zoom in/out
• Middle mouse button or Space+drag to pan
• Use Reset View to return to origin

KEYBOARD SHORTCUTS:
• Delete - Remove selected nodes
• Ctrl+Z - Undo
• Ctrl+Y - Redo
• F5 - Regenerate code

NODE CATEGORIES:
• Flow Control - Entry, loops, labels, jumps
• Variables - VAR, LET, CONST values
• Devices - Pin aliases, property read/write
• Math - Add, subtract, multiply, etc.
• Logic - Compare, AND, OR, NOT
• Comments - Documentation nodes";

        MessageBox.Show(helpText, "Visual Scripting Help", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ClearCanvas_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Clear all nodes and wires from the canvas?",
            "Clear Canvas",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Canvas.ClearNodes();
            _nodes.Clear();
            NotifyGraphChanged();
            StatusText.Text = "Canvas cleared";
        }
    }

    private void LoadExample_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Load the example script? This will clear the current canvas.",
            "Load Example",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Canvas.ClearNodes();
            _nodes.Clear();
            LoadExampleScript();
        }
    }

    private void LiveCodeGenerator_CodeGenerated(object? sender, CodeGeneratedEventArgs e)
    {
        // Update the highlight sync service with the new source map
        _highlightSyncService?.UpdateSourceMap(e.SourceMap);

        // Update status
        StatusText.Text = $"Code generated: {e.BasicCode.Split('\n').Length} lines BASIC, {e.IC10Code.Split('\n').Length} lines IC10";

        // Raise event for external listeners (e.g., main editor sync)
        BasicCodeGenerated?.Invoke(this, new BasicCodeGeneratedEventArgs(e.BasicCode, e.IC10Code));
    }

    private void LiveCodeGenerator_GenerationFailed(object? sender, CodeGenerationErrorEventArgs e)
    {
        StatusText.Text = $"Code generation failed: {e.ErrorMessage}";
    }

    /// <summary>
    /// Notify the code generator that the graph has changed.
    /// Call this whenever nodes or wires are added, removed, or modified.
    /// </summary>
    public void NotifyGraphChanged()
    {
        // Get current nodes from tracking list
        var nodes = _nodes.ToList();

        // Convert WireVisual from canvas to Wire objects for code generator
        var wires = Canvas.ApiGetWires()
            .Select(wv => new Wire(wv.SourcePin, wv.TargetPin))
            .ToList();

        _liveCodeGenerator?.NotifyGraphChanged(nodes, wires);
    }

    private void ExperienceMode_Changed(object sender, ExperienceLevel newMode)
    {
        // Mode change is already handled by ExperienceModeManager
        // Just need to apply the new settings
        ApplyExperienceMode(ExperienceModeManager.Instance.CurrentSettings);
    }

    private void OnExperienceModeChanged(object? sender, ModeChangedEventArgs e)
    {
        ApplyExperienceMode(e.Settings);
    }

    private void ApplyExperienceMode(ExperienceModeSettings settings)
    {
        // Update code panel visibility
        if (settings.ShowCodePanel && !_codePanelVisible)
        {
            // Show code panel
            _codePanelVisible = true;
            CodePanelColumn.Width = new GridLength(0.6, GridUnitType.Star);
            CodePanelColumn.MinWidth = 200;
            CodePanelSplitter.Visibility = Visibility.Visible;
        }
        else if (!settings.ShowCodePanel && _codePanelVisible)
        {
            // Hide code panel
            _codePanelVisible = false;
            CodePanelColumn.Width = new GridLength(0);
            CodePanelColumn.MinWidth = 0;
            CodePanelSplitter.Visibility = Visibility.Collapsed;
        }

        // Update code panel settings
        CodePanel.ShowIC10Toggle = settings.ShowIC10Toggle;
        CodePanel.ShowLineNumbers = settings.ShowLineNumbers;

        // Update status
        var modeName = ExperienceModeManager.GetModeName(ExperienceModeManager.Instance.CurrentMode);
        StatusText.Text = $"Experience mode: {modeName}";

        // Trigger code regeneration to reflect any changes
        _liveCodeGenerator?.RegenerateNow();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Unsubscribe from events
        ExperienceModeManager.Instance.ModeChanged -= OnExperienceModeChanged;

        // Cleanup services
        _liveCodeGenerator?.Dispose();
        _highlightSyncService?.Dispose();
    }

    #region MCP API Methods

    /// <summary>
    /// Get all available node types from the palette.
    /// </summary>
    public List<NodeTypeInfo> ApiGetNodeTypes()
    {
        var types = new List<NodeTypeInfo>();
        var nodesByCategory = NodePalette.Factory.GetNodeTypesByCategory();
        foreach (var category in nodesByCategory)
        {
            types.AddRange(category.Value);
        }
        return types;
    }

    /// <summary>
    /// Add a node to the canvas at a specific position.
    /// </summary>
    public VisualScriptingNodeResult ApiAddNode(string nodeType, double x, double y, Dictionary<string, string>? properties = null)
    {
        var node = NodePalette.CreateNode(nodeType);
        if (node == null)
        {
            return new VisualScriptingNodeResult
            {
                Success = false,
                Error = $"Unknown node type: {nodeType}"
            };
        }

        node.X = x;
        node.Y = y;

        // Apply properties if provided
        if (properties != null)
        {
            var nodeProps = node.GetEditableProperties();
            foreach (var prop in properties)
            {
                var nodeProp = nodeProps.FirstOrDefault(p => p.PropertyName == prop.Key || p.Name == prop.Key);
                if (nodeProp != null)
                {
                    nodeProp.Value = prop.Value;
                }
            }
        }

        _nodes.Add(node);
        Canvas.AddNode(node);
        NotifyGraphChanged();

        return new VisualScriptingNodeResult
        {
            Success = true,
            NodeId = node.Id.ToString(),
            NodeType = node.NodeType,
            X = node.X,
            Y = node.Y
        };
    }

    /// <summary>
    /// Remove a node from the canvas by ID.
    /// </summary>
    public VisualScriptingResult ApiRemoveNode(string nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.Id.ToString() == nodeId);
        if (node == null)
        {
            return new VisualScriptingResult
            {
                Success = false,
                Error = $"Node not found: {nodeId}"
            };
        }

        _nodes.Remove(node);
        Canvas.RemoveNode(node);
        NotifyGraphChanged();

        return new VisualScriptingResult { Success = true };
    }

    /// <summary>
    /// Connect two nodes via a wire.
    /// </summary>
    public VisualScriptingResult ApiConnectNodes(string sourceNodeId, string sourcePinId, string targetNodeId, string targetPinId)
    {
        var sourceNode = _nodes.FirstOrDefault(n => n.Id.ToString() == sourceNodeId);
        var targetNode = _nodes.FirstOrDefault(n => n.Id.ToString() == targetNodeId);

        if (sourceNode == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Source node not found: {sourceNodeId}" };
        }
        if (targetNode == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Target node not found: {targetNodeId}" };
        }

        var allPins = sourceNode.InputPins.Concat(sourceNode.OutputPins);
        var sourcePin = allPins.FirstOrDefault(p => p.Id.ToString() == sourcePinId);
        var targetPins = targetNode.InputPins.Concat(targetNode.OutputPins);
        var targetPin = targetPins.FirstOrDefault(p => p.Id.ToString() == targetPinId);

        if (sourcePin == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Source pin not found: {sourcePinId}" };
        }
        if (targetPin == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Target pin not found: {targetPinId}" };
        }

        // Use canvas to create the wire
        var success = Canvas.ApiConnectPins(sourceNode, sourcePin, targetNode, targetPin);
        if (success)
        {
            NotifyGraphChanged();
            return new VisualScriptingResult { Success = true };
        }

        return new VisualScriptingResult { Success = false, Error = "Failed to connect nodes" };
    }

    /// <summary>
    /// Get the current graph state (all nodes and their connections).
    /// </summary>
    public VisualScriptingGraphState ApiGetGraphState()
    {
        var state = new VisualScriptingGraphState
        {
            Nodes = _nodes.Select(n => new VisualScriptingNodeInfo
            {
                Id = n.Id.ToString(),
                NodeType = n.NodeType,
                Label = n.Label,
                X = n.X,
                Y = n.Y,
                Width = n.Width,
                Height = n.Height,
                Properties = n.GetEditableProperties().ToDictionary(p => p.PropertyName, p => p.Value),
                Pins = n.InputPins.Concat(n.OutputPins).Select(p => new VisualScriptingPinInfo
                {
                    Id = p.Id.ToString(),
                    Name = p.Name,
                    Direction = p.PinType.ToString(),
                    DataType = p.DataType.ToString(),
                    Connections = p.Connections.Select(c => c.ToString()).ToList()
                }).ToList()
            }).ToList(),
            Wires = Canvas.ApiGetWires().Select(w => new VisualScriptingWireInfo
            {
                SourceNodeId = w.SourceNode?.Id.ToString() ?? "",
                SourcePinId = w.SourcePin?.Id.ToString() ?? "",
                TargetNodeId = w.TargetNode?.Id.ToString() ?? "",
                TargetPinId = w.TargetPin?.Id.ToString() ?? ""
            }).ToList()
        };

        return state;
    }

    /// <summary>
    /// Update a node's property.
    /// </summary>
    public VisualScriptingResult ApiUpdateNodeProperty(string nodeId, string propertyName, string value)
    {
        var node = _nodes.FirstOrDefault(n => n.Id.ToString() == nodeId);
        if (node == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Node not found: {nodeId}" };
        }

        var props = node.GetEditableProperties();
        var prop = props.FirstOrDefault(p => p.PropertyName == propertyName || p.Name == propertyName);
        if (prop == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Property not found: {propertyName}" };
        }

        prop.Value = value;
        NotifyGraphChanged();

        return new VisualScriptingResult { Success = true };
    }

    /// <summary>
    /// Get a node by ID.
    /// </summary>
    public VisualScriptingNodeInfo? ApiGetNode(string nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.Id.ToString() == nodeId);
        if (node == null) return null;

        return new VisualScriptingNodeInfo
        {
            Id = node.Id.ToString(),
            NodeType = node.NodeType,
            Label = node.Label,
            X = node.X,
            Y = node.Y,
            Width = node.Width,
            Height = node.Height,
            Properties = node.GetEditableProperties().ToDictionary(p => p.PropertyName, p => p.Value),
            Pins = node.InputPins.Concat(node.OutputPins).Select(p => new VisualScriptingPinInfo
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                Direction = p.PinType.ToString(),
                DataType = p.DataType.ToString(),
                Connections = p.Connections.Select(c => c.ToString()).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// Clear all nodes from the canvas.
    /// </summary>
    public VisualScriptingResult ApiClearCanvas()
    {
        Canvas.ClearNodes();
        _nodes.Clear();
        NotifyGraphChanged();
        return new VisualScriptingResult { Success = true };
    }

    /// <summary>
    /// Get the generated BASIC code from the current graph.
    /// </summary>
    public VisualScriptingCodeResult ApiGetGeneratedCode()
    {
        var result = new VisualScriptingCodeResult();

        if (_liveCodeGenerator != null)
        {
            _liveCodeGenerator.RegenerateNow();
            result.BasicCode = CodePanel.BasicCode;
            result.Ic10Code = CodePanel.Ic10Code;
            result.Success = true;
        }
        else
        {
            result.Success = false;
            result.Error = "Code generator not initialized";
        }

        return result;
    }

    /// <summary>
    /// Move a node to a new position.
    /// </summary>
    public VisualScriptingResult ApiMoveNode(string nodeId, double x, double y)
    {
        var node = _nodes.FirstOrDefault(n => n.Id.ToString() == nodeId);
        if (node == null)
        {
            return new VisualScriptingResult { Success = false, Error = $"Node not found: {nodeId}" };
        }

        node.X = x;
        node.Y = y;
        Canvas.UpdateNodePosition(node);
        NotifyGraphChanged();

        return new VisualScriptingResult { Success = true };
    }

    /// <summary>
    /// Disconnect a wire between two pins.
    /// </summary>
    public VisualScriptingResult ApiDisconnectNodes(string sourceNodeId, string sourcePinId, string targetNodeId, string targetPinId)
    {
        var success = Canvas.ApiDisconnectPins(sourceNodeId, sourcePinId, targetNodeId, targetPinId);
        if (success)
        {
            NotifyGraphChanged();
            return new VisualScriptingResult { Success = true };
        }

        return new VisualScriptingResult { Success = false, Error = "Wire not found or could not be disconnected" };
    }

    #endregion
}

#region Visual Scripting API Response Models

/// <summary>
/// Basic result for visual scripting operations.
/// </summary>
public class VisualScriptingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result for node creation operations.
/// </summary>
public class VisualScriptingNodeResult : VisualScriptingResult
{
    public string NodeId { get; set; } = "";
    public string NodeType { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Information about a node.
/// </summary>
public class VisualScriptingNodeInfo
{
    public string Id { get; set; } = "";
    public string NodeType { get; set; } = "";
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public List<VisualScriptingPinInfo> Pins { get; set; } = new();
}

/// <summary>
/// Information about a pin.
/// </summary>
public class VisualScriptingPinInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Direction { get; set; } = "";
    public string DataType { get; set; } = "";
    public List<string> Connections { get; set; } = new();
}

/// <summary>
/// Information about a wire.
/// </summary>
public class VisualScriptingWireInfo
{
    public string SourceNodeId { get; set; } = "";
    public string SourcePinId { get; set; } = "";
    public string TargetNodeId { get; set; } = "";
    public string TargetPinId { get; set; } = "";
}

/// <summary>
/// Complete graph state.
/// </summary>
public class VisualScriptingGraphState
{
    public List<VisualScriptingNodeInfo> Nodes { get; set; } = new();
    public List<VisualScriptingWireInfo> Wires { get; set; } = new();
}

/// <summary>
/// Generated code result.
/// </summary>
public class VisualScriptingCodeResult : VisualScriptingResult
{
    public string BasicCode { get; set; } = "";
    public string Ic10Code { get; set; } = "";
}

/// <summary>
/// Event args for when BASIC code is generated from the visual graph.
/// </summary>
public class BasicCodeGeneratedEventArgs : EventArgs
{
    /// <summary>
    /// The generated BASIC code.
    /// </summary>
    public string BasicCode { get; }

    /// <summary>
    /// The compiled IC10 code.
    /// </summary>
    public string IC10Code { get; }

    public BasicCodeGeneratedEventArgs(string basicCode, string ic10Code)
    {
        BasicCode = basicCode;
        IC10Code = ic10Code;
    }
}

#endregion
