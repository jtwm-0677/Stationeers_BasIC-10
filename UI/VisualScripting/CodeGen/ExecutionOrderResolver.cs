using System;
using System.Collections.Generic;
using System.Linq;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.CodeGen
{
    /// <summary>
    /// Resolves the execution order of nodes in a visual graph
    /// Handles both execution flow (white wires) and data flow
    /// </summary>
    public class ExecutionOrderResolver
    {
        #region Properties

        private readonly List<NodeBase> _nodes;
        private readonly List<Wire> _wires;
        private readonly CodeGenerationContext _context;

        #endregion

        #region Constructor

        public ExecutionOrderResolver(List<NodeBase> nodes, List<Wire> wires, CodeGenerationContext context)
        {
            _nodes = nodes;
            _wires = wires;
            _context = context;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resolve the execution order of nodes
        /// Returns a list of execution chains, each chain is a list of nodes to execute in sequence
        /// </summary>
        public List<List<NodeBase>> ResolveExecutionOrder()
        {
            var executionChains = new List<List<NodeBase>>();

            // Find entry points (nodes with execution output but no execution input)
            var entryPoints = FindEntryPoints();

            if (entryPoints.Count == 0)
            {
                _context.AddWarning(Guid.Empty, "No entry points found. Graph will not execute.");
                return executionChains;
            }

            // Build execution chains starting from each entry point
            foreach (var entryPoint in entryPoints)
            {
                var chain = BuildExecutionChain(entryPoint);
                if (chain.Count > 0)
                {
                    executionChains.Add(chain);
                }
            }

            return executionChains;
        }

        /// <summary>
        /// Find nodes that are entry points (no execution input, or have execution output)
        /// </summary>
        private List<NodeBase> FindEntryPoints()
        {
            var entryPoints = new List<NodeBase>();

            foreach (var node in _nodes)
            {
                // Check if node has an execution input pin
                bool hasExecutionInput = node.InputPins.Any(p => p.DataType == DataType.Execution);

                if (hasExecutionInput)
                {
                    // Check if the execution input is connected
                    var execInputPin = node.InputPins.First(p => p.DataType == DataType.Execution);
                    bool isExecutionInputConnected = _wires.Any(w =>
                        w.TargetPinId == execInputPin.Id && w.DataType == DataType.Execution);

                    // Entry point if execution input is not connected
                    if (!isExecutionInputConnected)
                    {
                        entryPoints.Add(node);
                    }
                }
                else
                {
                    // No execution input pin at all - could be a pure data node
                    // Check if it has execution output
                    bool hasExecutionOutput = node.OutputPins.Any(p => p.DataType == DataType.Execution);
                    if (hasExecutionOutput)
                    {
                        entryPoints.Add(node);
                    }
                }
            }

            return entryPoints;
        }

        /// <summary>
        /// Build an execution chain starting from a given node
        /// </summary>
        private List<NodeBase> BuildExecutionChain(NodeBase startNode)
        {
            var chain = new List<NodeBase>();
            var visited = new HashSet<Guid>();
            var currentNode = startNode;

            while (currentNode != null)
            {
                // Check for cycles
                if (visited.Contains(currentNode.Id))
                {
                    _context.AddError(currentNode.Id, "Execution cycle detected");
                    break;
                }

                visited.Add(currentNode.Id);
                chain.Add(currentNode);

                // Find next node in execution chain
                currentNode = GetNextExecutionNode(currentNode);
            }

            return chain;
        }

        /// <summary>
        /// Get the next node in the execution chain
        /// </summary>
        private NodeBase? GetNextExecutionNode(NodeBase currentNode)
        {
            // Find execution output pin
            var execOutputPin = currentNode.OutputPins.FirstOrDefault(p => p.DataType == DataType.Execution);
            if (execOutputPin == null)
                return null;

            // Find wire connected to execution output
            var execWire = _wires.FirstOrDefault(w =>
                w.SourcePinId == execOutputPin.Id && w.DataType == DataType.Execution);

            if (execWire == null)
                return null;

            // Return target node
            return _nodes.FirstOrDefault(n => n.Id == execWire.TargetNodeId);
        }

        /// <summary>
        /// Get all nodes that provide data to a given pin (data dependencies)
        /// This is used for expression building
        /// </summary>
        public List<NodeBase> GetDataDependencies(NodePin pin)
        {
            var dependencies = new List<NodeBase>();

            if (pin.PinType != PinType.Input || pin.DataType == DataType.Execution)
                return dependencies;

            // Find wires connected to this input pin
            var inputWires = _wires.Where(w => w.TargetPinId == pin.Id).ToList();

            foreach (var wire in inputWires)
            {
                var sourceNode = _nodes.FirstOrDefault(n => n.Id == wire.SourceNodeId);
                if (sourceNode != null)
                {
                    dependencies.Add(sourceNode);

                    // Recursively get dependencies of source node
                    foreach (var sourceInputPin in sourceNode.InputPins.Where(p => p.DataType != DataType.Execution))
                    {
                        dependencies.AddRange(GetDataDependencies(sourceInputPin));
                    }
                }
            }

            return dependencies.Distinct().ToList();
        }

        /// <summary>
        /// Perform topological sort on data flow for a given node
        /// Returns nodes in the order they should be evaluated to compute the node's inputs
        /// </summary>
        public List<NodeBase> TopologicalSortForNode(NodeBase node)
        {
            var sorted = new List<NodeBase>();
            var visited = new HashSet<Guid>();
            var visiting = new HashSet<Guid>();

            bool success = TopologicalSortVisit(node, sorted, visited, visiting);

            if (!success)
            {
                _context.AddError(node.Id, "Data flow cycle detected");
                return new List<NodeBase>();
            }

            return sorted;
        }

        /// <summary>
        /// Recursive helper for topological sort (DFS)
        /// </summary>
        private bool TopologicalSortVisit(NodeBase node, List<NodeBase> sorted, HashSet<Guid> visited, HashSet<Guid> visiting)
        {
            if (visited.Contains(node.Id))
                return true;

            if (visiting.Contains(node.Id))
            {
                // Cycle detected
                return false;
            }

            visiting.Add(node.Id);

            // Visit all data dependencies first
            foreach (var inputPin in node.InputPins.Where(p => p.DataType != DataType.Execution))
            {
                var wire = _wires.FirstOrDefault(w => w.TargetPinId == inputPin.Id);
                if (wire != null)
                {
                    var sourceNode = _nodes.FirstOrDefault(n => n.Id == wire.SourceNodeId);
                    if (sourceNode != null)
                    {
                        if (!TopologicalSortVisit(sourceNode, sorted, visited, visiting))
                        {
                            return false;
                        }
                    }
                }
            }

            visiting.Remove(node.Id);
            visited.Add(node.Id);
            sorted.Add(node);

            return true;
        }

        /// <summary>
        /// Get the wire providing data to a specific input pin
        /// </summary>
        public Wire? GetInputWire(NodePin inputPin)
        {
            return _wires.FirstOrDefault(w => w.TargetPinId == inputPin.Id);
        }

        /// <summary>
        /// Get the source node providing data to a specific input pin
        /// </summary>
        public NodeBase? GetInputSourceNode(NodePin inputPin)
        {
            var wire = GetInputWire(inputPin);
            if (wire == null)
                return null;

            return _nodes.FirstOrDefault(n => n.Id == wire.SourceNodeId);
        }

        /// <summary>
        /// Get the source pin providing data to a specific input pin
        /// </summary>
        public NodePin? GetInputSourcePin(NodePin inputPin)
        {
            var wire = GetInputWire(inputPin);
            if (wire == null)
                return null;

            var sourceNode = GetInputSourceNode(inputPin);
            if (sourceNode == null)
                return null;

            return sourceNode.OutputPins.FirstOrDefault(p => p.Id == wire.SourcePinId);
        }

        #endregion
    }
}
