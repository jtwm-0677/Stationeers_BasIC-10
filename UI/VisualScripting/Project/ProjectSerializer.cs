using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;
using BasicToMips.UI.VisualScripting.CodeGen;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Handles serialization and deserialization of visual script projects
    /// </summary>
    public class ProjectSerializer
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Save a visual script project to disk
        /// Creates folder structure: visual.json, script.bas, script.ic10, instruction.xml
        /// </summary>
        public static void SaveProject(VisualScriptProject project, string folderPath,
            List<NodeBase> nodes, List<Wire> wires, string? author = null, string? description = null)
        {
            // Create project folder if it doesn't exist
            Directory.CreateDirectory(folderPath);

            // Update project data
            project.Name = Path.GetFileName(folderPath);
            project.Modified = DateTime.Now;
            project.FilePath = Path.Combine(folderPath, "visual.json");

            // Convert nodes to serializable format
            project.Nodes = nodes.Select(ConvertNodeToData).ToList();

            // Convert wires to serializable format
            project.Wires = wires.Select(ConvertWireToData).ToList();

            // Save visual.json
            var visualJsonPath = Path.Combine(folderPath, "visual.json");
            var json = JsonSerializer.Serialize(project, _jsonOptions);
            File.WriteAllText(visualJsonPath, json);

            // Generate BASIC code
            string basicCode = GenerateBasicCode(nodes, wires);
            var basicPath = Path.Combine(folderPath, "script.bas");
            File.WriteAllText(basicPath, basicCode);

            // Compile to IC10
            string ic10Code = CompileToIC10(basicCode);
            var ic10Path = Path.Combine(folderPath, "script.ic10");
            File.WriteAllText(ic10Path, ic10Code);

            // Create instruction.xml for Stationeers
            CreateInstructionXml(folderPath, project.Name, author, description);
        }

        /// <summary>
        /// Load a visual script project from disk
        /// </summary>
        public static VisualScriptProject LoadProject(string folderPath)
        {
            var visualJsonPath = Path.Combine(folderPath, "visual.json");

            if (!File.Exists(visualJsonPath))
            {
                throw new FileNotFoundException($"Project file not found: {visualJsonPath}");
            }

            var json = File.ReadAllText(visualJsonPath);
            var project = JsonSerializer.Deserialize<VisualScriptProject>(json, _jsonOptions);

            if (project == null)
            {
                throw new InvalidOperationException("Failed to deserialize project");
            }

            project.FilePath = visualJsonPath;

            // Check version and migrate if needed
            if (project.Version != "3.0")
            {
                MigrateProject(project, folderPath);
            }

            return project;
        }

        /// <summary>
        /// Restore nodes and wires from project data
        /// </summary>
        public static (List<NodeBase> nodes, List<Wire> wires) RestoreNodesAndWires(
            VisualScriptProject project, NodeFactory nodeFactory)
        {
            var nodes = new List<NodeBase>();
            var nodeDict = new Dictionary<Guid, NodeBase>();

            // Restore nodes
            foreach (var nodeData in project.Nodes)
            {
                var node = RestoreNode(nodeData, nodeFactory);
                if (node != null)
                {
                    nodes.Add(node);
                    nodeDict[node.Id] = node;
                }
            }

            // Restore wires
            var wires = new List<Wire>();
            foreach (var wireData in project.Wires)
            {
                var wire = RestoreWire(wireData, nodeDict);
                if (wire != null)
                {
                    wires.Add(wire);
                }
            }

            return (nodes, wires);
        }

        /// <summary>
        /// Convert NodeBase to serializable NodeData
        /// </summary>
        private static NodeData ConvertNodeToData(NodeBase node)
        {
            return new NodeData
            {
                Id = node.Id,
                NodeType = node.NodeType,
                X = node.X,
                Y = node.Y,
                Width = node.Width,
                Height = node.Height,
                Label = node.Label,
                InputPins = node.InputPins.Select(ConvertPinToData).ToList(),
                OutputPins = node.OutputPins.Select(ConvertPinToData).ToList(),
                Properties = ExtractNodeProperties(node)
            };
        }

        /// <summary>
        /// Convert NodePin to serializable PinData
        /// </summary>
        private static PinData ConvertPinToData(NodePin pin)
        {
            return new PinData
            {
                Id = pin.Id,
                Name = pin.Name,
                PinType = pin.PinType.ToString(),
                DataType = pin.DataType.ToString(),
                Connections = new List<Guid>(pin.Connections)
            };
        }

        /// <summary>
        /// Convert Wire to serializable WireData
        /// </summary>
        private static WireData ConvertWireToData(Wire wire)
        {
            return new WireData
            {
                Id = wire.Id,
                SourceNodeId = wire.SourceNodeId,
                SourcePinId = wire.SourcePinId,
                TargetNodeId = wire.TargetNodeId,
                TargetPinId = wire.TargetPinId,
                DataType = wire.DataType.ToString()
            };
        }

        /// <summary>
        /// Extract node-specific properties using reflection
        /// </summary>
        private static Dictionary<string, object>? ExtractNodeProperties(NodeBase node)
        {
            var nodeType = node.GetType();
            var baseType = typeof(NodeBase);
            var properties = nodeType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var dict = new Dictionary<string, object>();

            foreach (var prop in properties)
            {
                // Skip base class properties
                if (prop.DeclaringType == baseType || prop.DeclaringType == typeof(object))
                    continue;

                // Skip computed properties without setters
                if (!prop.CanWrite)
                    continue;

                // Skip properties with JsonIgnore attribute
                if (prop.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length > 0)
                    continue;

                var value = prop.GetValue(node);
                if (value != null)
                {
                    dict[prop.Name] = value;
                }
            }

            return dict.Count > 0 ? dict : null;
        }

        /// <summary>
        /// Restore NodeBase from NodeData
        /// </summary>
        private static NodeBase? RestoreNode(NodeData nodeData, NodeFactory nodeFactory)
        {
            var node = nodeFactory.CreateNode(nodeData.NodeType);
            if (node == null)
                return null;

            node.Id = nodeData.Id;
            node.X = nodeData.X;
            node.Y = nodeData.Y;
            node.Width = nodeData.Width;
            node.Height = nodeData.Height;
            node.Label = nodeData.Label;

            // Restore pins
            node.InputPins.Clear();
            foreach (var pinData in nodeData.InputPins)
            {
                var pin = RestorePin(pinData, node);
                node.InputPins.Add(pin);
            }

            node.OutputPins.Clear();
            foreach (var pinData in nodeData.OutputPins)
            {
                var pin = RestorePin(pinData, node);
                node.OutputPins.Add(pin);
            }

            // Restore node-specific properties
            if (nodeData.Properties != null)
            {
                RestoreNodeProperties(node, nodeData.Properties);
            }

            node.Initialize();
            return node;
        }

        /// <summary>
        /// Restore NodePin from PinData
        /// </summary>
        private static NodePin RestorePin(PinData pinData, NodeBase parentNode)
        {
            return new NodePin
            {
                Id = pinData.Id,
                Name = pinData.Name,
                PinType = Enum.Parse<PinType>(pinData.PinType),
                DataType = Enum.Parse<DataType>(pinData.DataType),
                Connections = new List<Guid>(pinData.Connections),
                ParentNode = parentNode
            };
        }

        /// <summary>
        /// Restore Wire from WireData
        /// </summary>
        private static Wire? RestoreWire(WireData wireData, Dictionary<Guid, NodeBase> nodes)
        {
            if (!nodes.TryGetValue(wireData.SourceNodeId, out var sourceNode))
                return null;

            if (!nodes.TryGetValue(wireData.TargetNodeId, out var targetNode))
                return null;

            var sourcePin = sourceNode.OutputPins.FirstOrDefault(p => p.Id == wireData.SourcePinId);
            if (sourcePin == null)
                return null;

            var targetPin = targetNode.InputPins.FirstOrDefault(p => p.Id == wireData.TargetPinId);
            if (targetPin == null)
                return null;

            return new Wire
            {
                Id = wireData.Id,
                SourceNodeId = wireData.SourceNodeId,
                SourcePinId = wireData.SourcePinId,
                TargetNodeId = wireData.TargetNodeId,
                TargetPinId = wireData.TargetPinId,
                DataType = Enum.Parse<DataType>(wireData.DataType),
                SourceNode = sourceNode,
                SourcePin = sourcePin,
                TargetNode = targetNode,
                TargetPin = targetPin
            };
        }

        /// <summary>
        /// Restore node-specific properties using reflection
        /// </summary>
        private static void RestoreNodeProperties(NodeBase node, Dictionary<string, object> properties)
        {
            var nodeType = node.GetType();

            foreach (var kvp in properties)
            {
                var prop = nodeType.GetProperty(kvp.Key);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        object? value = null;

                        if (kvp.Value is JsonElement jsonElement)
                        {
                            value = JsonSerializer.Deserialize(jsonElement.GetRawText(), prop.PropertyType);
                        }
                        else
                        {
                            value = Convert.ChangeType(kvp.Value, prop.PropertyType);
                        }

                        if (value != null)
                        {
                            prop.SetValue(node, value);
                        }
                    }
                    catch
                    {
                        // Ignore property restoration errors
                    }
                }
            }
        }

        /// <summary>
        /// Generate BASIC code from visual graph
        /// </summary>
        private static string GenerateBasicCode(List<NodeBase> nodes, List<Wire> wires)
        {
            try
            {
                var generator = new GraphToBasicGenerator(nodes, wires);
                var (basicCode, _) = generator.GenerateWithSourceMap();
                return basicCode;
            }
            catch (Exception ex)
            {
                return $"' Error generating BASIC code: {ex.Message}\n";
            }
        }

        /// <summary>
        /// Compile BASIC code to IC10
        /// </summary>
        private static string CompileToIC10(string basicCode)
        {
            try
            {
                var lexer = new BasicToMips.Lexer.Lexer(basicCode, preserveComments: false);
                var tokens = lexer.Tokenize();

                var parser = new BasicToMips.Parser.Parser(tokens);
                var ast = parser.Parse();

                var generator = new MipsGenerator();
                return generator.Generate(ast);
            }
            catch (Exception ex)
            {
                return $"# Compilation error: {ex.Message}\n";
            }
        }

        /// <summary>
        /// Create instruction.xml for Stationeers integration
        /// </summary>
        private static void CreateInstructionXml(string folderPath, string title,
            string? author = null, string? description = null)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Instruction",
                    new XElement("Title", title),
                    new XElement("Description", description ?? "Built in Basic-10 Visual Scripting"),
                    new XElement("Author", author ?? Environment.UserName)
                )
            );

            var xmlPath = Path.Combine(folderPath, "instruction.xml");
            doc.Save(xmlPath);
        }

        /// <summary>
        /// Migrate older project versions to current format
        /// </summary>
        private static void MigrateProject(VisualScriptProject project, string folderPath)
        {
            // Backup original file
            var originalPath = Path.Combine(folderPath, "visual.json");
            var backupPath = Path.Combine(folderPath, $"visual.json.backup.v{project.Version}");

            if (File.Exists(originalPath) && !File.Exists(backupPath))
            {
                File.Copy(originalPath, backupPath);
            }

            // Perform version-specific migrations
            // (Add migration logic here as needed)

            // Update to current version
            project.Version = "3.0";
        }

        /// <summary>
        /// Check if a folder contains a valid visual script project
        /// </summary>
        public static bool IsValidProjectFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return false;

            var visualJsonPath = Path.Combine(folderPath, "visual.json");
            return File.Exists(visualJsonPath);
        }

        /// <summary>
        /// Get project metadata without loading full project
        /// </summary>
        public static (string name, DateTime modified, int nodeCount)? GetProjectInfo(string folderPath)
        {
            try
            {
                var visualJsonPath = Path.Combine(folderPath, "visual.json");
                if (!File.Exists(visualJsonPath))
                    return null;

                var json = File.ReadAllText(visualJsonPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var name = root.GetProperty("name").GetString() ?? "Untitled";
                var modifiedStr = root.GetProperty("modified").GetString();
                var modified = DateTime.Parse(modifiedStr ?? DateTime.Now.ToString());
                var nodeCount = root.GetProperty("nodes").GetArrayLength();

                return (name, modified, nodeCount);
            }
            catch
            {
                return null;
            }
        }
    }
}
