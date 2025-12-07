using System.Collections.Generic;
using System.Linq;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Filters available nodes based on experience mode
    /// </summary>
    public static class NodePaletteFilter
    {
        /// <summary>
        /// Get available nodes filtered by current experience mode
        /// </summary>
        public static Dictionary<string, List<NodeTypeInfo>> GetFilteredNodes(
            Dictionary<string, List<NodeTypeInfo>> allNodes,
            ExperienceModeSettings settings)
        {
            var result = new Dictionary<string, List<NodeTypeInfo>>();

            // If no categories specified, return all
            if (settings.AvailableNodeCategories.Count == 0)
            {
                return allNodes;
            }

            // Filter by available categories
            foreach (var category in settings.AvailableNodeCategories)
            {
                if (allNodes.TryGetValue(category, out var nodes))
                {
                    result[category] = nodes;
                }
            }

            return result;
        }

        /// <summary>
        /// Get list of essential beginner nodes
        /// </summary>
        public static List<string> GetBeginnerNodeTypes()
        {
            return new List<string>
            {
                // Variables (5)
                "Variable",
                "Constant",

                // Devices (6)
                "PinDevice",
                "ReadProperty",
                "WriteProperty",
                "ThisDevice",

                // Basic Math (6)
                "Add",
                "Subtract",
                "Multiply",
                "Divide",

                // Flow Control (3)
                "Comment"
            };
        }

        /// <summary>
        /// Get list of intermediate nodes (includes beginner + more)
        /// </summary>
        public static List<string> GetIntermediateNodeTypes()
        {
            var nodes = new List<string>(GetBeginnerNodeTypes());

            nodes.AddRange(new[]
            {
                // More Variables
                "Const",
                "Define",

                // More Devices
                "NamedDevice",
                "SlotRead",
                "SlotWrite",
                "BatchRead",
                "BatchWrite",

                // More Math
                "Modulo",
                "Power",
                "Negate",
                "MathFunction",
                "MinMax",

                // Logic & Comparison
                "Compare",
                "And",
                "Or",
                "Not",

                // Arrays
                "Array",
                "ArrayAccess",
                "ArrayAssign",

                // Stack
                "Push",
                "Pop",
                "Peek"
            });

            return nodes;
        }

        /// <summary>
        /// Get all node types (expert mode)
        /// </summary>
        public static List<string> GetExpertNodeTypes()
        {
            var nodes = new List<string>(GetIntermediateNodeTypes());

            nodes.AddRange(new[]
            {
                // Advanced Math
                "Trig",
                "Atan2",
                "ExpLog",

                // Bitwise
                "Bitwise",
                "BitwiseNot",
                "Shift",

                // Advanced
                "Hash",
                "Increment",
                "CompoundAssign",

                // Device Advanced
                "DeviceDatabaseLookup"
            });

            return nodes;
        }

        /// <summary>
        /// Check if a node type is available in current mode
        /// </summary>
        public static bool IsNodeTypeAvailable(string nodeType, ExperienceModeSettings settings)
        {
            if (settings.AvailableNodeCategories.Count == 0)
            {
                return true; // All available
            }

            var level = ExperienceModeManager.Instance.CurrentMode;

            return level switch
            {
                ExperienceLevel.Beginner => GetBeginnerNodeTypes().Contains(nodeType),
                ExperienceLevel.Intermediate => GetIntermediateNodeTypes().Contains(nodeType),
                ExperienceLevel.Expert => true, // All available
                ExperienceLevel.Custom => CheckCustomAvailability(nodeType, settings),
                _ => true
            };
        }

        private static bool CheckCustomAvailability(string nodeType, ExperienceModeSettings settings)
        {
            // For custom mode, check if the node's category is in the allowed list
            // This requires looking up the node's category
            // For now, return true if any categories are allowed
            return settings.AvailableNodeCategories.Count == 0;
        }

        /// <summary>
        /// Get node count for a given experience level
        /// </summary>
        public static int GetNodeCountForLevel(ExperienceLevel level)
        {
            return level switch
            {
                ExperienceLevel.Beginner => GetBeginnerNodeTypes().Count,
                ExperienceLevel.Intermediate => GetIntermediateNodeTypes().Count,
                ExperienceLevel.Expert => 60, // Approximate total
                ExperienceLevel.Custom => 0, // Variable
                _ => 0
            };
        }

        /// <summary>
        /// Map category names to friendly names
        /// </summary>
        public static Dictionary<string, string> GetCategoryFriendlyNames()
        {
            return new Dictionary<string, string>
            {
                { "Variables", "Variables & Constants" },
                { "Devices", "Device Operations" },
                { "Basic Math", "Basic Math" },
                { "Flow Control", "Flow Control" },
                { "Math Functions", "Math Functions" },
                { "Logic", "Logic & Comparison" },
                { "Arrays", "Arrays" },
                { "Comparison", "Comparison" },
                { "Bitwise", "Bitwise Operations" },
                { "Advanced", "Advanced Features" },
                { "Stack", "Stack Operations" },
                { "Trigonometry", "Trigonometry" }
            };
        }

        /// <summary>
        /// Get all available categories
        /// </summary>
        public static List<string> GetAllCategories()
        {
            return new List<string>
            {
                "Variables",
                "Devices",
                "Basic Math",
                "Flow Control",
                "Math Functions",
                "Logic",
                "Arrays",
                "Comparison",
                "Bitwise",
                "Advanced",
                "Stack",
                "Trigonometry"
            };
        }
    }
}
