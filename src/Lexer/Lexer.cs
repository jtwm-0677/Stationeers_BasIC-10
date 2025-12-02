namespace BasicToMips.Lexer;

public class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line;
    private int _column;
    private bool _atLineStart;
    private bool _preserveComments;

    private static readonly Dictionary<string, TokenType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LET"] = TokenType.Let,
        ["PRINT"] = TokenType.Print,
        ["INPUT"] = TokenType.Input,
        ["IF"] = TokenType.If,
        ["THEN"] = TokenType.Then,
        ["ELSE"] = TokenType.Else,
        ["ELSEIF"] = TokenType.ElseIf,
        ["ENDIF"] = TokenType.EndIf,
        ["END IF"] = TokenType.EndIf,
        ["FOR"] = TokenType.For,
        ["TO"] = TokenType.To,
        ["STEP"] = TokenType.Step,
        ["NEXT"] = TokenType.Next,
        ["WHILE"] = TokenType.While,
        ["WEND"] = TokenType.Wend,
        ["DO"] = TokenType.Do,
        ["LOOP"] = TokenType.Loop,
        ["UNTIL"] = TokenType.Until,
        ["GOTO"] = TokenType.Goto,
        ["GOSUB"] = TokenType.Gosub,
        ["RETURN"] = TokenType.Return,
        ["END"] = TokenType.End,
        ["REM"] = TokenType.Rem,
        ["DIM"] = TokenType.Dim,
        ["ARRAY"] = TokenType.Dim,  // ARRAY is a synonym for DIM
        ["AS"] = TokenType.As,
        ["INTEGER"] = TokenType.Integer,
        ["SINGLE"] = TokenType.Single,
        ["AND"] = TokenType.And,
        ["OR"] = TokenType.Or,
        ["NOT"] = TokenType.Not,
        ["MOD"] = TokenType.Mod,
        ["DEF"] = TokenType.Def,
        ["FN"] = TokenType.Fn,
        ["SUB"] = TokenType.Sub,
        ["ENDSUB"] = TokenType.EndSub,
        ["END SUB"] = TokenType.EndSub,
        ["FUNCTION"] = TokenType.Function,
        ["ENDFUNCTION"] = TokenType.EndFunction,
        ["END FUNCTION"] = TokenType.EndFunction,
        ["CALL"] = TokenType.Call,
        ["EXIT"] = TokenType.Exit,
        ["SLEEP"] = TokenType.Sleep,
        ["WAIT"] = TokenType.Sleep,  // WAIT is a synonym for SLEEP
        ["YIELD"] = TokenType.Yield,
        ["DEVICE"] = TokenType.Device,
        ["ALIAS"] = TokenType.Alias,
        ["DEFINE"] = TokenType.Define,
        // Additional keywords
        ["VAR"] = TokenType.Var,
        ["CONST"] = TokenType.Const,
        ["BREAK"] = TokenType.Break,
        ["CONTINUE"] = TokenType.Continue,
        ["SELECT"] = TokenType.Select,
        ["CASE"] = TokenType.Case,
        ["DEFAULT"] = TokenType.Default,
        ["ENDSELECT"] = TokenType.EndSelect,
        ["END SELECT"] = TokenType.EndSelect,
        ["PUSH"] = TokenType.Push,
        ["POP"] = TokenType.Pop,
        ["PEEK"] = TokenType.Peek,
        ["INCLUDE"] = TokenType.Include,
        ["ON"] = TokenType.On,
        ["DATA"] = TokenType.Data,
        ["READ"] = TokenType.Read,
        ["RESTORE"] = TokenType.Restore,
        // Bitwise keywords
        ["SHL"] = TokenType.Shl,
        ["SHR"] = TokenType.Shr,
        ["BAND"] = TokenType.BitAnd,
        ["BOR"] = TokenType.BitOr,
        ["BXOR"] = TokenType.BitXor,
        ["XOR"] = TokenType.BitXor,  // XOR is an alias for BXOR
        ["BNOT"] = TokenType.BitNot,
        // Boolean literals
        ["TRUE"] = TokenType.True,
        ["FALSE"] = TokenType.False
    };

    public Lexer(string source, bool preserveComments = true)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _atLineStart = true;
        _preserveComments = preserveComments;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (!IsAtEnd())
        {
            SkipWhitespace();
            if (IsAtEnd()) break;

            var token = ScanToken();
            if (token != null)
            {
                tokens.Add(token);
                _atLineStart = token.Type == TokenType.Newline;
            }
        }

        tokens.Add(new Token(TokenType.Eof, "", _line, _column));
        return tokens;
    }

    private Token? ScanToken()
    {
        var startLine = _line;
        var startColumn = _column;
        var c = Advance();

        // Check for line numbers at start of line
        if (_atLineStart && char.IsDigit(c))
        {
            return ScanLineNumber(c, startLine, startColumn);
        }

        return c switch
        {
            '\n' => HandleNewline(startLine, startColumn),
            '\r' => HandleCarriageReturn(startLine, startColumn),
            '+' => ScanPlus(startLine, startColumn),
            '-' => ScanMinus(startLine, startColumn),
            '*' => ScanMultiply(startLine, startColumn),
            '/' => ScanDivide(startLine, startColumn),
            '%' => new Token(TokenType.Mod, "%", startLine, startColumn),
            '^' => new Token(TokenType.Power, "^", startLine, startColumn),
            '(' => new Token(TokenType.LeftParen, "(", startLine, startColumn),
            ')' => new Token(TokenType.RightParen, ")", startLine, startColumn),
            '[' => new Token(TokenType.LeftBracket, "[", startLine, startColumn),
            ']' => new Token(TokenType.RightBracket, "]", startLine, startColumn),
            ',' => new Token(TokenType.Comma, ",", startLine, startColumn),
            ':' => new Token(TokenType.Colon, ":", startLine, startColumn),
            ';' => new Token(TokenType.Semicolon, ";", startLine, startColumn),
            '.' => new Token(TokenType.Dot, ".", startLine, startColumn),
            '=' => ScanEqual(startLine, startColumn),
            '<' => ScanLessThan(startLine, startColumn),
            '>' => ScanGreaterThan(startLine, startColumn),
            '|' => ScanPipe(startLine, startColumn),
            '&' => ScanAmpersand(startLine, startColumn),
            '~' => new Token(TokenType.Tilde, "~", startLine, startColumn),
            '!' => ScanExclamation(startLine, startColumn),
            '"' => ScanString(startLine, startColumn),
            '#' => ScanIC10Comment(startLine, startColumn),
            _ when char.IsDigit(c) => ScanNumber(c, startLine, startColumn),
            _ when char.IsLetter(c) || c == '_' => ScanIdentifier(c, startLine, startColumn),
            _ => throw new LexerException($"Unexpected character '{c}'", startLine, startColumn)
        };
    }

    private Token HandleNewline(int line, int column)
    {
        _line++;
        _column = 1;
        return new Token(TokenType.Newline, "\\n", line, column);
    }

    private Token? HandleCarriageReturn(int line, int column)
    {
        if (Peek() == '\n')
        {
            Advance();
        }
        _line++;
        _column = 1;
        return new Token(TokenType.Newline, "\\n", line, column);
    }

    private Token ScanEqual(int line, int column)
    {
        // == is equality comparison (same as single = in BASIC context)
        if (Match('=')) return new Token(TokenType.Equal, "==", line, column);
        return new Token(TokenType.Equal, "=", line, column);
    }

    private Token ScanExclamation(int line, int column)
    {
        // != is not-equal (same as <> in BASIC)
        if (Match('=')) return new Token(TokenType.NotEqual, "!=", line, column);
        // Single ! is logical NOT (same as NOT keyword)
        return new Token(TokenType.Not, "!", line, column);
    }

    private Token ScanPlus(int line, int column)
    {
        if (Match('+')) return new Token(TokenType.Increment, "++", line, column);
        if (Match('=')) return new Token(TokenType.PlusEqual, "+=", line, column);
        return new Token(TokenType.Plus, "+", line, column);
    }

    private Token ScanMinus(int line, int column)
    {
        if (Match('-')) return new Token(TokenType.Decrement, "--", line, column);
        if (Match('=')) return new Token(TokenType.MinusEqual, "-=", line, column);
        return new Token(TokenType.Minus, "-", line, column);
    }

    private Token ScanMultiply(int line, int column)
    {
        if (Match('=')) return new Token(TokenType.MultiplyEqual, "*=", line, column);
        return new Token(TokenType.Multiply, "*", line, column);
    }

    private Token ScanDivide(int line, int column)
    {
        if (Match('=')) return new Token(TokenType.DivideEqual, "/=", line, column);
        return new Token(TokenType.Divide, "/", line, column);
    }

    private Token ScanLessThan(int line, int column)
    {
        if (Match('=')) return new Token(TokenType.LessEqual, "<=", line, column);
        if (Match('>')) return new Token(TokenType.NotEqual, "<>", line, column);
        if (Match('<')) return new Token(TokenType.ShiftLeft, "<<", line, column);
        return new Token(TokenType.LessThan, "<", line, column);
    }

    private Token ScanGreaterThan(int line, int column)
    {
        if (Match('=')) return new Token(TokenType.GreaterEqual, ">=", line, column);
        if (Match('>')) return new Token(TokenType.ShiftRight, ">>", line, column);
        return new Token(TokenType.GreaterThan, ">", line, column);
    }

    private Token ScanPipe(int line, int column)
    {
        // || is logical OR (same as OR keyword)
        if (Match('|')) return new Token(TokenType.Or, "||", line, column);
        // Single | is bitwise OR
        return new Token(TokenType.Pipe, "|", line, column);
    }

    private Token ScanAmpersand(int line, int column)
    {
        // && is logical AND (same as AND keyword)
        if (Match('&')) return new Token(TokenType.And, "&&", line, column);
        // Single & is bitwise AND
        return new Token(TokenType.Ampersand, "&", line, column);
    }

    private Token ScanString(int line, int column)
    {
        var sb = new System.Text.StringBuilder();
        while (!IsAtEnd() && Peek() != '"' && Peek() != '\n')
        {
            sb.Append(Advance());
        }

        if (IsAtEnd() || Peek() == '\n')
        {
            throw new LexerException("Unterminated string", line, column);
        }

        Advance(); // Consume closing quote
        return new Token(TokenType.String, sb.ToString(), line, column);
    }

    private Token ScanComment(int line, int column)
    {
        // BASIC ' comment
        var sb = new System.Text.StringBuilder();
        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            sb.Append(Advance());
        }

        if (_preserveComments)
        {
            // Return comment token with the text (prepend # for IC10 output)
            return new Token(TokenType.Comment, sb.ToString().Trim(), line, column);
        }

        // Skip comment - return next token
        if (!IsAtEnd())
        {
            return ScanToken()!;
        }
        return new Token(TokenType.Eof, "", _line, _column);
    }

    private Token ScanIC10Comment(int line, int column)
    {
        // IC10 style comment (# comment)
        var sb = new System.Text.StringBuilder();

        // Check for ##Meta: directive
        bool isMeta = false;
        if (Peek() == '#')
        {
            sb.Append(Advance()); // Second #
            if (_source.Substring(_position).StartsWith("Meta:", StringComparison.OrdinalIgnoreCase))
            {
                isMeta = true;
            }
        }

        // Read rest of comment
        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            sb.Append(Advance());
        }

        if (_preserveComments || isMeta)
        {
            // Return comment/meta token with the text
            var tokenType = isMeta ? TokenType.MetaComment : TokenType.Comment;
            return new Token(tokenType, sb.ToString().Trim(), line, column);
        }

        // Skip comment - return next token
        if (!IsAtEnd())
        {
            return ScanToken()!;
        }
        return new Token(TokenType.Eof, "", _line, _column);
    }

    private Token ScanNumber(char first, int line, int column)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(first);

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        // Check for decimal point
        if (!IsAtEnd() && Peek() == '.' && char.IsDigit(PeekNext()))
        {
            sb.Append(Advance()); // Consume '.'
            while (!IsAtEnd() && char.IsDigit(Peek()))
            {
                sb.Append(Advance());
            }
        }

        return new Token(TokenType.Number, sb.ToString(), line, column);
    }

    private Token ScanLineNumber(char first, int line, int column)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(first);

        while (!IsAtEnd() && char.IsDigit(Peek()))
        {
            sb.Append(Advance());
        }

        return new Token(TokenType.LineNumber, sb.ToString(), line, column);
    }

    private Token ScanIdentifier(char first, int line, int column)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(first);

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_' || Peek() == '$'))
        {
            sb.Append(Advance());
        }

        var text = sb.ToString();
        var upperText = text.ToUpperInvariant();

        // Handle REM specially - rest of line is comment
        if (upperText == "REM")
        {
            while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
            {
                Advance();
            }
            if (!IsAtEnd())
            {
                return ScanToken()!;
            }
            return new Token(TokenType.Eof, "", _line, _column);
        }

        // Check for compound keywords like "END IF"
        if (upperText == "END")
        {
            var savedPos = _position;
            var savedCol = _column;
            SkipWhitespace();
            if (!IsAtEnd() && char.IsLetter(Peek()))
            {
                var nextWord = new System.Text.StringBuilder();
                while (!IsAtEnd() && char.IsLetter(Peek()))
                {
                    nextWord.Append(Advance());
                }
                var compound = $"END {nextWord.ToString().ToUpperInvariant()}";
                if (Keywords.TryGetValue(compound, out var compoundType))
                {
                    return new Token(compoundType, compound, line, column);
                }
            }
            // Restore position if not a compound keyword
            _position = savedPos;
            _column = savedCol;
        }

        if (Keywords.TryGetValue(upperText, out var type))
        {
            // Preserve original case for property names (On, Color, etc.)
            // but use uppercase for actual BASIC keywords in statements
            return new Token(type, text, line, column);
        }

        return new Token(TokenType.Identifier, text, line, column);
    }

    private void SkipWhitespace()
    {
        while (!IsAtEnd())
        {
            var c = Peek();
            if (c == ' ' || c == '\t')
            {
                Advance();
            }
            else
            {
                break;
            }
        }
    }

    private bool IsAtEnd() => _position >= _source.Length;

    private char Peek() => IsAtEnd() ? '\0' : _source[_position];

    private char PeekNext() => _position + 1 >= _source.Length ? '\0' : _source[_position + 1];

    private char Advance()
    {
        var c = _source[_position++];
        _column++;
        return c;
    }

    private bool Match(char expected)
    {
        if (IsAtEnd() || _source[_position] != expected) return false;
        _position++;
        _column++;
        return true;
    }
}

public class LexerException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public LexerException(string message, int line, int column)
        : base($"{message} at line {line}, column {column}")
    {
        Line = line;
        Column = column;
    }
}
