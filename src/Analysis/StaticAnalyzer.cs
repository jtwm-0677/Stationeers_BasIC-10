using BasicToMips.AST;

namespace BasicToMips.Analysis;

/// <summary>
/// Static analyzer for detecting code issues like unused variables.
/// </summary>
public class StaticAnalyzer
{
    private readonly Dictionary<string, VariableInfo> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LabelInfo> _labels = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<AnalysisWarning> _warnings = new();

    /// <summary>
    /// Analyze the program and return warnings.
    /// </summary>
    public List<AnalysisWarning> Analyze(ProgramNode program)
    {
        _variables.Clear();
        _labels.Clear();
        _warnings.Clear();

        // First pass: collect declarations
        CollectDeclarations(program);

        // Second pass: collect usages
        CollectUsages(program);

        // Generate warnings
        GenerateWarnings();

        return _warnings;
    }

    private void CollectDeclarations(ProgramNode program)
    {
        foreach (var stmt in program.Statements)
        {
            switch (stmt)
            {
                case VarStatement varStmt:
                    DeclareVariable(varStmt.VariableName, stmt.Line, VariableKind.Variable);
                    break;

                case LetStatement let:
                    // First assignment counts as declaration
                    if (!_variables.ContainsKey(let.VariableName))
                    {
                        DeclareVariable(let.VariableName, stmt.Line, VariableKind.Variable);
                    }
                    break;

                case DimStatement dim:
                    DeclareVariable(dim.VariableName, stmt.Line, VariableKind.Array);
                    break;

                case ConstStatement constStmt:
                    DeclareVariable(constStmt.ConstantName, stmt.Line, VariableKind.Constant);
                    break;

                case DefineStatement define:
                    DeclareVariable(define.ConstantName, stmt.Line, VariableKind.Define);
                    break;

                case AliasStatement alias:
                    DeclareVariable(alias.AliasName, stmt.Line, VariableKind.Alias);
                    break;

                case LabelStatement label:
                    DeclareLabel(label.Name, stmt.Line);
                    break;

                case SubDefinition sub:
                    DeclareLabel(sub.Name, stmt.Line);
                    // Parameters are used by definition
                    foreach (var param in sub.Parameters)
                    {
                        DeclareVariable(param, stmt.Line, VariableKind.Parameter);
                        MarkUsed(param); // Parameters are considered used
                    }
                    break;

                case FunctionDefinition func:
                    DeclareLabel(func.Name, stmt.Line);
                    foreach (var param in func.Parameters)
                    {
                        DeclareVariable(param, stmt.Line, VariableKind.Parameter);
                        MarkUsed(param);
                    }
                    break;

                case ForStatement forStmt:
                    // Loop variable is declared and used
                    if (!_variables.ContainsKey(forStmt.VariableName))
                    {
                        DeclareVariable(forStmt.VariableName, stmt.Line, VariableKind.LoopVariable);
                    }
                    MarkUsed(forStmt.VariableName);
                    break;
            }
        }
    }

    private void CollectUsages(ProgramNode program)
    {
        foreach (var stmt in program.Statements)
        {
            CollectUsagesInStatement(stmt);
        }
    }

    private void CollectUsagesInStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case LetStatement let:
                CollectUsagesInExpression(let.Value);
                break;

            case PrintStatement print:
                foreach (var expr in print.Expressions)
                {
                    CollectUsagesInExpression(expr);
                }
                break;

            case IfStatement ifStmt:
                CollectUsagesInExpression(ifStmt.Condition);
                foreach (var thenStmt in ifStmt.ThenBranch)
                {
                    CollectUsagesInStatement(thenStmt);
                }
                foreach (var elseStmt in ifStmt.ElseBranch)
                {
                    CollectUsagesInStatement(elseStmt);
                }
                break;

            case WhileStatement whileStmt:
                CollectUsagesInExpression(whileStmt.Condition);
                foreach (var bodyStmt in whileStmt.Body)
                {
                    CollectUsagesInStatement(bodyStmt);
                }
                break;

            case ForStatement forStmt:
                CollectUsagesInExpression(forStmt.StartValue);
                CollectUsagesInExpression(forStmt.EndValue);
                if (forStmt.StepValue != null)
                {
                    CollectUsagesInExpression(forStmt.StepValue);
                }
                foreach (var bodyStmt in forStmt.Body)
                {
                    CollectUsagesInStatement(bodyStmt);
                }
                break;

            case SelectStatement selectStmt:
                CollectUsagesInExpression(selectStmt.TestExpression);
                foreach (var caseBlock in selectStmt.Cases)
                {
                    foreach (var value in caseBlock.Values)
                    {
                        CollectUsagesInExpression(value);
                    }
                    foreach (var bodyStmt in caseBlock.Body)
                    {
                        CollectUsagesInStatement(bodyStmt);
                    }
                }
                break;

            case DeviceWriteStatement devWrite:
                CollectUsagesInExpression(devWrite.Value);
                MarkUsed(devWrite.DeviceName);
                if (devWrite.SlotIndex != null)
                {
                    CollectUsagesInExpression(devWrite.SlotIndex);
                }
                break;

            case DeviceSlotWriteStatement devSlotWrite:
                CollectUsagesInExpression(devSlotWrite.Value);
                CollectUsagesInExpression(devSlotWrite.SlotIndex);
                MarkUsed(devSlotWrite.DeviceName);
                break;

            case BatchWriteStatement batchWrite:
                CollectUsagesInExpression(batchWrite.DeviceHash);
                CollectUsagesInExpression(batchWrite.Value);
                break;

            case BatchSlotWriteStatement batchSlotWrite:
                CollectUsagesInExpression(batchSlotWrite.DeviceHash);
                CollectUsagesInExpression(batchSlotWrite.SlotIndex);
                CollectUsagesInExpression(batchSlotWrite.Value);
                if (batchSlotWrite.NameHash != null)
                    CollectUsagesInExpression(batchSlotWrite.NameHash);
                break;

            case IndirectRegisterWriteStatement indirectRegWrite:
                CollectUsagesInExpression(indirectRegWrite.IndexExpression);
                CollectUsagesInExpression(indirectRegWrite.Value);
                break;

            case ExternalMemoryWriteStatement memWrite:
                CollectUsagesInExpression(memWrite.Address);
                CollectUsagesInExpression(memWrite.Value);
                MarkUsed(memWrite.DeviceName);
                break;

            case InputStatement input:
                if (input.DeviceName != null)
                {
                    MarkUsed(input.DeviceName);
                }
                break;

            case CallStatement call:
                foreach (var arg in call.Arguments)
                {
                    CollectUsagesInExpression(arg);
                }
                MarkLabelUsed(call.SubName);
                break;

            case GotoStatement gotoStmt:
                if (gotoStmt.TargetLabel != null)
                {
                    MarkLabelUsed(gotoStmt.TargetLabel);
                }
                break;

            case GosubStatement gosub:
                if (gosub.TargetLabel != null)
                {
                    MarkLabelUsed(gosub.TargetLabel);
                }
                break;

            case SubDefinition sub:
                foreach (var bodyStmt in sub.Body)
                {
                    CollectUsagesInStatement(bodyStmt);
                }
                break;

            case FunctionDefinition func:
                foreach (var bodyStmt in func.Body)
                {
                    CollectUsagesInStatement(bodyStmt);
                }
                break;

            case ReturnStatement ret:
                if (ret.ReturnValue != null)
                {
                    CollectUsagesInExpression(ret.ReturnValue);
                }
                break;

            case DoLoopStatement doLoop:
                if (doLoop.WhileCondition != null)
                {
                    CollectUsagesInExpression(doLoop.WhileCondition);
                }
                if (doLoop.UntilCondition != null)
                {
                    CollectUsagesInExpression(doLoop.UntilCondition);
                }
                foreach (var bodyStmt in doLoop.Body)
                {
                    CollectUsagesInStatement(bodyStmt);
                }
                break;

            case PushStatement push:
                CollectUsagesInExpression(push.Value);
                break;

            case PokeStatement poke:
                CollectUsagesInExpression(poke.Address);
                CollectUsagesInExpression(poke.Value);
                break;

            case SleepStatement sleep:
                CollectUsagesInExpression(sleep.Duration);
                break;
        }
    }

    private void CollectUsagesInExpression(ExpressionNode? expr)
    {
        if (expr == null) return;

        switch (expr)
        {
            case VariableExpression varExpr:
                MarkUsed(varExpr.Name);
                // Check array indices
                if (varExpr.ArrayIndices != null)
                {
                    foreach (var idx in varExpr.ArrayIndices)
                    {
                        CollectUsagesInExpression(idx);
                    }
                }
                break;

            case BinaryExpression binExpr:
                CollectUsagesInExpression(binExpr.Left);
                CollectUsagesInExpression(binExpr.Right);
                break;

            case UnaryExpression unaryExpr:
                CollectUsagesInExpression(unaryExpr.Operand);
                break;

            case FunctionCallExpression funcCall:
                foreach (var arg in funcCall.Arguments)
                {
                    CollectUsagesInExpression(arg);
                }
                // Check if it's a user-defined function
                MarkLabelUsed(funcCall.FunctionName);
                break;

            case DeviceReadExpression devRead:
                MarkUsed(devRead.DeviceName);
                if (devRead.SlotIndex != null)
                {
                    CollectUsagesInExpression(devRead.SlotIndex);
                }
                break;

            case DeviceSlotReadExpression devSlotRead:
                MarkUsed(devSlotRead.DeviceName);
                CollectUsagesInExpression(devSlotRead.SlotIndex);
                break;

            case ExternalMemoryReadExpression memRead:
                MarkUsed(memRead.DeviceName);
                CollectUsagesInExpression(memRead.Address);
                break;

            case ReagentReadExpression reagentRead:
                MarkUsed(reagentRead.DeviceName);
                CollectUsagesInExpression(reagentRead.ReagentMode);
                CollectUsagesInExpression(reagentRead.ReagentHash);
                break;

            case TernaryExpression ternary:
                CollectUsagesInExpression(ternary.Condition);
                CollectUsagesInExpression(ternary.TrueValue);
                CollectUsagesInExpression(ternary.FalseValue);
                break;

            case BatchReadExpression batch:
                CollectUsagesInExpression(batch.DeviceHash);
                break;

            case BatchSlotReadExpression batchSlot:
                CollectUsagesInExpression(batchSlot.DeviceHash);
                CollectUsagesInExpression(batchSlot.SlotIndex);
                if (batchSlot.NameHash != null)
                    CollectUsagesInExpression(batchSlot.NameHash);
                break;

            case IndirectRegisterExpression indirectReg:
                CollectUsagesInExpression(indirectReg.IndexExpression);
                break;
        }
    }

    private void DeclareVariable(string name, int line, VariableKind kind)
    {
        if (!_variables.ContainsKey(name))
        {
            _variables[name] = new VariableInfo(name, line, kind);
        }
    }

    private void DeclareLabel(string name, int line)
    {
        if (!_labels.ContainsKey(name))
        {
            _labels[name] = new LabelInfo(name, line);
        }
    }

    private void MarkUsed(string name)
    {
        if (_variables.TryGetValue(name, out var info))
        {
            info.IsUsed = true;
        }
    }

    private void MarkLabelUsed(string name)
    {
        if (_labels.TryGetValue(name, out var info))
        {
            info.IsUsed = true;
        }
    }

    private void GenerateWarnings()
    {
        // Check for unused variables
        foreach (var kvp in _variables)
        {
            var info = kvp.Value;
            if (!info.IsUsed && info.Kind != VariableKind.Parameter && info.Kind != VariableKind.LoopVariable)
            {
                var kindName = info.Kind switch
                {
                    VariableKind.Constant => "Constant",
                    VariableKind.Define => "Define",
                    VariableKind.Alias => "Alias",
                    VariableKind.Array => "Array",
                    _ => "Variable"
                };
                _warnings.Add(new AnalysisWarning(
                    WarningType.UnusedVariable,
                    $"{kindName} '{info.Name}' is declared but never used",
                    info.DeclarationLine
                ));
            }
        }

        // Check for unused labels/subs/functions (excluding "main" which is often entry point)
        foreach (var kvp in _labels)
        {
            var info = kvp.Value;
            if (!info.IsUsed && !info.Name.Equals("main", StringComparison.OrdinalIgnoreCase))
            {
                _warnings.Add(new AnalysisWarning(
                    WarningType.UnusedLabel,
                    $"Label/SUB/FUNCTION '{info.Name}' is declared but never called",
                    info.DeclarationLine
                ));
            }
        }

        // Sort by line number
        _warnings.Sort((a, b) => a.Line.CompareTo(b.Line));
    }

    private class VariableInfo
    {
        public string Name { get; }
        public int DeclarationLine { get; }
        public VariableKind Kind { get; }
        public bool IsUsed { get; set; }

        public VariableInfo(string name, int line, VariableKind kind)
        {
            Name = name;
            DeclarationLine = line;
            Kind = kind;
            IsUsed = false;
        }
    }

    private class LabelInfo
    {
        public string Name { get; }
        public int DeclarationLine { get; }
        public bool IsUsed { get; set; }

        public LabelInfo(string name, int line)
        {
            Name = name;
            DeclarationLine = line;
            IsUsed = false;
        }
    }

    private enum VariableKind
    {
        Variable,
        Constant,
        Define,
        Alias,
        Array,
        Parameter,
        LoopVariable
    }
}

/// <summary>
/// Types of analysis warnings.
/// </summary>
public enum WarningType
{
    UnusedVariable,
    UnusedLabel,
    UnreachableCode,
    PossibleError
}

/// <summary>
/// An analysis warning found during static analysis.
/// </summary>
public class AnalysisWarning
{
    public WarningType Type { get; }
    public string Message { get; }
    public int Line { get; }

    public AnalysisWarning(WarningType type, string message, int line)
    {
        Type = type;
        Message = message;
        Line = line;
    }
}
