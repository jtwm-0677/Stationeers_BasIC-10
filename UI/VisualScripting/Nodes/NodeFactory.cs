using System;
using System.Collections.Generic;
using System.Linq;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Factory for creating nodes by type name
    /// </summary>
    public class NodeFactory
    {
        private readonly Dictionary<string, Type> _nodeTypes = new();

        /// <summary>
        /// Register a node type with the factory
        /// </summary>
        public void RegisterNodeType<T>(string typeName) where T : NodeBase, new()
        {
            _nodeTypes[typeName] = typeof(T);
        }

        /// <summary>
        /// Register a node type with the factory using its NodeType property
        /// </summary>
        public void RegisterNodeType<T>() where T : NodeBase, new()
        {
            var instance = new T();
            _nodeTypes[instance.NodeType] = typeof(T);
        }

        /// <summary>
        /// Create a node instance by type name
        /// </summary>
        public NodeBase? CreateNode(string typeName)
        {
            if (!_nodeTypes.TryGetValue(typeName, out var type))
                return null;

            try
            {
                var node = (NodeBase?)Activator.CreateInstance(type);
                node?.Initialize();
                return node;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get all registered node type names
        /// </summary>
        public IEnumerable<string> GetRegisteredTypes()
        {
            return _nodeTypes.Keys;
        }

        /// <summary>
        /// Get all registered node types grouped by category
        /// </summary>
        public Dictionary<string, List<NodeTypeInfo>> GetNodeTypesByCategory()
        {
            var result = new Dictionary<string, List<NodeTypeInfo>>();

            foreach (var kvp in _nodeTypes)
            {
                try
                {
                    var instance = (NodeBase?)Activator.CreateInstance(kvp.Value);
                    if (instance != null)
                    {
                        var category = instance.Category;
                        if (!result.ContainsKey(category))
                        {
                            result[category] = new List<NodeTypeInfo>();
                        }

                        result[category].Add(new NodeTypeInfo
                        {
                            TypeName = kvp.Key,
                            DisplayName = instance.Label,
                            Category = category,
                            Icon = instance.Icon
                        });
                    }
                }
                catch
                {
                    // Skip nodes that can't be instantiated
                }
            }

            return result;
        }

        /// <summary>
        /// Check if a node type is registered
        /// </summary>
        public bool IsTypeRegistered(string typeName)
        {
            return _nodeTypes.ContainsKey(typeName);
        }

        /// <summary>
        /// Unregister a node type
        /// </summary>
        public void UnregisterNodeType(string typeName)
        {
            _nodeTypes.Remove(typeName);
        }

        /// <summary>
        /// Clear all registered node types
        /// </summary>
        public void Clear()
        {
            _nodeTypes.Clear();
        }

        /// <summary>
        /// Get filtered nodes based on experience mode settings
        /// </summary>
        public Dictionary<string, List<NodeTypeInfo>> GetFilteredNodes(ExperienceModeSettings settings)
        {
            var allNodes = GetNodeTypesByCategory();
            return NodePaletteFilter.GetFilteredNodes(allNodes, settings);
        }
    }

    /// <summary>
    /// Information about a registered node type
    /// </summary>
    public class NodeTypeInfo
    {
        public string TypeName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }
}
