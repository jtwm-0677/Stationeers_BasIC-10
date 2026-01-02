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
        if (Check(TokenType.Var)) return ParseVarStatement();
        if (Check(TokenType.Const)) return ParseConstStatement();
        if (Check(TokenType.Print)) return ParsePrintStatement();
        if (Check(TokenType.Input)) return ParseInputStatement();
        if (Check(TokenType.If)) return ParseIfStatement();
        if (Check(TokenType.For)) return ParseForStatement();
        if (Check(TokenType.While)) return ParseWhileStatement();
        if (Check(TokenType.Do)) return ParseDoLoopStatement();
        if (Check(TokenType.Select)) return ParseSelectStatement();
        if (Check(TokenType.On)) return ParseOnStatement();
        if (Check(TokenType.Goto)) return ParseGotoStatement();
        if (Check(TokenType.Gosub)) return ParseGosubStatement();
        if (Check(TokenType.Return)) return ParseReturnStatement();
        if (Check(TokenType.Break)) return ParseBreakStatement();
        if (Check(TokenType.Continue)) return ParseContinueStatement();
        if (Check(TokenType.End)) return ParseEndStatement();
        if (Check(TokenType.Dim)) return ParseDimStatement();
        if (Check(TokenType.Sub)) return ParseSubDefinition();
        if (Check(TokenType.Function)) return ParseFunctionDefinition();
        if (Check(TokenType.Call)) return ParseCallStatement();
        if (Check(TokenType.Sleep)) return ParseSleepStatement();
        if (Check(TokenType.Yield)) return ParseYieldStatement();
        if (Check(TokenType.Alias)) return ParseAliasStatement();
        if (Check(TokenType.Define)) return ParseDefineStatement();
        if (Check(TokenType.Push)) return ParsePushStatement();
        if (Check(TokenType.Pop)) return ParsePopStatement();
        if (Check(TokenType.Peek)) return ParsePeekStatement();
        if (Check(TokenType.Data)) return ParseDataStatement();
        if (Check(TokenType.Read)) return ParseReadStatement();
        if (Check(TokenType.Restore)) return ParseRestoreStatement();
        if (Check(TokenType.Comment) || Check(TokenType.MetaComment)) return ParseCommentStatement();
        if (Check(TokenType.Identifier)) return ParseAssignmentOrCall();

        // Handle standalone increment/decrement statements (++i, --i, i++, i--)
        if (Check(TokenType.Increment) || Check(TokenType.Decrement))
        {
            return ParseExpressionStatement();
        }

        // Skip unknown tokens
        if (!Check(TokenType.Newline) && !Check(TokenType.Eof))
        {
            Advance();
        }
        return null;
    }

    private CommentStatement ParseCommentStatement()
    {
        var token = Advance(); // Consume Comment or MetaComment
        return new CommentStatement
        {
            Line = token.Line,
            Column = token.Column,
            Text = token.Value,
            IsMetaComment = token.Type == TokenType.MetaComment
        };
    }

    /// <summary>
    /// Parses a standalone expression statement (e.g., ++i, --i, i++, i--).
    /// </summary>
    private ExpressionStatement ParseExpressionStatement()
    {
        var token = Current();
        var expr = ParseExpression();
        return new ExpressionStatement
        {
            Line = token.Line,
            Column = token.Column,
            Expression = expr
        };
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

        // Check for label definition: identifier followed by colon
        if (Check(TokenType.Colon))
        {
            Advance(); // Consume ':'
            return new LabelStatement
            {
                Line = token.Line,
                Column = token.Column,
                Name = name
            };
        }

        // Check for device property write: device.Property = value
        // or device.Slot[n].Property = value syntax
        if (Check(TokenType.Dot))
        {
            Advance(); // Consume '.'
            var propertyToken = ExpectPropertyName("Expected property name after '.'");
            var propertyName = propertyToken.Value;

            // Check for .Slot[n].Property = value syntax
            if (propertyName.Equals("Slot", StringComparison.OrdinalIgnoreCase) && Check(TokenType.LeftBracket))
            {
                Advance(); // Consume '['
                var slotIndex = ParseExpression();
                Expect(TokenType.RightBracket, "Expected ']'");
                Expect(TokenType.Dot, "Expected '.' after slot index");
                var slotPropToken = ExpectPropertyName("Expected property name after slot index");
                Expect(TokenType.Equal, "Expected '='");
                var slotValue = ParseExpression();

                return new DeviceSlotWriteStatement
                {
                    Line = token.Line,
                    Column = token.Column,
                    DeviceName = name,
                    SlotIndex = slotIndex,
                    PropertyName = slotPropToken.Value,
                    Value = slotValue
                };
            }

            // Check for .Memory[n] = value syntax (external memory write)
            if (propertyName.Equals("Memory", StringComparison.OrdinalIgnoreCase) && Check(TokenType.LeftBracket))
            {
                Advance(); // Consume '['
                var address = ParseExpression();
                Expect(TokenType.RightBracket, "Expected ']'");
                Expect(TokenType.Equal, "Expected '='");
                var memValue = ParseExpression();

                return new ExternalMemoryWriteStatement
                {
                    Line = token.Line,
                    Column = token.Column,
                    DeviceName = name,
                    Address = address,
                    Value = memValue
                };
            }

            Expect(TokenType.Equal, "Expected '=' after property name");
            var value = ParseExpression();

            return new DeviceWriteStatement
            {
                Line = token.Line,
                Column = token.Column,
                DeviceName = name,
                PropertyName = propertyName,
                Value = value
            };
        }

        // Check for device slot access: device[slot].Property = value
        if (Check(TokenType.LeftBracket))
        {
            Advance(); // Consume '['
            var slotIndex = ParseExpression();
            Expect(TokenType.RightBracket, "Expected ']'");

            if (Check(TokenType.Dot))
            {
                Advance(); // Consume '.'
                var propertyToken = ExpectPropertyName("Expected property name");
                var propertyName = propertyToken.Value;

                Expect(TokenType.Equal, "Expected '='");
                var value = ParseExpression();

                return new DeviceSlotWriteStatement
                {
                    Line = token.Line,
                    Column = token.Column,
                    DeviceName = name,
                    SlotIndex = slotIndex,
                    PropertyName = propertyName,
                    Value = value
                };
            }

            // Array assignment: arr[index] = value
            Expect(TokenType.Equal, "Expected '='");
            return new LetStatement
            {
                Line = token.Line,
                Column = token.Column,
                VariableName = name,
                ArrayIndices = new List<ExpressionNode> { slotIndex },
                Value = ParseExpression()
            };
        }

        // Check for BATCHWRITE statement: BATCHWRITE(hash, Property, value)
        if (name.Equals("BATCHWRITE", StringComparison.OrdinalIgnoreCase) && Check(TokenType.LeftParen))
        {
            return ParseBatchWriteStatement(token);
        }

        // Check for BATCHWRITE_NAMED statement: BATCHWRITE_NAMED(hash, nameHash, Property, value)
        if (name.Equals("BATCHWRITE_NAMED", StringComparison.OrdinalIgnoreCase) && Check(TokenType.LeftParen))
        {
            return ParseBatchWriteNamedStatement(token);
        }

        // Check for array assignment with parentheses
        if (Check(TokenType.LeftParen))
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

        // Check for simple assignment
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

        // Check for compound assignments (+=, -=, *=, /=)
        if (Check(TokenType.PlusEqual) || Check(TokenType.MinusEqual) ||
            Check(TokenType.MultiplyEqual) || Check(TokenType.DivideEqual))
        {
            var opToken = Advance();
            var op = opToken.Type switch
            {
                TokenType.PlusEqual => BinaryOperator.Add,
                TokenType.MinusEqual => BinaryOperator.Subtract,
                TokenType.MultiplyEqual => BinaryOperator.Multiply,
                TokenType.DivideEqual => BinaryOperator.Divide,
                _ => throw new ParserException("Invalid compound operator", opToken.Line, opToken.Column)
            };

            var rightValue = ParseExpression();

            // x += 5 becomes x = x + 5
            return new LetStatement
            {
                Line = token.Line,
                Column = token.Column,
                VariableName = name,
                Value = new BinaryExpression
                {
                    Line = token.Line,
                    Column = token.Column,
                    Left = new VariableExpression
                    {
                        Line = token.Line,
                        Column = token.Column,
                        Name = name
                    },
                    Operator = op,
                    Right = rightValue
                }
            };
        }

        // Check for postfix increment/decrement (i++, i--)
        if (Check(TokenType.Increment) || Check(TokenType.Decrement))
        {
            var opToken = Advance();
            var op = opToken.Type == TokenType.Increment
                ? UnaryOperator.PostIncrement
                : UnaryOperator.PostDecrement;

            return new ExpressionStatement
            {
                Line = token.Line,
                Column = token.Column,
                Expression = new UnaryExpression
                {
                    Line = token.Line,
                    Column = token.Column,
                    Operator = op,
                    Operand = new VariableExpression
                    {
                        Line = token.Line,
                        Column = token.Column,
                        Name = name
                    }
                }
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
        var rootStmt = new IfStatement { Line = token.Line, Column = token.Column };

        // Handle IF on its own line - skip newlines before condition
        SkipNewlines();

        rootStmt.Condition = ParseExpression();

        // Handle THEN on its own line - skip newlines before THEN
        SkipNewlines();

        Expect(TokenType.Then, "Expected THEN");

        // Check if multi-line or single-line IF
        // Multi-line: THEN followed by newline, body on subsequent lines, ends with ENDIF
        // Single-line: THEN followed by statement on same line, optional ELSE on same line
        // Hybrid: THEN followed by statement on same line, but ELSEIF/ELSE/ENDIF on subsequent lines
        if (Check(TokenType.Newline) || Check(TokenType.Eof))
        {
            // Pure multi-line: THEN followed by newline
            rootStmt.IsMultiLine = true;
            SkipNewlines();

            // Parse THEN block - multiple statements until ELSE/ELSEIF/ENDIF
            int lastPosition = -1;
            while (!Check(TokenType.Else) && !Check(TokenType.ElseIf) &&
                   !Check(TokenType.EndIf) && !Check(TokenType.Eof))
            {
                // Safeguard: detect if we're stuck in an infinite loop
                if (_position == lastPosition)
                {
                    var currentToken = Current();
                    throw new ParserException(
                        $"Incomplete IF statement - expected ENDIF, ELSE, or ELSEIF but found {currentToken.Type}",
                        currentToken.Line, currentToken.Column);
                }
                lastPosition = _position;

                if (Check(TokenType.LineNumber)) Advance();

                var bodyStmt = ParseStatement();
                if (bodyStmt != null)
                {
                    rootStmt.ThenBranch.Add(bodyStmt);
                }

                while (Check(TokenType.Colon))
                {
                    Advance();
                    var nextStmt = ParseStatement();
                    if (nextStmt != null) rootStmt.ThenBranch.Add(nextStmt);
                }

                SkipNewlines();
            }
        }
        else
        {
            // THEN followed by statement on same line - could be single-line or hybrid
            var thenStmt = ParseStatement();
            if (thenStmt != null)
            {
                rootStmt.ThenBranch.Add(thenStmt);
            }

            // Check if this is truly single-line (ELSE on same line) or hybrid (newline then ELSEIF/ELSE/ENDIF)
            if (Check(TokenType.Else))
            {
                // Single-line with ELSE on same line
                Advance();
                var elseStmt = ParseStatement();
                if (elseStmt != null)
                {
                    rootStmt.ElseBranch.Add(elseStmt);
                }
                // Check for ENDIF on the same line (e.g., "ELSE x = 0 ENDIF")
                if (Check(TokenType.EndIf))
                {
                    Advance();
                }
                return rootStmt;
            }

            // Check for ENDIF right after THEN statement (e.g., "IF x THEN y = 1 ENDIF")
            if (Check(TokenType.EndIf))
            {
                Advance();
                return rootStmt;
            }

            // Check for newline - could be hybrid format
            if (Check(TokenType.Newline))
            {
                SkipNewlines();

                // If we see ELSEIF, ELSE, or ENDIF, this is a hybrid multi-line format
                if (Check(TokenType.ElseIf) || Check(TokenType.Else) || Check(TokenType.EndIf))
                {
                    rootStmt.IsMultiLine = true;
                    // Fall through to ELSEIF/ELSE/ENDIF handling below
                }
                else
                {
                    // Truly single-line IF, we're done
                    return rootStmt;
                }
            }
            else
            {
                // No newline, no ELSE - single-line IF complete
                return rootStmt;
            }
        }

        // At this point we're in multi-line mode (pure or hybrid) - handle ELSEIF/ELSE/ENDIF
        {

            // Parse ELSEIF chains - currentStmt tracks where to add the next else branch
            var currentStmt = rootStmt;
            while (Check(TokenType.ElseIf))
            {
                Advance(); // Consume ELSEIF

                // Handle ELSEIF condition on its own line
                SkipNewlines();

                var elseIfCondition = ParseExpression();

                // Handle THEN on its own line
                SkipNewlines();

                Expect(TokenType.Then, "Expected THEN after ELSEIF condition");

                var elseIfBody = new List<StatementNode>();

                // Check if THEN is followed by statement on same line (hybrid) or newline (pure multi-line)
                if (!Check(TokenType.Newline) && !Check(TokenType.Eof))
                {
                    // Hybrid: statement on same line after THEN
                    var bodyStmt = ParseStatement();
                    if (bodyStmt != null)
                    {
                        elseIfBody.Add(bodyStmt);
                    }
                    SkipNewlines();
                }
                else
                {
                    // Pure multi-line: parse multiple statements
                    SkipNewlines();

                    int elseIfLastPosition = -1;
                    while (!Check(TokenType.Else) && !Check(TokenType.ElseIf) &&
                           !Check(TokenType.EndIf) && !Check(TokenType.Eof))
                    {
                        // Safeguard: detect if we're stuck in an infinite loop
                        if (_position == elseIfLastPosition)
                        {
                            var currentToken = Current();
                            throw new ParserException(
                                $"Incomplete ELSEIF statement - expected ENDIF, ELSE, or ELSEIF but found {currentToken.Type}",
                                currentToken.Line, currentToken.Column);
                        }
                        elseIfLastPosition = _position;

                        if (Check(TokenType.LineNumber)) Advance();

                        var bodyStmt = ParseStatement();
                        if (bodyStmt != null)
                        {
                            elseIfBody.Add(bodyStmt);
                        }

                        while (Check(TokenType.Colon))
                        {
                            Advance();
                            var nextStmt = ParseStatement();
                            if (nextStmt != null) elseIfBody.Add(nextStmt);
                        }

                        SkipNewlines();
                    }
                }

                // Convert ELSEIF to nested IF in the else branch
                var nestedIf = new IfStatement
                {
                    Line = token.Line,
                    Column = token.Column,
                    Condition = elseIfCondition,
                    IsMultiLine = true
                };
                foreach (var item in elseIfBody)
                {
                    nestedIf.ThenBranch.Add(item);
                }

                // Add nested IF to current statement's else branch, then continue chain
                currentStmt.ElseBranch.Add(nestedIf);
                currentStmt = nestedIf;
            }

            // Parse ELSE block - add to the last statement in the chain
            if (Check(TokenType.Else))
            {
                Advance();
                SkipNewlines();

                int elseLastPosition = -1;
                while (!Check(TokenType.EndIf) && !Check(TokenType.Eof))
                {
                    // Safeguard: detect if we're stuck in an infinite loop
                    if (_position == elseLastPosition)
                    {
                        var currentToken = Current();
                        throw new ParserException(
                            $"Incomplete ELSE statement - expected ENDIF but found {currentToken.Type}",
                            currentToken.Line, currentToken.Column);
                    }
                    elseLastPosition = _position;

                    if (Check(TokenType.LineNumber)) Advance();

                    var bodyStmt = ParseStatement();
                    if (bodyStmt != null)
                    {
                        currentStmt.ElseBranch.Add(bodyStmt);
                    }

                    while (Check(TokenType.Colon))
                    {
                        Advance();
                        var nextStmt = ParseStatement();
                        if (nextStmt != null) currentStmt.ElseBranch.Add(nextStmt);
                    }

                    SkipNewlines();
                }
            }

            if (Check(TokenType.EndIf))
            {
                Advance();
            }
            else if (rootStmt.IsMultiLine)
            {
                // Multi-line IF must have ENDIF
                var currentToken = Current();
                throw new ParserException(
                    $"Expected ENDIF to close multi-line IF statement, got {currentToken.Type}",
                    currentToken.Line, currentToken.Column);
            }
        }

        return rootStmt;
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
        int lastPosition = -1;
        while (!Check(TokenType.Next) && !Check(TokenType.Eof))
        {
            // Safeguard: detect if we're stuck in an infinite loop
            if (_position == lastPosition)
            {
                var currentToken = Current();
                throw new ParserException(
                    $"Incomplete FOR statement - expected NEXT but found {currentToken.Type}",
                    currentToken.Line, currentToken.Column);
            }
            lastPosition = _position;

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
        int lastPosition = -1;
        while (!Check(TokenType.Wend) && !Check(TokenType.Eof))
        {
            // Safeguard: detect if we're stuck in an infinite loop
            if (_position == lastPosition)
            {
                var currentToken = Current();
                throw new ParserException(
                    $"Incomplete WHILE statement - expected WEND but found {currentToken.Type}",
                    currentToken.Line, currentToken.Column);
            }
            lastPosition = _position;

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
        int lastPosition = -1;
        while (!Check(TokenType.Loop) && !Check(TokenType.Eof))
        {
            // Safeguard: detect if we're stuck in an infinite loop
            if (_position == lastPosition)
            {
                var currentToken = Current();
                throw new ParserException(
                    $"Incomplete DO statement - expected LOOP but found {currentToken.Type}",
                    currentToken.Line, currentToken.Column);
            }
            lastPosition = _position;

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
        var stmt = new GotoStatement { Line = token.Line, Column = token.Column };

        // Accept either a line number or a label (identifier)
        if (Check(TokenType.Number))
        {
            var lineToken = Advance();
            stmt.TargetLine = (int)double.Parse(lineToken.Value);
        }
        else if (Check(TokenType.Identifier))
        {
            var labelToken = Advance();
            stmt.TargetLabel = labelToken.Value;
        }
        else
        {
            throw new ParserException("Expected line number or label", Current().Line, Current().Column);
        }

        return stmt;
    }

    private GosubStatement ParseGosubStatement()
    {
        var token = Advance(); // Consume GOSUB
        var stmt = new GosubStatement { Line = token.Line, Column = token.Column };

        // Accept either a line number or a label (identifier)
        if (Check(TokenType.Number))
        {
            var lineToken = Advance();
            stmt.TargetLine = (int)double.Parse(lineToken.Value);
        }
        else if (Check(TokenType.Identifier))
        {
            var labelToken = Advance();
            stmt.TargetLabel = labelToken.Value;
        }
        else
        {
            throw new ParserException("Expected line number or label", Current().Line, Current().Column);
        }

        return stmt;
    }

    private StatementNode ParseOnStatement()
    {
        var token = Advance(); // Consume ON

        // Parse the index expression
        var indexExpr = ParseExpression();

        // Expect either GOTO or GOSUB
        if (Check(TokenType.Goto))
        {
            Advance(); // Consume GOTO
            var stmt = new OnGotoStatement
            {
                Line = token.Line,
                Column = token.Column,
                IndexExpression = indexExpr
            };

            // Parse comma-separated list of labels
            do
            {
                if (Check(TokenType.Comma)) Advance(); // Consume comma

                if (Check(TokenType.Identifier))
                {
                    stmt.TargetLabels.Add(Advance().Value);
                }
                else if (Check(TokenType.Number))
                {
                    // Accept line numbers as labels
                    stmt.TargetLabels.Add("_line" + Advance().Value);
                }
                else
                {
                    throw new ParserException("Expected label name", Current().Line, Current().Column);
                }
            } while (Check(TokenType.Comma));

            return stmt;
        }
        else if (Check(TokenType.Gosub))
        {
            Advance(); // Consume GOSUB
            var stmt = new OnGosubStatement
            {
                Line = token.Line,
                Column = token.Column,
                IndexExpression = indexExpr
            };

            // Parse comma-separated list of labels
            do
            {
                if (Check(TokenType.Comma)) Advance(); // Consume comma

                if (Check(TokenType.Identifier))
                {
                    stmt.TargetLabels.Add(Advance().Value);
                }
                else if (Check(TokenType.Number))
                {
                    // Accept line numbers as labels
                    stmt.TargetLabels.Add("_line" + Advance().Value);
                }
                else
                {
                    throw new ParserException("Expected label name", Current().Line, Current().Column);
                }
            } while (Check(TokenType.Comma));

            return stmt;
        }
        else
        {
            throw new ParserException("Expected GOTO or GOSUB after ON expression", Current().Line, Current().Column);
        }
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
                if (Check(TokenType.Eof)) break; // Safeguard against incomplete code
                var dimToken = Expect(TokenType.Number, "Expected dimension size");
                stmt.Dimensions.Add(new NumberLiteral { Value = double.Parse(dimToken.Value), Line = dimToken.Line, Column = dimToken.Column });
            } while (Check(TokenType.Comma));

            Expect(closeType, "Expected closing bracket");
        }

        // Handle optional initializer: DIM x = expression
        if (Check(TokenType.Equal))
        {
            Advance(); // Consume '='
            stmt.InitialValue = ParseExpression();
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
        int lastPosition = -1;
        while (!Check(TokenType.EndSub) && !Check(TokenType.Eof))
        {
            // Safeguard: detect if we're stuck in an infinite loop
            if (_position == lastPosition)
            {
                var currentToken = Current();
                throw new ParserException(
                    $"Incomplete SUB statement - expected END SUB but found {currentToken.Type}",
                    currentToken.Line, currentToken.Column);
            }
            lastPosition = _position;

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
        var token = Advance(); // Consume SLEEP/WAIT

        // Support both SLEEP 1 and WAIT(1) syntax
        bool hasParens = Check(TokenType.LeftParen);
        if (hasParens)
        {
            Advance(); // Consume '('
        }

        var duration = ParseExpression();

        if (hasParens)
        {
            Expect(TokenType.RightParen, "Expected ')' after WAIT/SLEEP duration");
        }

        return new SleepStatement
        {
            Line = token.Line,
            Column = token.Column,
            Duration = duration
        };
    }

    private YieldStatement ParseYieldStatement()
    {
        var token = Advance(); // Consume YIELD

        // Support optional parentheses: YIELD or YIELD()
        if (Check(TokenType.LeftParen))
        {
            Advance(); // Consume '('
            Expect(TokenType.RightParen, "Expected ')' after YIELD(");
        }

        return new YieldStatement { Line = token.Line, Column = token.Column };
    }

    private AliasStatement ParseAliasStatement()
    {
        var token = Advance(); // Consume ALIAS
        var stmt = new AliasStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected alias name");
        stmt.AliasName = nameToken.Value;

        // Check for array subscript: ALIAS name[index] = ...
        if (Check(TokenType.LeftBracket))
        {
            Advance(); // Consume '['
            stmt.AliasIndex = ParseExpression();
            Expect(TokenType.RightBracket, "Expected ']' after alias index");
        }

        // Optional '=' sign
        if (Check(TokenType.Equal))
        {
            Advance();
        }

        // Check for advanced device reference syntax: IC.Device, IC.Pin, IC.ID, IC.Port
        if (Check(TokenType.Identifier) && Current().Value.Equals("IC", StringComparison.OrdinalIgnoreCase))
        {
            stmt.DeviceReference = ParseDeviceReference();
        }
        else
        {
            // Simple device specification (e.g., d0, db, Pin0, THIS)
            var deviceToken = Expect(TokenType.Identifier, "Expected device specifier");
            // THIS is a special keyword meaning the IC chip housing (db)
            if (deviceToken.Value.Equals("THIS", StringComparison.OrdinalIgnoreCase))
            {
                stmt.DeviceSpec = "db";
            }
            else
            {
                stmt.DeviceSpec = deviceToken.Value;
            }
        }

        return stmt;
    }

    private DeviceReference ParseDeviceReference()
    {
        var reference = new DeviceReference();

        Advance(); // Consume 'IC'
        Expect(TokenType.Dot, "Expected '.' after IC");

        // Accept either Identifier or Device keyword (since DEVICE is a reserved keyword)
        Token typeToken;
        if (Check(TokenType.Device))
        {
            typeToken = Advance();
        }
        else
        {
            typeToken = Expect(TokenType.Identifier, "Expected Pin, Device, ID, or Port");
        }
        var refType = typeToken.Value.ToUpperInvariant();

        switch (refType)
        {
            case "PIN":
                reference.Type = DeviceReferenceType.Pin;
                Expect(TokenType.LeftBracket, "Expected '[' after Pin");
                var pinIndexToken = Expect(TokenType.Number, "Expected pin number");
                reference.PinIndex = (int)double.Parse(pinIndexToken.Value);
                Expect(TokenType.RightBracket, "Expected ']'");

                // Check for .Port[n].Channel[m] suffix
                if (Check(TokenType.Dot))
                {
                    Advance();
                    if (Check(TokenType.Identifier) && Current().Value.Equals("Port", StringComparison.OrdinalIgnoreCase))
                    {
                        Advance();
                        reference.HasPort = true;
                        Expect(TokenType.LeftBracket, "Expected '[' after Port");
                        var portToken = Expect(TokenType.Number, "Expected port number");
                        reference.PortIndex = (int)double.Parse(portToken.Value);
                        Expect(TokenType.RightBracket, "Expected ']'");

                        Expect(TokenType.Dot, "Expected '.' after Port[n]");
                        var channelKw = Expect(TokenType.Identifier, "Expected 'Channel'");
                        if (!channelKw.Value.Equals("Channel", StringComparison.OrdinalIgnoreCase))
                            throw new ParserException("Expected 'Channel'", channelKw.Line, channelKw.Column);
                        Expect(TokenType.LeftBracket, "Expected '[' after Channel");
                        var channelToken = Expect(TokenType.Number, "Expected channel number");
                        reference.ChannelIndex = (int)double.Parse(channelToken.Value);
                        Expect(TokenType.RightBracket, "Expected ']'");
                        reference.Type = DeviceReferenceType.Channel;
                    }
                }
                break;

            case "DEVICE":
                Expect(TokenType.LeftBracket, "Expected '[' after Device");
                reference.DeviceHash = ParseExpression();
                Expect(TokenType.RightBracket, "Expected ']'");
                reference.Type = DeviceReferenceType.Device;

                // Check for .Name["deviceName"] or .Name[variable] suffix
                if (Check(TokenType.Dot))
                {
                    Advance();
                    if (Check(TokenType.Identifier) && Current().Value.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        Advance();
                        Expect(TokenType.LeftBracket, "Expected '[' after Name");

                        // Support both static string and dynamic expression
                        if (Check(TokenType.String))
                        {
                            var deviceNameToken = Advance();
                            reference.DeviceName = deviceNameToken.Value;
                        }
                        else
                        {
                            // Dynamic device name (variable or expression)
                            reference.DeviceNameExpression = ParseExpression();
                        }

                        Expect(TokenType.RightBracket, "Expected ']'");
                        reference.Type = DeviceReferenceType.DeviceNamed;
                    }
                }
                break;

            case "ID":
                reference.Type = DeviceReferenceType.ReferenceId;
                Expect(TokenType.LeftBracket, "Expected '[' after ID");
                var refIdToken = Expect(TokenType.Number, "Expected reference ID");
                reference.ReferenceId = (long)double.Parse(refIdToken.Value);
                Expect(TokenType.RightBracket, "Expected ']'");
                break;

            case "PORT":
                reference.Type = DeviceReferenceType.Channel;
                Expect(TokenType.LeftBracket, "Expected '[' after Port");
                var portNumToken = Expect(TokenType.Number, "Expected port number");
                reference.PortIndex = (int)double.Parse(portNumToken.Value);
                Expect(TokenType.RightBracket, "Expected ']'");

                Expect(TokenType.Dot, "Expected '.' after Port[n]");
                var chanKw = Expect(TokenType.Identifier, "Expected 'Channel'");
                if (!chanKw.Value.Equals("Channel", StringComparison.OrdinalIgnoreCase))
                    throw new ParserException("Expected 'Channel'", chanKw.Line, chanKw.Column);
                Expect(TokenType.LeftBracket, "Expected '[' after Channel");
                var chanNumToken = Expect(TokenType.Number, "Expected channel number");
                reference.ChannelIndex = (int)double.Parse(chanNumToken.Value);
                Expect(TokenType.RightBracket, "Expected ']'");
                break;

            default:
                throw new ParserException($"Unknown IC reference type: {refType}", typeToken.Line, typeToken.Column);
        }

        return reference;
    }

    private DefineStatement ParseDefineStatement()
    {
        var token = Advance(); // Consume DEFINE
        var stmt = new DefineStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected constant name");
        stmt.ConstantName = nameToken.Value;

        // Handle optional negative sign
        bool isNegative = false;
        if (Check(TokenType.Minus))
        {
            Advance();
            isNegative = true;
        }

        var valueToken = Expect(TokenType.Number, "Expected numeric value");
        var value = double.Parse(valueToken.Value);
        if (isNegative) value = -value;

        stmt.Value = new NumberLiteral { Value = value, Line = valueToken.Line, Column = valueToken.Column };

        return stmt;
    }

    private VarStatement ParseVarStatement()
    {
        var token = Advance(); // Consume VAR
        var stmt = new VarStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;

        if (Check(TokenType.Equal))
        {
            Advance();
            stmt.InitialValue = ParseExpression();
        }

        return stmt;
    }

    private ConstStatement ParseConstStatement()
    {
        var token = Advance(); // Consume CONST
        var stmt = new ConstStatement { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected constant name");
        stmt.ConstantName = nameToken.Value;

        Expect(TokenType.Equal, "Expected '='");
        stmt.Value = ParseExpression();

        return stmt;
    }

    private BreakStatement ParseBreakStatement()
    {
        var token = Advance(); // Consume BREAK
        return new BreakStatement { Line = token.Line, Column = token.Column };
    }

    private ContinueStatement ParseContinueStatement()
    {
        var token = Advance(); // Consume CONTINUE
        return new ContinueStatement { Line = token.Line, Column = token.Column };
    }

    private PushStatement ParsePushStatement()
    {
        var token = Advance(); // Consume PUSH
        var stmt = new PushStatement { Line = token.Line, Column = token.Column };
        stmt.Value = ParseExpression();
        return stmt;
    }

    private PopStatement ParsePopStatement()
    {
        var token = Advance(); // Consume POP
        var stmt = new PopStatement { Line = token.Line, Column = token.Column };
        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;
        return stmt;
    }

    private PeekStatement ParsePeekStatement()
    {
        var token = Advance(); // Consume PEEK
        var stmt = new PeekStatement { Line = token.Line, Column = token.Column };
        var nameToken = Expect(TokenType.Identifier, "Expected variable name");
        stmt.VariableName = nameToken.Value;
        return stmt;
    }

    /// <summary>
    /// Parses BATCHWRITE(hash, Property, value) statement.
    /// Compiles to: sb hash Property value
    /// </summary>
    private BatchWriteStatement ParseBatchWriteStatement(Token startToken)
    {
        Advance(); // Consume '('

        var hashExpr = ParseExpression();
        Expect(TokenType.Comma, "Expected ',' after device hash");

        // Property can be an identifier or string
        string propertyName;
        if (Check(TokenType.String))
        {
            propertyName = Advance().Value;
        }
        else
        {
            var propToken = Expect(TokenType.Identifier, "Expected property name");
            propertyName = propToken.Value;
        }

        Expect(TokenType.Comma, "Expected ',' after property name");
        var valueExpr = ParseExpression();

        Expect(TokenType.RightParen, "Expected ')'");

        return new BatchWriteStatement
        {
            Line = startToken.Line,
            Column = startToken.Column,
            DeviceHash = hashExpr,
            PropertyName = propertyName,
            Value = valueExpr
        };
    }

    /// <summary>
    /// Parses BATCHWRITE_NAMED(hash, nameHash, Property, value) statement.
    /// Compiles to: sbn hash nameHash Property value
    /// </summary>
    private BatchWriteStatement ParseBatchWriteNamedStatement(Token startToken)
    {
        Advance(); // Consume '('

        var hashExpr = ParseExpression();
        Expect(TokenType.Comma, "Expected ',' after device hash");

        // Name hash - can be expression, string, or identifier
        ExpressionNode nameHashExpr;
        string? nameHashStr = null;
        if (Check(TokenType.String))
        {
            nameHashStr = Advance().Value;
            nameHashExpr = new StringLiteral { Value = nameHashStr, Line = startToken.Line, Column = startToken.Column };
        }
        else
        {
            nameHashExpr = ParseExpression();
            // If it's a literal, extract the string value
            if (nameHashExpr is StringLiteral sl)
                nameHashStr = sl.Value;
            else if (nameHashExpr is NumberLiteral nl)
                nameHashStr = nl.Value.ToString();
        }

        Expect(TokenType.Comma, "Expected ',' after name hash");

        // Property can be an identifier or string
        string propertyName;
        if (Check(TokenType.String))
        {
            propertyName = Advance().Value;
        }
        else
        {
            var propToken = Expect(TokenType.Identifier, "Expected property name");
            propertyName = propToken.Value;
        }

        Expect(TokenType.Comma, "Expected ',' after property name");
        var valueExpr = ParseExpression();

        Expect(TokenType.RightParen, "Expected ')'");

        return new BatchWriteStatement
        {
            Line = startToken.Line,
            Column = startToken.Column,
            DeviceHash = hashExpr,
            PropertyName = propertyName,
            Value = valueExpr,
            NameHash = nameHashStr
        };
    }

    private DataStatement ParseDataStatement()
    {
        var token = Advance(); // Consume DATA
        var stmt = new DataStatement { Line = token.Line, Column = token.Column };

        // Parse comma-separated list of values
        do
        {
            if (Check(TokenType.Comma)) Advance(); // Skip comma

            var value = ParseExpression();
            stmt.Values.Add(value);
        } while (Check(TokenType.Comma));

        return stmt;
    }

    private ReadStatement ParseReadStatement()
    {
        var token = Advance(); // Consume READ
        var stmt = new ReadStatement { Line = token.Line, Column = token.Column };

        // Parse comma-separated list of variable names
        do
        {
            if (Check(TokenType.Comma)) Advance(); // Skip comma

            var nameToken = Expect(TokenType.Identifier, "Expected variable name");
            stmt.VariableNames.Add(nameToken.Value);
        } while (Check(TokenType.Comma));

        return stmt;
    }

    private RestoreStatement ParseRestoreStatement()
    {
        var token = Advance(); // Consume RESTORE
        return new RestoreStatement { Line = token.Line, Column = token.Column };
    }

    private SelectStatement ParseSelectStatement()
    {
        var token = Advance(); // Consume SELECT
        var stmt = new SelectStatement { Line = token.Line, Column = token.Column };

        // Optional CASE keyword after SELECT
        if (Check(TokenType.Case))
        {
            Advance();
        }

        stmt.TestExpression = ParseExpression();
        SkipNewlines();

        // Parse CASE clauses
        while (Check(TokenType.Case))
        {
            Advance(); // Consume CASE

            // Check for CASE ELSE (alternative to DEFAULT)
            if (Check(TokenType.Else))
            {
                Advance(); // Consume ELSE
                if (Check(TokenType.Colon))
                {
                    Advance();
                }
                SkipNewlines();

                // Parse default body until END SELECT
                while (!Check(TokenType.EndSelect) && !Check(TokenType.Eof))
                {
                    var bodyStmt = ParseStatement();
                    if (bodyStmt != null)
                    {
                        stmt.DefaultBody.Add(bodyStmt);
                    }
                    SkipNewlines();
                }
                break; // CASE ELSE must be last, exit the loop
            }

            var caseClause = new CaseClause();

            // Parse case values (comma-separated)
            caseClause.Values.Add(ParseExpression());
            while (Check(TokenType.Comma))
            {
                Advance();
                caseClause.Values.Add(ParseExpression());
            }

            // Optional colon after case values
            if (Check(TokenType.Colon))
            {
                Advance();
            }

            SkipNewlines();

            // Parse case body until next CASE, DEFAULT, or END SELECT
            while (!Check(TokenType.Case) && !Check(TokenType.Default) &&
                   !Check(TokenType.EndSelect) && !Check(TokenType.Eof))
            {
                var bodyStmt = ParseStatement();
                if (bodyStmt != null)
                {
                    caseClause.Body.Add(bodyStmt);
                }
                SkipNewlines();
            }

            stmt.Cases.Add(caseClause);
        }

        // Parse DEFAULT clause if present (alternative to CASE ELSE)
        if (Check(TokenType.Default))
        {
            Advance();
            if (Check(TokenType.Colon))
            {
                Advance();
            }
            SkipNewlines();

            while (!Check(TokenType.EndSelect) && !Check(TokenType.Eof))
            {
                var bodyStmt = ParseStatement();
                if (bodyStmt != null)
                {
                    stmt.DefaultBody.Add(bodyStmt);
                }
                SkipNewlines();
            }
        }

        // Consume END SELECT
        if (Check(TokenType.EndSelect))
        {
            Advance();
        }

        return stmt;
    }

    private FunctionDefinition ParseFunctionDefinition()
    {
        var token = Advance(); // Consume FUNCTION
        var stmt = new FunctionDefinition { Line = token.Line, Column = token.Column };

        var nameToken = Expect(TokenType.Identifier, "Expected function name");
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

        // Parse body until END FUNCTION
        int lastPosition = -1;
        while (!Check(TokenType.EndFunction) && !Check(TokenType.Eof))
        {
            // Safeguard: detect if we're stuck in an infinite loop
            if (_position == lastPosition)
            {
                var currentToken = Current();
                throw new ParserException(
                    $"Incomplete FUNCTION statement - expected END FUNCTION but found {currentToken.Type}",
                    currentToken.Line, currentToken.Column);
            }
            lastPosition = _position;

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

        if (Check(TokenType.EndFunction))
        {
            Advance();
        }

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
            if (Check(TokenType.Eof)) break; // Safeguard against incomplete code
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
        var left = ParseBitwiseOr();

        while (Check(TokenType.And))
        {
            Advance();
            var right = ParseBitwiseOr();
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

    private ExpressionNode ParseBitwiseOr()
    {
        var left = ParseBitwiseXor();

        while (Check(TokenType.BitOr) || Check(TokenType.Pipe))
        {
            Advance();
            var right = ParseBitwiseXor();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = BinaryOperator.BitOr,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseBitwiseXor()
    {
        var left = ParseBitwiseAnd();

        while (Check(TokenType.BitXor))
        {
            Advance();
            var right = ParseBitwiseAnd();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = BinaryOperator.BitXor,
                Right = right
            };
        }

        return left;
    }

    private ExpressionNode ParseBitwiseAnd()
    {
        var left = ParseNot();

        while (Check(TokenType.BitAnd) || Check(TokenType.Ampersand))
        {
            Advance();
            var right = ParseNot();
            left = new BinaryExpression
            {
                Line = left.Line,
                Column = left.Column,
                Left = left,
                Operator = BinaryOperator.BitAnd,
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
        var left = ParseShift();

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
            var right = ParseShift();
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

    private ExpressionNode ParseShift()
    {
        var left = ParseAddSub();

        while (Check(TokenType.ShiftLeft) || Check(TokenType.ShiftRight) ||
               Check(TokenType.Shl) || Check(TokenType.Shr))
        {
            var opToken = Advance();
            var op = (opToken.Type == TokenType.ShiftLeft || opToken.Type == TokenType.Shl)
                ? BinaryOperator.ShiftLeft
                : BinaryOperator.ShiftRight;
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

        // Prefix increment (++x)
        if (Check(TokenType.Increment))
        {
            var token = Advance();
            var operand = ParseUnary();
            return new UnaryExpression
            {
                Line = token.Line,
                Column = token.Column,
                Operator = UnaryOperator.PreIncrement,
                Operand = operand
            };
        }

        // Prefix decrement (--x)
        if (Check(TokenType.Decrement))
        {
            var token = Advance();
            var operand = ParseUnary();
            return new UnaryExpression
            {
                Line = token.Line,
                Column = token.Column,
                Operator = UnaryOperator.PreDecrement,
                Operand = operand
            };
        }

        return ParsePostfix();
    }

    private ExpressionNode ParsePostfix()
    {
        var expr = ParsePrimary();

        // Postfix increment (x++)
        if (Check(TokenType.Increment))
        {
            var token = Advance();
            return new UnaryExpression
            {
                Line = token.Line,
                Column = token.Column,
                Operator = UnaryOperator.PostIncrement,
                Operand = expr
            };
        }

        // Postfix decrement (x--)
        if (Check(TokenType.Decrement))
        {
            var token = Advance();
            return new UnaryExpression
            {
                Line = token.Line,
                Column = token.Column,
                Operator = UnaryOperator.PostDecrement,
                Operand = expr
            };
        }

        return expr;
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

        // Boolean literals
        if (Check(TokenType.True))
        {
            Advance();
            return new BooleanLiteral
            {
                Line = token.Line,
                Column = token.Column,
                Value = true
            };
        }

        if (Check(TokenType.False))
        {
            Advance();
            return new BooleanLiteral
            {
                Line = token.Line,
                Column = token.Column,
                Value = false
            };
        }

        // Handle SHL, SHR, and other bitwise keyword functions when used as function calls
        if (Check(TokenType.Shl) || Check(TokenType.Shr) ||
            Check(TokenType.BitAnd) || Check(TokenType.BitOr) || Check(TokenType.BitXor) || Check(TokenType.BitNot))
        {
            var funcToken = Advance();
            var funcName = funcToken.Type switch
            {
                TokenType.Shl => "SHL",
                TokenType.Shr => "SHR",
                TokenType.BitAnd => "BAND",
                TokenType.BitOr => "BOR",
                TokenType.BitXor => "BXOR",
                TokenType.BitNot => "BNOT",
                _ => funcToken.Value.ToUpperInvariant()
            };

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

                return new FunctionCallExpression
                {
                    Line = funcToken.Line,
                    Column = funcToken.Column,
                    FunctionName = funcName,
                    Arguments = { }
                }.Also(f => f.Arguments.AddRange(args));
            }

            throw new ParserException($"Expected '(' after {funcName}", funcToken.Line, funcToken.Column);
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
                Advance(); // Consume '['
                var slotIndex = ParseExpression();
                Expect(TokenType.RightBracket, "Expected ']'");

                // Check for slot property read: device[slot].Property
                if (Check(TokenType.Dot))
                {
                    Advance(); // Consume '.'
                    var propToken = ExpectPropertyName("Expected property name");
                    return new DeviceSlotReadExpression
                    {
                        Line = token.Line,
                        Column = token.Column,
                        DeviceName = name,
                        SlotIndex = slotIndex,
                        PropertyName = propToken.Value
                    };
                }

                // Otherwise it's an array access
                return new VariableExpression
                {
                    Line = token.Line,
                    Column = token.Column,
                    Name = name,
                    ArrayIndices = new List<ExpressionNode> { slotIndex }
                };
            }

            // Check for device property read: device.Property or device.Property.BatchMode
            // Also handles device.Slot[n].Property syntax
            if (Check(TokenType.Dot))
            {
                Advance(); // Consume '.'
                var propToken = ExpectPropertyName("Expected property name");
                var propertyName = propToken.Value;

                // Check for .Slot[n].Property syntax (alternative to device[n].Property)
                if (propertyName.Equals("Slot", StringComparison.OrdinalIgnoreCase) && Check(TokenType.LeftBracket))
                {
                    Advance(); // Consume '['
                    var slotIndex = ParseExpression();
                    Expect(TokenType.RightBracket, "Expected ']'");
                    Expect(TokenType.Dot, "Expected '.' after slot index");
                    var slotPropToken = ExpectPropertyName("Expected property name after slot index");
                    return new DeviceSlotReadExpression
                    {
                        Line = token.Line,
                        Column = token.Column,
                        DeviceName = name,
                        SlotIndex = slotIndex,
                        PropertyName = slotPropToken.Value
                    };
                }

                // Check for .Memory[n] syntax (external memory read)
                if (propertyName.Equals("Memory", StringComparison.OrdinalIgnoreCase) && Check(TokenType.LeftBracket))
                {
                    Advance(); // Consume '['
                    var address = ParseExpression();
                    Expect(TokenType.RightBracket, "Expected ']'");
                    return new ExternalMemoryReadExpression
                    {
                        Line = token.Line,
                        Column = token.Column,
                        DeviceName = name,
                        Address = address
                    };
                }

                // Check for batch mode suffix (.Average, .Sum, .Min, .Max, .Minimum, .Maximum, .Count)
                if (Check(TokenType.Dot))
                {
                    Advance(); // Consume '.'
                    var batchModeToken = Expect(TokenType.Identifier, "Expected batch mode");
                    var batchMode = batchModeToken.Value.ToUpperInvariant();
                    if (batchMode is "AVERAGE" or "SUM" or "MIN" or "MAX" or "MINIMUM" or "MAXIMUM" or "COUNT")
                    {
                        propertyName = $"{propertyName}.{batchModeToken.Value}";
                    }
                    else
                    {
                        throw new ParserException($"Unknown batch mode: {batchModeToken.Value}", batchModeToken.Line, batchModeToken.Column);
                    }
                }

                return new DeviceReadExpression
                {
                    Line = token.Line,
                    Column = token.Column,
                    DeviceName = name,
                    PropertyName = propertyName
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
            or "RAND" or "POW"
            // Device existence checks
            or "SDSE" or "SDNS"
            // Comparison functions
            or "SELECT" or "IIF" or "APPROX" or "SAP" or "SAPZ" or "ISNAN" or "SNAN" or "ISNANORZERO" or "SNAZ"
            // Comparison to zero
            or "SEQZ" or "SNEZ" or "SGTZ" or "SLTZ" or "SGEZ" or "SLEZ"
            // Bitwise functions
            or "BAND" or "BOR" or "BXOR" or "BNOT" or "BNOR" or "BITAND" or "BITOR" or "BITXOR" or "BITNOT" or "BITNOR"
            // Shift functions
            or "SHL" or "SHR" or "SHRA" or "SHIFTL" or "SHIFTR" or "SHIFTRA" or "LSHIFT" or "RSHIFT" or "RSHIFTA"
            // Utility functions
            or "INRANGE" or "LERP" or "CLAMP" or "HASH";
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

    /// <summary>
    /// Expect an identifier or a keyword that can be used as a property name.
    /// Keywords like ON, DATA, READ can also be valid device property names.
    /// </summary>
    private Token ExpectPropertyName(string message)
    {
        var token = Current();

        // Accept identifiers
        if (Check(TokenType.Identifier))
            return Advance();

        // Accept keywords that could be property names (On, Data, Read, etc.)
        // These are valid IC10 device properties that conflict with BASIC keywords
        if (token.Type == TokenType.On ||
            token.Type == TokenType.Data ||
            token.Type == TokenType.Read ||
            token.Type == TokenType.Step ||
            token.Type == TokenType.Input ||
            token.Type == TokenType.Print ||
            token.Type == TokenType.Return ||
            token.Type == TokenType.End ||
            token.Type == TokenType.To ||
            token.Type == TokenType.As ||
            token.Type == TokenType.Or ||
            token.Type == TokenType.And ||
            token.Type == TokenType.Not ||
            token.Type == TokenType.Mod)
        {
            return Advance();
        }

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
