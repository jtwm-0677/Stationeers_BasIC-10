using BasicToMips.AST;
using BasicToMips.Lexer;

namespace BasicToMips.Parser;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _position = 0;
    }

    public ProgramNode Parse()
    {
        var program = new ProgramNode();
        int statementIndex = 0;

        while (!IsAtEnd())
        {
            SkipNewlines();
            if (IsAtEnd()) break;

            int? lineNumber = null;
            if (Check(TokenType.LineNumber))
            {
                lineNumber = int.Parse(Advance().Value);
            }

            if (IsAtEnd() || Check(TokenType.Newline))
            {
                SkipNewlines();
                continue;
            }

            var statement = ParseStatement();
            if (statement != null)
            {
                statement.BasicLineNumber = lineNumber;
                if (lineNumber.HasValue)
                {
                    program.LineNumberMap[lineNumber.Value] = statementIndex;
                }
                program.Statements.Add(statement);
                statementIndex++;
            }

            // Handle colon-separated statements on same line
            while (Check(TokenType.Colon))
            {
                Advance(); // Consume ':'
                var nextStatement = ParseStatement();
                if (nextStatement != null)
                {
                    program.Statements.Add(nextStatement);
                    statementIndex++;
                }
            }

            SkipNewlines();
        }

        return program;
    }

    private StatementNode? ParseStatement()
    {
        if (Check(TokenType.Let)) return ParseLetStatement();
        if (Check(TokenType.Print)) return ParsePrintStatement();
        if (Check(TokenType.Input)) return ParseInputStatement();
        if (Check(TokenType.If)) return ParseIfStatement();
        if (Check(TokenType.For)) return ParseForStatement();
        if (Check(TokenType.While)) return ParseWhileStatement();
        if (Check(TokenType.Do)) return ParseDoLoopStatement();
        if (Check(TokenType.Goto)) return ParseGotoStatement();
        if (Check(TokenType.Gosub)) return ParseGosubStatement();
        if (Check(TokenType.Return)) return ParseReturnStatement();
        if (Check(TokenType.End)) return ParseEndStatement();
        if (Check(TokenType.Dim)) return ParseDimStatement();
        if (Check(TokenType.Sub)) return ParseSubDefinition();
        if (Check(TokenType.Call)) return ParseCallStatement();
        if (Check(TokenType.Sleep)) return ParseSleepStatement();
        if (Check(TokenType.Yield)) return ParseYieldStatement();
        if (Check(TokenType.Alias)) return ParseAliasStatement();
        if (Check(TokenType.Define)) return ParseDefineStatement();
        if (Check(TokenType.Identifier)) return ParseAssignmentOrCall();

        // Skip unknown tokens
        if (!Check(TokenType.Newline) && !Check(TokenType.Eof))
        {
            Advance();
        }
        return null;
    }

    private LetStatement ParseLetStatement()
    {
        var token = Advance(); // Consume LET
        var stmt = new LetStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;

        if (Check(TokenType.LeftParen) || Check(TokenType.LeftBracket))
        {
            stmt.ArrayIndices = ParseArrayIndices();
        }

        Expect(TokenType.Equal, "Expected '='");
        stmt.Value = ParseExpression();

        return stmt;
    }

    private StatementNode ParseAssignmentOrCall()
    {
        var token = Current();
        var name = Advance().Value; // Consume identifier

        // Check for device property write: device.Property = value
        if (Check(TokenType.Identifier) && Previous().Value.Equals("device", StringComparison.OrdinalIgnoreCase))
        {
            // Actually this needs different handling - let's check for dot notation pattern
        }

        // Check for array assignment
        if (Check(TokenType.LeftParen) || Check(TokenType.LeftBracket))
        {
            var indices = ParseArrayIndices();
            Expect(TokenType.Equal, "Expected '='");
            return new LetStatement
            {
                Line = token.Line,
                Column = token.Column,
                VariableName = name,
                ArrayIndices = indices,
                Value = ParseExpression()
            };
        }

        // Check for assignment
        if (Check(TokenType.Equal))
        {
            Advance();
            return new LetStatement
            {
                Line = token.Line,
                Column = token.Column,
                VariableName = name,
                Value = ParseExpression()
            };
        }

        // Otherwise it's a subroutine call
        var args = new List<ExpressionNode>();
        if (!Check(TokenType.Newline) && !Check(TokenType.Colon) && !Check(TokenType.Eof))
        {
            args.Add(ParseExpression());
            while (Check(TokenType.Comma))
            {
                Advance();
                args.Add(ParseExpression());
            }
        }

        return new CallStatement
        {
            Line = token.Line,
            Column = token.Column,
            SubName = name,
            Arguments = { }
        };
    }

    private PrintStatement ParsePrintStatement()
    {
        var token = Advance(); // Consume PRINT
        var stmt = new PrintStatement { Line = token.Line, Column = token.Column };

        while (!Check(TokenType.Newline) && !Check(TokenType.Colon) && !Check(TokenType.Eof))
        {
            if (Check(TokenType.Semicolon))
            {
                Advance();
                if (Check(TokenType.Newline) || Check(TokenType.Colon) || Check(TokenType.Eof))
                {
                    stmt.NoNewline = true;
                    break;
                }
                continue;
            }

            if (Check(TokenType.Comma))
            {
                Advance();
                continue;
            }

            stmt.Expressions.Add(ParseExpression());
        }

        return stmt;
    }

    private InputStatement ParseInputStatement()
    {
        var token = Advance(); // Consume INPUT
        var stmt = new InputStatement { Line = token.Line, Column = token.Column };

        // Check for optional prompt string
        if (Check(TokenType.String))
        {
            stmt.Prompt = Advance().Value;
            if (Check(TokenType.Semicolon) || Check(TokenType.Comma))
            {
                Advance();
            }
        }

        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;

        return stmt;
    }

    private IfStatement ParseIfStatement()
    {
        var token = Advance(); // Consume IF
        var stmt = new IfStatement { Line = token.Line, Column = token.Column };

        stmt.Condition = ParseExpression();
        Expect(TokenType.Then, "Expected THEN");

        // Check if multi-line or single-line IF
        if (Check(TokenType.Newline) || Check(TokenType.Eof))
        {
            stmt.IsMultiLine = true;
            SkipNewlines();

            // Parse THEN block
            while (!Check(TokenType.Else) && !Check(TokenType.ElseIf) &&
                   !Check(TokenType.EndIf) && !Check(TokenType.Eof))
            {
                // Skip line numbers in multi-line if
                if (Check(TokenType.LineNumber)) Advance();

                var bodyStmt = ParseStatement();
                if (bodyStmt != null)
                {
                    stmt.ThenBranch.Add(bodyStmt);
                }

                while (Check(TokenType.Colon))
                {
                    Advance();
                    var nextStmt = ParseStatement();
                    if (nextStmt != null) stmt.ThenBranch.Add(nextStmt);
                }

                SkipNewlines();
            }

            // Parse ELSE block
            if (Check(TokenType.Else))
            {
                Advance();
                SkipNewlines();

                while (!Check(TokenType.EndIf) && !Check(TokenType.Eof))
                {
                    if (Check(TokenType.LineNumber)) Advance();

                    var bodyStmt = ParseStatement();
                    if (bodyStmt != null)
                    {
                        stmt.ElseBranch.Add(bodyStmt);
                    }

                    while (Check(TokenType.Colon))
                    {
                        Advance();
                        var nextStmt = ParseStatement();
                        if (nextStmt != null) stmt.ElseBranch.Add(nextStmt);
                    }

                    SkipNewlines();
                }
            }

            if (Check(TokenType.EndIf))
            {
                Advance();
            }
        }
        else
        {
            // Single-line IF
            var thenStmt = ParseStatement();
            if (thenStmt != null)
            {
                stmt.ThenBranch.Add(thenStmt);
            }

            if (Check(TokenType.Else))
            {
                Advance();
                var elseStmt = ParseStatement();
                if (elseStmt != null)
                {
                    stmt.ElseBranch.Add(elseStmt);
                }
            }
        }

        return stmt;
    }

    private ForStatement ParseForStatement()
    {
        var token = Advance(); // Consume FOR
        var stmt = new ForStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;

        Expect(TokenType.Equal, "Expected '='");
        stmt.StartValue = ParseExpression();

        Expect(TokenType.To, "Expected TO");
        stmt.EndValue = ParseExpression();

        if (Check(TokenType.Step))
        {
            Advance();
            stmt.StepValue = ParseExpression();
        }

        SkipNewlines();

        // Parse loop body until NEXT
        while (!Check(TokenType.Next) && !Check(TokenType.Eof))
        {
            if (Check(TokenType.LineNumber)) Advance();

            var bodyStmt = ParseStatement();
            if (bodyStmt != null)
            {
                stmt.Body.Add(bodyStmt);
            }

            while (Check(TokenType.Colon))
            {
                Advance();
                var nextStmt = ParseStatement();
                if (nextStmt != null) stmt.Body.Add(nextStmt);
            }

            SkipNewlines();
        }

        if (Check(TokenType.Next))
        {
            Advance();
            // Optionally consume variable name
            if (Check(TokenType.Identifier))
            {
                Advance();
            }
        }

        return stmt;
    }

    private WhileStatement ParseWhileStatement()
    {
        var token = Advance(); // Consume WHILE
        var stmt = new WhileStatement { Line = token.Line, Column = token.Column };

        stmt.Condition = ParseExpression();
        SkipNewlines();

        // Parse loop body until WEND
        while (!Check(TokenType.Wend) && !Check(TokenType.Eof))
        {
            if (Check(TokenType.LineNumber)) Advance();

            var bodyStmt = ParseStatement();
            if (bodyStmt != null)
            {
                stmt.Body.Add(bodyStmt);
            }

            while (Check(TokenType.Colon))
            {
                Advance();
                var nextStmt = ParseStatement();
                if (nextStmt != null) stmt.Body.Add(nextStmt);
            }

            SkipNewlines();
        }

        if (Check(TokenType.Wend))
        {
            Advance();
        }

        return stmt;
    }

    private DoLoopStatement ParseDoLoopStatement()
    {
        var token = Advance(); // Consume DO
        var stmt = new DoLoopStatement { Line = token.Line, Column = token.Column };

        // Check for DO WHILE or DO UNTIL at start
        if (Check(TokenType.While))
        {
            Advance();
            stmt.WhileCondition = ParseExpression();
            stmt.ConditionAtStart = true;
        }
        else if (Check(TokenType.Until))
        {
            Advance();
            stmt.UntilCondition = ParseExpression();
            stmt.ConditionAtStart = true;
        }

        SkipNewlines();

        // Parse loop body until LOOP
        while (!Check(TokenType.Loop) && !Check(TokenType.Eof))
        {
            if (Check(TokenType.LineNumber)) Advance();

            var bodyStmt = ParseStatement();
            if (bodyStmt != null)
            {
                stmt.Body.Add(bodyStmt);
            }

            while (Check(TokenType.Colon))
            {
                Advance();
                var nextStmt = ParseStatement();
                if (nextStmt != null) stmt.Body.Add(nextStmt);
            }

            SkipNewlines();
        }

        if (Check(TokenType.Loop))
        {
            Advance();

            // Check for LOOP WHILE or LOOP UNTIL at end
            if (Check(TokenType.While))
            {
                Advance();
                stmt.WhileCondition = ParseExpression();
            }
            else if (Check(TokenType.Until))
            {
                Advance();
                stmt.UntilCondition = ParseExpression();
            }
        }

        return stmt;
    }

    private GotoStatement ParseGotoStatement()
    {
        var token = Advance(); // Consume GOTO
        var lineToken = Expect(TokenType.Number, "Expected line number");
        return new GotoStatement
        {
            Line = token.Line,
            Column = token.Column,
            TargetLine = (int)double.Parse(lineToken.Value)
        };
    }

    private GosubStatement ParseGosubStatement()
    {
        var token = Advance(); // Consume GOSUB
        var lineToken = Expect(TokenType.Number, "Expected line number");
        return new GosubStatement
        {
            Line = token.Line,
            Column = token.Column,
            TargetLine = (int)double.Parse(lineToken.Value)
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        var token = Advance(); // Consume RETURN
        return new ReturnStatement { Line = token.Line, Column = token.Column };
    }

    private EndStatement ParseEndStatement()
    {
        var token = Advance(); // Consume END
        return new EndStatement { Line = token.Line, Column = token.Column };
    }

    private DimStatement ParseDimStatement()
    {
        var token = Advance(); // Consume DIM
        var stmt = new DimStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;

        if (Check(TokenType.LeftParen) || Check(TokenType.LeftBracket))
        {
            var openToken = Advance();
            var closeType = openToken.Type == TokenType.LeftParen
                ? TokenType.RightParen
                : TokenType.RightBracket;

            do
            {
                if (Check(TokenType.Comma)) Advance();
                var dimToken = Expect(TokenType.Number, "Expected dimension size");
                stmt.Dimensions.Add((int)double.Parse(dimToken.Value));
            } while (Check(TokenType.Comma));

            Expect(closeType, "Expected closing bracket");
        }

        return stmt;
    }

    private SubDefinition ParseSubDefinition()
    {
        var token = Advance(); // Consume SUB
        var stmt = new SubDefinition { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected subroutine name");
        stmt.Name = nameToken.Value;

        // Parse parameters
        if (Check(TokenType.LeftParen))
        {
            Advance();
            while (!Check(TokenType.RightParen) && !Check(TokenType.Eof))
            {
                var paramToken = Expect(TokenType.Identifier, "Expected parameter name");
                stmt.Parameters.Add(paramToken.Value);
                if (Check(TokenType.Comma)) Advance();
            }
            Expect(TokenType.RightParen, "Expected ')'");
        }

        SkipNewlines();

        // Parse body until END SUB
        while (!Check(TokenType.EndSub) && !Check(TokenType.Eof))
        {
            if (Check(TokenType.LineNumber)) Advance();

            var bodyStmt = ParseStatement();
            if (bodyStmt != null)
            {
                stmt.Body.Add(bodyStmt);
            }

            while (Check(TokenType.Colon))
            {
                Advance();
                var nextStmt = ParseStatement();
                if (nextStmt != null) stmt.Body.Add(nextStmt);
            }

            SkipNewlines();
        }

        if (Check(TokenType.EndSub))
        {
            Advance();
        }

        return stmt;
    }

    private CallStatement ParseCallStatement()
    {
        var token = Advance(); // Consume CALL
        var stmt = new CallStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected subroutine name");
        stmt.SubName = nameToken.Value;

        // Parse arguments
        if (Check(TokenType.LeftParen))
        {
            Advance();
            while (!Check(TokenType.RightParen) && !Check(TokenType.Eof))
            {
                stmt.Arguments.Add(ParseExpression());
                if (Check(TokenType.Comma)) Advance();
            }
            Expect(TokenType.RightParen, "Expected ')'");
        }

        return stmt;
    }

    private SleepStatement ParseSleepStatement()
    {
        var token = Advance(); // Consume SLEEP
        return new SleepStatement
        {
            Line = token.Line,
            Column = token.Column,
            Duration = ParseExpression()
        };
    }

    private YieldStatement ParseYieldStatement()
    {
        var token = Advance(); // Consume YIELD
        return new YieldStatement { Line = token.Line, Column = token.Column };
    }

    private AliasStatement ParseAliasStatement()
    {
        var token = Advance(); // Consume ALIAS
        var stmt = new AliasStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected alias name");
        stmt.AliasName = nameToken.Value;

        // Parse device specification (e.g., d0, db)
        var deviceToken = Expect(TokenType.Identifier, "Expected device specifier");
        stmt.DeviceSpec = deviceToken.Value;

        return stmt;
    }

    private DefineStatement ParseDefineStatement()
    {
        var token = Advance(); // Consume DEFINE
        var stmt = new DefineStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected constant name");
        stmt.ConstantName = nameToken.Value;

        var valueToken = Expect(TokenType.Number, "Expected numeric value");
        stmt.Value = double.Parse(valueToken.Value);

        return stmt;
    }

    private List<ExpressionNode> ParseArrayIndices()
    {
        var indices = new List<ExpressionNode>();
        var openType = Current().Type;
        Advance(); // Consume '(' or '['

        do
        {
            if (Check(TokenType.Comma)) Advance();
            indices.Add(ParseExpression());
        } while (Check(TokenType.Comma));

        var closeType = openType == TokenType.LeftParen
            ? TokenType.RightParen
            : TokenType.RightBracket;
        Expect(closeType, "Expected closing bracket");

        return indices;
    }

    // Expression parsing with precedence climbing
    private ExpressionNode ParseExpression()
    {
        return ParseOr();
    }

    private ExpressionNode ParseOr()
    {
        var left = ParseAnd();

        while (Check(TokenType.Or))
        {
            Advance();
            var right = ParseAnd();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = BinaryOperator.Or,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseAnd()
    {
        var left = ParseNot();

        while (Check(TokenType.And))
        {
            Advance();
            var right = ParseNot();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = BinaryOperator.And,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseNot()
    {
        if (Check(TokenType.Not))
        {
            var token = Advance();
            return new UnaryExpression
            {
                Line = token.Line,
                Column = token.Column,
                Operator = UnaryOperator.Not,
                Operand = ParseNot()
            };
        }

        return ParseComparison();
    }

    private ExpressionNode ParseComparison()
    {
        var left = ParseAddSub();

        while (Check(TokenType.Equal) || Check(TokenType.NotEqual) ||
               Check(TokenType.LessThan) || Check(TokenType.GreaterThan) ||
               Check(TokenType.LessEqual) || Check(TokenType.GreaterEqual))
        {
            var opToken = Advance();
            var op = opToken.Type switch
            {
                TokenType.Equal => BinaryOperator.Equal,
                TokenType.NotEqual => BinaryOperator.NotEqual,
                TokenType.LessThan => BinaryOperator.LessThan,
                TokenType.GreaterThan => BinaryOperator.GreaterThan,
                TokenType.LessEqual => BinaryOperator.LessEqual,
                TokenType.GreaterEqual => BinaryOperator.GreaterEqual,
                _ => throw new ParserException("Invalid comparison operator", opToken.Line, opToken.Column)
            };
            var right = ParseAddSub();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = op,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseAddSub()
    {
        var left = ParseMulDivMod();

        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            var opToken = Advance();
            var op = opToken.Type == TokenType.Plus
                ? BinaryOperator.Add
                : BinaryOperator.Subtract;
            var right = ParseMulDivMod();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = op,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseMulDivMod()
    {
        var left = ParsePower();

        while (Check(TokenType.Multiply) || Check(TokenType.Divide) || Check(TokenType.Mod))
        {
            var opToken = Advance();
            var op = opToken.Type switch
            {
                TokenType.Multiply => BinaryOperator.Multiply,
                TokenType.Divide => BinaryOperator.Divide,
                TokenType.Mod => BinaryOperator.Modulo,
                _ => throw new ParserException("Invalid operator", opToken.Line, opToken.Column)
            };
            var right = ParsePower();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = op,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParsePower()
    {
        var left = ParseUnary();

        if (Check(TokenType.Power))
        {
            Advance();
            var right = ParsePower(); // Right associative
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = BinaryOperator.Power,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseUnary()
    {
        if (Check(TokenType.Minus))
        {
            var token = Advance();
            return new UnaryExpression
            {
                Line = token.Line,
                Column = token.Column,
                Operator = UnaryOperator.Negate,
                Operand = ParseUnary()
            };
        }

        if (Check(TokenType.Plus))
        {
            Advance(); // Unary plus is a no-op
            return ParseUnary();
        }

        return ParsePrimary();
    }

    private ExpressionNode ParsePrimary()
    {
        var token = Current();

        if (Check(TokenType.Number))
        {
            Advance();
            return new NumberLiteral
            {
                Line = token.Line,
                Column = token.Column,
                Value = double.Parse(token.Value)
            };
        }

        if (Check(TokenType.String))
        {
            Advance();
            return new StringLiteral
            {
                Line = token.Line,
                Column = token.Column,
                Value = token.Value
            };
        }

        if (Check(TokenType.Identifier))
        {
            Advance();
            var name = token.Value;

            // Check for function call or array access
            if (Check(TokenType.LeftParen))
            {
                Advance();
                var args = new List<ExpressionNode>();

                if (!Check(TokenType.RightParen))
                {
                    args.Add(ParseExpression());
                    while (Check(TokenType.Comma))
                    {
                        Advance();
                        args.Add(ParseExpression());
                    }
                }

                Expect(TokenType.RightParen, "Expected ')'");

                // Check if it's a known function
                if (IsBuiltInFunction(name))
                {
                    return new FunctionCallExpression
                    {
                        Line = token.Line,
                        Column = token.Column,
                        FunctionName = name,
                        Arguments = { }
                    }.Also(f => f.Arguments.AddRange(args));
                }

                // Otherwise treat as array access
                return new VariableExpression
                {
                    Line = token.Line,
                    Column = token.Column,
                    Name = name,
                    ArrayIndices = args
                };
            }

            // Check for array access with brackets
            if (Check(TokenType.LeftBracket))
            {
                var indices = ParseArrayIndices();
                return new VariableExpression
                {
                    Line = token.Line,
                    Column = token.Column,
                    Name = name,
                    ArrayIndices = indices
                };
            }

            return new VariableExpression
            {
                Line = token.Line,
                Column = token.Column,
                Name = name
            };
        }

        if (Check(TokenType.LeftParen))
        {
            Advance();
            var expr = ParseExpression();
            Expect(TokenType.RightParen, "Expected ')'");
            return expr;
        }

        throw new ParserException($"Unexpected token: {token.Type}", token.Line, token.Column);
    }

    private static bool IsBuiltInFunction(string name)
    {
        var upper = name.ToUpperInvariant();
        return upper is "ABS" or "SIN" or "COS" or "TAN" or "ASIN" or "ACOS" or "ATAN" or "ATAN2"
            or "SQRT" or "EXP" or "LOG" or "LOG10" or "CEIL" or "FLOOR" or "ROUND" or "TRUNC"
            or "MIN" or "MAX" or "RND" or "SGN" or "INT" or "FIX" or "SQR" or "ATN"
            or "RAND" or "POW";
    }

    // Helper methods
    private bool Check(TokenType type) => !IsAtEnd() && Current().Type == type;

    private bool IsAtEnd() => Current().Type == TokenType.Eof;

    private Token Current() => _tokens[_position];

    private Token Previous() => _tokens[_position - 1];

    private Token Advance()
    {
        if (!IsAtEnd()) _position++;
        return Previous();
    }

    private Token Expect(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        var token = Current();
        throw new ParserException($"{message}, got {token.Type}", token.Line, token.Column);
    }

    private void SkipNewlines()
    {
        while (Check(TokenType.Newline))
        {
            Advance();
        }
    }
}

public static class Extensions
{
    public static T Also<T>(this T obj, Action<T> action)
    {
        action(obj);
        return obj;
    }
}

public class ParserException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParserException(string message, int line, int column)
        : base($"{message} at line {line}, column {column}")
    {
        Line = line;
        Column = column;
    }
}
