using System;
using System.Collections.Generic;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.CodeGen
{
    /// <summary>
    /// Example usage of the code generation system
    /// </summary>
    public static class CodeGeneratorExample
    {
        /// <summary>
        /// Create a simple example graph and generate code
        /// Example: VAR counter = 0; counter++
        /// </summary>
        public static (string code, SourceMap sourceMap, List<CodeGenerationError> errors) GenerateSimpleExample()
        {
            var nodes = new List<NodeBase>();
            var wires = new List<Wire>();

            // Create a variable node
            var varNode = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "counter",
                InitialValue = "0",
                IsDeclaration = true,
                X = 100,
                Y = 100
            };
            varNode.Initialize();
            nodes.Add(varNode);

            // Create an increment node
            var incNode = new IncrementNode
            {
                Id = Guid.NewGuid(),
                VariableName = "counter",
                Type = IncrementType.Increment,
                Position = IncrementPosition.Postfix,
                X = 100,
                Y = 200
            };
            incNode.Initialize();
            nodes.Add(incNode);

            // Create generator and generate code
            var generator = new GraphToBasicGenerator(nodes, wires);
            var (code, sourceMap) = generator.GenerateWithSourceMap();

            return (code, sourceMap, generator.GetErrors());
        }

        /// <summary>
        /// Create an example with device reading
        /// Example: ALIAS pump d0; VAR temp; temp = pump.Temperature
        /// </summary>
        public static (string code, SourceMap sourceMap, List<CodeGenerationError> errors) GenerateDeviceExample()
        {
            var nodes = new List<NodeBase>();
            var wires = new List<Wire>();

            // Create a pin device node (d0)
            var deviceNode = new PinDeviceNode
            {
                Id = Guid.NewGuid(),
                AliasName = "pump",
                PinNumber = 0,
                X = 100,
                Y = 100
            };
            deviceNode.Initialize();
            nodes.Add(deviceNode);

            // Create a variable to store the temperature
            var varNode = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "temp",
                InitialValue = "0",
                IsDeclaration = true,
                X = 100,
                Y = 200
            };
            varNode.Initialize();
            nodes.Add(varNode);

            // Create a read property node
            var readNode = new ReadPropertyNode
            {
                Id = Guid.NewGuid(),
                PropertyName = "Temperature",
                X = 300,
                Y = 150
            };
            readNode.Initialize();
            nodes.Add(readNode);

            // Wire device to read property
            var deviceOutputPin = deviceNode.OutputPins[0]; // Device output
            var readDevicePin = readNode.InputPins[0]; // Device input

            var wire1 = new Wire(deviceOutputPin, readDevicePin);
            wires.Add(wire1);

            // Create assignment node for temp = pump.Temperature
            var assignNode = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "temp",
                IsDeclaration = false,
                X = 500,
                Y = 200
            };
            assignNode.Initialize();
            nodes.Add(assignNode);

            // Wire read property to assignment
            var readOutputPin = readNode.OutputPins[0]; // Value output
            var assignValuePin = assignNode.InputPins.Find(p => p.Name == "Value");

            if (assignValuePin != null)
            {
                var wire2 = new Wire(readOutputPin, assignValuePin);
                wires.Add(wire2);
            }

            // Create generator and generate code
            var generator = new GraphToBasicGenerator(nodes, wires);
            var (code, sourceMap) = generator.GenerateWithSourceMap();

            return (code, sourceMap, generator.GetErrors());
        }

        /// <summary>
        /// Create an example with math operations
        /// Example: VAR a = 5; VAR b = 3; VAR result; result = a + b
        /// </summary>
        public static (string code, SourceMap sourceMap, List<CodeGenerationError> errors) GenerateMathExample()
        {
            var nodes = new List<NodeBase>();
            var wires = new List<Wire>();

            // Create variable a = 5
            var varA = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "a",
                InitialValue = "5",
                IsDeclaration = true,
                X = 100,
                Y = 100
            };
            varA.Initialize();
            nodes.Add(varA);

            // Create variable b = 3
            var varB = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "b",
                InitialValue = "3",
                IsDeclaration = true,
                X = 100,
                Y = 150
            };
            varB.Initialize();
            nodes.Add(varB);

            // Create result variable
            var varResult = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "result",
                InitialValue = "0",
                IsDeclaration = true,
                X = 100,
                Y = 200
            };
            varResult.Initialize();
            nodes.Add(varResult);

            // Create add node
            var addNode = new AddNode
            {
                Id = Guid.NewGuid(),
                X = 300,
                Y = 150
            };
            addNode.Initialize();
            nodes.Add(addNode);

            // Wire a to add.A
            var wire1 = new Wire(
                varA.OutputPins.Find(p => p.Name == "a"),
                addNode.InputPins.Find(p => p.Name == "A")
            );
            wires.Add(wire1);

            // Wire b to add.B
            var wire2 = new Wire(
                varB.OutputPins.Find(p => p.Name == "b"),
                addNode.InputPins.Find(p => p.Name == "B")
            );
            wires.Add(wire2);

            // Create assignment node for result = a + b
            var assignNode = new VariableNode
            {
                Id = Guid.NewGuid(),
                VariableName = "result",
                IsDeclaration = false,
                X = 500,
                Y = 200
            };
            assignNode.Initialize();
            nodes.Add(assignNode);

            // Wire add result to assignment
            var addOutputPin = addNode.OutputPins.Find(p => p.Name == "Result");
            var assignValuePin = assignNode.InputPins.Find(p => p.Name == "Value");

            if (addOutputPin != null && assignValuePin != null)
            {
                var wire3 = new Wire(addOutputPin, assignValuePin);
                wires.Add(wire3);
            }

            // Create generator and generate code
            var generator = new GraphToBasicGenerator(nodes, wires);
            var (code, sourceMap) = generator.GenerateWithSourceMap();

            return (code, sourceMap, generator.GetErrors());
        }

        /// <summary>
        /// Run all examples and print results
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("=== Simple Example ===");
            var (code1, map1, errors1) = GenerateSimpleExample();
            Console.WriteLine(code1);
            Console.WriteLine($"Errors: {errors1.Count}");
            Console.WriteLine();

            Console.WriteLine("=== Device Example ===");
            var (code2, map2, errors2) = GenerateDeviceExample();
            Console.WriteLine(code2);
            Console.WriteLine($"Errors: {errors2.Count}");
            Console.WriteLine();

            Console.WriteLine("=== Math Example ===");
            var (code3, map3, errors3) = GenerateMathExample();
            Console.WriteLine(code3);
            Console.WriteLine($"Errors: {errors3.Count}");
            Console.WriteLine();
        }
    }
}
