using System;
using System.Collections.Generic;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Nodes.FlowControl;
using BasicToMips.UI.VisualScripting.Wires;
using BasicToMips.UI.VisualScripting.Dialogs;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Built-in project templates for visual scripting
    /// </summary>
    public static class ProjectTemplates
    {
        /// <summary>
        /// Create nodes and wires from a template
        /// </summary>
        public static (List<NodeBase> nodes, List<Wire> wires) CreateFromTemplate(
            ProjectTemplate template, NodeFactory nodeFactory)
        {
            return template switch
            {
                ProjectTemplate.Blank => CreateBlankTemplate(nodeFactory),
                ProjectTemplate.HelloWorld => CreateHelloWorldTemplate(nodeFactory),
                ProjectTemplate.SensorMonitor => CreateSensorMonitorTemplate(nodeFactory),
                ProjectTemplate.DeviceController => CreateDeviceControllerTemplate(nodeFactory),
                _ => CreateBlankTemplate(nodeFactory)
            };
        }

        /// <summary>
        /// Blank project - just an entry point
        /// </summary>
        private static (List<NodeBase> nodes, List<Wire> wires) CreateBlankTemplate(NodeFactory nodeFactory)
        {
            var nodes = new List<NodeBase>();

            // Create entry point node
            var entryPoint = nodeFactory.CreateNode("EntryPoint");
            if (entryPoint != null)
            {
                entryPoint.X = 100;
                entryPoint.Y = 100;
                entryPoint.Initialize();
                nodes.Add(entryPoint);
            }

            return (nodes, new List<Wire>());
        }

        /// <summary>
        /// Hello World - Blink a light on/off
        /// </summary>
        private static (List<NodeBase> nodes, List<Wire> wires) CreateHelloWorldTemplate(NodeFactory nodeFactory)
        {
            var nodes = new List<NodeBase>();
            var wires = new List<Wire>();

            // Entry point
            var entryPoint = CreateNode(nodeFactory, "EntryPoint", 100, 100);
            nodes.Add(entryPoint);

            // Named device (Light)
            var light = CreateNode(nodeFactory, "NamedDevice", 100, 200);
            if (light is NamedDeviceNode lightNode)
            {
                lightNode.AliasName = "light";
                lightNode.PrefabName = "StructureWallLight";
            }
            nodes.Add(light);

            // Constant 1 (On)
            var constOn = CreateNode(nodeFactory, "Constant", 100, 300);
            if (constOn is ConstantNode onNode)
            {
                onNode.Value = 1;
            }
            nodes.Add(constOn);

            // Write Property (Turn On)
            var writeOn = CreateNode(nodeFactory, "WriteProperty", 350, 200);
            if (writeOn is WritePropertyNode writeOnNode)
            {
                writeOnNode.PropertyName = "On";
            }
            nodes.Add(writeOn);

            // Yield (pause)
            var yield1 = CreateNode(nodeFactory, "Yield", 600, 200);
            nodes.Add(yield1);

            // Constant 0 (Off)
            var constOff = CreateNode(nodeFactory, "Constant", 100, 400);
            if (constOff is ConstantNode offNode)
            {
                offNode.Value = 0;
            }
            nodes.Add(constOff);

            // Write Property (Turn Off)
            var writeOff = CreateNode(nodeFactory, "WriteProperty", 850, 200);
            if (writeOff is WritePropertyNode writeOffNode)
            {
                writeOffNode.PropertyName = "On";
            }
            nodes.Add(writeOff);

            // Yield (pause)
            var yield2 = CreateNode(nodeFactory, "Yield", 1100, 200);
            nodes.Add(yield2);

            // Comment node
            var comment = CreateNode(nodeFactory, "Comment", 100, 500);
            if (comment is CommentNode commentNode)
            {
                commentNode.CommentText = "This script blinks a light on and off each tick";
            }
            nodes.Add(comment);

            // Wire connections
            TryConnect(entryPoint, 0, writeOn, 0, wires); // Entry -> Write On
            TryConnect(light, 0, writeOn, 1, wires);       // Light -> Write On (device)
            TryConnect(constOn, 0, writeOn, 2, wires);     // 1 -> Write On (value)
            TryConnect(writeOn, 0, yield1, 0, wires);      // Write On -> Yield1
            TryConnect(yield1, 0, writeOff, 0, wires);     // Yield1 -> Write Off
            TryConnect(light, 0, writeOff, 1, wires);      // Light -> Write Off (device)
            TryConnect(constOff, 0, writeOff, 2, wires);   // 0 -> Write Off (value)
            TryConnect(writeOff, 0, yield2, 0, wires);     // Write Off -> Yield2
            TryConnect(yield2, 0, entryPoint, 0, wires);   // Yield2 -> Entry (loop)

            return (nodes, wires);
        }

        /// <summary>
        /// Sensor Monitor - Read and display sensor values
        /// </summary>
        private static (List<NodeBase> nodes, List<Wire> wires) CreateSensorMonitorTemplate(NodeFactory nodeFactory)
        {
            var nodes = new List<NodeBase>();
            var wires = new List<Wire>();

            // Entry point
            var entryPoint = CreateNode(nodeFactory, "EntryPoint", 100, 100);
            nodes.Add(entryPoint);

            // Named device (Sensor)
            var sensor = CreateNode(nodeFactory, "NamedDevice", 100, 200);
            if (sensor is NamedDeviceNode sensorNode)
            {
                sensorNode.AliasName = "sensor";
                sensorNode.PrefabName = "StructureGasSensor";
            }
            nodes.Add(sensor);

            // Read Property (Temperature)
            var readTemp = CreateNode(nodeFactory, "ReadProperty", 350, 200);
            if (readTemp is ReadPropertyNode readTempNode)
            {
                readTempNode.PropertyName = "Temperature";
            }
            nodes.Add(readTemp);

            // Variable (temp)
            var tempVar = CreateNode(nodeFactory, "Variable", 600, 200);
            if (tempVar is VariableNode tempVarNode)
            {
                tempVarNode.VariableName = "temp";
            }
            nodes.Add(tempVar);

            // Read Property (Pressure)
            var readPress = CreateNode(nodeFactory, "ReadProperty", 350, 350);
            if (readPress is ReadPropertyNode readPressNode)
            {
                readPressNode.PropertyName = "Pressure";
            }
            nodes.Add(readPress);

            // Variable (pressure)
            var pressVar = CreateNode(nodeFactory, "Variable", 600, 350);
            if (pressVar is VariableNode pressVarNode)
            {
                pressVarNode.VariableName = "pressure";
            }
            nodes.Add(pressVar);

            // Yield
            var yield = CreateNode(nodeFactory, "Yield", 850, 200);
            nodes.Add(yield);

            // Comment
            var comment = CreateNode(nodeFactory, "Comment", 100, 500);
            if (comment is CommentNode commentNode)
            {
                commentNode.CommentText = "Monitor sensor temperature and pressure.\nStore values in variables for later use.";
            }
            nodes.Add(comment);

            // Wire connections
            TryConnect(entryPoint, 0, readTemp, 0, wires);
            TryConnect(sensor, 0, readTemp, 1, wires);
            TryConnect(readTemp, 1, tempVar, 1, wires);
            TryConnect(tempVar, 0, readPress, 0, wires);
            TryConnect(sensor, 0, readPress, 1, wires);
            TryConnect(readPress, 1, pressVar, 1, wires);
            TryConnect(pressVar, 0, yield, 0, wires);
            TryConnect(yield, 0, entryPoint, 0, wires);

            return (nodes, wires);
        }

        /// <summary>
        /// Device Controller - Control devices based on conditions
        /// </summary>
        private static (List<NodeBase> nodes, List<Wire> wires) CreateDeviceControllerTemplate(NodeFactory nodeFactory)
        {
            var nodes = new List<NodeBase>();
            var wires = new List<Wire>();

            // Entry point
            var entryPoint = CreateNode(nodeFactory, "EntryPoint", 100, 100);
            nodes.Add(entryPoint);

            // Sensor device
            var sensor = CreateNode(nodeFactory, "NamedDevice", 100, 200);
            if (sensor is NamedDeviceNode sensorNode)
            {
                sensorNode.AliasName = "sensor";
                sensorNode.PrefabName = "StructureGasSensor";
            }
            nodes.Add(sensor);

            // Read temperature
            var readTemp = CreateNode(nodeFactory, "ReadProperty", 350, 200);
            if (readTemp is ReadPropertyNode readTempNode)
            {
                readTempNode.PropertyName = "Temperature";
            }
            nodes.Add(readTemp);

            // Constant threshold (300)
            var threshold = CreateNode(nodeFactory, "Constant", 350, 300);
            if (threshold is ConstantNode thresholdNode)
            {
                thresholdNode.Value = 300;
            }
            nodes.Add(threshold);

            // Compare (Greater Than)
            var compare = CreateNode(nodeFactory, "Compare", 600, 200);
            if (compare is CompareNode compareNode)
            {
                compareNode.Operator = ComparisonOperator.GreaterThan;
            }
            nodes.Add(compare);

            // If node
            var ifNode = CreateNode(nodeFactory, "If", 850, 200);
            nodes.Add(ifNode);

            // Cooler device
            var cooler = CreateNode(nodeFactory, "NamedDevice", 1100, 150);
            if (cooler is NamedDeviceNode coolerNode)
            {
                coolerNode.AliasName = "cooler";
                coolerNode.PrefabName = "StructureActiveVent";
            }
            nodes.Add(cooler);

            // Constant 1
            var constOn = CreateNode(nodeFactory, "Constant", 1100, 250);
            if (constOn is ConstantNode onNode)
            {
                onNode.Value = 1;
            }
            nodes.Add(constOn);

            // Write On (True branch)
            var writeOn = CreateNode(nodeFactory, "WriteProperty", 1350, 150);
            if (writeOn is WritePropertyNode writeOnNode)
            {
                writeOnNode.PropertyName = "On";
            }
            nodes.Add(writeOn);

            // Constant 0
            var constOff = CreateNode(nodeFactory, "Constant", 1100, 400);
            if (constOff is ConstantNode offNode)
            {
                offNode.Value = 0;
            }
            nodes.Add(constOff);

            // Write Off (False branch)
            var writeOff = CreateNode(nodeFactory, "WriteProperty", 1350, 350);
            if (writeOff is WritePropertyNode writeOffNode)
            {
                writeOffNode.PropertyName = "On";
            }
            nodes.Add(writeOff);

            // Yield
            var yield = CreateNode(nodeFactory, "Yield", 1600, 250);
            nodes.Add(yield);

            // Comment
            var comment = CreateNode(nodeFactory, "Comment", 100, 450);
            if (comment is CommentNode commentNode)
            {
                commentNode.CommentText = "Turn on cooler if temperature > 300K\nTurn off cooler if temperature <= 300K";
            }
            nodes.Add(comment);

            // Wire connections
            TryConnect(entryPoint, 0, readTemp, 0, wires);
            TryConnect(sensor, 0, readTemp, 1, wires);
            TryConnect(readTemp, 1, compare, 1, wires);
            TryConnect(threshold, 0, compare, 2, wires);
            TryConnect(compare, 1, ifNode, 1, wires);
            TryConnect(ifNode, 1, writeOn, 0, wires);  // True branch
            TryConnect(cooler, 0, writeOn, 1, wires);
            TryConnect(constOn, 0, writeOn, 2, wires);
            TryConnect(ifNode, 2, writeOff, 0, wires); // False branch
            TryConnect(cooler, 0, writeOff, 1, wires);
            TryConnect(constOff, 0, writeOff, 2, wires);
            TryConnect(writeOn, 0, yield, 0, wires);
            TryConnect(writeOff, 0, yield, 0, wires);
            TryConnect(yield, 0, entryPoint, 0, wires);

            return (nodes, wires);
        }

        /// <summary>
        /// Helper to create and initialize a node
        /// </summary>
        private static NodeBase CreateNode(NodeFactory factory, string nodeType, double x, double y)
        {
            var node = factory.CreateNode(nodeType);
            if (node != null)
            {
                node.X = x;
                node.Y = y;
                node.Initialize();
            }
            return node ?? throw new InvalidOperationException($"Failed to create node of type: {nodeType}");
        }

        /// <summary>
        /// Helper to create wire connections
        /// </summary>
        private static void TryConnect(NodeBase sourceNode, int sourceOutputIndex,
            NodeBase targetNode, int targetInputIndex, List<Wire> wires)
        {
            try
            {
                if (sourceOutputIndex >= sourceNode.OutputPins.Count)
                    return;

                if (targetInputIndex >= targetNode.InputPins.Count)
                    return;

                var sourcePin = sourceNode.OutputPins[sourceOutputIndex];
                var targetPin = targetNode.InputPins[targetInputIndex];

                var wire = new Wire(sourcePin, targetPin);
                wires.Add(wire);

                // Update pin connections
                if (!sourcePin.Connections.Contains(targetPin.Id))
                    sourcePin.Connections.Add(targetPin.Id);

                if (!targetPin.Connections.Contains(sourcePin.Id))
                    targetPin.Connections.Add(sourcePin.Id);
            }
            catch
            {
                // Ignore connection errors in templates
            }
        }
    }
}
