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

public class PrintStatement : StatementNode
{
    public List<ExpressionNode> Expressions { get; } = new();
    public bool NoNewline { get; set; } // If ends with semicolon
}

public class InputStatement : StatementNode
{
    public string? Prompt { get; set; }
    public string VariableName { get; set; } = "";
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
}

public class GosubStatement : StatementNode
{
    public int TargetLine { get; set; }
}

public class ReturnStatement : StatementNode { }

public class EndStatement : StatementNode { }

public class DimStatement : StatementNode
{
    public string VariableName { get; set; } = "";
    public List<int> Dimensions { get; } = new();
}

public class SubDefinition : StatementNode
{
    public string Name { get; set; } = "";
    public List<string> Parameters { get; } = new();
    public List<StatementNode> Body { get; } = new();
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

public class AliasStatement : StatementNode
{
    public string AliasName { get; set; } = "";
    public string DeviceSpec { get; set; } = "";
}

public class DefineStatement : StatementNode
{
    public string ConstantName { get; set; } = "";
    public double Value { get; set; }
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

public class FunctionCallExpression : ExpressionNode
{
    public string FunctionName { get; set; } = "";
    public List<ExpressionNode> Arguments { get; } = new();
}

public class DeviceReadExpression : ExpressionNode
{
    public string DeviceName { get; set; } = "";
    public string PropertyName { get; set; } = "";
}

public class DeviceWriteStatement : StatementNode
{
    public string DeviceName { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public ExpressionNode Value { get; set; } = null!;
}

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power,
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessEqual,
    GreaterEqual,
    And,
    Or
}

public enum UnaryOperator
{
    Negate,
    Not
}
