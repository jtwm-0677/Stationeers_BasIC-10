namespace BasicToMips.AST;

public abstract class AstNode
{
    public int Line { get; set; }
    public int Column { get; set; }
}

// Program root
public class ProgramNode : AstNode
{
    public List<StatementNode> Statements { get; } = new();
    public Dictionary<int, int> LineNumberMap { get; } = new(); // BASIC line # -> statement index
}

// Statements
public abstract class StatementNode : AstNode
{
    public int? BasicLineNumber { get; set; }
}

public class LetStatement : StatementNode
{
    public string VariableName { get; set; } = "";
    public List<ExpressionNode>? ArrayIndices { get; set; }
    public ExpressionNode Value { get; set; } = null!;
}

public class VarStatement : StatementNode
{
    public string VariableName { get; set; } = "";
    public ExpressionNode? InitialValue { get; set; }
}

public class ConstStatement : StatementNode
{
    public string ConstantName { get; set; } = "";
    public ExpressionNode Value { get; set; } = null!;
}

public class PrintStatement : StatementNode
{
    public List<ExpressionNode> Expressions { get; } = new();
    public bool NoNewline { get; set; } // If ends with semicolon
}

public class InputStatement : StatementNode
{
    public string? Prompt { get; set; }
    public string VariableName { get; set; } = "";
    public string? DeviceName { get; set; }
    public string? PropertyName { get; set; }
}

public class IfStatement : StatementNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public List<StatementNode> ThenBranch { get; } = new();
    public List<StatementNode> ElseBranch { get; } = new();
    public bool IsMultiLine { get; set; }
}

public class ForStatement : StatementNode
{
    public string VariableName { get; set; } = "";
    public ExpressionNode StartValue { get; set; } = null!;
    public ExpressionNode EndValue { get; set; } = null!;
    public ExpressionNode? StepValue { get; set; }
    public List<StatementNode> Body { get; } = new();
}

public class WhileStatement : StatementNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public List<StatementNode> Body { get; } = new();
}

public class DoLoopStatement : StatementNode
{
    public ExpressionNode? WhileCondition { get; set; }
    public ExpressionNode? UntilCondition { get; set; }
    public bool ConditionAtStart { get; set; }
    public List<StatementNode> Body { get; } = new();
}

public class GotoStatement : StatementNode
{
    public int TargetLine { get; set; }
    public string? TargetLabel { get; set; }
}

public class GosubStatement : StatementNode
{
    public int TargetLine { get; set; }
    public string? TargetLabel { get; set; }
}

public class OnGotoStatement : StatementNode
{
    public ExpressionNode IndexExpression { get; set; } = null!;
    public List<string> TargetLabels { get; } = new();
}

public class OnGosubStatement : StatementNode
{
    public ExpressionNode IndexExpression { get; set; } = null!;
    public List<string> TargetLabels { get; } = new();
}

public class DataStatement : StatementNode
{
    public List<ExpressionNode> Values { get; } = new();
}

public class ReadStatement : StatementNode
{
    public List<string> VariableNames { get; } = new();
}

public class RestoreStatement : StatementNode { }

public class LabelStatement : StatementNode
{
    public string Name { get; set; } = "";
}

public class ReturnStatement : StatementNode
{
    public ExpressionNode? ReturnValue { get; set; }
}

public class EndStatement : StatementNode { }

public class BreakStatement : StatementNode { }

public class ContinueStatement : StatementNode { }

public class PushStatement : StatementNode
{
    public ExpressionNode Value { get; set; } = null!;
}

public class PopStatement : StatementNode
{
    public string VariableName { get; set; } = "";
}

public class PeekStatement : StatementNode
{
    public string VariableName { get; set; } = "";
}

public class SelectStatement : StatementNode
{
    public ExpressionNode TestExpression { get; set; } = null!;
    public List<CaseClause> Cases { get; } = new();
    public List<StatementNode> DefaultBody { get; } = new();
}

public class CaseClause
{
    public List<ExpressionNode> Values { get; } = new();
    public List<StatementNode> Body { get; } = new();
}

public class DimStatement : StatementNode
{
    public string VariableName { get; set; } = "";
    public List<ExpressionNode> Dimensions { get; } = new();
}

public class SubDefinition : StatementNode
{
    public string Name { get; set; } = "";
    public List<string> Parameters { get; } = new();
    public List<StatementNode> Body { get; } = new();
}

public class FunctionDefinition : StatementNode
{
    public string Name { get; set; } = "";
    public List<string> Parameters { get; } = new();
    public List<StatementNode> Body { get; } = new();
    public string? ReturnType { get; set; }
}

public class CallStatement : StatementNode
{
    public string SubName { get; set; } = "";
    public List<ExpressionNode> Arguments { get; } = new();
}

public class SleepStatement : StatementNode
{
    public ExpressionNode Duration { get; set; } = null!;
}

public class YieldStatement : StatementNode { }

/// <summary>
/// Represents a comment from the source code that should be preserved in output.
/// </summary>
public class CommentStatement : StatementNode
{
    public string Text { get; set; } = "";
    public bool IsMetaComment { get; set; }  // ##Meta: directives
}

public class AliasStatement : StatementNode
{
    public string AliasName { get; set; } = "";
    public ExpressionNode? AliasIndex { get; set; }  // For ALIAS name[index] = ... (array-based alias)
    public string DeviceSpec { get; set; } = "";  // Simple d0-d5, db reference
    public DeviceReference? DeviceReference { get; set; }  // Advanced IC.Device/IC.ID/IC.Port reference
}

/// <summary>
/// Represents advanced device reference types:
/// - IC.Pin[n] - Direct pin reference (same as d0-d5)
/// - IC.Device[hash] - Batch reference by device type hash
/// - IC.Device[hash].Name["name"] - Named device reference (bypasses 6-pin limit)
/// - IC.ID[refId] - Reference by device Reference ID
/// - IC.Port[n].Channel[m] - Channel-based communication
/// </summary>
public class DeviceReference
{
    public DeviceReferenceType Type { get; set; }

    // For IC.Pin[n] - the pin number (0-5)
    public int? PinIndex { get; set; }

    // For IC.Device[hash] - the device type hash (number or string like "StructureGasSensor")
    public ExpressionNode? DeviceHash { get; set; }

    // For IC.Device[hash].Name["name"] - the device name for named reference
    public string? DeviceName { get; set; }

    // For IC.Device[hash].Name[variable] - dynamic device name expression
    public ExpressionNode? DeviceNameExpression { get; set; }

    // For IC.ID[refId] - the Reference ID from configuration card
    public long? ReferenceId { get; set; }

    // For IC.Port[n].Channel[m] - port and channel numbers
    public int? PortIndex { get; set; }
    public int? ChannelIndex { get; set; }

    // For IC.Pin[n].Port[m].Channel[c] - pin with port/channel
    public bool HasPort { get; set; }
}

public enum DeviceReferenceType
{
    Pin,           // IC.Pin[n] or simple d0-d5
    Device,        // IC.Device[hash] - batch operations
    DeviceNamed,   // IC.Device[hash].Name["name"] - named device (key feature!)
    ReferenceId,   // IC.ID[refId]
    Channel        // IC.Port[n].Channel[m] or IC.Pin[n].Port[m].Channel[c]
}

public class DefineStatement : StatementNode
{
    public string ConstantName { get; set; } = "";
    public ExpressionNode Value { get; set; } = null!;
}

public class DeviceWriteStatement : StatementNode
{
    public string DeviceName { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public ExpressionNode Value { get; set; } = null!;
    public ExpressionNode? SlotIndex { get; set; }
}

public class DeviceSlotWriteStatement : StatementNode
{
    public string DeviceName { get; set; } = "";
    public ExpressionNode SlotIndex { get; set; } = null!;
    public string PropertyName { get; set; } = "";
    public ExpressionNode Value { get; set; } = null!;
}

public class BatchWriteStatement : StatementNode
{
    public ExpressionNode DeviceHash { get; set; } = null!;
    public string PropertyName { get; set; } = "";
    public ExpressionNode Value { get; set; } = null!;
    public string? NameHash { get; set; }
}

// Expressions
public abstract class ExpressionNode : AstNode { }

public class NumberLiteral : ExpressionNode
{
    public double Value { get; set; }
}

public class StringLiteral : ExpressionNode
{
    public string Value { get; set; } = "";
}

public class BooleanLiteral : ExpressionNode
{
    public bool Value { get; set; }
}

public class VariableExpression : ExpressionNode
{
    public string Name { get; set; } = "";
    public List<ExpressionNode>? ArrayIndices { get; set; }
}

public class BinaryExpression : ExpressionNode
{
    public ExpressionNode Left { get; set; } = null!;
    public BinaryOperator Operator { get; set; }
    public ExpressionNode Right { get; set; } = null!;
}

public class UnaryExpression : ExpressionNode
{
    public UnaryOperator Operator { get; set; }
    public ExpressionNode Operand { get; set; } = null!;
}

public class TernaryExpression : ExpressionNode
{
    public ExpressionNode Condition { get; set; } = null!;
    public ExpressionNode TrueValue { get; set; } = null!;
    public ExpressionNode FalseValue { get; set; } = null!;
}

public class FunctionCallExpression : ExpressionNode
{
    public string FunctionName { get; set; } = "";
    public List<ExpressionNode> Arguments { get; } = new();
}

public class DeviceReadExpression : ExpressionNode
{
    public string DeviceName { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public ExpressionNode? SlotIndex { get; set; }
}

public class DeviceSlotReadExpression : ExpressionNode
{
    public string DeviceName { get; set; } = "";
    public ExpressionNode SlotIndex { get; set; } = null!;
    public string PropertyName { get; set; } = "";
}

public class BatchReadExpression : ExpressionNode
{
    public ExpressionNode DeviceHash { get; set; } = null!;
    public string PropertyName { get; set; } = "";
    public BatchMode Mode { get; set; } = BatchMode.Average;
    public string? NameHash { get; set; }
}

public class ReagentReadExpression : ExpressionNode
{
    public string DeviceName { get; set; } = "";
    public ExpressionNode ReagentMode { get; set; } = null!;
    public ExpressionNode ReagentHash { get; set; } = null!;
}

public class HashExpression : ExpressionNode
{
    public string StringValue { get; set; } = "";
}

public enum BinaryOperator
{
    // Arithmetic
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power,

    // Comparison
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessEqual,
    GreaterEqual,
    ApproxEqual,  // Approximately equal (with epsilon)

    // Logical
    And,
    Or,

    // Bitwise
    BitAnd,
    BitOr,
    BitXor,
    ShiftLeft,
    ShiftRight,
    ShiftRightArith  // Arithmetic shift right (preserves sign)
}

public enum UnaryOperator
{
    Negate,
    Not,
    BitNot
}

public enum BatchMode
{
    Average,
    Sum,
    Minimum,
    Maximum
}
