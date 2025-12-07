using System;
using System.Collections.Generic;
using System.Linq;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.CodeGen
{
    /// <summary>
    /// Builds BASIC expressions from connected data nodes
    /// Handles operator precedence, parentheses, and expression caching
    /// </summary>
    public class ExpressionBuilder
    {
        #region Properties

        private readonly List<NodeBase> _nodes;
        private readonly List<Wire> _wires;
        private readonly CodeGenerationContext _context;
        private readonly ExecutionOrderResolver _resolver;

        /// <summary>
        /// Cache of already-built expressions for pins
        /// </summary>
        private readonly Dictionary<Guid, string> _expressionCache = new();

        /// <summary>
        /// Operator precedence levels (higher = tighter binding)
        /// </summary>
        private static readonly Dictionary<string, int> OperatorPrecedence = new()
        {
            { "OR", 1 },
            { "AND", 2 },
            { "NOT", 3 },
            { "==", 4 }, { "!=", 4 }, { "<", 4 }, { "<=", 4 }, { ">", 4 }, { ">=", 4 },
            { "+", 5 }, { "-", 5 },
            { "*", 6 }, { "/", 6 }, { "%", 6 },
            { "^", 7 },
            { "UNARY-", 8 }, // Unary minus
        };

        #endregion

        #region Constructor

        public ExpressionBuilder(List<NodeBase> nodes, List<Wire> wires, CodeGenerationContext context, ExecutionOrderResolver resolver)
        {
            _nodes = nodes;
            _wires = wires;
            _context = context;
            _resolver = resolver;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Build an expression for a given input pin
        /// </summary>
        /// <param name="inputPin">The input pin that needs a value</param>
        /// <returns>BASIC expression string, or null if no connection</returns>
        public string? BuildExpression(NodePin inputPin)
        {
            // Check cache first
            if (_expressionCache.TryGetValue(inputPin.Id, out var cached))
            {
                return cached;
            }

            // Find the wire providing data to this pin
            var wire = _resolver.GetInputWire(inputPin);
            if (wire == null)
            {
                // No connection - pin is unconnected
                return null;
            }

            // Get source node and pin
            var sourceNode = _resolver.GetInputSourceNode(inputPin);
            var sourcePin = _resolver.GetInputSourcePin(inputPin);

            if (sourceNode == null || sourcePin == null)
            {
                _context.AddError(inputPin.ParentNode?.Id ?? Guid.Empty,
                    $"Invalid wire connection for pin {inputPin.Name}");
                return null;
            }

            // Build expression based on source node type
            string expression = BuildNodeExpression(sourceNode, sourcePin);

            // Cache the result
            _expressionCache[inputPin.Id] = expression;

            return expression;
        }

        /// <summary>
        /// Build an expression for a specific node and output pin
        /// </summary>
        private string BuildNodeExpression(NodeBase node, NodePin outputPin)
        {
            // Check if we already have an expression for this pin
            if (_context.PinExpressions.TryGetValue(outputPin.Id, out var pinExpr))
            {
                return pinExpr;
            }

            // Check if node has a variable name assigned
            if (_context.NodeVariableNames.TryGetValue(node.Id, out var varName))
            {
                return varName;
            }

            // Build expression based on node type
            return node.NodeType switch
            {
                "Variable" => BuildVariableExpression(node),
                "Constant" => BuildConstantExpression(node),
                "Const" => BuildConstExpression(node),
                "Define" => BuildDefineExpression(node),
                "Add" => BuildBinaryOpExpression(node, "+", 5),
                "Subtract" => BuildBinaryOpExpression(node, "-", 5),
                "Multiply" => BuildBinaryOpExpression(node, "*", 6),
                "Divide" => BuildBinaryOpExpression(node, "/", 6),
                "Modulo" => BuildBinaryOpExpression(node, "%", 6),
                "Power" => BuildBinaryOpExpression(node, "^", 7),
                "Negate" => BuildUnaryOpExpression(node, "-"),
                "Compare" => BuildCompareExpression(node),
                "And" => BuildBinaryOpExpression(node, "AND", 2),
                "Or" => BuildBinaryOpExpression(node, "OR", 1),
                "Not" => BuildUnaryOpExpression(node, "NOT"),
                "MathFunction" => BuildFunctionExpression(node),
                "ReadProperty" => BuildReadPropertyExpression(node),
                "ArrayAccess" => BuildArrayAccessExpression(node),
                _ => BuildGenericExpression(node)
            };
        }

        /// <summary>
        /// Build expression for a variable node
        /// </summary>
        private string BuildVariableExpression(NodeBase node)
        {
            var varNode = node as VariableNode;
            return varNode?.VariableName ?? "unknown";
        }

        /// <summary>
        /// Build expression for a constant value node
        /// </summary>
        private string BuildConstantExpression(NodeBase node)
        {
            if (node is ConstantNode constNode)
            {
                return constNode.Value.ToString();
            }
            return "0";
        }

        /// <summary>
        /// Build expression for a CONST node
        /// </summary>
        private string BuildConstExpression(NodeBase node)
        {
            if (node is ConstNode constNode)
            {
                return constNode.ConstName;
            }
            return "UNKNOWN_CONST";
        }

        /// <summary>
        /// Build expression for a DEFINE node
        /// </summary>
        private string BuildDefineExpression(NodeBase node)
        {
            if (node is DefineNode defineNode)
            {
                return defineNode.DefineName;
            }
            return "UNKNOWN_DEFINE";
        }

        /// <summary>
        /// Build expression for a binary operator (a OP b)
        /// </summary>
        private string BuildBinaryOpExpression(NodeBase node, string op, int precedence)
        {
            // Get input pins
            var inputA = node.InputPins.FirstOrDefault(p => p.Name == "A");
            var inputB = node.InputPins.FirstOrDefault(p => p.Name == "B");

            if (inputA == null || inputB == null)
            {
                _context.AddError(node.Id, $"Binary operator missing input pins");
                return "0";
            }

            // Build expressions for inputs
            string? exprA = BuildExpression(inputA);
            string? exprB = BuildExpression(inputB);

            if (exprA == null)
            {
                _context.AddError(node.Id, $"Input A not connected");
                exprA = "0";
            }

            if (exprB == null)
            {
                _context.AddError(node.Id, $"Input B not connected");
                exprB = "0";
            }

            // Add parentheses if needed based on precedence
            // For now, always add parentheses for safety
            return $"({exprA} {op} {exprB})";
        }

        /// <summary>
        /// Build expression for a unary operator (OP a)
        /// </summary>
        private string BuildUnaryOpExpression(NodeBase node, string op)
        {
            // Get input pin (could be "Value", "A", or first data pin)
            var inputPin = node.InputPins.FirstOrDefault(p => p.DataType != DataType.Execution);

            if (inputPin == null)
            {
                _context.AddError(node.Id, $"Unary operator missing input pin");
                return "0";
            }

            // Build expression for input
            string? expr = BuildExpression(inputPin);

            if (expr == null)
            {
                _context.AddError(node.Id, $"Input not connected");
                expr = "0";
            }

            return $"{op}({expr})";
        }

        /// <summary>
        /// Build expression for a comparison node
        /// </summary>
        private string BuildCompareExpression(NodeBase node)
        {
            if (node is CompareNode compareNode)
            {
                var inputA = node.InputPins.FirstOrDefault(p => p.Name == "A");
                var inputB = node.InputPins.FirstOrDefault(p => p.Name == "B");

                if (inputA == null || inputB == null)
                {
                    _context.AddError(node.Id, "Compare node missing inputs");
                    return "0";
                }

                string? exprA = BuildExpression(inputA);
                string? exprB = BuildExpression(inputB);

                if (exprA == null) exprA = "0";
                if (exprB == null) exprB = "0";

                string op = compareNode.Operator switch
                {
                    ComparisonOperator.Equal => "=",
                    ComparisonOperator.NotEqual => "<>",
                    ComparisonOperator.LessThan => "<",
                    ComparisonOperator.LessThanOrEqual => "<=",
                    ComparisonOperator.GreaterThan => ">",
                    ComparisonOperator.GreaterThanOrEqual => ">=",
                    _ => "="
                };

                return $"({exprA} {op} {exprB})";
            }

            return "0";
        }

        /// <summary>
        /// Build expression for a function call
        /// </summary>
        private string BuildFunctionExpression(NodeBase node)
        {
            if (node is MathFunctionNode funcNode)
            {
                // RND doesn't take a parameter
                if (funcNode.Function == MathFunctionType.RND)
                {
                    return "RND()";
                }

                var inputPin = node.InputPins.FirstOrDefault(p => p.DataType == DataType.Number);
                if (inputPin == null)
                {
                    _context.AddError(node.Id, "Function node missing input");
                    return "0";
                }

                string? expr = BuildExpression(inputPin);
                if (expr == null) expr = "0";

                string funcName = funcNode.Function.ToString().ToUpper();
                return $"{funcName}({expr})";
            }

            return "0";
        }

        /// <summary>
        /// Build expression for reading a device property
        /// </summary>
        private string BuildReadPropertyExpression(NodeBase node)
        {
            if (node is ReadPropertyNode readNode)
            {
                // Get device input
                var devicePin = node.InputPins.FirstOrDefault(p => p.DataType == DataType.Device);
                if (devicePin != null)
                {
                    string? deviceExpr = BuildExpression(devicePin);
                    if (deviceExpr != null)
                    {
                        return $"{deviceExpr}.{readNode.PropertyName}";
                    }
                }

                // Fallback to device name if available
                return $"device.{readNode.PropertyName}";
            }

            return "0";
        }

        /// <summary>
        /// Build expression for array access
        /// </summary>
        private string BuildArrayAccessExpression(NodeBase node)
        {
            if (node is ArrayAccessNode arrayNode)
            {
                var indexPin = node.InputPins.FirstOrDefault(p => p.Name == "Index");
                if (indexPin == null)
                {
                    _context.AddError(node.Id, "Array access missing index");
                    return "0";
                }

                string? indexExpr = BuildExpression(indexPin);
                if (indexExpr == null) indexExpr = "0";

                return $"{arrayNode.ArrayName}[{indexExpr}]";
            }

            return "0";
        }

        /// <summary>
        /// Generic expression builder for unknown node types
        /// </summary>
        private string BuildGenericExpression(NodeBase node)
        {
            // Try to use the node's GenerateCode method
            try
            {
                return node.GenerateCode();
            }
            catch
            {
                _context.AddError(node.Id, $"Cannot generate expression for node type {node.NodeType}");
                return "0";
            }
        }

        /// <summary>
        /// Determine if an expression is simple enough to inline
        /// Simple expressions: variable names, constants, simple binary ops
        /// Complex expressions: nested operations, function calls
        /// </summary>
        public bool IsSimpleExpression(string expression)
        {
            // Count operators and parentheses
            int operatorCount = 0;
            int parenDepth = 0;
            int maxParenDepth = 0;

            foreach (char c in expression)
            {
                if (c == '(') parenDepth++;
                if (c == ')') parenDepth--;
                maxParenDepth = Math.Max(maxParenDepth, parenDepth);

                if ("+-*/%^".Contains(c))
                    operatorCount++;
            }

            // Simple if: no operators, or one operator at depth 1
            return operatorCount <= 1 && maxParenDepth <= 1;
        }

        #endregion
    }
}
