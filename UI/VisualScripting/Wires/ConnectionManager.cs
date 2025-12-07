using System;
using System.Collections.Generic;
using System.Linq;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting.Wires
{
    /// <summary>
    /// Manages wire connections between nodes
    /// </summary>
    public class ConnectionManager
    {
        private readonly Dictionary<Guid, Wire> _wires = new();
        private readonly Dictionary<Guid, List<Guid>> _nodeConnections = new(); // NodeId -> List of WireIds
        private readonly Dictionary<Guid, List<Guid>> _pinConnections = new(); // PinId -> List of WireIds

        #region Events

        /// <summary>
        /// Event raised when a connection is created
        /// </summary>
        public event EventHandler<ConnectionEventArgs>? ConnectionCreated;

        /// <summary>
        /// Event raised when a connection is removed
        /// </summary>
        public event EventHandler<ConnectionEventArgs>? ConnectionRemoved;

        /// <summary>
        /// Event raised when a connection validation fails
        /// </summary>
        public event EventHandler<ConnectionValidationEventArgs>? ConnectionValidationFailed;

        #endregion

        #region Properties

        /// <summary>
        /// Get all wires
        /// </summary>
        public IReadOnlyCollection<Wire> Wires => _wires.Values.ToList().AsReadOnly();

        /// <summary>
        /// Get number of connections
        /// </summary>
        public int ConnectionCount => _wires.Count;

        #endregion

        #region Connection Management

        /// <summary>
        /// Create a connection between two pins
        /// </summary>
        /// <param name="sourcePin">Source output pin</param>
        /// <param name="targetPin">Target input pin</param>
        /// <param name="errorMessage">Error message if creation fails</param>
        /// <returns>Created wire, or null if creation failed</returns>
        public Wire? CreateConnection(NodePin sourcePin, NodePin targetPin, out string errorMessage)
        {
            // Validate connection
            if (!ValidateConnection(sourcePin, targetPin, out errorMessage))
            {
                ConnectionValidationFailed?.Invoke(this, new ConnectionValidationEventArgs(sourcePin, targetPin, errorMessage));
                return null;
            }

            // Check if connection already exists
            if (IsConnected(sourcePin, targetPin))
            {
                errorMessage = "Connection already exists";
                return null;
            }

            // Create wire
            var wire = new Wire(sourcePin, targetPin);

            // Add to dictionaries
            _wires[wire.Id] = wire;

            // Update node connections
            if (sourcePin.ParentNode != null)
            {
                if (!_nodeConnections.ContainsKey(sourcePin.ParentNode.Id))
                    _nodeConnections[sourcePin.ParentNode.Id] = new List<Guid>();
                _nodeConnections[sourcePin.ParentNode.Id].Add(wire.Id);
            }

            if (targetPin.ParentNode != null)
            {
                if (!_nodeConnections.ContainsKey(targetPin.ParentNode.Id))
                    _nodeConnections[targetPin.ParentNode.Id] = new List<Guid>();
                _nodeConnections[targetPin.ParentNode.Id].Add(wire.Id);
            }

            // Update pin connections
            if (!_pinConnections.ContainsKey(sourcePin.Id))
                _pinConnections[sourcePin.Id] = new List<Guid>();
            _pinConnections[sourcePin.Id].Add(wire.Id);

            if (!_pinConnections.ContainsKey(targetPin.Id))
                _pinConnections[targetPin.Id] = new List<Guid>();
            _pinConnections[targetPin.Id].Add(wire.Id);

            // Update pin connection lists
            sourcePin.Connections.Add(targetPin.Id);
            targetPin.Connections.Add(sourcePin.Id);

            // Raise event
            ConnectionCreated?.Invoke(this, new ConnectionEventArgs(wire));

            errorMessage = string.Empty;
            return wire;
        }

        /// <summary>
        /// Remove a connection by wire ID
        /// </summary>
        /// <param name="wireId">Wire ID to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool RemoveConnection(Guid wireId)
        {
            if (!_wires.TryGetValue(wireId, out var wire))
                return false;

            return RemoveConnection(wire);
        }

        /// <summary>
        /// Remove a connection
        /// </summary>
        /// <param name="wire">Wire to remove</param>
        /// <returns>True if removed, false if not found</returns>
        public bool RemoveConnection(Wire wire)
        {
            if (!_wires.ContainsKey(wire.Id))
                return false;

            // Remove from dictionaries
            _wires.Remove(wire.Id);

            // Update node connections
            if (_nodeConnections.TryGetValue(wire.SourceNodeId, out var sourceNodeWires))
            {
                sourceNodeWires.Remove(wire.Id);
                if (sourceNodeWires.Count == 0)
                    _nodeConnections.Remove(wire.SourceNodeId);
            }

            if (_nodeConnections.TryGetValue(wire.TargetNodeId, out var targetNodeWires))
            {
                targetNodeWires.Remove(wire.Id);
                if (targetNodeWires.Count == 0)
                    _nodeConnections.Remove(wire.TargetNodeId);
            }

            // Update pin connections
            if (_pinConnections.TryGetValue(wire.SourcePinId, out var sourcePinWires))
            {
                sourcePinWires.Remove(wire.Id);
                if (sourcePinWires.Count == 0)
                    _pinConnections.Remove(wire.SourcePinId);
            }

            if (_pinConnections.TryGetValue(wire.TargetPinId, out var targetPinWires))
            {
                targetPinWires.Remove(wire.Id);
                if (targetPinWires.Count == 0)
                    _pinConnections.Remove(wire.TargetPinId);
            }

            // Update pin connection lists
            if (wire.SourcePin != null && wire.TargetPin != null)
            {
                wire.SourcePin.Connections.Remove(wire.TargetPin.Id);
                wire.TargetPin.Connections.Remove(wire.SourcePin.Id);
            }

            // Raise event
            ConnectionRemoved?.Invoke(this, new ConnectionEventArgs(wire));

            return true;
        }

        /// <summary>
        /// Remove all connections for a node
        /// </summary>
        /// <param name="nodeId">Node ID</param>
        /// <returns>Number of connections removed</returns>
        public int RemoveConnectionsForNode(Guid nodeId)
        {
            var connections = GetConnectionsForNode(nodeId).ToList();
            int count = 0;

            foreach (var wire in connections)
            {
                if (RemoveConnection(wire))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Remove all connections for a pin
        /// </summary>
        /// <param name="pinId">Pin ID</param>
        /// <returns>Number of connections removed</returns>
        public int RemoveConnectionsForPin(Guid pinId)
        {
            var connections = GetConnectionsForPin(pinId).ToList();
            int count = 0;

            foreach (var wire in connections)
            {
                if (RemoveConnection(wire))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Clear all connections
        /// </summary>
        public void Clear()
        {
            _wires.Clear();
            _nodeConnections.Clear();
            _pinConnections.Clear();
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get a wire by ID
        /// </summary>
        /// <param name="wireId">Wire ID</param>
        /// <returns>Wire, or null if not found</returns>
        public Wire? GetWire(Guid wireId)
        {
            return _wires.TryGetValue(wireId, out var wire) ? wire : null;
        }

        /// <summary>
        /// Get all connections for a node
        /// </summary>
        /// <param name="nodeId">Node ID</param>
        /// <returns>List of wires connected to the node</returns>
        public IEnumerable<Wire> GetConnectionsForNode(Guid nodeId)
        {
            if (!_nodeConnections.TryGetValue(nodeId, out var wireIds))
                return Enumerable.Empty<Wire>();

            return wireIds.Select(id => _wires[id]).Where(w => w != null);
        }

        /// <summary>
        /// Get all connections for a pin
        /// </summary>
        /// <param name="pinId">Pin ID</param>
        /// <returns>List of wires connected to the pin</returns>
        public IEnumerable<Wire> GetConnectionsForPin(Guid pinId)
        {
            if (!_pinConnections.TryGetValue(pinId, out var wireIds))
                return Enumerable.Empty<Wire>();

            return wireIds.Select(id => _wires[id]).Where(w => w != null);
        }

        /// <summary>
        /// Check if two pins are connected
        /// </summary>
        /// <param name="sourcePin">Source pin</param>
        /// <param name="targetPin">Target pin</param>
        /// <returns>True if connected</returns>
        public bool IsConnected(NodePin sourcePin, NodePin targetPin)
        {
            if (!_pinConnections.TryGetValue(sourcePin.Id, out var wireIds))
                return false;

            return wireIds.Any(wireId =>
            {
                var wire = _wires[wireId];
                return (wire.SourcePinId == sourcePin.Id && wire.TargetPinId == targetPin.Id) ||
                       (wire.SourcePinId == targetPin.Id && wire.TargetPinId == sourcePin.Id);
            });
        }

        /// <summary>
        /// Check if a pin has any connections
        /// </summary>
        /// <param name="pinId">Pin ID</param>
        /// <returns>True if the pin has connections</returns>
        public bool HasConnections(Guid pinId)
        {
            return _pinConnections.ContainsKey(pinId) && _pinConnections[pinId].Count > 0;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate a potential connection between two pins
        /// </summary>
        /// <param name="sourcePin">Source pin</param>
        /// <param name="targetPin">Target pin</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateConnection(NodePin sourcePin, NodePin targetPin, out string errorMessage)
        {
            // Check pins are not null
            if (sourcePin == null || targetPin == null)
            {
                errorMessage = "Source or target pin is null";
                return false;
            }

            // Check parent nodes exist
            if (sourcePin.ParentNode == null || targetPin.ParentNode == null)
            {
                errorMessage = "Source or target pin has no parent node";
                return false;
            }

            // Check pin directions (source must be output, target must be input)
            if (sourcePin.PinType != PinType.Output)
            {
                errorMessage = "Source pin must be an output pin";
                return false;
            }

            if (targetPin.PinType != PinType.Input)
            {
                errorMessage = "Target pin must be an input pin";
                return false;
            }

            // Check not connecting node to itself
            if (sourcePin.ParentNode.Id == targetPin.ParentNode.Id)
            {
                errorMessage = "Cannot connect a node to itself";
                return false;
            }

            // Check type compatibility
            if (!Wire.IsTypeCompatible(sourcePin.DataType, targetPin.DataType))
            {
                errorMessage = $"Incompatible types: {sourcePin.DataType} cannot connect to {targetPin.DataType}";
                return false;
            }

            // Check if target input already has a connection (inputs can only have one connection)
            if (targetPin.PinType == PinType.Input && HasConnections(targetPin.Id))
            {
                errorMessage = "Input pin already has a connection";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Check if a connection would create a cycle (not allowed)
        /// </summary>
        /// <param name="sourceNode">Source node</param>
        /// <param name="targetNode">Target node</param>
        /// <returns>True if connection would create a cycle</returns>
        public bool WouldCreateCycle(NodeBase sourceNode, NodeBase targetNode)
        {
            // Use depth-first search to detect cycles
            var visited = new HashSet<Guid>();
            var stack = new Stack<Guid>();
            stack.Push(targetNode.Id);

            while (stack.Count > 0)
            {
                var nodeId = stack.Pop();

                if (nodeId == sourceNode.Id)
                    return true; // Found a path back to source, would create cycle

                if (visited.Contains(nodeId))
                    continue;

                visited.Add(nodeId);

                // Get all nodes connected to this node's outputs
                var connections = GetConnectionsForNode(nodeId);
                foreach (var wire in connections)
                {
                    if (wire.SourceNodeId == nodeId)
                    {
                        stack.Push(wire.TargetNodeId);
                    }
                }
            }

            return false;
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Event arguments for connection events
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        public Wire Wire { get; }

        public ConnectionEventArgs(Wire wire)
        {
            Wire = wire;
        }
    }

    /// <summary>
    /// Event arguments for connection validation failures
    /// </summary>
    public class ConnectionValidationEventArgs : EventArgs
    {
        public NodePin SourcePin { get; }
        public NodePin TargetPin { get; }
        public string ErrorMessage { get; }

        public ConnectionValidationEventArgs(NodePin sourcePin, NodePin targetPin, string errorMessage)
        {
            SourcePin = sourcePin;
            TargetPin = targetPin;
            ErrorMessage = errorMessage;
        }
    }

    #endregion
}
