using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Nodes.Subroutines;
using BasicToMips.UI.VisualScripting.Wires;

namespace BasicToMips.UI.VisualScripting.CodeGen
{
    /// <summary>
    /// Main code generator - converts a visual graph to BASIC source code
    /// </summary>
    public class GraphToBasicGenerator
    {
        #region Properties

        private readonly List<NodeBase> _nodes;
        private readonly List<Wire> _wires;
        private CodeGenerationContext _context;
        private ExecutionOrderResolver _resolver;
        private ExpressionBuilder _expressionBuilder;

        #endregion

        #region Constructor

        public GraphToBasicGenerator(List<NodeBase> nodes, List<Wire> wires)
        {
            _nodes = nodes;
            _wires = wires;
            _context = new CodeGenerationContext();
            _resolver = new ExecutionOrderResolver(nodes, wires, _context);
            _expressionBuilder = new ExpressionBuilder(nodes, wires, _context, _resolver);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generate BASIC code from the visual graph
        /// </summary>
        /// <returns>Generated BASIC code as a string</returns>
        public string Generate()
        {
            _context = new CodeGenerationContext();
            _resolver = new ExecutionOrderResolver(_nodes, _wires, _context);
            _expressionBuilder = new ExpressionBuilder(_nodes, _wires, _context, _resolver);

            // Refresh subroutine registry with current nodes
            SubroutineRegistry.Instance.RefreshRegistry(_nodes);

            // Generate code in sections
            GenerateVariablesSection();
            GenerateConstantsSection();
            GenerateDevicesSection();
            GenerateArraysSection();
            GenerateMainCodeSection();
            GenerateSubroutinesSection();

            // Return the generated code
            return _context.GetCode();
        }

        /// <summary>
        /// Generate BASIC code with source mapping
        /// </summary>
        /// <returns>Tuple of (code, sourceMap)</returns>
        public (string code, SourceMap sourceMap) GenerateWithSourceMap()
        {
            string code = Generate();
            return (code, _context.SourceMap);
        }

        /// <summary>
        /// Get errors from the last generation
        /// </summary>
        public List<CodeGenerationError> GetErrors()
        {
            return _context.Errors;
        }

        /// <summary>
        /// Get warnings from the last generation
        /// </summary>
        public List<CodeGenerationWarning> GetWarnings()
        {
            return _context.Warnings;
        }

        /// <summary>
        /// Check if last generation was successful
        /// </summary>
        public bool IsSuccessful => _context.IsSuccessful;

        #endregion

        #region Generation Sections

        /// <summary>
        /// Generate the variables section (VAR statements)
        /// </summary>
        private void GenerateVariablesSection()
        {
            var variableNodes = _nodes.Where(n => n.NodeType == "Variable" && n is VariableNode vn && vn.IsDeclaration).ToList();

            if (variableNodes.Count == 0)
                return;

            _context.AddSectionHeader("Variables");

            foreach (var node in variableNodes)
            {
                if (node is VariableNode varNode)
                {
                    GenerateVariableDeclaration(varNode);
                }
            }
        }

        /// <summary>
        /// Generate a variable declaration
        /// </summary>
        private void GenerateVariableDeclaration(VariableNode node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            // Validate variable name
            if (!node.Validate(out string error))
            {
                _context.AddError(node.Id, error);
                return;
            }

            // Check for duplicate variable names
            if (_context.DeclaredVariables.Contains(node.VariableName))
            {
                _context.AddWarning(node.Id, $"Variable '{node.VariableName}' is already declared");
                return;
            }

            // Generate VAR statement
            if (!string.IsNullOrWhiteSpace(node.InitialValue))
            {
                _context.AddLine(node.Id, $"VAR {node.VariableName} = {node.InitialValue}");
            }
            else
            {
                _context.AddLine(node.Id, $"VAR {node.VariableName}");
            }

            // Track variable
            _context.DeclaredVariables.Add(node.VariableName);
            _context.NodeVariableNames[node.Id] = node.VariableName;
            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate the constants section (CONST and DEFINE statements)
        /// </summary>
        private void GenerateConstantsSection()
        {
            var constNodes = _nodes.Where(n => n.NodeType == "Const" || n.NodeType == "Define").ToList();

            if (constNodes.Count == 0)
                return;

            _context.AddSectionHeader("Constants");

            foreach (var node in constNodes)
            {
                if (node.NodeType == "Const" && node is ConstNode constNode)
                {
                    GenerateConstDeclaration(constNode);
                }
                else if (node.NodeType == "Define" && node is DefineNode defineNode)
                {
                    GenerateDefineDeclaration(defineNode);
                }
            }
        }

        /// <summary>
        /// Generate a CONST declaration
        /// </summary>
        private void GenerateConstDeclaration(ConstNode node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            if (!node.Validate(out string error))
            {
                _context.AddError(node.Id, error);
                return;
            }

            _context.AddLine(node.Id, $"CONST {node.ConstName} = {node.Value}");
            _context.NodeVariableNames[node.Id] = node.ConstName;
            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate a DEFINE declaration
        /// </summary>
        private void GenerateDefineDeclaration(DefineNode node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            if (!node.Validate(out string error))
            {
                _context.AddError(node.Id, error);
                return;
            }

            _context.AddLine(node.Id, $"DEFINE {node.DefineName} {node.Value}");
            _context.NodeVariableNames[node.Id] = node.DefineName;
            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate the devices section (ALIAS and DEVICE statements)
        /// </summary>
        private void GenerateDevicesSection()
        {
            var deviceNodes = _nodes.Where(n =>
                n.NodeType == "PinDevice" ||
                n.NodeType == "NamedDevice" ||
                n.NodeType == "ThisDevice").ToList();

            if (deviceNodes.Count == 0)
                return;

            _context.AddSectionHeader("Devices");

            foreach (var node in deviceNodes)
            {
                GenerateDeviceDeclaration(node);
            }
        }

        /// <summary>
        /// Generate a device declaration
        /// </summary>
        private void GenerateDeviceDeclaration(NodeBase node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            if (node is PinDeviceNode pinNode)
            {
                _context.AddLine(node.Id, $"ALIAS {pinNode.AliasName} d{pinNode.PinNumber}");
                _context.NodeVariableNames[node.Id] = pinNode.AliasName;
            }
            else if (node is NamedDeviceNode namedNode)
            {
                _context.AddLine(node.Id, $"ALIAS {namedNode.AliasName} = IC.Device[{namedNode.PrefabName}].Name[\"{namedNode.DeviceName}\"]");
                _context.NodeVariableNames[node.Id] = namedNode.AliasName;
            }
            else if (node is ThisDeviceNode thisNode)
            {
                _context.AddLine(node.Id, $"ALIAS {thisNode.AliasName} db");
                _context.NodeVariableNames[node.Id] = thisNode.AliasName;
            }

            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate the arrays section (DIM statements)
        /// </summary>
        private void GenerateArraysSection()
        {
            var arrayNodes = _nodes.Where(n => n.NodeType == "Array").ToList();

            if (arrayNodes.Count == 0)
                return;

            _context.AddSectionHeader("Arrays");

            foreach (var node in arrayNodes)
            {
                if (node is ArrayNode arrayNode)
                {
                    GenerateArrayDeclaration(arrayNode);
                }
            }
        }

        /// <summary>
        /// Generate an array declaration
        /// </summary>
        private void GenerateArrayDeclaration(ArrayNode node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            if (!node.Validate(out string error))
            {
                _context.AddError(node.Id, error);
                return;
            }

            _context.AddLine(node.Id, $"DIM {node.ArrayName}({node.Size})");
            _context.NodeVariableNames[node.Id] = node.ArrayName;
            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate the main code section (execution flow)
        /// </summary>
        private void GenerateMainCodeSection()
        {
            _context.AddSectionHeader("Main");

            // Add Main label for program entry point (allows GOTO Main for looping)
            _context.AddLine(Guid.Empty, "Main:");

            // Get execution chains - these are already in correct execution order
            var executionChains = _resolver.ResolveExecutionOrder();

            // Get all comment nodes (they don't have execution pins)
            var commentNodes = _nodes.Where(n => n.NodeType == "Comment").ToList();

            if (executionChains.Count == 0 && commentNodes.Count == 0)
            {
                _context.AddComment("No executable code");
                return;
            }

            // Generate comments at the top (sorted by Y position)
            foreach (var commentNode in commentNodes.OrderBy(n => n.Y))
            {
                GenerateNodeCode(commentNode);
            }

            // Generate code following execution chains (NOT Y-sorted!)
            // This ensures control flow (IF branches, loop bodies) are generated correctly
            foreach (var chain in executionChains)
            {
                foreach (var node in chain)
                {
                    GenerateNodeCode(node);
                }
            }
        }

        /// <summary>
        /// Generate code for an execution chain
        /// </summary>
        private void GenerateExecutionChain(List<NodeBase> chain)
        {
            foreach (var node in chain)
            {
                GenerateNodeCode(node);
            }
        }

        /// <summary>
        /// Generate code for a specific node
        /// </summary>
        private void GenerateNodeCode(NodeBase node)
        {
            // Skip if already processed (e.g., declarations)
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            // Generate code based on node type
            switch (node.NodeType)
            {
                case "Variable":
                    GenerateVariableAssignment(node as VariableNode);
                    break;

                case "ReadProperty":
                    GenerateReadProperty(node as ReadPropertyNode);
                    break;

                case "WriteProperty":
                    GenerateWriteProperty(node as WritePropertyNode);
                    break;

                case "ArrayAssign":
                    GenerateArrayAssignment(node as ArrayAssignNode);
                    break;

                case "CompoundAssign":
                    GenerateCompoundAssignment(node as CompoundAssignNode);
                    break;

                case "Increment":
                    GenerateIncrement(node as IncrementNode);
                    break;

                // Flow Control Nodes
                case "EntryPoint":
                    GenerateEntryPoint(node);
                    break;

                case "If":
                    GenerateIf(node);
                    break;

                case "While":
                    GenerateWhile(node);
                    break;

                case "For":
                    GenerateFor(node);
                    break;

                case "DoUntil":
                    GenerateDoUntil(node);
                    break;

                case "Break":
                    GenerateBreak(node);
                    break;

                case "Continue":
                    GenerateContinue(node);
                    break;

                case "Label":
                    GenerateLabel(node);
                    break;

                case "Goto":
                    GenerateGoto(node);
                    break;

                case "Gosub":
                    GenerateGosub(node);
                    break;

                case "Return":
                    GenerateReturn(node);
                    break;

                case "SelectCase":
                    GenerateSelectCase(node);
                    break;

                // Subroutine Nodes
                case "SubDefinition":
                    // Handled separately in GenerateSubroutinesSection
                    break;

                case "CallSub":
                    GenerateCallSub(node);
                    break;

                case "ExitSub":
                    GenerateExitSub(node);
                    break;

                case "FunctionDefinition":
                    // Handled separately in GenerateSubroutinesSection
                    break;

                case "CallFunction":
                    GenerateCallFunction(node);
                    break;

                case "ExitFunction":
                    GenerateExitFunction(node);
                    break;

                case "SetReturnValue":
                    GenerateSetReturnValue(node);
                    break;

                case "Yield":
                    GenerateYield(node);
                    break;

                case "Sleep":
                    GenerateSleep(node);
                    break;

                case "End":
                    GenerateEnd(node);
                    break;

                case "Comment":
                    GenerateComment(node);
                    break;

                default:
                    // For other nodes, try to generate generic code
                    GenerateGenericNode(node);
                    break;
            }

            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate a variable assignment (LET statement)
        /// </summary>
        private void GenerateVariableAssignment(VariableNode? node)
        {
            if (node == null || node.IsDeclaration)
                return;

            // Get value from connected input
            var valuePin = node.InputPins.FirstOrDefault(p => p.Name == "Value");
            if (valuePin == null)
            {
                _context.AddError(node.Id, "Assignment node missing value input");
                return;
            }

            string? valueExpr = _expressionBuilder.BuildExpression(valuePin);
            if (valueExpr == null)
            {
                _context.AddError(node.Id, "Value input not connected");
                return;
            }

            _context.AddLine(node.Id, $"LET {node.VariableName} = {valueExpr}");
        }

        /// <summary>
        /// Generate a read property operation
        /// </summary>
        private void GenerateReadProperty(ReadPropertyNode? node)
        {
            if (node == null)
                return;

            // ReadProperty is usually part of an expression, not a standalone statement
            // If it appears in execution flow, we might need to assign it to a temp variable
            var devicePin = node.InputPins.FirstOrDefault(p => p.DataType == DataType.Device);
            if (devicePin == null)
            {
                _context.AddError(node.Id, "ReadProperty missing device input");
                return;
            }

            // This would typically be handled by the expression builder
            // For now, just generate a comment
            _context.AddComment($"Reading {node.PropertyName}");
        }

        /// <summary>
        /// Generate a write property operation
        /// </summary>
        private void GenerateWriteProperty(WritePropertyNode? node)
        {
            if (node == null)
                return;

            var devicePin = node.InputPins.FirstOrDefault(p => p.DataType == DataType.Device);
            var valuePin = node.InputPins.FirstOrDefault(p => p.Name == "Value");

            if (devicePin == null || valuePin == null)
            {
                _context.AddError(node.Id, "WriteProperty missing inputs");
                return;
            }

            string? deviceExpr = _expressionBuilder.BuildExpression(devicePin);
            string? valueExpr = _expressionBuilder.BuildExpression(valuePin);

            if (deviceExpr == null) deviceExpr = "device";
            if (valueExpr == null) valueExpr = "0";

            _context.AddLine(node.Id, $"{deviceExpr}.{node.PropertyName} = {valueExpr}");
        }

        /// <summary>
        /// Generate an array assignment
        /// </summary>
        private void GenerateArrayAssignment(ArrayAssignNode? node)
        {
            if (node == null)
                return;

            var indexPin = node.InputPins.FirstOrDefault(p => p.Name == "Index");
            var valuePin = node.InputPins.FirstOrDefault(p => p.Name == "Value");

            if (indexPin == null || valuePin == null)
            {
                _context.AddError(node.Id, "Array assignment missing inputs");
                return;
            }

            string? indexExpr = _expressionBuilder.BuildExpression(indexPin);
            string? valueExpr = _expressionBuilder.BuildExpression(valuePin);

            if (indexExpr == null) indexExpr = "0";
            if (valueExpr == null) valueExpr = "0";

            _context.AddLine(node.Id, $"{node.ArrayName}[{indexExpr}] = {valueExpr}");
        }

        /// <summary>
        /// Generate a compound assignment (+=, -=, etc.)
        /// </summary>
        private void GenerateCompoundAssignment(CompoundAssignNode? node)
        {
            if (node == null)
                return;

            var valuePin = node.InputPins.FirstOrDefault(p => p.Name == "Value");
            if (valuePin == null)
            {
                _context.AddError(node.Id, "Compound assignment missing value input");
                return;
            }

            string? valueExpr = _expressionBuilder.BuildExpression(valuePin);
            if (valueExpr == null) valueExpr = "0";

            string op = node.Operator switch
            {
                CompoundOperator.AddAssign => "+=",
                CompoundOperator.SubtractAssign => "-=",
                CompoundOperator.MultiplyAssign => "*=",
                CompoundOperator.DivideAssign => "/=",
                _ => "+="
            };

            _context.AddLine(node.Id, $"{node.VariableName} {op} {valueExpr}");
        }

        /// <summary>
        /// Generate an increment/decrement operation
        /// </summary>
        private void GenerateIncrement(IncrementNode? node)
        {
            if (node == null)
                return;

            string op = node.Type == IncrementType.Increment ? "++" : "--";

            if (node.Position == IncrementPosition.Prefix)
            {
                _context.AddLine(node.Id, $"{op}{node.VariableName}");
            }
            else
            {
                _context.AddLine(node.Id, $"{node.VariableName}{op}");
            }
        }

        /// <summary>
        /// Generate code for a generic node using its GenerateCode method
        /// </summary>
        private void GenerateGenericNode(NodeBase node)
        {
            try
            {
                string code = node.GenerateCode();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    _context.AddLine(node.Id, code);
                }
            }
            catch (Exception ex)
            {
                _context.AddError(node.Id, $"Error generating code: {ex.Message}");
            }
        }

        #endregion

        #region Flow Control Node Generation

        /// <summary>
        /// Generate entry point comment
        /// </summary>
        private void GenerateEntryPoint(NodeBase node)
        {
            _context.AddLine(node.Id, "# --- Program Start ---");
        }

        /// <summary>
        /// Generate IF/THEN/ELSE/ENDIF block
        /// </summary>
        private void GenerateIf(NodeBase node)
        {
            var conditionPin = node.InputPins.FirstOrDefault(p => p.Name == "Condition");
            if (conditionPin == null)
            {
                _context.AddError(node.Id, "IF node missing condition input");
                return;
            }

            string? condition = _expressionBuilder.BuildExpression(conditionPin);
            if (condition == null)
            {
                _context.AddError(node.Id, "IF condition not connected");
                return;
            }

            // Generate IF statement
            _context.AddLine(node.Id, $"IF {condition} THEN");
            _context.Indent();

            // Generate True branch (only if connected)
            var truePin = node.OutputPins.FirstOrDefault(p => p.Name == "True");
            if (truePin != null && truePin.IsConnected)
            {
                var trueChain = GetExecutionChainFromPin(truePin);
                foreach (var chainNode in trueChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            // Generate ELSE branch if connected
            var falsePin = node.OutputPins.FirstOrDefault(p => p.Name == "False");
            if (falsePin != null && falsePin.IsConnected)
            {
                _context.Unindent();
                _context.AddLine(node.Id, "ELSE");
                _context.Indent();

                var falseChain = GetExecutionChainFromPin(falsePin);
                foreach (var chainNode in falseChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            _context.Unindent();
            _context.AddLine(node.Id, "ENDIF");

            // Continue with Done pin if connected
            var donePin = node.OutputPins.FirstOrDefault(p => p.Name == "Done");
            if (donePin != null && donePin.IsConnected)
            {
                var doneChain = GetExecutionChainFromPin(donePin);
                foreach (var chainNode in doneChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }
        }

        /// <summary>
        /// Generate WHILE/WEND loop
        /// </summary>
        private void GenerateWhile(NodeBase node)
        {
            var conditionPin = node.InputPins.FirstOrDefault(p => p.Name == "Condition");
            if (conditionPin == null)
            {
                _context.AddError(node.Id, "WHILE node missing condition input");
                return;
            }

            string? condition = _expressionBuilder.BuildExpression(conditionPin);
            if (condition == null)
            {
                _context.AddError(node.Id, "WHILE condition not connected");
                return;
            }

            // Get AutoYield property using reflection
            bool autoYield = true;
            var autoYieldProp = node.GetType().GetProperty("AutoYield");
            if (autoYieldProp != null)
            {
                autoYield = (bool)(autoYieldProp.GetValue(node) ?? true);
            }

            // Generate WHILE statement
            _context.AddLine(node.Id, $"WHILE {condition}");
            _context.Indent();

            // Generate loop body
            var loopBodyPin = node.OutputPins.FirstOrDefault(p => p.Name == "LoopBody");
            if (loopBodyPin != null)
            {
                var loopChain = GetExecutionChainFromPin(loopBodyPin);
                foreach (var chainNode in loopChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            // Add YIELD if auto-yield is enabled
            if (autoYield)
            {
                _context.AddLine(node.Id, "YIELD");
            }

            _context.Unindent();
            _context.AddLine(node.Id, "WEND");

            // Continue with Done pin if connected
            var donePin = node.OutputPins.FirstOrDefault(p => p.Name == "Done");
            if (donePin != null && donePin.IsConnected)
            {
                var doneChain = GetExecutionChainFromPin(donePin);
                foreach (var chainNode in doneChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }
        }

        /// <summary>
        /// Generate FOR/NEXT loop
        /// </summary>
        private void GenerateFor(NodeBase node)
        {
            var startPin = node.InputPins.FirstOrDefault(p => p.Name == "Start");
            var endPin = node.InputPins.FirstOrDefault(p => p.Name == "End");

            if (startPin == null || endPin == null)
            {
                _context.AddError(node.Id, "FOR node missing start/end inputs");
                return;
            }

            string? start = _expressionBuilder.BuildExpression(startPin);
            string? end = _expressionBuilder.BuildExpression(endPin);

            if (start == null || end == null)
            {
                _context.AddError(node.Id, "FOR start/end not connected");
                return;
            }

            // Get variable name and step using reflection
            string varName = "i";
            double step = 1.0;
            bool autoYield = true;

            var varNameProp = node.GetType().GetProperty("VariableName");
            if (varNameProp != null)
            {
                varName = (string)(varNameProp.GetValue(node) ?? "i");
            }

            var stepProp = node.GetType().GetProperty("Step");
            if (stepProp != null)
            {
                step = (double)(stepProp.GetValue(node) ?? 1.0);
            }

            var autoYieldProp = node.GetType().GetProperty("AutoYield");
            if (autoYieldProp != null)
            {
                autoYield = (bool)(autoYieldProp.GetValue(node) ?? true);
            }

            // Generate FOR statement
            _context.AddLine(node.Id, $"FOR {varName} = {start} TO {end} STEP {step}");
            _context.Indent();

            // Generate loop body
            var loopBodyPin = node.OutputPins.FirstOrDefault(p => p.Name == "LoopBody");
            if (loopBodyPin != null)
            {
                var loopChain = GetExecutionChainFromPin(loopBodyPin);
                foreach (var chainNode in loopChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            // Add YIELD if auto-yield is enabled
            if (autoYield)
            {
                _context.AddLine(node.Id, "YIELD");
            }

            _context.Unindent();
            _context.AddLine(node.Id, $"NEXT {varName}");

            // Continue with Done pin if connected
            var donePin = node.OutputPins.FirstOrDefault(p => p.Name == "Done");
            if (donePin != null && donePin.IsConnected)
            {
                var doneChain = GetExecutionChainFromPin(donePin);
                foreach (var chainNode in doneChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }
        }

        /// <summary>
        /// Generate DO/LOOP UNTIL
        /// </summary>
        private void GenerateDoUntil(NodeBase node)
        {
            var conditionPin = node.InputPins.FirstOrDefault(p => p.Name == "Condition");
            if (conditionPin == null)
            {
                _context.AddError(node.Id, "DO UNTIL node missing condition input");
                return;
            }

            string? condition = _expressionBuilder.BuildExpression(conditionPin);
            if (condition == null)
            {
                _context.AddError(node.Id, "DO UNTIL condition not connected");
                return;
            }

            // Get AutoYield property
            bool autoYield = true;
            var autoYieldProp = node.GetType().GetProperty("AutoYield");
            if (autoYieldProp != null)
            {
                autoYield = (bool)(autoYieldProp.GetValue(node) ?? true);
            }

            // Generate DO statement
            _context.AddLine(node.Id, "DO");
            _context.Indent();

            // Generate loop body
            var loopBodyPin = node.OutputPins.FirstOrDefault(p => p.Name == "LoopBody");
            if (loopBodyPin != null)
            {
                var loopChain = GetExecutionChainFromPin(loopBodyPin);
                foreach (var chainNode in loopChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            // Add YIELD if auto-yield is enabled
            if (autoYield)
            {
                _context.AddLine(node.Id, "YIELD");
            }

            _context.Unindent();
            _context.AddLine(node.Id, $"LOOP UNTIL {condition}");

            // Continue with Done pin if connected
            var donePin = node.OutputPins.FirstOrDefault(p => p.Name == "Done");
            if (donePin != null && donePin.IsConnected)
            {
                var doneChain = GetExecutionChainFromPin(donePin);
                foreach (var chainNode in doneChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }
        }

        /// <summary>
        /// Generate BREAK statement
        /// </summary>
        private void GenerateBreak(NodeBase node)
        {
            _context.AddLine(node.Id, "BREAK");
        }

        /// <summary>
        /// Generate CONTINUE statement
        /// </summary>
        private void GenerateContinue(NodeBase node)
        {
            _context.AddLine(node.Id, "CONTINUE");
        }

        /// <summary>
        /// Generate label
        /// </summary>
        private void GenerateLabel(NodeBase node)
        {
            string labelName = "MyLabel";
            var labelNameProp = node.GetType().GetProperty("LabelName");
            if (labelNameProp != null)
            {
                labelName = (string)(labelNameProp.GetValue(node) ?? "MyLabel");
            }

            _context.AddLine(node.Id, $"{labelName}:");
        }

        /// <summary>
        /// Generate GOTO statement
        /// </summary>
        private void GenerateGoto(NodeBase node)
        {
            string targetLabel = "MyLabel";
            var targetProp = node.GetType().GetProperty("TargetLabel");
            if (targetProp != null)
            {
                targetLabel = (string)(targetProp.GetValue(node) ?? "MyLabel");
            }

            _context.AddLine(node.Id, $"GOTO {targetLabel}");
        }

        /// <summary>
        /// Generate GOSUB statement
        /// </summary>
        private void GenerateGosub(NodeBase node)
        {
            string targetLabel = "MySubroutine";
            var targetProp = node.GetType().GetProperty("TargetLabel");
            if (targetProp != null)
            {
                targetLabel = (string)(targetProp.GetValue(node) ?? "MySubroutine");
            }

            _context.AddLine(node.Id, $"GOSUB {targetLabel}");
        }

        /// <summary>
        /// Generate RETURN statement
        /// </summary>
        private void GenerateReturn(NodeBase node)
        {
            _context.AddLine(node.Id, "RETURN");
        }

        /// <summary>
        /// Generate SELECT CASE/END SELECT
        /// </summary>
        private void GenerateSelectCase(NodeBase node)
        {
            var valuePin = node.InputPins.FirstOrDefault(p => p.Name == "Value");
            if (valuePin == null)
            {
                _context.AddError(node.Id, "SELECT CASE node missing value input");
                return;
            }

            string? value = _expressionBuilder.BuildExpression(valuePin);
            if (value == null)
            {
                _context.AddError(node.Id, "SELECT CASE value not connected");
                return;
            }

            // Get case values using reflection
            var caseValuesProp = node.GetType().GetProperty("CaseValues");
            List<int> caseValues = new List<int>();
            if (caseValuesProp != null)
            {
                var values = caseValuesProp.GetValue(node);
                if (values is List<int> list)
                {
                    caseValues = list;
                }
            }

            // Generate SELECT CASE statement
            _context.AddLine(node.Id, $"SELECT CASE {value}");
            _context.Indent();

            // Generate each case
            foreach (var caseValue in caseValues)
            {
                var casePin = node.OutputPins.FirstOrDefault(p => p.Name == $"Case {caseValue}");
                if (casePin != null && casePin.IsConnected)
                {
                    _context.AddLine(node.Id, $"CASE {caseValue}");
                    _context.Indent();

                    var caseChain = GetExecutionChainFromPin(casePin);
                    foreach (var chainNode in caseChain)
                    {
                        GenerateNodeCode(chainNode);
                    }

                    _context.Unindent();
                }
            }

            // Generate default case
            var defaultPin = node.OutputPins.FirstOrDefault(p => p.Name == "Default");
            if (defaultPin != null && defaultPin.IsConnected)
            {
                _context.AddLine(node.Id, "DEFAULT");
                _context.Indent();

                var defaultChain = GetExecutionChainFromPin(defaultPin);
                foreach (var chainNode in defaultChain)
                {
                    GenerateNodeCode(chainNode);
                }

                _context.Unindent();
            }

            _context.Unindent();
            _context.AddLine(node.Id, "END SELECT");

            // Continue with Done pin if connected
            var donePin = node.OutputPins.FirstOrDefault(p => p.Name == "Done");
            if (donePin != null && donePin.IsConnected)
            {
                var doneChain = GetExecutionChainFromPin(donePin);
                foreach (var chainNode in doneChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }
        }

        /// <summary>
        /// Generate YIELD statement
        /// </summary>
        private void GenerateYield(NodeBase node)
        {
            _context.AddLine(node.Id, "YIELD");
        }

        /// <summary>
        /// Generate SLEEP statement
        /// </summary>
        private void GenerateSleep(NodeBase node)
        {
            var durationPin = node.InputPins.FirstOrDefault(p => p.Name == "Duration");
            if (durationPin == null)
            {
                _context.AddError(node.Id, "SLEEP node missing duration input");
                return;
            }

            string? duration = _expressionBuilder.BuildExpression(durationPin);
            if (duration == null)
            {
                _context.AddError(node.Id, "SLEEP duration not connected");
                return;
            }

            _context.AddLine(node.Id, $"SLEEP {duration}");
        }

        /// <summary>
        /// Generate END statement
        /// </summary>
        private void GenerateEnd(NodeBase node)
        {
            _context.AddLine(node.Id, "END");
        }

        /// <summary>
        /// Generate comment from CommentNode
        /// </summary>
        private void GenerateComment(NodeBase node)
        {
            if (node is CommentNode commentNode)
            {
                string code = commentNode.GenerateCode();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    // Split by lines and add each line individually for source mapping
                    var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        _context.AddLine(node.Id, line);
                    }
                }
            }
        }

        /// <summary>
        /// Get the execution chain starting from a given output pin
        /// </summary>
        private List<NodeBase> GetExecutionChainFromPin(NodePin outputPin)
        {
            var chain = new List<NodeBase>();

            // Find wire connected to this output pin
            var wire = _wires.FirstOrDefault(w => w.SourcePinId == outputPin.Id && w.DataType == DataType.Execution);
            if (wire == null)
                return chain;

            // Find target node
            var targetNode = _nodes.FirstOrDefault(n => n.Id == wire.TargetNodeId);
            if (targetNode == null)
                return chain;

            // Build chain from target node
            var visited = new HashSet<Guid>();
            var currentNode = targetNode;

            while (currentNode != null && !visited.Contains(currentNode.Id))
            {
                visited.Add(currentNode.Id);
                chain.Add(currentNode);

                // Find next node in execution chain
                var execOutputPin = currentNode.OutputPins.FirstOrDefault(p => p.DataType == DataType.Execution && p.Name == "Exec");
                if (execOutputPin == null)
                    break;

                var nextWire = _wires.FirstOrDefault(w => w.SourcePinId == execOutputPin.Id && w.DataType == DataType.Execution);
                if (nextWire == null)
                    break;

                currentNode = _nodes.FirstOrDefault(n => n.Id == nextWire.TargetNodeId);
            }

            return chain;
        }

        #endregion

        #region Subroutine Generation

        /// <summary>
        /// Generate the subroutines section (SUB and FUNCTION definitions)
        /// </summary>
        private void GenerateSubroutinesSection()
        {
            var subDefinitions = _nodes.Where(n => n.NodeType == "SubDefinition").ToList();
            var funcDefinitions = _nodes.Where(n => n.NodeType == "FunctionDefinition").ToList();

            if (subDefinitions.Count == 0 && funcDefinitions.Count == 0)
                return;

            _context.AddSectionHeader("Subroutines");

            // Generate all SUB definitions
            foreach (var node in subDefinitions)
            {
                if (node is SubDefinitionNode subDef)
                {
                    GenerateSubDefinition(subDef);
                }
            }

            // Generate all FUNCTION definitions
            foreach (var node in funcDefinitions)
            {
                if (node is FunctionDefinitionNode funcDef)
                {
                    GenerateFunctionDefinition(funcDef);
                }
            }
        }

        /// <summary>
        /// Generate a SUB definition block
        /// </summary>
        private void GenerateSubDefinition(SubDefinitionNode node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            if (!node.Validate(out string error))
            {
                _context.AddError(node.Id, error);
                return;
            }

            // Generate SUB header
            _context.AddBlankLine();
            _context.AddLine(node.Id, $"SUB {node.SubroutineName}");
            _context.Indent();

            // Generate body from Body output pin
            var bodyPin = node.OutputPins.FirstOrDefault(p => p.Name == "Body");
            if (bodyPin != null && bodyPin.IsConnected)
            {
                var bodyChain = GetExecutionChainFromPin(bodyPin);
                foreach (var chainNode in bodyChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            // Generate END SUB
            _context.Unindent();
            _context.AddLine(node.Id, "END SUB");

            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate a FUNCTION definition block
        /// </summary>
        private void GenerateFunctionDefinition(FunctionDefinitionNode node)
        {
            if (_context.ProcessedNodes.Contains(node.Id))
                return;

            if (!node.Validate(out string error))
            {
                _context.AddError(node.Id, error);
                return;
            }

            // Generate FUNCTION header
            _context.AddBlankLine();
            _context.AddLine(node.Id, $"FUNCTION {node.FunctionName}");
            _context.Indent();

            // Track return value if set
            string? returnValue = null;
            var returnPin = node.OutputPins.FirstOrDefault(p => p.Name == "ReturnValue");
            if (returnPin != null && returnPin.IsConnected)
            {
                // The return value will be set by nodes in the function body
                returnValue = _expressionBuilder.BuildExpression(returnPin);
            }

            // Generate body from Body output pin
            var bodyPin = node.OutputPins.FirstOrDefault(p => p.Name == "Body");
            if (bodyPin != null && bodyPin.IsConnected)
            {
                var bodyChain = GetExecutionChainFromPin(bodyPin);
                foreach (var chainNode in bodyChain)
                {
                    GenerateNodeCode(chainNode);
                }
            }

            // Add default RETURN statement if not already present
            // (functions should always have a return value)
            if (returnValue != null)
            {
                _context.AddLine(node.Id, $"RETURN {returnValue}");
            }
            else
            {
                _context.AddLine(node.Id, "RETURN 0");
            }

            // Generate END FUNCTION
            _context.Unindent();
            _context.AddLine(node.Id, "END FUNCTION");

            _context.ProcessedNodes.Add(node.Id);
        }

        /// <summary>
        /// Generate CALL SUB statement
        /// </summary>
        private void GenerateCallSub(NodeBase node)
        {
            if (node is CallSubNode callNode)
            {
                if (!callNode.Validate(out string error))
                {
                    _context.AddError(node.Id, error);
                    return;
                }

                _context.AddLine(node.Id, $"CALL {callNode.TargetSubroutine}");
            }
        }

        /// <summary>
        /// Generate EXIT SUB statement
        /// </summary>
        private void GenerateExitSub(NodeBase node)
        {
            _context.AddLine(node.Id, "EXIT SUB");
        }

        /// <summary>
        /// Generate function call (can be used in expressions or as statement)
        /// </summary>
        private void GenerateCallFunction(NodeBase node)
        {
            if (node is CallFunctionNode callNode)
            {
                if (!callNode.Validate(out string error))
                {
                    _context.AddError(node.Id, error);
                    return;
                }

                // Check if result is used
                var resultPin = callNode.OutputPins.FirstOrDefault(p => p.Name == "Result");
                if (resultPin != null && resultPin.IsConnected)
                {
                    // Result is used - will be handled by expression builder
                    // Just track the function call expression
                    _context.PinExpressions[resultPin.Id] = $"{callNode.TargetFunction}()";
                }
                else
                {
                    // Result not used - call as statement
                    _context.AddLine(node.Id, $"{callNode.TargetFunction}()");
                }
            }
        }

        /// <summary>
        /// Generate EXIT FUNCTION with return value
        /// </summary>
        private void GenerateExitFunction(NodeBase node)
        {
            if (node is ExitFunctionNode exitNode)
            {
                var returnPin = exitNode.InputPins.FirstOrDefault(p => p.Name == "ReturnValue");
                string? returnValue = null;

                if (returnPin != null)
                {
                    returnValue = _expressionBuilder.BuildExpression(returnPin);
                }

                if (returnValue != null)
                {
                    _context.AddLine(node.Id, $"RETURN {returnValue}");
                }
                else
                {
                    _context.AddLine(node.Id, "RETURN 0");
                }

                _context.AddLine(node.Id, "EXIT FUNCTION");
            }
        }

        /// <summary>
        /// Generate SET RETURN VALUE statement (sets return but continues execution)
        /// </summary>
        private void GenerateSetReturnValue(NodeBase node)
        {
            if (node is SetReturnValueNode setNode)
            {
                var valuePin = setNode.InputPins.FirstOrDefault(p => p.Name == "Value");
                string? value = null;

                if (valuePin != null)
                {
                    value = _expressionBuilder.BuildExpression(valuePin);
                }

                if (value != null)
                {
                    // In BASIC-10, we can set a temp variable to track return value
                    // Or just generate the RETURN inline
                    _context.AddLine(node.Id, $"# Return value set to {value}");
                    _context.AddLine(node.Id, $"RETURN {value}");
                }
                else
                {
                    _context.AddWarning(node.Id, "Return value not connected, using 0");
                    _context.AddLine(node.Id, "RETURN 0");
                }
            }
        }

        #endregion
    }
}
