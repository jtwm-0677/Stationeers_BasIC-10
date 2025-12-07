using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting.Wires
{
    /// <summary>
    /// Serializes and deserializes wire connections to/from JSON
    /// </summary>
    public class WireSerializer
    {
        /// <summary>
        /// Serialize wires to JSON
        /// </summary>
        /// <param name="wires">Wires to serialize</param>
        /// <returns>JSON string</returns>
        public static string SerializeWires(IEnumerable<Wire> wires)
        {
            var serializableWires = wires.Select(w => new SerializableWire
            {
                Id = w.Id,
                SourceNodeId = w.SourceNodeId,
                SourcePinId = w.SourcePinId,
                TargetNodeId = w.TargetNodeId,
                TargetPinId = w.TargetPinId,
                DataType = w.DataType
            }).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Serialize(serializableWires, options);
        }

        /// <summary>
        /// Deserialize wires from JSON
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <param name="nodes">Dictionary of nodes to reconnect wires to (nodeId -> node)</param>
        /// <returns>List of deserialized wires</returns>
        public static List<Wire> DeserializeWires(string json, Dictionary<Guid, NodeBase> nodes)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var serializableWires = JsonSerializer.Deserialize<List<SerializableWire>>(json, options);
            if (serializableWires == null)
                return new List<Wire>();

            var wires = new List<Wire>();

            foreach (var serializableWire in serializableWires)
            {
                // Find source and target nodes
                if (!nodes.TryGetValue(serializableWire.SourceNodeId, out var sourceNode))
                {
                    Console.WriteLine($"Warning: Source node {serializableWire.SourceNodeId} not found for wire {serializableWire.Id}");
                    continue;
                }

                if (!nodes.TryGetValue(serializableWire.TargetNodeId, out var targetNode))
                {
                    Console.WriteLine($"Warning: Target node {serializableWire.TargetNodeId} not found for wire {serializableWire.Id}");
                    continue;
                }

                // Find source and target pins
                var sourcePin = sourceNode.OutputPins.FirstOrDefault(p => p.Id == serializableWire.SourcePinId);
                if (sourcePin == null)
                {
                    Console.WriteLine($"Warning: Source pin {serializableWire.SourcePinId} not found for wire {serializableWire.Id}");
                    continue;
                }

                var targetPin = targetNode.InputPins.FirstOrDefault(p => p.Id == serializableWire.TargetPinId);
                if (targetPin == null)
                {
                    Console.WriteLine($"Warning: Target pin {serializableWire.TargetPinId} not found for wire {serializableWire.Id}");
                    continue;
                }

                // Create wire
                var wire = new Wire
                {
                    Id = serializableWire.Id,
                    SourceNodeId = serializableWire.SourceNodeId,
                    SourcePinId = serializableWire.SourcePinId,
                    TargetNodeId = serializableWire.TargetNodeId,
                    TargetPinId = serializableWire.TargetPinId,
                    DataType = serializableWire.DataType,
                    SourceNode = sourceNode,
                    SourcePin = sourcePin,
                    TargetNode = targetNode,
                    TargetPin = targetPin
                };

                // Update pin connections
                if (!sourcePin.Connections.Contains(targetPin.Id))
                    sourcePin.Connections.Add(targetPin.Id);

                if (!targetPin.Connections.Contains(sourcePin.Id))
                    targetPin.Connections.Add(sourcePin.Id);

                wires.Add(wire);
            }

            return wires;
        }

        /// <summary>
        /// Serialize wires to a file
        /// </summary>
        /// <param name="filePath">File path to save to</param>
        /// <param name="wires">Wires to serialize</param>
        public static void SaveToFile(string filePath, IEnumerable<Wire> wires)
        {
            var json = SerializeWires(wires);
            System.IO.File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Deserialize wires from a file
        /// </summary>
        /// <param name="filePath">File path to load from</param>
        /// <param name="nodes">Dictionary of nodes to reconnect wires to</param>
        /// <returns>List of deserialized wires</returns>
        public static List<Wire> LoadFromFile(string filePath, Dictionary<Guid, NodeBase> nodes)
        {
            if (!System.IO.File.Exists(filePath))
                return new List<Wire>();

            var json = System.IO.File.ReadAllText(filePath);
            return DeserializeWires(json, nodes);
        }

        /// <summary>
        /// Create a copy of a wire with new IDs (for copy/paste)
        /// </summary>
        /// <param name="wire">Wire to copy</param>
        /// <param name="nodeIdMapping">Mapping of old node IDs to new node IDs</param>
        /// <param name="pinIdMapping">Mapping of old pin IDs to new pin IDs</param>
        /// <param name="nodes">Dictionary of all nodes</param>
        /// <returns>Copied wire, or null if nodes/pins not found</returns>
        public static Wire? CopyWire(Wire wire, Dictionary<Guid, Guid> nodeIdMapping, Dictionary<Guid, Guid> pinIdMapping, Dictionary<Guid, NodeBase> nodes)
        {
            // Get new node IDs
            if (!nodeIdMapping.TryGetValue(wire.SourceNodeId, out var newSourceNodeId))
                return null;

            if (!nodeIdMapping.TryGetValue(wire.TargetNodeId, out var newTargetNodeId))
                return null;

            // Get new pin IDs
            if (!pinIdMapping.TryGetValue(wire.SourcePinId, out var newSourcePinId))
                return null;

            if (!pinIdMapping.TryGetValue(wire.TargetPinId, out var newTargetPinId))
                return null;

            // Get nodes
            if (!nodes.TryGetValue(newSourceNodeId, out var sourceNode))
                return null;

            if (!nodes.TryGetValue(newTargetNodeId, out var targetNode))
                return null;

            // Get pins
            var sourcePin = sourceNode.OutputPins.FirstOrDefault(p => p.Id == newSourcePinId);
            if (sourcePin == null)
                return null;

            var targetPin = targetNode.InputPins.FirstOrDefault(p => p.Id == newTargetPinId);
            if (targetPin == null)
                return null;

            // Create new wire
            return new Wire(sourcePin, targetPin);
        }

        /// <summary>
        /// Validate all wires after deserialization
        /// </summary>
        /// <param name="wires">Wires to validate</param>
        /// <returns>List of validation errors (empty if all valid)</returns>
        public static List<string> ValidateWires(IEnumerable<Wire> wires)
        {
            var errors = new List<string>();

            foreach (var wire in wires)
            {
                if (!wire.Validate(out string error))
                {
                    errors.Add($"Wire {wire.Id}: {error}");
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Serializable representation of a wire
    /// </summary>
    internal class SerializableWire
    {
        public Guid Id { get; set; }
        public Guid SourceNodeId { get; set; }
        public Guid SourcePinId { get; set; }
        public Guid TargetNodeId { get; set; }
        public Guid TargetPinId { get; set; }
        public DataType DataType { get; set; }
    }
}
