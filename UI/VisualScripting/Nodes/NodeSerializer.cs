using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Handles serialization and deserialization of nodes to/from JSON
    /// </summary>
    public static class NodeSerializer
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        /// <summary>
        /// Serialize a node to a JSON object
        /// </summary>
        public static JsonObject SerializeNode(NodeBase node)
        {
            var json = new JsonObject
            {
                ["id"] = node.Id.ToString(),
                ["nodeType"] = node.NodeType,
                ["x"] = node.X,
                ["y"] = node.Y,
                ["width"] = node.Width,
                ["height"] = node.Height,
                ["label"] = node.Label
            };

            // Serialize input pins
            var inputPinsArray = new JsonArray();
            foreach (var pin in node.InputPins)
            {
                inputPinsArray.Add(SerializePin(pin));
            }
            json["inputPins"] = inputPinsArray;

            // Serialize output pins
            var outputPinsArray = new JsonArray();
            foreach (var pin in node.OutputPins)
            {
                outputPinsArray.Add(SerializePin(pin));
            }
            json["outputPins"] = outputPinsArray;

            // Add node-specific properties
            var propertiesJson = SerializeNodeProperties(node);
            if (propertiesJson != null)
            {
                json["properties"] = propertiesJson;
            }

            return json;
        }

        /// <summary>
        /// Serialize a pin to a JSON object
        /// </summary>
        private static JsonObject SerializePin(NodePin pin)
        {
            var json = new JsonObject
            {
                ["id"] = pin.Id.ToString(),
                ["name"] = pin.Name,
                ["pinType"] = pin.PinType.ToString(),
                ["dataType"] = pin.DataType.ToString()
            };

            // Serialize connections
            var connectionsArray = new JsonArray();
            foreach (var connectionId in pin.Connections)
            {
                connectionsArray.Add(connectionId.ToString());
            }
            json["connections"] = connectionsArray;

            return json;
        }

        /// <summary>
        /// Serialize node-specific properties using reflection
        /// </summary>
        private static JsonObject? SerializeNodeProperties(NodeBase node)
        {
            // Get all properties that are not in the base class
            var nodeType = node.GetType();
            var baseType = typeof(NodeBase);
            var properties = nodeType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var propertiesJson = new JsonObject();
            bool hasProperties = false;

            foreach (var prop in properties)
            {
                // Skip base class properties
                if (prop.DeclaringType == baseType || prop.DeclaringType == typeof(object))
                    continue;

                // Skip computed properties without setters
                if (!prop.CanWrite)
                    continue;

                // Skip properties with JsonIgnore attribute
                if (prop.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length > 0)
                    continue;

                var value = prop.GetValue(node);
                if (value != null)
                {
                    propertiesJson[ToCamelCase(prop.Name)] = JsonValue.Create(value);
                    hasProperties = true;
                }
            }

            return hasProperties ? propertiesJson : null;
        }

        /// <summary>
        /// Deserialize a node from a JSON object using the factory
        /// </summary>
        public static NodeBase? DeserializeNode(JsonObject json, NodeFactory factory)
        {
            var nodeType = json["nodeType"]?.ToString();
            if (string.IsNullOrEmpty(nodeType))
                return null;

            // Create the node instance
            var node = factory.CreateNode(nodeType);
            if (node == null)
                return null;

            // Deserialize base properties
            if (json["id"] != null)
                node.Id = Guid.Parse(json["id"]!.ToString());

            node.X = json["x"]?.GetValue<double>() ?? 0;
            node.Y = json["y"]?.GetValue<double>() ?? 0;
            node.Width = json["width"]?.GetValue<double>() ?? 200;
            node.Height = json["height"]?.GetValue<double>() ?? 100;
            node.Label = json["label"]?.ToString() ?? string.Empty;

            // Deserialize input pins
            if (json["inputPins"] is JsonArray inputPins)
            {
                node.InputPins.Clear();
                foreach (var pinJson in inputPins)
                {
                    if (pinJson is JsonObject pinObj)
                    {
                        var pin = DeserializePin(pinObj);
                        if (pin != null)
                        {
                            pin.ParentNode = node;
                            node.InputPins.Add(pin);
                        }
                    }
                }
            }

            // Deserialize output pins
            if (json["outputPins"] is JsonArray outputPins)
            {
                node.OutputPins.Clear();
                foreach (var pinJson in outputPins)
                {
                    if (pinJson is JsonObject pinObj)
                    {
                        var pin = DeserializePin(pinObj);
                        if (pin != null)
                        {
                            pin.ParentNode = node;
                            node.OutputPins.Add(pin);
                        }
                    }
                }
            }

            // Deserialize node-specific properties
            if (json["properties"] is JsonObject properties)
            {
                DeserializeNodeProperties(node, properties);
            }

            // Initialize the node
            node.Initialize();

            return node;
        }

        /// <summary>
        /// Deserialize a pin from a JSON object
        /// </summary>
        private static NodePin? DeserializePin(JsonObject json)
        {
            var pin = new NodePin
            {
                Id = Guid.Parse(json["id"]!.ToString()),
                Name = json["name"]?.ToString() ?? string.Empty,
                PinType = Enum.Parse<PinType>(json["pinType"]?.ToString() ?? "Input"),
                DataType = Enum.Parse<DataType>(json["dataType"]?.ToString() ?? "Execution")
            };

            // Deserialize connections
            if (json["connections"] is JsonArray connections)
            {
                foreach (var connJson in connections)
                {
                    if (Guid.TryParse(connJson?.ToString(), out var connId))
                    {
                        pin.Connections.Add(connId);
                    }
                }
            }

            return pin;
        }

        /// <summary>
        /// Deserialize node-specific properties using reflection
        /// </summary>
        private static void DeserializeNodeProperties(NodeBase node, JsonObject properties)
        {
            var nodeType = node.GetType();

            foreach (var kvp in properties)
            {
                var propName = ToPascalCase(kvp.Key);
                var prop = nodeType.GetProperty(propName);

                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var value = JsonSerializer.Deserialize(kvp.Value, prop.PropertyType, _options);
                        if (value != null)
                        {
                            prop.SetValue(node, value);
                        }
                    }
                    catch
                    {
                        // Ignore deserialization errors for individual properties
                    }
                }
            }
        }

        /// <summary>
        /// Convert string to camelCase
        /// </summary>
        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        /// <summary>
        /// Convert string to PascalCase
        /// </summary>
        private static string ToPascalCase(string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsUpper(str[0]))
                return str;

            return char.ToUpperInvariant(str[0]) + str.Substring(1);
        }
    }
}
