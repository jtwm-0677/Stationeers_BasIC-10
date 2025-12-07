using System;
using BasicToMips.UI.VisualScripting.Nodes;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Provides node labels based on experience mode
    /// Generates friendly, mixed, or technical labels for nodes
    /// </summary>
    public static class NodeLabelProvider
    {
        /// <summary>
        /// Get the appropriate label for a node based on current mode
        /// </summary>
        public static string GetLabel(NodeBase node, NodeLabelStyle style)
        {
            return style switch
            {
                NodeLabelStyle.Friendly => GetFriendlyLabel(node),
                NodeLabelStyle.Mixed => GetMixedLabel(node),
                NodeLabelStyle.Technical => GetTechnicalLabel(node),
                _ => node.Label
            };
        }

        /// <summary>
        /// Get friendly, beginner-oriented label
        /// </summary>
        public static string GetFriendlyLabel(NodeBase node)
        {
            return node switch
            {
                // Variables
                VariableNode varNode => varNode.IsDeclaration
                    ? $"Create Variable '{varNode.VariableName}'"
                    : $"Set '{varNode.VariableName}' to Value",

                ConstantNode => "Set Constant Value",
                ConstNode constNode => $"Define Constant '{constNode.ConstName}'",
                DefineNode defineNode => $"Define '{defineNode.DefineName}'",

                // Device nodes
                ReadPropertyNode readNode => $"Read {readNode.PropertyName} from Device",
                WritePropertyNode writeNode => $"Write {writeNode.PropertyName} to Device",
                PinDeviceNode pinNode => $"Get Device from Pin {pinNode.PinNumber}",
                NamedDeviceNode namedNode => $"Get Device '{namedNode.AliasName}'",
                ThisDeviceNode => "Get This IC Housing",
                BatchReadNode => "Read from All Devices",
                BatchWriteNode => "Write to All Devices",
                SlotReadNode slotNode => $"Read Slot ({slotNode.PropertyName})",
                SlotWriteNode slotNode => $"Write to Slot ({slotNode.PropertyName})",

                // Math operations
                AddNode => "Add Numbers Together",
                SubtractNode => "Subtract Numbers",
                MultiplyNode => "Multiply Numbers",
                DivideNode => "Divide Numbers",
                ModuloNode => "Get Remainder",
                PowerNode => "Raise to Power",
                NegateNode => "Make Number Negative",

                // Math functions
                MathFunctionNode mathFunc => mathFunc.Function switch
                {
                    MathFunctionType.ABS => "Absolute Value",
                    MathFunctionType.SQRT => "Square Root",
                    MathFunctionType.CEIL => "Round Up",
                    MathFunctionType.FLOOR => "Round Down",
                    MathFunctionType.ROUND => "Round to Nearest",
                    MathFunctionType.TRUNC => "Remove Decimal Part",
                    _ => $"Math: {mathFunc.Function}"
                },

                TrigNode trigNode => trigNode.Function switch
                {
                    TrigFunction.SIN => "Sine",
                    TrigFunction.COS => "Cosine",
                    TrigFunction.TAN => "Tangent",
                    TrigFunction.ASIN => "Inverse Sine",
                    TrigFunction.ACOS => "Inverse Cosine",
                    TrigFunction.ATAN => "Inverse Tangent",
                    _ => $"Trig: {trigNode.Function}"
                },

                Atan2Node => "Angle from Two Points",
                ExpLogNode expLog => expLog.Type switch
                {
                    ExpLogType.EXP => "Exponential (e^x)",
                    ExpLogType.LOG => "Natural Log",
                    _ => $"ExpLog: {expLog.Type}"
                },

                MinMaxNode minMax => minMax.Type == MinMaxType.MIN ? "Get Minimum Value" : "Get Maximum Value",

                // Logic operations
                CompareNode cmpNode => cmpNode.Operator switch
                {
                    ComparisonOperator.Equal => "Check if Equal",
                    ComparisonOperator.NotEqual => "Check if Not Equal",
                    ComparisonOperator.GreaterThan => "Check if Greater Than",
                    ComparisonOperator.LessThan => "Check if Less Than",
                    ComparisonOperator.GreaterThanOrEqual => "Check if Greater or Equal",
                    ComparisonOperator.LessThanOrEqual => "Check if Less or Equal",
                    _ => $"Compare: {cmpNode.Operator}"
                },

                AndNode => "Logical AND (both true)",
                OrNode => "Logical OR (either true)",
                NotNode => "Logical NOT (invert)",

                // Bitwise operations
                BitwiseNode bitwise => bitwise.Operation switch
                {
                    BitwiseOperation.And => "Bitwise AND",
                    BitwiseOperation.Or => "Bitwise OR",
                    BitwiseOperation.Xor => "Bitwise XOR",
                    _ => $"Bitwise: {bitwise.Operation}"
                },

                BitwiseNotNode => "Bitwise NOT (flip bits)",
                ShiftNode shift => shift.Direction == ShiftDirection.Left ? "Shift Bits Left" : "Shift Bits Right",

                // Arrays
                ArrayNode arrayNode => $"Create Array '{arrayNode.ArrayName}'",
                ArrayAccessNode accessNode => $"Get Value from Array '{accessNode.ArrayName}'",
                ArrayAssignNode assignNode => $"Set Value in Array '{assignNode.ArrayName}'",

                // Stack operations
                PushNode => "Push Value to Stack",
                PopNode => "Pop Value from Stack",
                PeekNode => "Peek at Stack Top",

                // Advanced
                HashNode => "Calculate Hash Value",
                IncrementNode incNode => incNode.Type == IncrementType.Increment ? "Increase by 1" : "Decrease by 1",
                CompoundAssignNode compNode => compNode.Operator switch
                {
                    CompoundOperator.AddAssign => "Add and Assign",
                    CompoundOperator.SubtractAssign => "Subtract and Assign",
                    CompoundOperator.MultiplyAssign => "Multiply and Assign",
                    CompoundOperator.DivideAssign => "Divide and Assign",
                    _ => $"Compound: {compNode.Operator}"
                },

                // Comments
                CommentNode => "Comment Note",

                _ => node.Label
            };
        }

        /// <summary>
        /// Get mixed technical/friendly label
        /// </summary>
        public static string GetMixedLabel(NodeBase node)
        {
            return node switch
            {
                VariableNode varNode => varNode.IsDeclaration
                    ? $"VAR {varNode.VariableName}"
                    : $"LET {varNode.VariableName}",

                ConstNode constNode => $"CONST {constNode.ConstName}",
                DefineNode defineNode => $"DEFINE {defineNode.DefineName}",

                ReadPropertyNode readNode => $"device.{readNode.PropertyName}",
                WritePropertyNode writeNode => $"device.{writeNode.PropertyName} = value",
                PinDeviceNode pinNode => $"d{pinNode.PinNumber}",
                NamedDeviceNode namedNode => namedNode.AliasName,

                AddNode => "A + B",
                SubtractNode => "A - B",
                MultiplyNode => "A * B",
                DivideNode => "A / B",
                ModuloNode => "A % B",
                PowerNode => "A ^ B",

                CompareNode cmpNode => cmpNode.Operator switch
                {
                    ComparisonOperator.Equal => "A = B",
                    ComparisonOperator.NotEqual => "A <> B",
                    ComparisonOperator.GreaterThan => "A > B",
                    ComparisonOperator.LessThan => "A < B",
                    ComparisonOperator.GreaterThanOrEqual => "A >= B",
                    ComparisonOperator.LessThanOrEqual => "A <= B",
                    _ => $"A {cmpNode.Operator} B"
                },

                ArrayNode arrayNode => $"DIM {arrayNode.ArrayName}[{arrayNode.Size}]",
                ArrayAccessNode accessNode => $"{accessNode.ArrayName}[index]",
                ArrayAssignNode assignNode => $"{assignNode.ArrayName}[index] = value",

                _ => GetFriendlyLabel(node)
            };
        }

        /// <summary>
        /// Get technical IC10-style label
        /// </summary>
        public static string GetTechnicalLabel(NodeBase node)
        {
            return node switch
            {
                VariableNode varNode => varNode.IsDeclaration
                    ? $"move r{GetRegisterHint(varNode)} {varNode.InitialValue}"
                    : $"move r{GetRegisterHint(varNode)} r0",

                ReadPropertyNode readNode => $"l r0 d0 {readNode.PropertyName}",
                WritePropertyNode writeNode => $"s d0 {writeNode.PropertyName} r0",
                PinDeviceNode pinNode => $"# Pin d{pinNode.PinNumber}",

                AddNode => "add r0 r1 r2",
                SubtractNode => "sub r0 r1 r2",
                MultiplyNode => "mul r0 r1 r2",
                DivideNode => "div r0 r1 r2",
                ModuloNode => "mod r0 r1 r2",

                CompareNode cmpNode => cmpNode.Operator switch
                {
                    ComparisonOperator.Equal => "seq r0 r1 r2",
                    ComparisonOperator.NotEqual => "sne r0 r1 r2",
                    ComparisonOperator.GreaterThan => "sgt r0 r1 r2",
                    ComparisonOperator.LessThan => "slt r0 r1 r2",
                    ComparisonOperator.GreaterThanOrEqual => "sge r0 r1 r2",
                    ComparisonOperator.LessThanOrEqual => "sle r0 r1 r2",
                    _ => $"s?? r0 r1 r2"
                },

                AndNode => "and r0 r1 r2",
                OrNode => "or r0 r1 r2",
                NotNode => "not r0 r1",

                MathFunctionNode mathFunc => $"{mathFunc.Function.ToString().ToLower()} r0 r1",

                _ => GetMixedLabel(node)
            };
        }

        /// <summary>
        /// Get a register hint for a variable (simplified)
        /// </summary>
        private static int GetRegisterHint(VariableNode node)
        {
            // Simple hash to give consistent register numbers
            return Math.Abs(node.VariableName.GetHashCode()) % 16;
        }
    }
}
