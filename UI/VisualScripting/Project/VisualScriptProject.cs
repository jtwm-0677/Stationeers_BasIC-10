using System;
using System.Collections.Generic;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Represents a visual scripting project with all its data
    /// </summary>
    public class VisualScriptProject
    {
        /// <summary>
        /// Project file format version
        /// </summary>
        public string Version { get; set; } = "3.0";

        /// <summary>
        /// Project name (also used as folder name)
        /// </summary>
        public string Name { get; set; } = "Untitled";

        /// <summary>
        /// Project creation timestamp
        /// </summary>
        public DateTime Created { get; set; } = DateTime.Now;

        /// <summary>
        /// Last modification timestamp
        /// </summary>
        public DateTime Modified { get; set; } = DateTime.Now;

        /// <summary>
        /// Canvas state (zoom, pan position)
        /// </summary>
        public CanvasState Canvas { get; set; } = new();

        /// <summary>
        /// All nodes in the project
        /// </summary>
        public List<NodeData> Nodes { get; set; } = new();

        /// <summary>
        /// All wire connections
        /// </summary>
        public List<WireData> Wires { get; set; } = new();

        /// <summary>
        /// Experience mode setting
        /// </summary>
        public ExperienceLevel ExperienceMode { get; set; } = ExperienceLevel.Beginner;

        /// <summary>
        /// Additional metadata (author, description, tags, etc.)
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Project file path (not serialized, set at runtime)
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Mark project as modified
        /// </summary>
        public void MarkModified()
        {
            Modified = DateTime.Now;
        }

        /// <summary>
        /// Get project directory path
        /// </summary>
        public string? GetProjectDirectory()
        {
            if (string.IsNullOrEmpty(FilePath))
                return null;

            return System.IO.Path.GetDirectoryName(FilePath);
        }

        /// <summary>
        /// Get metadata value with type conversion
        /// </summary>
        public T GetMetadata<T>(string key, T defaultValue = default!)
        {
            if (Metadata.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T typedValue)
                        return typedValue;

                    // Try to convert
                    return (T)Convert.ChangeType(value, typeof(T))!;
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Set metadata value
        /// </summary>
        public void SetMetadata(string key, object value)
        {
            Metadata[key] = value;
            MarkModified();
        }
    }

    /// <summary>
    /// Canvas viewport state
    /// </summary>
    public class CanvasState
    {
        /// <summary>
        /// Zoom level (1.0 = 100%)
        /// </summary>
        public double Zoom { get; set; } = 1.0;

        /// <summary>
        /// Pan offset X
        /// </summary>
        public double OffsetX { get; set; } = 0;

        /// <summary>
        /// Pan offset Y
        /// </summary>
        public double OffsetY { get; set; } = 0;
    }

    /// <summary>
    /// Serializable node data
    /// </summary>
    public class NodeData
    {
        public Guid Id { get; set; }
        public string NodeType { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Label { get; set; } = "";
        public List<PinData> InputPins { get; set; } = new();
        public List<PinData> OutputPins { get; set; } = new();
        public Dictionary<string, object>? Properties { get; set; }
    }

    /// <summary>
    /// Serializable pin data
    /// </summary>
    public class PinData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string PinType { get; set; } = "";
        public string DataType { get; set; } = "";
        public List<Guid> Connections { get; set; } = new();
    }

    /// <summary>
    /// Serializable wire data
    /// </summary>
    public class WireData
    {
        public Guid Id { get; set; }
        public Guid SourceNodeId { get; set; }
        public Guid SourcePinId { get; set; }
        public Guid TargetNodeId { get; set; }
        public Guid TargetPinId { get; set; }
        public string DataType { get; set; } = "";
    }
}
