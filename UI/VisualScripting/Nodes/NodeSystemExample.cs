using System;
using System.Text.Json.Nodes;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Example usage of the node system
    /// This class demonstrates how to:
    /// - Create a factory and register node types
    /// - Create node instances
    /// - Serialize/deserialize nodes
    /// - Connect pins between nodes
    /// </summary>
    public static class NodeSystemExample
    {
        /// <summary>
        /// Create and configure the node factory with all available node types
        /// </summary>
        public static NodeFactory CreateFactory()
        {
            var factory = new NodeFactory();

            // Register basic node types
            factory.RegisterNodeType<CommentNode>();
            factory.RegisterNodeType<VariableNode>();
            factory.RegisterNodeType<MathNode>();

            // Register variable nodes (Phase 3A)
            factory.RegisterNodeType<ConstantNode>();
            factory.RegisterNodeType<ConstNode>();
            factory.RegisterNodeType<DefineNode>();
            factory.RegisterNodeType<ArrayNode>();
            factory.RegisterNodeType<ArrayAccessNode>();
            factory.RegisterNodeType<ArrayAssignNode>();

            // Register basic math operation nodes (Phase 3A)
            factory.RegisterNodeType<AddNode>();
            factory.RegisterNodeType<SubtractNode>();
            factory.RegisterNodeType<MultiplyNode>();
            factory.RegisterNodeType<DivideNode>();
            factory.RegisterNodeType<ModuloNode>();
            factory.RegisterNodeType<PowerNode>();

            // Register advanced math nodes (Phase 3A)
            factory.RegisterNodeType<NegateNode>();
            factory.RegisterNodeType<MathFunctionNode>();
            factory.RegisterNodeType<MinMaxNode>();
            factory.RegisterNodeType<TrigNode>();
            factory.RegisterNodeType<Atan2Node>();
            factory.RegisterNodeType<ExpLogNode>();

            // Register compound assignment nodes (Phase 3A)
            factory.RegisterNodeType<CompoundAssignNode>();
            factory.RegisterNodeType<IncrementNode>();

            // Register comparison and logical nodes (Phase 3A)
            factory.RegisterNodeType<CompareNode>();
            factory.RegisterNodeType<AndNode>();
            factory.RegisterNodeType<OrNode>();
            factory.RegisterNodeType<NotNode>();

            // Register bitwise nodes (Phase 3A)
            factory.RegisterNodeType<BitwiseNode>();
            factory.RegisterNodeType<BitwiseNotNode>();
            factory.RegisterNodeType<ShiftNode>();

            // Register device declaration nodes
            factory.RegisterNodeType<PinDeviceNode>();
            factory.RegisterNodeType<NamedDeviceNode>();
            factory.RegisterNodeType<ThisDeviceNode>();

            // Register device I/O nodes
            factory.RegisterNodeType<ReadPropertyNode>();
            factory.RegisterNodeType<WritePropertyNode>();
            factory.RegisterNodeType<SlotReadNode>();
            factory.RegisterNodeType<SlotWriteNode>();

            // Register batch operation nodes
            factory.RegisterNodeType<BatchReadNode>();
            factory.RegisterNodeType<BatchWriteNode>();

            // Register stack operation nodes
            factory.RegisterNodeType<PushNode>();
            factory.RegisterNodeType<PopNode>();
            factory.RegisterNodeType<PeekNode>();

            // Register utility nodes
            factory.RegisterNodeType<HashNode>();

            // Register flow control nodes (Phase 5A)
            factory.RegisterNodeType<FlowControl.EntryPointNode>();
            factory.RegisterNodeType<FlowControl.IfNode>();
            factory.RegisterNodeType<FlowControl.WhileNode>();
            factory.RegisterNodeType<FlowControl.ForNode>();
            factory.RegisterNodeType<FlowControl.DoUntilNode>();
            factory.RegisterNodeType<FlowControl.BreakNode>();
            factory.RegisterNodeType<FlowControl.ContinueNode>();
            factory.RegisterNodeType<FlowControl.LabelNode>();
            factory.RegisterNodeType<FlowControl.GotoNode>();
            factory.RegisterNodeType<FlowControl.GosubNode>();
            factory.RegisterNodeType<FlowControl.ReturnNode>();
            factory.RegisterNodeType<FlowControl.SelectCaseNode>();
            factory.RegisterNodeType<FlowControl.YieldNode>();
            factory.RegisterNodeType<FlowControl.SleepNode>();
            factory.RegisterNodeType<FlowControl.EndNode>();

            // Register subroutine nodes (Phase 5B)
            factory.RegisterNodeType<Subroutines.SubDefinitionNode>();
            factory.RegisterNodeType<Subroutines.CallSubNode>();
            factory.RegisterNodeType<Subroutines.ExitSubNode>();
            factory.RegisterNodeType<Subroutines.FunctionDefinitionNode>();
            factory.RegisterNodeType<Subroutines.CallFunctionNode>();
            factory.RegisterNodeType<Subroutines.ExitFunctionNode>();
            factory.RegisterNodeType<Subroutines.SetReturnValueNode>();

            // You can also register with explicit type names:
            // factory.RegisterNodeType<CommentNode>("Comment");

            return factory;
        }

        /// <summary>
        /// Example: Create a simple variable declaration node
        /// </summary>
        public static VariableNode CreateExampleVariableNode()
        {
            var node = new VariableNode
            {
                X = 100,
                Y = 100,
                VariableName = "temperature",
                VariableType = "Number",
                InitialValue = "25.5",
                IsDeclaration = true
            };

            node.Initialize();
            return node;
        }

        /// <summary>
        /// Example: Create a comment node
        /// </summary>
        public static CommentNode CreateExampleCommentNode()
        {
            var node = new CommentNode
            {
                X = 50,
                Y = 50,
                CommentText = "This is a sample comment\nIt can have multiple lines",
                CommentColor = "#4A9EFF"
            };

            node.Initialize();
            return node;
        }

        /// <summary>
        /// Example: Create a math node
        /// </summary>
        public static MathNode CreateExampleMathNode()
        {
            var node = new MathNode
            {
                X = 300,
                Y = 100,
                Operation = MathOperation.Add
            };

            node.Initialize();
            return node;
        }

        /// <summary>
        /// Example: Connect two nodes via their pins
        /// </summary>
        public static void ConnectNodes(NodeBase sourceNode, string sourcePinName, NodeBase targetNode, string targetPinName)
        {
            // Find the output pin on the source node
            var sourcePin = sourceNode.OutputPins.Find(p => p.Name == sourcePinName);
            if (sourcePin == null)
            {
                throw new ArgumentException($"Source node does not have an output pin named '{sourcePinName}'");
            }

            // Find the input pin on the target node
            var targetPin = targetNode.InputPins.Find(p => p.Name == targetPinName);
            if (targetPin == null)
            {
                throw new ArgumentException($"Target node does not have an input pin named '{targetPinName}'");
            }

            // Check type compatibility
            if (sourcePin.DataType != targetPin.DataType)
            {
                throw new InvalidOperationException(
                    $"Cannot connect pins of different types: {sourcePin.DataType} -> {targetPin.DataType}");
            }

            // Create the connection
            if (!sourcePin.Connections.Contains(targetPin.Id))
            {
                sourcePin.Connections.Add(targetPin.Id);
            }

            if (!targetPin.Connections.Contains(sourcePin.Id))
            {
                targetPin.Connections.Add(sourcePin.Id);
            }
        }

        /// <summary>
        /// Example: Serialize a node to JSON
        /// </summary>
        public static string SerializeNodeExample(NodeBase node)
        {
            var json = NodeSerializer.SerializeNode(node);
            return json.ToJsonString(new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        /// <summary>
        /// Example: Deserialize a node from JSON
        /// </summary>
        public static NodeBase? DeserializeNodeExample(string jsonString, NodeFactory factory)
        {
            var json = JsonNode.Parse(jsonString);
            if (json is JsonObject jsonObj)
            {
                return NodeSerializer.DeserializeNode(jsonObj, factory);
            }
            return null;
        }

        /// <summary>
        /// Example: Validate all nodes in a collection
        /// </summary>
        public static bool ValidateAllNodes(System.Collections.Generic.List<NodeBase> nodes, out string errorReport)
        {
            var errors = new System.Text.StringBuilder();
            bool allValid = true;

            foreach (var node in nodes)
            {
                if (!node.Validate(out string errorMessage))
                {
                    allValid = false;
                    errors.AppendLine($"Node '{node.Label}' ({node.Id}): {errorMessage}");
                }
            }

            errorReport = errors.ToString();
            return allValid;
        }

        /// <summary>
        /// Example: Generate code from all nodes
        /// </summary>
        public static string GenerateCodeFromNodes(System.Collections.Generic.List<NodeBase> nodes)
        {
            var code = new System.Text.StringBuilder();
            code.AppendLine("REM Generated from Visual Scripting");
            code.AppendLine();

            foreach (var node in nodes)
            {
                var nodeCode = node.GenerateCode();
                if (!string.IsNullOrWhiteSpace(nodeCode))
                {
                    code.AppendLine(nodeCode);
                }
            }

            return code.ToString();
        }

        /// <summary>
        /// Complete example showing the full workflow
        /// </summary>
        public static void RunCompleteExample()
        {
            Console.WriteLine("=== Node System Example ===\n");

            // 1. Create factory and register nodes
            var factory = CreateFactory();
            Console.WriteLine("1. Factory created with registered node types:");
            foreach (var typeName in factory.GetRegisteredTypes())
            {
                Console.WriteLine($"   - {typeName}");
            }
            Console.WriteLine();

            // 2. Create some nodes
            var commentNode = CreateExampleCommentNode();
            var varNode = CreateExampleVariableNode();
            var mathNode = CreateExampleMathNode();

            Console.WriteLine("2. Created example nodes:");
            Console.WriteLine($"   - Comment: {commentNode.Label} at ({commentNode.X}, {commentNode.Y})");
            Console.WriteLine($"   - Variable: {varNode.Label} at ({varNode.X}, {varNode.Y})");
            Console.WriteLine($"   - Math: {mathNode.Label} at ({mathNode.X}, {mathNode.Y})");
            Console.WriteLine();

            // 3. Connect nodes
            try
            {
                ConnectNodes(varNode, "Out", mathNode, "In");
                ConnectNodes(varNode, "temperature", mathNode, "A");
                Console.WriteLine("3. Connected nodes successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"3. Connection failed: {ex.Message}");
            }
            Console.WriteLine();

            // 4. Serialize a node
            var serialized = SerializeNodeExample(varNode);
            Console.WriteLine("4. Serialized variable node:");
            Console.WriteLine(serialized);
            Console.WriteLine();

            // 5. Deserialize it back
            var deserialized = DeserializeNodeExample(serialized, factory);
            Console.WriteLine($"5. Deserialized node: {deserialized?.NodeType ?? "null"} - {deserialized?.Label ?? "null"}");
            Console.WriteLine();

            // 6. Validate nodes
            var nodes = new System.Collections.Generic.List<NodeBase> { commentNode, varNode, mathNode };
            bool valid = ValidateAllNodes(nodes, out string errorReport);
            Console.WriteLine($"6. Validation: {(valid ? "PASSED" : "FAILED")}");
            if (!string.IsNullOrEmpty(errorReport))
            {
                Console.WriteLine($"   Errors:\n{errorReport}");
            }
            Console.WriteLine();

            // 7. Generate code
            var generatedCode = GenerateCodeFromNodes(nodes);
            Console.WriteLine("7. Generated BASIC code:");
            Console.WriteLine(generatedCode);
        }
    }
}
