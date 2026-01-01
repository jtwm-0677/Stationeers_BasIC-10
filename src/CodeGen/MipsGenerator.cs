using System.Text;
using BasicToMips.AST;
using BasicToMips.Shared;

namespace BasicToMips.CodeGen;

/// <summary>
/// Result of code generation including output and source map.
/// </summary>
public class GenerationResult
{
    public string Code { get; set; } = "";
    public SourceMap SourceMap { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int LineCount { get; set; }
}

public class MipsGenerator
{
    private readonly StringBuilder _output = new();
    private readonly Dictionary<string, int> _variables = new();
    private readonly Dictionary<string, int[]> _arrays = new();
    private readonly Dictionary<int, string> _lineLabels = new();
    private readonly Dictionary<string, string> _labelMap = new();
    private readonly Dictionary<string, string> _aliases = new();
    private readonly Dictionary<string, DeviceReference> _deviceReferences = new();
    private readonly Dictionary<string, int> _dynamicAliases = new(); // Track dynamic alias arrays (name -> device hash)
    private readonly Dictionary<string, double> _defines = new();
    private readonly HashSet<string> _declaredVariables = new(); // Track explicitly declared variables
    private CompilerOptions _options = new();

    // Built-in constants for IC10 (slot types, colors, etc.)
    private static readonly Dictionary<string, int> BuiltInConstants = new(StringComparer.OrdinalIgnoreCase)
    {
        // Slot types
        ["Import"] = 0,
        ["Export"] = 1,
        ["Content"] = 2,
        ["Fuel"] = 3,
        ["Input"] = 0,  // Alias for Import
        ["Output"] = 1, // Alias for Export

        // Colors (LogicColor enum values)
        ["Blue"] = 0,
        ["Gray"] = 1,
        ["Grey"] = 1,
        ["Green"] = 2,
        ["Orange"] = 3,
        ["Red"] = 4,
        ["Yellow"] = 5,
        ["White"] = 6,
        ["Black"] = 7,
        ["Brown"] = 8,
        ["Khaki"] = 9,
        ["Pink"] = 10,
        ["Purple"] = 11,

        // Logic states
        ["Off"] = 0,
        // ["On"] = 1,  // Conflicts with ON keyword - handled via true/1

        // Common mode values
        ["None"] = 0,
    };

    // Store resolved hash values for inline use (no defines emitted)
    private readonly Dictionary<string, int> _deviceHashes = new();
    private readonly Dictionary<string, int> _deviceNameHashes = new();
    private readonly Stack<string> _loopEndLabels = new();
    private readonly Stack<string> _loopStartLabels = new();
    private readonly Stack<string> _gosubStack = new();

    // DATA/READ/RESTORE support
    private readonly List<double> _dataValues = new();

    private int _nextLabel = 0;
    private int _stackPointer = 0;
    private int _currentSourceLine = 0; // Track current BASIC source line for comments
    private int _currentIC10Line = 0; // Track current IC10 output line for source map
    private readonly SourceMap _sourceMap = new(); // Source map for debugging
    private const int MaxRegisters = 14; // r0-r13 available for variables
    private const int TempRegister = 14; // r14 for temp operations
    private const int TempRegister2 = 15; // r15 for second temp operations
    // Note: IC10 uses 'sp' and 'ra' as named registers, not numbered

    public string Generate(ProgramNode program) => Generate(program, new CompilerOptions());

    public string Generate(ProgramNode program, CompilerOptions options)
    {
        var result = GenerateWithSourceMap(program, options);
        return result.Code;
    }

    /// <summary>
    /// Generate IC10 code with source map for debugging.
    /// </summary>
    public GenerationResult GenerateWithSourceMap(ProgramNode program)
        => GenerateWithSourceMap(program, new CompilerOptions());

    /// <summary>
    /// Generate IC10 code with source map for debugging.
    /// </summary>
    public GenerationResult GenerateWithSourceMap(ProgramNode program, CompilerOptions options)
    {
        _options = options;
        _currentIC10Line = 0;

        // First pass: collect all line labels and named labels
        foreach (var (lineNum, index) in program.LineNumberMap)
        {
            _lineLabels[lineNum] = $"line_{lineNum}";
        }

        // Collect named labels and symbols for source map
        foreach (var stmt in program.Statements)
        {
            if (stmt is LabelStatement label)
            {
                _labelMap[label.Name] = label.Name;
                _sourceMap.AddSymbol(label.Name, stmt.Line, 0, SymbolKind.Label);
            }
            else if (stmt is SubDefinition sub)
            {
                _sourceMap.AddSymbol(sub.Name, stmt.Line, 0, SymbolKind.Subroutine);
            }
            else if (stmt is FunctionDefinition func)
            {
                _sourceMap.AddSymbol(func.Name, stmt.Line, 0, SymbolKind.Function);
            }
        }

        // First pass: process aliases, defines, and collect ALL variable declarations (including nested)
        foreach (var stmt in program.Statements)
        {
            CollectDeclarationsRecursive(stmt);
        }

        // Generate code for each statement
        for (int i = 0; i < program.Statements.Count; i++)
        {
            var stmt = program.Statements[i];
            GenerateStatement(stmt);
        }

        // Post-process: convert labels to numeric offsets
        var code = ConvertLabelsToOffsets(_output.ToString());

        // Add compiler signature comment
        var now = DateTime.Now;
        var signature = $"# Stationeers Basic-10 By Dog Tired Studios on {now:MM_dd_yyyy} at {now:HH:mm:ss}";

        // Count actual IC10 instructions (excluding comments and blank lines)
        var codeLines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var instructionCount = codeLines.Count(line =>
        {
            var trimmed = line.Trim();
            return !string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#");
        });

        var warnings = new List<string>();
        if (instructionCount > 128)
        {
            warnings.Add($"IC10 line limit exceeded: {instructionCount} instructions (max 128). Your script will not fit on an IC chip.");
        }
        else if (instructionCount > 115)
        {
            warnings.Add($"Approaching IC10 line limit: {instructionCount}/128 instructions.");
        }

        return new GenerationResult
        {
            Code = code + Environment.NewLine + signature,
            SourceMap = _sourceMap,
            Warnings = warnings,
            LineCount = instructionCount
        };
    }

    /// <summary>
    /// Converts compiler-generated labels to numeric line offsets for IC10 compatibility.
    /// Internal labels like "else_0:", "endif_1:" become line numbers.
    /// User-defined labels like "main:" are preserved.
    /// </summary>
    private string ConvertLabelsToOffsets(string code)
    {
        var lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        // Internal label prefixes that should be converted to numeric offsets
        var internalPrefixes = new[] { "else_", "endif_", "while_", "wend_", "for_", "next_",
                                        "do_", "loop_", "case_", "endselect_", "line_", "_end" };

        bool IsInternalLabel(string labelName)
        {
            return internalPrefixes.Any(p => labelName.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                   || labelName == "_end";
        }

        // First pass: find all labels and their target instruction numbers
        var labelToLine = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var outputLines = new List<string>();
        int instructionNumber = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.EndsWith(":") && !trimmed.Contains(" "))
            {
                var labelName = trimmed.TrimEnd(':');
                // Use outputLines.Count (actual line number) not instructionNumber
                // IC10 counts ALL lines including comments for jump targets
                labelToLine[labelName] = outputLines.Count;

                // Keep user-defined labels, remove internal ones
                if (!IsInternalLabel(labelName))
                {
                    outputLines.Add(line);
                    instructionNumber++; // User labels count as lines in IC10 output
                }
            }
            else
            {
                outputLines.Add(line);
                // Don't count comment lines - IC10 ignores them for jump targets
                if (!trimmed.StartsWith("#"))
                {
                    instructionNumber++;
                }
            }
        }

        // Second pass: replace internal label references with numeric offsets
        var result = new StringBuilder();
        foreach (var line in outputLines)
        {
            var modifiedLine = line;

            // Only replace internal labels with offsets
            foreach (var kvp in labelToLine)
            {
                var labelName = kvp.Key;
                var targetLine = kvp.Value;

                if (IsInternalLabel(labelName))
                {
                    // Replace label references with numeric offset
                    // Use word boundary matching to avoid partial replacements
                    if (modifiedLine.EndsWith($" {labelName}"))
                    {
                        modifiedLine = modifiedLine.Substring(0, modifiedLine.Length - labelName.Length) + targetLine.ToString();
                    }
                    else if (modifiedLine.Contains($" {labelName} "))
                    {
                        modifiedLine = modifiedLine.Replace($" {labelName} ", $" {targetLine} ");
                    }
                }
            }

            result.AppendLine(modifiedLine);
        }

        return result.ToString().TrimEnd();
    }

    private void GenerateStatement(StatementNode stmt)
    {
        // Track source line for optional line number comments
        if (stmt.Line > 0)
        {
            _currentSourceLine = stmt.Line;
        }

        switch (stmt)
        {
            case LetStatement let:
                GenerateLet(let);
                break;
            case VarStatement varStmt:
                GenerateVar(varStmt);
                break;
            case ConstStatement constStmt:
                GenerateConst(constStmt);
                break;
            case PrintStatement print:
                GeneratePrint(print);
                break;
            case InputStatement input:
                GenerateInput(input);
                break;
            case IfStatement ifStmt:
                GenerateIf(ifStmt);
                break;
            case ForStatement forStmt:
                GenerateFor(forStmt);
                break;
            case WhileStatement whileStmt:
                GenerateWhile(whileStmt);
                break;
            case DoLoopStatement doLoop:
                GenerateDoLoop(doLoop);
                break;
            case GotoStatement gotoStmt:
                GenerateGoto(gotoStmt);
                break;
            case GosubStatement gosubStmt:
                GenerateGosub(gosubStmt);
                break;
            case OnGotoStatement onGoto:
                GenerateOnGoto(onGoto);
                break;
            case OnGosubStatement onGosub:
                GenerateOnGosub(onGosub);
                break;
            case DataStatement dataStmt:
                GenerateData(dataStmt);
                break;
            case ReadStatement readStmt:
                GenerateRead(readStmt);
                break;
            case RestoreStatement:
                GenerateRestore();
                break;
            case LabelStatement labelStmt:
                EmitLabel(labelStmt.Name);
                break;
            case ReturnStatement ret:
                GenerateReturn(ret);
                break;
            case EndStatement:
                GenerateEnd();
                break;
            case BreakStatement:
                GenerateBreak();
                break;
            case ContinueStatement:
                GenerateContinue();
                break;
            case PushStatement push:
                GeneratePush(push);
                break;
            case PopStatement pop:
                GeneratePop(pop);
                break;
            case PeekStatement peek:
                GeneratePeek(peek);
                break;
            case SelectStatement select:
                GenerateSelect(select);
                break;
            case SleepStatement sleep:
                GenerateSleep(sleep);
                break;
            case YieldStatement:
                GenerateYield();
                break;
            case DeviceWriteStatement deviceWrite:
                GenerateDeviceWrite(deviceWrite);
                break;
            case DeviceSlotWriteStatement slotWrite:
                GenerateDeviceSlotWrite(slotWrite);
                break;
            case BatchWriteStatement batchWrite:
                GenerateBatchWrite(batchWrite);
                break;
            case ExternalMemoryWriteStatement memWrite:
                GenerateExternalMemoryWrite(memWrite);
                break;
            case AliasStatement aliasStmt:
                // Static aliases handled in first pass
                // Dynamic aliases (with index) need runtime code generation
                if (aliasStmt.AliasIndex != null)
                {
                    GenerateDynamicAlias(aliasStmt);
                }
                break;
            case DefineStatement:
                // Already handled in first pass
                break;
            case DimStatement dim:
                GenerateDim(dim);
                break;
            case SubDefinition sub:
                GenerateSubDefinition(sub);
                break;
            case FunctionDefinition func:
                GenerateFunctionDefinition(func);
                break;
            case CallStatement call:
                GenerateCall(call);
                break;
            case CommentStatement comment:
                GenerateComment(comment);
                break;
            case ExpressionStatement exprStmt:
                GenerateExpressionStatement(exprStmt);
                break;
        }
    }

    private void GenerateExpressionStatement(ExpressionStatement stmt)
    {
        // Evaluate the expression for its side effects (e.g., ++i, --i)
        // We don't need the result, but the code generation produces the side effect
        GenerateExpression(stmt.Expression);
    }

    private void GenerateComment(CommentStatement comment)
    {
        if (!_options.PreserveComments && !comment.IsMetaComment)
        {
            return; // Skip regular comments when not preserving
        }

        // For meta comments, always emit them (they may contain useful info)
        // For regular comments, emit as IC10 comments
        if (comment.IsMetaComment)
        {
            Emit($"# #{comment.Text}"); // Preserve the ##Meta: prefix
        }
        else
        {
            Emit($"# {comment.Text}");
        }
    }

    private void GenerateLet(LetStatement let)
    {
        var varReg = GetOrCreateVariable(let.VariableName);

        if (let.ArrayIndices != null && let.ArrayIndices.Count > 0)
        {
            var valueReg = GenerateExpression(let.Value);
            var indexReg = GenerateExpression(let.ArrayIndices[0]);
            EmitComment($"Array store: {let.VariableName}[...]");
            Emit($"add r{TempRegister} {varReg} {indexReg}");
            Emit($"poke {valueReg} r{TempRegister}");
            FreeIfTemp(indexReg);
            FreeIfTemp(valueReg);
        }
        else
        {
            // Check if the value is a device read - can we read directly into the variable register?
            if (let.Value is DeviceReadExpression deviceRead)
            {
                GenerateDeviceReadIntoRegister(deviceRead, varReg);
            }
            else if (let.Value is BatchReadExpression batchRead)
            {
                GenerateBatchReadIntoRegister(batchRead, varReg);
            }
            else
            {
                var valueReg = GenerateExpression(let.Value);
                // Skip move if already in the right register
                if (valueReg != varReg)
                {
                    Emit($"move {varReg} {valueReg}");
                }
                FreeIfTemp(valueReg);
            }
        }
    }

    private void GenerateVar(VarStatement varStmt)
    {
        var varReg = GetOrCreateVariable(varStmt.VariableName);
        if (varStmt.InitialValue != null)
        {
            // Check if the value is a device read - can we read directly into the variable register?
            if (varStmt.InitialValue is DeviceReadExpression deviceRead)
            {
                GenerateDeviceReadIntoRegister(deviceRead, varReg);
            }
            else if (varStmt.InitialValue is BatchReadExpression batchRead)
            {
                GenerateBatchReadIntoRegister(batchRead, varReg);
            }
            else
            {
                var valueReg = GenerateExpression(varStmt.InitialValue);
                // Skip move if already in the right register
                if (valueReg != varReg)
                {
                    Emit($"move {varReg} {valueReg}");
                }
                FreeIfTemp(valueReg);
            }
        }
        else
        {
            Emit($"move {varReg} 0");
        }
    }

    private void GenerateConst(ConstStatement constStmt)
    {
        // Already processed in first pass - don't emit
    }

    private void GeneratePrint(PrintStatement print)
    {
        foreach (var expr in print.Expressions)
        {
            var reg = GenerateExpression(expr);
            EmitComment($"PRINT value in {reg}");
            // Write to db Setting for display
            Emit($"s db Setting {reg}");
            FreeRegister(reg);
        }
    }

    private void GenerateInput(InputStatement input)
    {
        var varReg = GetOrCreateVariable(input.VariableName);
        var device = input.DeviceName ?? "d0";
        var property = input.PropertyName ?? "Setting";
        EmitComment($"INPUT {input.VariableName}");
        Emit($"l {varReg} {device} {property}");
    }

    private void GenerateIf(IfStatement ifStmt)
    {
        var elseLabel = NewLabel("else");
        var endLabel = NewLabel("endif");
        var targetLabel = ifStmt.ElseBranch.Count > 0 ? elseLabel : endLabel;

        // Try to generate optimized conditional branch
        if (!TryGenerateOptimizedBranch(ifStmt.Condition, targetLabel, negate: true))
        {
            // Fallback: evaluate condition to register and branch on zero
            var condOp = GenerateExpression(ifStmt.Condition);
            Emit($"beqz {condOp} {targetLabel}");
            FreeIfTemp(condOp);
        }

        foreach (var stmt in ifStmt.ThenBranch)
        {
            GenerateStatement(stmt);
        }

        if (ifStmt.ElseBranch.Count > 0)
        {
            Emit($"j {endLabel}");
            EmitLabel(elseLabel);

            foreach (var stmt in ifStmt.ElseBranch)
            {
                GenerateStatement(stmt);
            }
        }

        EmitLabel(endLabel);
    }

    /// <summary>
    /// Try to generate an optimized branch instruction for a comparison expression.
    /// Returns true if successful, false if fallback evaluation is needed.
    /// When negate=true, branch to target if condition is FALSE (skip then-branch).
    /// </summary>
    private bool TryGenerateOptimizedBranch(ExpressionNode condition, string targetLabel, bool negate)
    {
        // Check for device.Set property access (e.g., IF device.Set THEN)
        if (condition is DeviceReadExpression devRead &&
            devRead.PropertyName.Equals("Set", StringComparison.OrdinalIgnoreCase))
        {
            var deviceSpec = _aliases.GetValueOrDefault(devRead.DeviceName, devRead.DeviceName);
            if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
                deviceSpec = "db";

            // device.Set is true if device is connected
            // negate=true means branch if condition is false (device NOT set)
            Emit(negate ? $"bdns {deviceSpec} {targetLabel}" : $"bdse {deviceSpec} {targetLabel}");
            return true;
        }

        // Check for device existence function calls: SDSE(device) or SDNS(device)
        if (condition is FunctionCallExpression funcCall)
        {
            var funcName = funcCall.FunctionName.ToUpperInvariant();
            if ((funcName == "SDSE" || funcName == "SDNS") && funcCall.Arguments.Count == 1)
            {
                // Get device specifier
                string deviceSpec;
                if (funcCall.Arguments[0] is VariableExpression varArg)
                {
                    deviceSpec = _aliases.GetValueOrDefault(varArg.Name, varArg.Name);
                    if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
                        deviceSpec = "db";
                }
                else
                {
                    return false; // Can't optimize non-variable device argument
                }

                // SDSE returns 1 if device is set, 0 if not
                // SDNS returns 1 if device is NOT set, 0 if set
                if (funcName == "SDSE")
                {
                    // IF SDSE(device) THEN -> branch if device IS set (skip if NOT set)
                    // negate=true means branch if condition is false (device NOT set)
                    Emit(negate ? $"bdns {deviceSpec} {targetLabel}" : $"bdse {deviceSpec} {targetLabel}");
                }
                else // SDNS
                {
                    // IF SDNS(device) THEN -> branch if device is NOT set (skip if IS set)
                    // negate=true means branch if condition is false (device IS set)
                    Emit(negate ? $"bdse {deviceSpec} {targetLabel}" : $"bdns {deviceSpec} {targetLabel}");
                }
                return true;
            }
        }

        if (condition is BinaryExpression bin)
        {
            // Handle OR with negate=true (IF with OR condition):
            // NOT (A OR B) = NOT A AND NOT B
            // Branch to target only if BOTH conditions are false
            // So: if A is true, skip to then-branch (don't go to target)
            //     if B is true, skip to then-branch (don't go to target)
            //     if both are false, go to target (skip then-branch)
            if (bin.Operator == BinaryOperator.Or && negate)
            {
                // Create a label for the then-branch (if either condition is true, execute then-branch)
                var thenLabel = NewLabel("or_then");

                // If left condition is true, jump to then-branch
                TryGenerateOptimizedBranch(bin.Left, thenLabel, negate: false);
                // If right condition is true, jump to then-branch
                TryGenerateOptimizedBranch(bin.Right, thenLabel, negate: false);
                // Both conditions are false - skip to target
                Emit($"j {targetLabel}");
                // Label for when either condition was true
                EmitLabel(thenLabel);
                return true;
            }

            // Handle OR without negation: branch if either is true
            if (bin.Operator == BinaryOperator.Or && !negate)
            {
                // For "a || b" with negate=false: branch if a OR b is true
                TryGenerateOptimizedBranch(bin.Left, targetLabel, negate: false);
                TryGenerateOptimizedBranch(bin.Right, targetLabel, negate: false);
                return true;
            }

            // Handle AND with negate=true (IF with AND condition):
            // NOT (A AND B) = NOT A OR NOT B
            // Branch to target if EITHER condition is false
            if (bin.Operator == BinaryOperator.And && negate)
            {
                // If left is false, skip (branch to target)
                TryGenerateOptimizedBranch(bin.Left, targetLabel, negate: true);
                // If right is false, skip (branch to target)
                TryGenerateOptimizedBranch(bin.Right, targetLabel, negate: true);
                return true;
            }

            // Handle AND without negation: need both to be true
            if (bin.Operator == BinaryOperator.And && !negate)
            {
                // Both must be true to branch
                var bothTrueLabel = NewLabel("and_check");
                var endCheckLabel = NewLabel("and_end");

                // If left is false, skip to end (don't branch to target)
                TryGenerateOptimizedBranch(bin.Left, endCheckLabel, negate: true);
                // If right is true (left was true), branch to target
                TryGenerateOptimizedBranch(bin.Right, targetLabel, negate: false);
                EmitLabel(endCheckLabel);
                return true;
            }

            // Simple comparison - can use direct branch
            var leftOp = GenerateExpression(bin.Left);
            var rightOp = GenerateExpression(bin.Right);

            // Map operator to branch instruction (and its negation)
            var (branchOp, negatedOp) = bin.Operator switch
            {
                BinaryOperator.Equal => ("beq", "bne"),
                BinaryOperator.NotEqual => ("bne", "beq"),
                BinaryOperator.LessThan => ("blt", "bge"),
                BinaryOperator.GreaterThan => ("bgt", "ble"),
                BinaryOperator.LessEqual => ("ble", "bgt"),
                BinaryOperator.GreaterEqual => ("bge", "blt"),
                _ => (null, null)
            };

            if (branchOp != null)
            {
                var op = negate ? negatedOp : branchOp;
                Emit($"{op} {leftOp} {rightOp} {targetLabel}");
                FreeIfTemp(leftOp);
                FreeIfTemp(rightOp);
                return true;
            }

            // Handle approximate equal with bap/bna instructions
            if (bin.Operator == BinaryOperator.ApproxEqual)
            {
                // bap branches if approximately equal, bna branches if NOT approximately equal
                // Use default epsilon of 0.0001
                var op = negate ? "bna" : "bap";
                Emit($"{op} {leftOp} {rightOp} 0.0001 {targetLabel}");
                FreeIfTemp(leftOp);
                FreeIfTemp(rightOp);
                return true;
            }

            // Not a simple comparison - free operands and fall through
            FreeIfTemp(leftOp);
            FreeIfTemp(rightOp);
        }

        return false;
    }

    private void GenerateFor(ForStatement forStmt)
    {
        var loopVar = GetOrCreateVariable(forStmt.VariableName);
        var startReg = GenerateExpression(forStmt.StartValue);
        var endReg = AllocateRegister();
        var stepReg = AllocateRegister();

        var startLabel = NewLabel("for_start");
        var endLabel = NewLabel("for_end");
        var continueLabel = NewLabel("for_continue");

        _loopStartLabels.Push(continueLabel);
        _loopEndLabels.Push(endLabel);

        Emit($"move {loopVar} {startReg}");
        FreeRegister(startReg);

        var endValReg = GenerateExpression(forStmt.EndValue);
        Emit($"move {endReg} {endValReg}");
        FreeRegister(endValReg);

        if (forStmt.StepValue != null)
        {
            var stepValReg = GenerateExpression(forStmt.StepValue);
            Emit($"move {stepReg} {stepValReg}");
            FreeRegister(stepValReg);
        }
        else
        {
            Emit($"move {stepReg} 1");
        }

        EmitLabel(startLabel);

        // Use select for conditional comparison based on step sign
        Emit($"sgt r{TempRegister} {loopVar} {endReg}");
        Emit($"slt r{TempRegister2} {loopVar} {endReg}");
        Emit($"select r{TempRegister} {stepReg} r{TempRegister} r{TempRegister2}");
        Emit($"bgtz r{TempRegister} {endLabel}");

        foreach (var stmt in forStmt.Body)
        {
            GenerateStatement(stmt);
        }

        EmitLabel(continueLabel);
        Emit($"add {loopVar} {loopVar} {stepReg}");
        Emit($"j {startLabel}");

        EmitLabel(endLabel);

        _loopStartLabels.Pop();
        _loopEndLabels.Pop();
        FreeRegister(endReg);
        FreeRegister(stepReg);
    }

    private void GenerateWhile(WhileStatement whileStmt)
    {
        var startLabel = NewLabel("while_start");
        var endLabel = NewLabel("while_end");

        _loopStartLabels.Push(startLabel);
        _loopEndLabels.Push(endLabel);

        EmitLabel(startLabel);

        var condReg = GenerateExpression(whileStmt.Condition);
        Emit($"beqz {condReg} {endLabel}");
        FreeRegister(condReg);

        foreach (var stmt in whileStmt.Body)
        {
            GenerateStatement(stmt);
        }

        Emit($"j {startLabel}");
        EmitLabel(endLabel);

        _loopStartLabels.Pop();
        _loopEndLabels.Pop();
    }

    private void GenerateDoLoop(DoLoopStatement doLoop)
    {
        var startLabel = NewLabel("do_start");
        var endLabel = NewLabel("do_end");

        _loopStartLabels.Push(startLabel);
        _loopEndLabels.Push(endLabel);

        EmitLabel(startLabel);

        if (doLoop.ConditionAtStart)
        {
            if (doLoop.WhileCondition != null)
            {
                var condReg = GenerateExpression(doLoop.WhileCondition);
                Emit($"beqz {condReg} {endLabel}");
                FreeRegister(condReg);
            }
            else if (doLoop.UntilCondition != null)
            {
                var condReg = GenerateExpression(doLoop.UntilCondition);
                Emit($"bnez {condReg} {endLabel}");
                FreeRegister(condReg);
            }
        }

        foreach (var stmt in doLoop.Body)
        {
            GenerateStatement(stmt);
        }

        if (!doLoop.ConditionAtStart)
        {
            if (doLoop.WhileCondition != null)
            {
                var condReg = GenerateExpression(doLoop.WhileCondition);
                Emit($"bnez {condReg} {startLabel}");
                FreeRegister(condReg);
            }
            else if (doLoop.UntilCondition != null)
            {
                var condReg = GenerateExpression(doLoop.UntilCondition);
                Emit($"beqz {condReg} {startLabel}");
                FreeRegister(condReg);
            }
            else
            {
                Emit($"j {startLabel}");
            }
        }
        else
        {
            Emit($"j {startLabel}");
        }

        EmitLabel(endLabel);

        _loopStartLabels.Pop();
        _loopEndLabels.Pop();
    }

    private void GenerateGoto(GotoStatement gotoStmt)
    {
        if (gotoStmt.TargetLabel != null)
        {
            Emit($"j {gotoStmt.TargetLabel}");
        }
        else if (_lineLabels.TryGetValue(gotoStmt.TargetLine, out var label))
        {
            Emit($"j {label}");
        }
        else
        {
            EmitComment($"WARNING: Unknown line {gotoStmt.TargetLine}");
            Emit($"j line_{gotoStmt.TargetLine}");
        }
    }

    private void GenerateGosub(GosubStatement gosubStmt)
    {
        if (gosubStmt.TargetLabel != null)
        {
            Emit($"jal {gosubStmt.TargetLabel}");
        }
        else if (_lineLabels.TryGetValue(gosubStmt.TargetLine, out var label))
        {
            Emit($"jal {label}");
        }
        else
        {
            Emit($"jal line_{gosubStmt.TargetLine}");
        }
    }

    private void GenerateOnGoto(OnGotoStatement onGoto)
    {
        // ON index GOTO label1, label2, label3
        // Implementation: index 0 -> first label, index 1 -> second label, etc.
        // For efficiency, use a series of branch instructions

        var indexReg = GenerateExpression(onGoto.IndexExpression);
        var endLabel = NewLabel("on_goto_end");

        // If index expression returned a literal, move it to a register
        string indexRegToUse;
        if (!indexReg.StartsWith("r"))
        {
            indexRegToUse = GetOrCreateVariable("_on_idx");
            Emit($"move {indexRegToUse} {indexReg}");
        }
        else
        {
            indexRegToUse = indexReg;
        }

        // Check bounds - if index < 0 or index >= count, skip all
        Emit($"brltz {indexRegToUse} {endLabel}"); // index < 0 -> skip
        Emit($"brge {indexRegToUse} {onGoto.TargetLabels.Count} {endLabel}"); // index >= count -> skip

        // Generate branch for each label
        for (int i = 0; i < onGoto.TargetLabels.Count; i++)
        {
            var targetLabel = onGoto.TargetLabels[i];
            if (i == 0)
            {
                // First label: if index == 0
                Emit($"breqz {indexRegToUse} {targetLabel}");
            }
            else
            {
                // Other labels: if index == i
                Emit($"breq {indexRegToUse} {i} {targetLabel}");
            }
        }

        EmitLabel(endLabel);
    }

    private void GenerateOnGosub(OnGosubStatement onGosub)
    {
        // ON index GOSUB label1, label2, label3
        // Similar to ON GOTO but uses jal for subroutine calls

        var indexReg = GenerateExpression(onGosub.IndexExpression);
        var endLabel = NewLabel("on_gosub_end");
        var doneLabel = NewLabel("on_gosub_done");

        // If index expression returned a literal, move it to a register
        string indexRegToUse;
        if (!indexReg.StartsWith("r"))
        {
            indexRegToUse = GetOrCreateVariable("_on_idx");
            Emit($"move {indexRegToUse} {indexReg}");
        }
        else
        {
            indexRegToUse = indexReg;
        }

        // Check bounds - if index < 0 or index >= count, skip all
        Emit($"brltz {indexRegToUse} {endLabel}"); // index < 0 -> skip
        Emit($"brge {indexRegToUse} {onGosub.TargetLabels.Count} {endLabel}"); // index >= count -> skip

        // Generate branch and call for each label
        for (int i = 0; i < onGosub.TargetLabels.Count; i++)
        {
            var targetLabel = onGosub.TargetLabels[i];
            var nextCheck = NewLabel("on_gosub_next");

            if (i == 0)
            {
                // First label: if index == 0
                Emit($"brnez {indexRegToUse} {nextCheck}");
            }
            else
            {
                // Other labels: if index != i
                Emit($"brne {indexRegToUse} {i} {nextCheck}");
            }

            // Call the subroutine
            Emit($"jal {targetLabel}");
            Emit($"j {doneLabel}");

            EmitLabel(nextCheck);
        }

        EmitLabel(endLabel);
        EmitLabel(doneLabel);
    }

    private void GenerateData(DataStatement dataStmt)
    {
        // DATA statements collect values at compile time
        // They don't generate any code directly - values are used by READ
        foreach (var valueExpr in dataStmt.Values)
        {
            double value = 0;
            if (valueExpr is NumberLiteral num)
            {
                value = num.Value;
            }
            else if (valueExpr is BooleanLiteral boolean)
            {
                value = boolean.Value ? 1 : 0;
            }
            else if (valueExpr is StringLiteral str)
            {
                // Strings become hashes
                value = CalculateHash(str.Value);
            }
            else
            {
                // For non-literal expressions, evaluate at compile time if possible
                // For now, just use 0 as a placeholder
                value = 0;
            }
            _dataValues.Add(value);
        }
    }

    private void GenerateRead(ReadStatement readStmt)
    {
        // READ loads values from DATA into variables
        // Uses a pointer register to track position

        if (_dataValues.Count == 0)
        {
            // No DATA defined - this is an error but we'll handle gracefully
            Emit("# Warning: READ with no DATA");
            return;
        }

        var ptrReg = GetOrCreateVariable("_data_ptr");
        var endLabel = NewLabel("read_end");

        foreach (var varName in readStmt.VariableNames)
        {
            var varReg = GetOrCreateVariable(varName);

            // Generate a series of conditional loads based on pointer
            for (int i = 0; i < _dataValues.Count; i++)
            {
                var nextCheck = NewLabel("read_next");
                var value = FormatNumber(_dataValues[i]);

                Emit($"brne {ptrReg} {i} {nextCheck}");
                Emit($"move {varReg} {value}");

                if (i < _dataValues.Count - 1)
                {
                    Emit($"add {ptrReg} {ptrReg} 1");
                    Emit($"j {endLabel}");
                }
                else
                {
                    // At end of data - wrap around to start
                    Emit($"move {ptrReg} 0");
                    Emit($"j {endLabel}");
                }

                EmitLabel(nextCheck);
            }
        }

        EmitLabel(endLabel);
    }

    private void GenerateRestore()
    {
        // RESTORE resets the DATA pointer to the beginning
        var ptrReg = GetOrCreateVariable("_data_ptr");
        Emit($"move {ptrReg} 0");
    }

    private void GenerateReturn(ReturnStatement ret)
    {
        if (ret.ReturnValue != null)
        {
            var valReg = GenerateExpression(ret.ReturnValue);
            Emit($"push {valReg}");
            FreeRegister(valReg);
        }
        Emit("j ra");
    }

    private void GenerateEnd()
    {
        EmitLabel("_end");
        Emit("hcf");
    }

    private void GenerateBreak()
    {
        if (_loopEndLabels.Count > 0)
        {
            Emit($"j {_loopEndLabels.Peek()}");
        }
    }

    private void GenerateContinue()
    {
        if (_loopStartLabels.Count > 0)
        {
            Emit($"j {_loopStartLabels.Peek()}");
        }
    }

    private void GeneratePush(PushStatement push)
    {
        var valueReg = GenerateExpression(push.Value);
        EmitComment("PUSH onto stack");
        Emit($"push {valueReg}");
        FreeRegister(valueReg);
    }

    private void GeneratePop(PopStatement pop)
    {
        var varReg = GetOrCreateVariable(pop.VariableName);
        EmitComment($"POP into {pop.VariableName}");
        Emit($"pop {varReg}");
    }

    private void GeneratePeek(PeekStatement peek)
    {
        var varReg = GetOrCreateVariable(peek.VariableName);
        EmitComment($"PEEK into {peek.VariableName}");
        Emit($"peek {varReg}");
    }

    private void GenerateSelect(SelectStatement select)
    {
        EmitComment("SELECT CASE");
        var testReg = GenerateExpression(select.TestExpression);
        var endLabel = NewLabel("select_end");

        foreach (var caseClause in select.Cases)
        {
            var caseBodyLabel = NewLabel("case_body");
            var nextCaseLabel = NewLabel("case_next");

            // Check each value in this case
            foreach (var caseValue in caseClause.Values)
            {
                var valueReg = GenerateExpression(caseValue);
                Emit($"beq {testReg} {valueReg} {caseBodyLabel}");
                FreeRegister(valueReg);
            }

            // None matched, jump to next case
            Emit($"j {nextCaseLabel}");

            // Case body
            EmitLabel(caseBodyLabel);
            foreach (var bodyStmt in caseClause.Body)
            {
                GenerateStatement(bodyStmt);
            }
            Emit($"j {endLabel}");

            EmitLabel(nextCaseLabel);
        }

        // Default case
        if (select.DefaultBody.Count > 0)
        {
            EmitComment("DEFAULT");
            foreach (var bodyStmt in select.DefaultBody)
            {
                GenerateStatement(bodyStmt);
            }
        }

        EmitLabel(endLabel);
        FreeRegister(testReg);
    }

    private void GenerateSleep(SleepStatement sleep)
    {
        var durationReg = GenerateExpression(sleep.Duration);
        Emit($"sleep {durationReg}");
        FreeRegister(durationReg);
    }

    private void GenerateYield()
    {
        Emit("yield");
    }

    /// <summary>
    /// Process alias in first pass - store values without emitting code.
    /// </summary>
    private void ProcessAlias(AliasStatement alias)
    {
        if (alias.DeviceReference != null)
        {
            var devRef = alias.DeviceReference;
            _deviceReferences[alias.AliasName] = devRef;

            switch (devRef.Type)
            {
                case DeviceReferenceType.Pin:
                    _aliases[alias.AliasName] = $"d{devRef.PinIndex}";
                    // Track BASIC alias -> device pin mapping for watch panel
                    _sourceMap.AliasDevices[alias.AliasName] = $"d{devRef.PinIndex}";
                    break;

                case DeviceReferenceType.Device:
                    // Store hash for inline use
                    if (devRef.DeviceHash is NumberLiteral numHash)
                    {
                        _deviceHashes[alias.AliasName] = (int)numHash.Value;
                    }
                    else if (devRef.DeviceHash is StringLiteral strHash)
                    {
                        _deviceHashes[alias.AliasName] = BasicToMips.Data.DeviceDatabase.GetDeviceHash(strHash.Value);
                    }
                    else if (devRef.DeviceHash is VariableExpression varHash)
                    {
                        _deviceHashes[alias.AliasName] = BasicToMips.Data.DeviceDatabase.GetDeviceHash(varHash.Name);
                    }
                    break;

                case DeviceReferenceType.DeviceNamed:
                    // Store both device hash and name hash for inline use
                    if (devRef.DeviceHash is NumberLiteral numHash2)
                    {
                        _deviceHashes[alias.AliasName] = (int)numHash2.Value;
                    }
                    else if (devRef.DeviceHash is StringLiteral strHash2)
                    {
                        _deviceHashes[alias.AliasName] = BasicToMips.Data.DeviceDatabase.GetDeviceHash(strHash2.Value);
                    }
                    else if (devRef.DeviceHash is VariableExpression varHash2)
                    {
                        _deviceHashes[alias.AliasName] = BasicToMips.Data.DeviceDatabase.GetDeviceHash(varHash2.Name);
                    }
                    _deviceNameHashes[alias.AliasName] = CalculateHash(devRef.DeviceName!);
                    break;

                case DeviceReferenceType.ReferenceId:
                    // Store reference ID
                    _deviceHashes[alias.AliasName] = (int)(devRef.ReferenceId ?? 0);
                    break;

                case DeviceReferenceType.Channel:
                    // Channel references need special handling - store as-is for now
                    break;
            }
        }
        else
        {
            // Simple alias (d0, db, etc.) - still need to emit these
            _aliases[alias.AliasName] = alias.DeviceSpec;
        }
    }

    /// <summary>
    /// Process define in first pass - store value without emitting code.
    /// </summary>
    private void ProcessDefine(DefineStatement define)
    {
        if (define.Value is NumberLiteral num)
        {
            _defines[define.ConstantName] = num.Value;
        }
    }

    /// <summary>
    /// Process const in first pass - store value without emitting code.
    /// </summary>
    private void ProcessConst(ConstStatement constStmt)
    {
        var value = TryEvaluateConstant(constStmt.Value);
        if (value.HasValue)
        {
            _defines[constStmt.ConstantName] = value.Value;
        }
    }

    /// <summary>
    /// Try to evaluate an expression as a compile-time constant.
    /// Returns null if the expression cannot be evaluated at compile time.
    /// </summary>
    private double? TryEvaluateConstant(ExpressionNode expr)
    {
        switch (expr)
        {
            case NumberLiteral num:
                return num.Value;

            case UnaryExpression unary when unary.Operator == UnaryOperator.Negate:
                var operand = TryEvaluateConstant(unary.Operand);
                return operand.HasValue ? -operand.Value : null;

            case UnaryExpression unary when unary.Operator == UnaryOperator.Not:
                var notOperand = TryEvaluateConstant(unary.Operand);
                return notOperand.HasValue ? (notOperand.Value == 0 ? 1 : 0) : null;

            case UnaryExpression unary when unary.Operator == UnaryOperator.BitNot:
                var bitNotOperand = TryEvaluateConstant(unary.Operand);
                return bitNotOperand.HasValue ? ~(long)bitNotOperand.Value : null;

            case BinaryExpression bin:
                var left = TryEvaluateConstant(bin.Left);
                var right = TryEvaluateConstant(bin.Right);
                if (!left.HasValue || !right.HasValue) return null;
                return bin.Operator switch
                {
                    BinaryOperator.Add => left.Value + right.Value,
                    BinaryOperator.Subtract => left.Value - right.Value,
                    BinaryOperator.Multiply => left.Value * right.Value,
                    BinaryOperator.Divide when right.Value != 0 => left.Value / right.Value,
                    BinaryOperator.Modulo when right.Value != 0 => left.Value % right.Value,
                    BinaryOperator.Power => Math.Pow(left.Value, right.Value),
                    BinaryOperator.BitAnd => (long)left.Value & (long)right.Value,
                    BinaryOperator.BitOr => (long)left.Value | (long)right.Value,
                    BinaryOperator.BitXor => (long)left.Value ^ (long)right.Value,
                    BinaryOperator.ShiftLeft => (long)left.Value << (int)right.Value,
                    BinaryOperator.ShiftRight => (long)left.Value >> (int)right.Value,
                    _ => null
                };

            case VariableExpression varExpr when _defines.ContainsKey(varExpr.Name):
                // Allow referencing other constants
                return _defines[varExpr.Name];

            default:
                return null;
        }
    }

    /// <summary>
    /// Recursively collect all variable declarations, aliases, defines, and constants
    /// from a statement and all its nested children.
    /// </summary>
    private void CollectDeclarationsRecursive(StatementNode stmt)
    {
        switch (stmt)
        {
            case AliasStatement alias:
                // Only process static aliases in first pass (no array index)
                if (alias.AliasIndex == null)
                {
                    ProcessAlias(alias);
                }
                _sourceMap.AddSymbol(alias.AliasName, stmt.Line, 0, SymbolKind.Alias);
                // Also register the alias name as a declared variable for array-based aliases
                _declaredVariables.Add(alias.AliasName);
                break;

            case DefineStatement define:
                ProcessDefine(define);
                _sourceMap.AddSymbol(define.ConstantName, stmt.Line, 0, SymbolKind.Define);
                break;

            case ConstStatement constStmt:
                ProcessConst(constStmt);
                _sourceMap.AddSymbol(constStmt.ConstantName, stmt.Line, 0, SymbolKind.Constant);
                break;

            case VarStatement varStmt:
                _declaredVariables.Add(varStmt.VariableName);
                if (!_sourceMap.Symbols.ContainsKey(varStmt.VariableName))
                    _sourceMap.AddSymbol(varStmt.VariableName, stmt.Line, 0, SymbolKind.Variable);
                break;

            case LetStatement let:
                _declaredVariables.Add(let.VariableName);
                if (!_sourceMap.Symbols.ContainsKey(let.VariableName))
                    _sourceMap.AddSymbol(let.VariableName, stmt.Line, 0, SymbolKind.Variable);
                break;

            case ForStatement forStmt:
                _declaredVariables.Add(forStmt.VariableName);
                if (!_sourceMap.Symbols.ContainsKey(forStmt.VariableName))
                    _sourceMap.AddSymbol(forStmt.VariableName, stmt.Line, 0, SymbolKind.Variable);
                // Recursively process FOR body
                foreach (var bodyStmt in forStmt.Body)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case WhileStatement whileStmt:
                foreach (var bodyStmt in whileStmt.Body)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case DoLoopStatement doLoop:
                foreach (var bodyStmt in doLoop.Body)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case IfStatement ifStmt:
                foreach (var bodyStmt in ifStmt.ThenBranch)
                    CollectDeclarationsRecursive(bodyStmt);
                foreach (var bodyStmt in ifStmt.ElseBranch)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case SelectStatement selectStmt:
                foreach (var caseClause in selectStmt.Cases)
                    foreach (var bodyStmt in caseClause.Body)
                        CollectDeclarationsRecursive(bodyStmt);
                foreach (var bodyStmt in selectStmt.DefaultBody)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case SubDefinition sub:
                foreach (var param in sub.Parameters)
                    _declaredVariables.Add(param);
                foreach (var bodyStmt in sub.Body)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case FunctionDefinition func:
                foreach (var param in func.Parameters)
                    _declaredVariables.Add(param);
                foreach (var bodyStmt in func.Body)
                    CollectDeclarationsRecursive(bodyStmt);
                break;

            case InputStatement inputStmt:
                _declaredVariables.Add(inputStmt.VariableName);
                if (!_sourceMap.Symbols.ContainsKey(inputStmt.VariableName))
                    _sourceMap.AddSymbol(inputStmt.VariableName, stmt.Line, 0, SymbolKind.Variable);
                break;

            case DimStatement dim:
                _declaredVariables.Add(dim.VariableName);
                if (!_sourceMap.Symbols.ContainsKey(dim.VariableName))
                    _sourceMap.AddSymbol(dim.VariableName, stmt.Line, 0, SymbolKind.Variable);
                break;
        }
    }

    // GenerateAlias and GenerateDefine are no longer used - aliases/defines processed in first pass
    private void GenerateAlias(AliasStatement alias)
    {
        // Already processed in first pass - only emit pin aliases
        if (alias.DeviceReference?.Type == DeviceReferenceType.Pin)
        {
            Emit($"alias {alias.AliasName} d{alias.DeviceReference.PinIndex}");
        }
        else if (alias.DeviceReference == null && alias.DeviceSpec != null)
        {
            // Simple alias like "alias sensor d0"
            Emit($"alias {alias.AliasName} {alias.DeviceSpec}");
        }
        // Batch device aliases don't emit anything
    }

    private void GenerateDefine(DefineStatement define)
    {
        // Already processed in first pass - don't emit
    }

    /// <summary>
    /// Generate code for dynamic alias (ALIAS name[index] = IC.Device[...].Name[expr])
    /// This stores device reference info in an array at runtime.
    /// </summary>
    private void GenerateDynamicAlias(AliasStatement alias)
    {
        if (alias.DeviceReference == null || alias.AliasIndex == null)
            return;

        var devRef = alias.DeviceReference;

        // Get device type hash
        int deviceHash = 0;
        if (devRef.DeviceHash is NumberLiteral numHash)
        {
            deviceHash = (int)numHash.Value;
        }
        else if (devRef.DeviceHash is StringLiteral strHash)
        {
            deviceHash = BasicToMips.Data.DeviceDatabase.GetDeviceHash(strHash.Value);
        }
        else if (devRef.DeviceHash is VariableExpression varHash)
        {
            deviceHash = BasicToMips.Data.DeviceDatabase.GetDeviceHash(varHash.Name);
        }

        // Calculate name hash - handle string + number concatenation pattern
        if (devRef.DeviceNameExpression != null)
        {
            // Dynamic name expression like "LED Display 3(EDITOR)" + i
            var nameHashReg = GenerateDeviceNameHash(devRef.DeviceNameExpression);

            // Get the array base and calculate target address
            var arrayBaseReg = GetOrCreateVariable(alias.AliasName);
            var indexReg = GenerateExpression(alias.AliasIndex);

            // Calculate target stack address: arrayBase + index
            Emit($"add r{TempRegister} {arrayBaseReg} {indexReg}");
            // Store the name hash at that stack address using poke
            Emit($"poke {nameHashReg} r{TempRegister}");

            FreeIfTemp(indexReg);
            FreeIfTemp(nameHashReg);

            // Track this as a dynamic device alias for later property access
            if (!_dynamicAliases.ContainsKey(alias.AliasName))
            {
                _dynamicAliases[alias.AliasName] = deviceHash;
            }
        }
        else if (devRef.DeviceName != null)
        {
            // Static name - calculate hash at compile time
            var nameHash = CalculateHash(devRef.DeviceName);

            // Get the array base and calculate target address
            var arrayBaseReg = GetOrCreateVariable(alias.AliasName);
            var indexReg = GenerateExpression(alias.AliasIndex);

            // Calculate target stack address: arrayBase + index
            Emit($"add r{TempRegister} {arrayBaseReg} {indexReg}");
            // Store the name hash at that stack address using poke
            Emit($"poke {nameHash} r{TempRegister}");

            FreeIfTemp(indexReg);

            // Track this as a dynamic device alias
            if (!_dynamicAliases.ContainsKey(alias.AliasName))
            {
                _dynamicAliases[alias.AliasName] = deviceHash;
            }
        }
    }

    /// <summary>
    /// Generate code to calculate a device name hash from an expression.
    /// Handles patterns like "BaseString" + numericExpr which becomes HASH(BaseString) + numericExpr
    /// </summary>
    private string GenerateDeviceNameHash(ExpressionNode expr)
    {
        // Check for string + number concatenation pattern
        if (expr is BinaryExpression binExpr && binExpr.Operator == BinaryOperator.Add)
        {
            // Left operand should be a string literal
            if (binExpr.Left is StringLiteral strLit)
            {
                // Calculate base hash at compile time
                var baseHash = CalculateHash(strLit.Value);

                // Generate code for the right operand (the offset)
                var offsetReg = GenerateExpression(binExpr.Right);

                // Calculate final hash = baseHash + offset
                var resultReg = AllocateRegister();
                Emit($"add {resultReg} {baseHash} {offsetReg}");
                FreeIfTemp(offsetReg);

                return resultReg;
            }
        }

        // Fall back to evaluating as a regular expression
        return GenerateExpression(expr);
    }

    private void GenerateDim(DimStatement dim)
    {
        // Check if this is a simple variable with initializer (no dimensions)
        if (dim.Dimensions.Count == 0 && dim.InitialValue != null)
        {
            // Simple initialized variable: DIM x = expression
            var valueReg = GenerateExpression(dim.InitialValue);
            var varReg = GetOrCreateVariable(dim.VariableName);
            Emit($"move {varReg} {valueReg}");
            if (valueReg != varReg)
            {
                FreeRegister(valueReg);
            }
            EmitComment($"DIM {dim.VariableName} = initialized");
            return;
        }

        // Array allocation
        int size = 1;
        foreach (var d in dim.Dimensions)
        {
            if (d is NumberLiteral num)
            {
                size *= (int)(num.Value + 1);
            }
        }

        var baseReg = GetOrCreateVariable(dim.VariableName);
        Emit($"move {baseReg} {_stackPointer}");
        _stackPointer += size;

        EmitComment($"DIM {dim.VariableName} - allocated {size} slots");

        // If array has an initializer, it applies to first element only
        if (dim.InitialValue != null)
        {
            var valueReg = GenerateExpression(dim.InitialValue);
            Emit($"poke {baseReg} {valueReg}");
            FreeRegister(valueReg);
        }
    }

    private void GenerateDeviceWrite(DeviceWriteStatement write)
    {
        var valueReg = GenerateExpression(write.Value);

        // Check for advanced device reference (batch/named) - use inline hash values
        if (_deviceReferences.TryGetValue(write.DeviceName, out var devRef))
        {
            switch (devRef.Type)
            {
                case DeviceReferenceType.Device:
                    // Batch write: sb HASH Property value - use inline hash
                    var deviceHash = _deviceHashes[write.DeviceName];
                    Emit($"sb {deviceHash} {write.PropertyName} {valueReg}");
                    FreeRegister(valueReg);
                    return;

                case DeviceReferenceType.DeviceNamed:
                    // Named device write: sbn HASH NAMEHASH Property value - use inline hashes
                    var devHash = _deviceHashes[write.DeviceName];
                    var nameHash = _deviceNameHashes[write.DeviceName];
                    Emit($"sbn {devHash} {nameHash} {write.PropertyName} {valueReg}");
                    FreeRegister(valueReg);
                    return;

                case DeviceReferenceType.ReferenceId:
                    // Reference ID write - use inline value
                    var refId = _deviceHashes[write.DeviceName];
                    Emit($"sbn {refId} 0 {write.PropertyName} {valueReg}");
                    FreeRegister(valueReg);
                    return;

                case DeviceReferenceType.Pin:
                    // Pin reference - use standard device write
                    break;

                case DeviceReferenceType.Channel:
                    // Channel write - not typically used for property writes
                    break;
            }
        }

        // Standard device write
        var deviceSpec = _aliases.GetValueOrDefault(write.DeviceName, write.DeviceName);

        // IC is a special keyword that refers to the IC chip housing (db in IC10)
        if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
        {
            deviceSpec = "db";
        }

        if (write.SlotIndex != null)
        {
            var slotReg = GenerateExpression(write.SlotIndex);
            Emit($"ss {deviceSpec} {slotReg} {write.PropertyName} {valueReg}");
            FreeRegister(slotReg);
        }
        else
        {
            Emit($"s {deviceSpec} {write.PropertyName} {valueReg}");
        }
        FreeRegister(valueReg);
    }

    private void GenerateDeviceSlotWrite(DeviceSlotWriteStatement write)
    {
        var valueReg = GenerateExpression(write.Value);
        var slotReg = GenerateExpression(write.SlotIndex);
        var deviceSpec = _aliases.GetValueOrDefault(write.DeviceName, write.DeviceName);
        Emit($"ss {deviceSpec} {slotReg} {write.PropertyName} {valueReg}");
        FreeRegister(slotReg);
        FreeRegister(valueReg);
    }

    private void GenerateBatchWrite(BatchWriteStatement write)
    {
        var hashReg = GenerateExpression(write.DeviceHash);
        var valueReg = GenerateExpression(write.Value);

        if (write.NameHash != null)
        {
            Emit($"sbn {hashReg} {write.NameHash} {write.PropertyName} {valueReg}");
        }
        else
        {
            Emit($"sb {hashReg} {write.PropertyName} {valueReg}");
        }

        FreeRegister(hashReg);
        FreeRegister(valueReg);
    }

    private void GenerateExternalMemoryWrite(ExternalMemoryWriteStatement write)
    {
        var addressReg = GenerateExpression(write.Address);
        var valueReg = GenerateExpression(write.Value);

        // Check for advanced device reference (named device)
        if (_deviceReferences.TryGetValue(write.DeviceName, out var devRef))
        {
            switch (devRef.Type)
            {
                case DeviceReferenceType.DeviceNamed:
                    // IC10's putd instruction only accepts device type hash, not name hash.
                    // Named devices cannot be used for memory access.
                    throw new InvalidOperationException(
                        $"Memory access (.Memory[]) requires pin alias (d0-d5) at line {_currentSourceLine}. " +
                        $"Named device '{write.DeviceName}' cannot be used with .Memory[] due to IC10 limitations. " +
                        $"Use: ALIAS {write.DeviceName} = d0");

                case DeviceReferenceType.ReferenceId:
                    // Reference ID: putd refId 0 address value
                    var refId = _deviceHashes[write.DeviceName];
                    Emit($"putd {refId} 0 {addressReg} {valueReg}");
                    FreeRegister(addressReg);
                    FreeRegister(valueReg);
                    return;

                case DeviceReferenceType.Pin:
                case DeviceReferenceType.Device:
                case DeviceReferenceType.Channel:
                    // Fall through to standard handling
                    break;
            }
        }

        // Standard pin alias: put device address value
        var deviceSpec = _aliases.GetValueOrDefault(write.DeviceName, write.DeviceName);
        if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
        {
            deviceSpec = "db";
        }
        Emit($"put {deviceSpec} {addressReg} {valueReg}");
        FreeRegister(addressReg);
        FreeRegister(valueReg);
    }

    private void GenerateSubDefinition(SubDefinition sub)
    {
        EmitLabel(sub.Name);

        // Save parameters
        for (int i = 0; i < sub.Parameters.Count; i++)
        {
            var paramReg = GetOrCreateVariable(sub.Parameters[i]);
            Emit($"pop {paramReg}");
        }

        foreach (var stmt in sub.Body)
        {
            GenerateStatement(stmt);
        }

        Emit("j ra");
    }

    private void GenerateFunctionDefinition(FunctionDefinition func)
    {
        EmitLabel(func.Name);

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var paramReg = GetOrCreateVariable(func.Parameters[i]);
            Emit($"pop {paramReg}");
        }

        foreach (var stmt in func.Body)
        {
            GenerateStatement(stmt);
        }

        Emit("j ra");
    }

    private void GenerateCall(CallStatement call)
    {
        // Push arguments in reverse order
        for (int i = call.Arguments.Count - 1; i >= 0; i--)
        {
            var argReg = GenerateExpression(call.Arguments[i]);
            Emit($"push {argReg}");
            FreeRegister(argReg);
        }

        Emit($"jal {call.SubName}");
    }

    /// <summary>
    /// Generate code for an expression and return the operand string.
    /// The operand could be:
    /// - A register (e.g., "r0")
    /// - A literal (e.g., "42", "3.14")
    /// - A defined constant name (e.g., "TARGET")
    /// This avoids unnecessary move instructions.
    /// </summary>
    private string GenerateExpression(ExpressionNode expr)
    {
        switch (expr)
        {
            case NumberLiteral num:
                // Return the literal directly - no register needed!
                return FormatNumber(num.Value);

            case BooleanLiteral boolean:
                return boolean.Value ? "1" : "0";

            case StringLiteral str:
                // Strings become hashes - return as literal
                return CalculateHash(str.Value).ToString();

            case HashExpression hashExpr:
                return CalculateHash(hashExpr.StringValue).ToString();

            case VariableExpression varExpr:
                return GenerateVariableExpression(varExpr);

            case BinaryExpression bin:
                return GenerateBinaryExpression(bin);

            case UnaryExpression unary:
                return GenerateUnaryExpression(unary);

            case TernaryExpression ternary:
                return GenerateTernaryExpression(ternary);

            case FunctionCallExpression func:
                return GenerateFunctionCall(func);

            case DeviceReadExpression deviceRead:
                return GenerateDeviceRead(deviceRead);

            case DeviceSlotReadExpression slotRead:
                return GenerateDeviceSlotRead(slotRead);

            case ExternalMemoryReadExpression memRead:
                return GenerateExternalMemoryRead(memRead);

            case BatchReadExpression batchRead:
                return GenerateBatchRead(batchRead);

            case ReagentReadExpression reagentRead:
                return GenerateReagentRead(reagentRead);

            default:
                EmitComment("Unknown expression type");
                return "0";
        }
    }

    private string GenerateVariableExpression(VariableExpression varExpr)
    {
        // Built-in constants (slot types, colors, etc.) - use inline value
        if (BuiltInConstants.TryGetValue(varExpr.Name, out var builtInValue))
        {
            return builtInValue.ToString();
        }

        // Defined constants - use inline value instead of name
        if (_defines.TryGetValue(varExpr.Name, out var constValue))
        {
            return FormatNumber(constValue);
        }

        // Validate variable is declared - error on undeclared identifiers
        if (!_declaredVariables.Contains(varExpr.Name))
        {
            var suggestion = FindSimilarIdentifier(varExpr.Name);
            var message = $"Undeclared variable '{varExpr.Name}' on line {_currentSourceLine}";
            if (suggestion != null)
            {
                message += $". Did you mean '{suggestion}'?";
            }
            throw new InvalidOperationException(message);
        }

        var varReg = GetOrCreateVariable(varExpr.Name);
        if (varExpr.ArrayIndices != null && varExpr.ArrayIndices.Count > 0)
        {
            var indexOp = GenerateExpression(varExpr.ArrayIndices[0]);
            var resultReg = AllocateRegister();
            Emit($"add r{TempRegister} {varReg} {indexOp}");
            Emit($"peek {resultReg} r{TempRegister}");
            // Only free if it's a temp register (not a literal or define)
            if (indexOp.StartsWith("r"))
                FreeRegister(indexOp);
            return resultReg;
        }

        // Return variable register directly - no copy needed!
        return varReg;
    }

    private string GenerateBinaryExpression(BinaryExpression bin)
    {
        var leftOp = GenerateExpression(bin.Left);
        var rightOp = GenerateExpression(bin.Right);
        var resultReg = AllocateRegister();

        switch (bin.Operator)
        {
            case BinaryOperator.Add:
                Emit($"add {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.Subtract:
                Emit($"sub {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.Multiply:
                Emit($"mul {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.Divide:
                Emit($"div {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.Modulo:
                Emit($"mod {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.Power:
                // a^b = exp(b * log(a))
                Emit($"log r{TempRegister} {leftOp}");
                Emit($"mul r{TempRegister} r{TempRegister} {rightOp}");
                Emit($"exp {resultReg} r{TempRegister}");
                break;
            case BinaryOperator.Equal:
                Emit($"seq {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.NotEqual:
                Emit($"sne {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.LessThan:
                Emit($"slt {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.GreaterThan:
                Emit($"sgt {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.LessEqual:
                Emit($"sle {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.GreaterEqual:
                Emit($"sge {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.ApproxEqual:
                // sap with default epsilon
                Emit($"sap {resultReg} {leftOp} {rightOp} 0.0001");
                break;
            case BinaryOperator.And:
                Emit($"and {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.Or:
                Emit($"or {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.BitAnd:
                Emit($"and {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.BitOr:
                Emit($"or {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.BitXor:
                Emit($"xor {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.ShiftLeft:
                Emit($"sll {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.ShiftRight:
                Emit($"srl {resultReg} {leftOp} {rightOp}");
                break;
            case BinaryOperator.ShiftRightArith:
                Emit($"sra {resultReg} {leftOp} {rightOp}");
                break;
        }

        FreeIfTemp(leftOp);
        FreeIfTemp(rightOp);
        return resultReg;
    }

    private string GenerateUnaryExpression(UnaryExpression unary)
    {
        // Handle increment/decrement specially - they modify the variable
        if (unary.Operator is UnaryOperator.PreIncrement or UnaryOperator.PreDecrement
            or UnaryOperator.PostIncrement or UnaryOperator.PostDecrement)
        {
            return GenerateIncrementDecrement(unary);
        }

        var operandOp = GenerateExpression(unary.Operand);
        var resultReg = AllocateRegister();

        switch (unary.Operator)
        {
            case UnaryOperator.Negate:
                Emit($"sub {resultReg} 0 {operandOp}");
                break;
            case UnaryOperator.Not:
                Emit($"seqz {resultReg} {operandOp}");
                break;
            case UnaryOperator.BitNot:
                Emit($"nor {resultReg} {operandOp} {operandOp}");
                break;
        }

        FreeIfTemp(operandOp);
        return resultReg;
    }

    private string GenerateIncrementDecrement(UnaryExpression unary)
    {
        // The operand must be a variable
        if (unary.Operand is not VariableExpression varExpr)
        {
            throw new InvalidOperationException(
                $"Increment/decrement operator can only be applied to variables at line {_currentSourceLine}");
        }

        var varReg = GetOrCreateVariable(varExpr.Name);
        var delta = unary.Operator is UnaryOperator.PreIncrement or UnaryOperator.PostIncrement ? "1" : "-1";

        switch (unary.Operator)
        {
            case UnaryOperator.PreIncrement:
            case UnaryOperator.PreDecrement:
                // Pre: increment first, then return the new value
                Emit($"add {varReg} {varReg} {delta}");
                return varReg;

            case UnaryOperator.PostIncrement:
            case UnaryOperator.PostDecrement:
                // Post: save old value, increment, return old value
                var tempReg = AllocateRegister();
                Emit($"move {tempReg} {varReg}");
                Emit($"add {varReg} {varReg} {delta}");
                return tempReg;

            default:
                throw new InvalidOperationException($"Unknown increment/decrement operator: {unary.Operator}");
        }
    }

    private string GenerateTernaryExpression(TernaryExpression ternary)
    {
        var condOp = GenerateExpression(ternary.Condition);
        var trueOp = GenerateExpression(ternary.TrueValue);
        var falseOp = GenerateExpression(ternary.FalseValue);
        var resultReg = AllocateRegister();

        // Use select instruction: select result condition trueVal falseVal
        Emit($"select {resultReg} {condOp} {trueOp} {falseOp}");

        FreeIfTemp(condOp);
        FreeIfTemp(trueOp);
        FreeIfTemp(falseOp);
        return resultReg;
    }

    private string GenerateDeviceRead(DeviceReadExpression read)
    {
        var resultReg = AllocateRegister();

        // Handle special "Set" property that checks if device is connected
        // device.Set compiles to sdse (returns 1 if device is set, 0 if not)
        if (read.PropertyName.Equals("Set", StringComparison.OrdinalIgnoreCase))
        {
            var setDeviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);
            if (setDeviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
            {
                setDeviceSpec = "db";
            }
            Emit($"sdse {resultReg} {setDeviceSpec}");
            return resultReg;
        }

        // Extract batch mode from property name (e.g., "Pressure.Average" -> "Pressure", mode 0)
        var (propertyName, batchMode, isCount) = ExtractBatchMode(read.PropertyName);

        // Check for advanced device reference (batch/named) - use inline hash values
        if (_deviceReferences.TryGetValue(read.DeviceName, out var devRef))
        {
            switch (devRef.Type)
            {
                case DeviceReferenceType.Device:
                    var deviceHash = _deviceHashes[read.DeviceName];
                    if (isCount)
                    {
                        // Device count: ldc result HASH
                        Emit($"ldc {resultReg} {deviceHash}");
                    }
                    else
                    {
                        // Batch read: lb result HASH Property Mode - use inline hash
                        Emit($"lb {resultReg} {deviceHash} {propertyName} {batchMode}");
                    }
                    return resultReg;

                case DeviceReferenceType.DeviceNamed:
                    var devHash = _deviceHashes[read.DeviceName];
                    var nameHash = _deviceNameHashes[read.DeviceName];
                    if (isCount)
                    {
                        // Named device count: ldcn result HASH NAMEHASH
                        Emit($"ldcn {resultReg} {devHash} {nameHash}");
                    }
                    else
                    {
                        // Named device read: lbn result HASH NAMEHASH Property Mode - use inline hashes
                        Emit($"lbn {resultReg} {devHash} {nameHash} {propertyName} {batchMode}");
                    }
                    return resultReg;

                case DeviceReferenceType.ReferenceId:
                    // Reference ID read - use inline value
                    var refId = _deviceHashes[read.DeviceName];
                    if (isCount)
                    {
                        Emit($"ldcn {resultReg} {refId} 0");
                    }
                    else
                    {
                        Emit($"lbn {resultReg} {refId} 0 {propertyName} {batchMode}");
                    }
                    return resultReg;

                case DeviceReferenceType.Pin:
                    // Pin reference - use standard device read
                    break;

                case DeviceReferenceType.Channel:
                    // Channel read - not typically used for property reads
                    break;
            }
        }

        // Standard device read
        var deviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);

        // IC is a special keyword that refers to the IC chip housing (db in IC10)
        if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
        {
            deviceSpec = "db";
        }

        if (read.SlotIndex != null)
        {
            var slotReg = GenerateExpression(read.SlotIndex);
            Emit($"ls {resultReg} {deviceSpec} {slotReg} {propertyName}");
            FreeIfTemp(slotReg);
        }
        else
        {
            Emit($"l {resultReg} {deviceSpec} {propertyName}");
        }
        return resultReg;
    }

    /// <summary>
    /// Generate a device read directly into a specified register (avoids unnecessary move).
    /// </summary>
    private void GenerateDeviceReadIntoRegister(DeviceReadExpression read, string targetReg)
    {
        // Extract batch mode from property name (e.g., "Pressure.Average" -> "Pressure", mode 0)
        var (propertyName, batchMode, isCount) = ExtractBatchMode(read.PropertyName);

        // Check for advanced device reference (batch/named) - use inline hash values
        if (_deviceReferences.TryGetValue(read.DeviceName, out var devRef))
        {
            switch (devRef.Type)
            {
                case DeviceReferenceType.Device:
                    var deviceHash = _deviceHashes[read.DeviceName];
                    if (isCount)
                    {
                        Emit($"ldc {targetReg} {deviceHash}");
                    }
                    else
                    {
                        Emit($"lb {targetReg} {deviceHash} {propertyName} {batchMode}");
                    }
                    return;

                case DeviceReferenceType.DeviceNamed:
                    var devHash = _deviceHashes[read.DeviceName];
                    var nameHash = _deviceNameHashes[read.DeviceName];
                    if (isCount)
                    {
                        Emit($"ldcn {targetReg} {devHash} {nameHash}");
                    }
                    else
                    {
                        Emit($"lbn {targetReg} {devHash} {nameHash} {propertyName} {batchMode}");
                    }
                    return;

                case DeviceReferenceType.ReferenceId:
                    var refId = _deviceHashes[read.DeviceName];
                    if (isCount)
                    {
                        Emit($"ldcn {targetReg} {refId} 0");
                    }
                    else
                    {
                        Emit($"lbn {targetReg} {refId} 0 {propertyName} {batchMode}");
                    }
                    return;

                case DeviceReferenceType.Pin:
                case DeviceReferenceType.Channel:
                    break;
            }
        }

        // Standard device read
        var deviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);

        // IC is a special keyword that refers to the IC chip housing (db in IC10)
        if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
        {
            deviceSpec = "db";
        }

        if (read.SlotIndex != null)
        {
            var slotReg = GenerateExpression(read.SlotIndex);
            Emit($"ls {targetReg} {deviceSpec} {slotReg} {propertyName}");
            FreeIfTemp(slotReg);
        }
        else
        {
            Emit($"l {targetReg} {deviceSpec} {propertyName}");
        }
    }

    /// <summary>
    /// Generate a batch read directly into a specified register (avoids unnecessary move).
    /// </summary>
    private void GenerateBatchReadIntoRegister(BatchReadExpression read, string targetReg)
    {
        var hashReg = GenerateExpression(read.DeviceHash);

        var modeNum = read.Mode switch
        {
            BatchMode.Average => 0,
            BatchMode.Sum => 1,
            BatchMode.Minimum => 2,
            BatchMode.Maximum => 3,
            _ => 0
        };

        if (read.NameHash != null)
        {
            Emit($"lbn {targetReg} {hashReg} {read.NameHash} {read.PropertyName} {modeNum}");
        }
        else
        {
            Emit($"lb {targetReg} {hashReg} {read.PropertyName} {modeNum}");
        }

        FreeIfTemp(hashReg);
    }

    /// <summary>
    /// Extracts batch mode suffix from property name.
    /// Returns (basePropertyName, batchModeNumber).
    /// Batch modes: Average=0, Sum=1, Minimum=2, Maximum=3
    /// </summary>
    private static (string propertyName, int batchMode, bool isCount) ExtractBatchMode(string fullPropertyName)
    {
        // Check for Count first (uses ldc instruction, not lb batch mode)
        if (fullPropertyName.EndsWith(".Count", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^6], 0, true);

        if (fullPropertyName.EndsWith(".Average", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^8], 0, false);
        if (fullPropertyName.EndsWith(".Sum", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^4], 1, false);
        if (fullPropertyName.EndsWith(".Minimum", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^8], 2, false);
        if (fullPropertyName.EndsWith(".Min", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^4], 2, false);
        if (fullPropertyName.EndsWith(".Maximum", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^8], 3, false);
        if (fullPropertyName.EndsWith(".Max", StringComparison.OrdinalIgnoreCase))
            return (fullPropertyName[..^4], 3, false);

        // No batch mode suffix - default to Average (0)
        return (fullPropertyName, 0, false);
    }

    private string GenerateDeviceSlotRead(DeviceSlotReadExpression read)
    {
        var resultReg = AllocateRegister();
        var slotReg = GenerateExpression(read.SlotIndex);
        var deviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);
        Emit($"ls {resultReg} {deviceSpec} {slotReg} {read.PropertyName}");
        FreeRegister(slotReg);
        return resultReg;
    }

    private string GenerateExternalMemoryRead(ExternalMemoryReadExpression read)
    {
        var resultReg = AllocateRegister();
        var addressReg = GenerateExpression(read.Address);

        // Check for advanced device reference (named device)
        if (_deviceReferences.TryGetValue(read.DeviceName, out var devRef))
        {
            switch (devRef.Type)
            {
                case DeviceReferenceType.DeviceNamed:
                    // IC10's getd instruction only accepts device type hash, not name hash.
                    // Named devices cannot be used for memory access.
                    throw new InvalidOperationException(
                        $"Memory access (.Memory[]) requires pin alias (d0-d5) at line {_currentSourceLine}. " +
                        $"Named device '{read.DeviceName}' cannot be used with .Memory[] due to IC10 limitations. " +
                        $"Use: ALIAS {read.DeviceName} = d0");

                case DeviceReferenceType.ReferenceId:
                    // Reference ID: getd result refId 0 address
                    var refId = _deviceHashes[read.DeviceName];
                    Emit($"getd {resultReg} {refId} 0 {addressReg}");
                    FreeRegister(addressReg);
                    return resultReg;

                case DeviceReferenceType.Pin:
                case DeviceReferenceType.Device:
                case DeviceReferenceType.Channel:
                    // Fall through to standard handling
                    break;
            }
        }

        // Standard pin alias: get result device address
        var deviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);
        if (deviceSpec.Equals("IC", StringComparison.OrdinalIgnoreCase))
        {
            deviceSpec = "db";
        }
        Emit($"get {resultReg} {deviceSpec} {addressReg}");
        FreeRegister(addressReg);
        return resultReg;
    }

    private string GenerateBatchRead(BatchReadExpression read)
    {
        var resultReg = AllocateRegister();
        var hashReg = GenerateExpression(read.DeviceHash);

        var modeNum = read.Mode switch
        {
            BatchMode.Average => 0,
            BatchMode.Sum => 1,
            BatchMode.Minimum => 2,
            BatchMode.Maximum => 3,
            _ => 0
        };

        if (read.NameHash != null)
        {
            Emit($"lbn {resultReg} {hashReg} {read.NameHash} {read.PropertyName} {modeNum}");
        }
        else
        {
            Emit($"lb {resultReg} {hashReg} {read.PropertyName} {modeNum}");
        }

        FreeRegister(hashReg);
        return resultReg;
    }

    private string GenerateReagentRead(ReagentReadExpression read)
    {
        var resultReg = AllocateRegister();
        var modeReg = GenerateExpression(read.ReagentMode);
        var hashReg = GenerateExpression(read.ReagentHash);
        var deviceSpec = _aliases.GetValueOrDefault(read.DeviceName, read.DeviceName);

        Emit($"lr {resultReg} {deviceSpec} {modeReg} {hashReg}");

        FreeRegister(modeReg);
        FreeRegister(hashReg);
        return resultReg;
    }

    private string GenerateFunctionCall(FunctionCallExpression func)
    {
        var resultReg = AllocateRegister();
        var funcName = func.FunctionName.ToUpperInvariant();
        var argRegs = func.Arguments.Select(GenerateExpression).ToList();

        switch (funcName)
        {
            // Math functions
            case "ABS":
                Emit($"abs {resultReg} {argRegs[0]}");
                break;
            case "SIN":
                Emit($"sin {resultReg} {argRegs[0]}");
                break;
            case "COS":
                Emit($"cos {resultReg} {argRegs[0]}");
                break;
            case "TAN":
                Emit($"tan {resultReg} {argRegs[0]}");
                break;
            case "ASIN":
                Emit($"asin {resultReg} {argRegs[0]}");
                break;
            case "ACOS":
                Emit($"acos {resultReg} {argRegs[0]}");
                break;
            case "ATAN":
            case "ATN":
                Emit($"atan {resultReg} {argRegs[0]}");
                break;
            case "ATAN2":
                Emit($"atan2 {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "SQRT":
            case "SQR":
                Emit($"sqrt {resultReg} {argRegs[0]}");
                break;
            case "EXP":
                Emit($"exp {resultReg} {argRegs[0]}");
                break;
            case "LOG":
            case "LN":
                Emit($"log {resultReg} {argRegs[0]}");
                break;
            case "LOG10":
                Emit($"log {resultReg} {argRegs[0]}");
                Emit($"div {resultReg} {resultReg} 2.302585");
                break;
            case "CEIL":
                Emit($"ceil {resultReg} {argRegs[0]}");
                break;
            case "FLOOR":
                Emit($"floor {resultReg} {argRegs[0]}");
                break;
            case "ROUND":
                Emit($"round {resultReg} {argRegs[0]}");
                break;
            case "TRUNC":
            case "INT":
            case "FIX":
                Emit($"trunc {resultReg} {argRegs[0]}");
                break;
            case "MIN":
                Emit($"min {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "MAX":
                Emit($"max {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "RND":
            case "RAND":
                if (argRegs.Count > 0)
                {
                    Emit($"rand {resultReg}");
                    Emit($"mul {resultReg} {resultReg} {argRegs[0]}");
                }
                else
                {
                    Emit($"rand {resultReg}");
                }
                break;
            case "SGN":
                var sgnLabel = NewLabel("sgn");
                Emit($"sgtz {resultReg} {argRegs[0]}");
                Emit($"bgtz {resultReg} {sgnLabel}");
                Emit($"sltz {resultReg} {argRegs[0]}");
                Emit($"sub {resultReg} 0 {resultReg}");
                EmitLabel(sgnLabel);
                break;
            case "POW":
                Emit($"log r{TempRegister} {argRegs[0]}");
                Emit($"mul r{TempRegister} r{TempRegister} {argRegs[1]}");
                Emit($"exp {resultReg} r{TempRegister}");
                break;

            // Select/ternary function
            case "SELECT":
            case "IIF":
                Emit($"select {resultReg} {argRegs[0]} {argRegs[1]} {argRegs[2]}");
                break;

            // NaN handling functions
            case "ISNAN":
            case "SNAN":
                Emit($"snan {resultReg} {argRegs[0]}");
                break;
            case "ISNANORZERO":
            case "SNAZ":
                Emit($"snaz {resultReg} {argRegs[0]}");
                break;

            // Approximately equal
            case "APPROX":
            case "SAP":
                if (argRegs.Count >= 3)
                {
                    Emit($"sap {resultReg} {argRegs[0]} {argRegs[1]} {argRegs[2]}");
                }
                else
                {
                    Emit($"sap {resultReg} {argRegs[0]} {argRegs[1]} 0.0001");
                }
                break;
            case "SAPZ":
                if (argRegs.Count >= 2)
                {
                    Emit($"sapz {resultReg} {argRegs[0]} {argRegs[1]}");
                }
                else
                {
                    Emit($"sapz {resultReg} {argRegs[0]} 0.0001");
                }
                break;

            // Device existence checks
            case "SDSE":
                Emit($"sdse {resultReg} {argRegs[0]}");
                break;
            case "SDNS":
                Emit($"sdns {resultReg} {argRegs[0]}");
                break;

            // Bitwise functions
            case "BAND":
            case "BITAND":
                Emit($"and {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "BOR":
            case "BITOR":
                Emit($"or {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "BXOR":
            case "BITXOR":
                Emit($"xor {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "BNOT":
            case "BITNOT":
                Emit($"nor {resultReg} {argRegs[0]} {argRegs[0]}");
                break;
            case "BNOR":
            case "BITNOR":
                Emit($"nor {resultReg} {argRegs[0]} {argRegs[1]}");
                break;

            // Shift functions
            case "SHL":
            case "SHIFTL":
            case "LSHIFT":
                Emit($"sll {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "SHR":
            case "SHIFTR":
            case "RSHIFT":
                Emit($"srl {resultReg} {argRegs[0]} {argRegs[1]}");
                break;
            case "SHRA":
            case "SHIFTRA":
            case "RSHIFTA":
                Emit($"sra {resultReg} {argRegs[0]} {argRegs[1]}");
                break;

            // Comparison to zero functions
            case "SEQZ":
                Emit($"seqz {resultReg} {argRegs[0]}");
                break;
            case "SNEZ":
                Emit($"snez {resultReg} {argRegs[0]}");
                break;
            case "SGTZ":
                Emit($"sgtz {resultReg} {argRegs[0]}");
                break;
            case "SLTZ":
                Emit($"sltz {resultReg} {argRegs[0]}");
                break;
            case "SGEZ":
                Emit($"sgez {resultReg} {argRegs[0]}");
                break;
            case "SLEZ":
                Emit($"slez {resultReg} {argRegs[0]}");
                break;

            // Range check
            case "INRANGE":
                // Check if arg0 is between arg1 and arg2
                Emit($"sge r{TempRegister} {argRegs[0]} {argRegs[1]}");
                Emit($"sle r{TempRegister2} {argRegs[0]} {argRegs[2]}");
                Emit($"and {resultReg} r{TempRegister} r{TempRegister2}");
                break;

            // Interpolation
            case "LERP":
                // lerp(a, b, t) = a + t * (b - a)
                Emit($"sub r{TempRegister} {argRegs[1]} {argRegs[0]}");
                Emit($"mul r{TempRegister} r{TempRegister} {argRegs[2]}");
                Emit($"add {resultReg} {argRegs[0]} r{TempRegister}");
                break;

            // Clamp
            case "CLAMP":
                Emit($"max r{TempRegister} {argRegs[0]} {argRegs[1]}");
                Emit($"min {resultReg} r{TempRegister} {argRegs[2]}");
                break;

            // Hash function
            case "HASH":
                if (func.Arguments[0] is StringLiteral strArg)
                {
                    var hashVal = CalculateHash(strArg.Value);
                    Emit($"move {resultReg} {hashVal}");
                }
                else
                {
                    Emit($"move {resultReg} {argRegs[0]}");
                }
                break;

            default:
                // Check if it's a user-defined function
                Emit($"jal {funcName}");
                Emit($"pop {resultReg}");
                break;
        }

        foreach (var reg in argRegs)
        {
            FreeRegister(reg);
        }

        return resultReg;
    }

    // Stationeers uses CRC-32 for hash calculation
    private static readonly uint[] Crc32Table = GenerateCrc32Table();

    private static uint[] GenerateCrc32Table()
    {
        var table = new uint[256];
        const uint polynomial = 0xEDB88320; // Standard CRC-32 polynomial (reversed)

        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) == 1)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            table[i] = crc;
        }
        return table;
    }

    private static int CalculateHash(string str)
    {
        // Stationeers CRC-32 hash calculation
        uint crc = 0xFFFFFFFF;

        foreach (char c in str)
        {
            byte b = (byte)c;
            crc = (crc >> 8) ^ Crc32Table[(crc ^ b) & 0xFF];
        }

        // Return as signed int32 (Stationeers convention)
        var hash = unchecked((int)(crc ^ 0xFFFFFFFF));

        // Register in the living hash dictionary for decompilation support
        Data.HashDictionary.RegisterHash(str, hash);

        return hash;
    }

    // Register allocation
    private readonly HashSet<int> _usedRegisters = new();

    private string AllocateRegister()
    {
        for (int i = 0; i < MaxRegisters; i++)
        {
            if (!_usedRegisters.Contains(i) && !_variables.ContainsValue(i))
            {
                _usedRegisters.Add(i);
                return $"r{i}";
            }
        }
        return $"r{TempRegister}";
    }

    private void FreeRegister(string reg)
    {
        if (reg.StartsWith("r") && int.TryParse(reg[1..], out var num))
        {
            if (!_variables.ContainsValue(num))
            {
                _usedRegisters.Remove(num);
            }
        }
    }

    /// <summary>
    /// Check if an operand is a temporary register that should be freed after use.
    /// Returns false for literals, defines, and variable registers.
    /// </summary>
    private bool IsTempRegister(string operand)
    {
        if (!operand.StartsWith("r")) return false;
        if (!int.TryParse(operand[1..], out var num)) return false;
        return !_variables.ContainsValue(num);
    }

    /// <summary>
    /// Free an operand if it's a temporary register.
    /// Safe to call on literals, defines, or variable registers (does nothing).
    /// </summary>
    private void FreeIfTemp(string operand)
    {
        if (IsTempRegister(operand))
        {
            FreeRegister(operand);
        }
    }

    private string GetOrCreateVariable(string name)
    {
        if (!_variables.TryGetValue(name, out var regNum))
        {
            for (int i = 0; i < MaxRegisters; i++)
            {
                if (!_variables.ContainsValue(i))
                {
                    regNum = i;
                    _variables[name] = i;
                    // Track BASIC variable -> register mapping for watch panel
                    _sourceMap.VariableRegisters[name] = $"r{i}";
                    break;
                }
            }
        }
        return $"r{regNum}";
    }

    private string NewLabel(string prefix)
    {
        return $"{prefix}_{_nextLabel++}";
    }

    /// <summary>
    /// Finds a similar identifier to suggest for typos using Levenshtein distance.
    /// </summary>
    private string? FindSimilarIdentifier(string name)
    {
        string? bestMatch = null;
        int bestDistance = int.MaxValue;
        const int maxDistance = 3; // Maximum edit distance to consider

        // Check all declared variables
        foreach (var declared in _declaredVariables)
        {
            var distance = LevenshteinDistance(name, declared);
            if (distance < bestDistance && distance <= maxDistance)
            {
                bestDistance = distance;
                bestMatch = declared;
            }
        }

        // Also check aliases
        foreach (var alias in _aliases.Keys)
        {
            var distance = LevenshteinDistance(name, alias);
            if (distance < bestDistance && distance <= maxDistance)
            {
                bestDistance = distance;
                bestMatch = alias;
            }
        }

        // And defines/constants
        foreach (var define in _defines.Keys)
        {
            var distance = LevenshteinDistance(name, define);
            if (distance < bestDistance && distance <= maxDistance)
            {
                bestDistance = distance;
                bestMatch = define;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings (edit distance).
    /// </summary>
    private static int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
        if (string.IsNullOrEmpty(s2)) return s1.Length;

        var m = s1.Length;
        var n = s2.Length;
        var d = new int[m + 1, n + 1];

        for (var i = 0; i <= m; i++) d[i, 0] = i;
        for (var j = 0; j <= n; j++) d[0, j] = j;

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[m, n];
    }

    private static string FormatNumber(double value)
    {
        if (value == Math.Floor(value) && Math.Abs(value) < 1e10)
        {
            return ((long)value).ToString();
        }
        return value.ToString("G");
    }

    private void Emit(string line)
    {
        // Add source map entry for this IC10 line
        if (_currentSourceLine > 0)
        {
            _sourceMap.AddMapping(_currentIC10Line, _currentSourceLine);
        }

        if (_options.EmitSourceLineComments && _currentSourceLine > 0)
        {
            _output.AppendLine($"{line} # {_currentSourceLine}");
        }
        else
        {
            _output.AppendLine(line);
        }

        _currentIC10Line++;
    }

    private void EmitLabel(string label)
    {
        // Labels are on their own line but map to same source
        if (_currentSourceLine > 0)
        {
            _sourceMap.AddMapping(_currentIC10Line, _currentSourceLine);
        }
        _output.AppendLine($"{label}:");
        _currentIC10Line++;
    }

    private void EmitComment(string comment)
    {
        // Comments don't count as executable lines, but track them
        _output.AppendLine($"# {comment}");
        _currentIC10Line++;
    }
}
