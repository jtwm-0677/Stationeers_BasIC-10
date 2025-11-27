namespace BasicToMips.Lexer;

public class Lexer
{
    private readonly string _source;
    private int _position;
    private int _line;
    private int _column;
    private bool _atLineStart;

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
        ["YIELD"] = TokenType.Yield,
        ["DEVICE"] = TokenType.Device,
        ["ALIAS"] = TokenType.Alias,
        ["DEFINE"] = TokenType.Define
    };

    public Lexer(string source)
    {
        _source = source;
        _position = 0;
        _line = 1;
        _column = 1;
        _atLineStart = true;
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
            '+' => new Token(TokenType.Plus, "+", startLine, startColumn),
            '-' => new Token(TokenType.Minus, "-", startLine, startColumn),
            '*' => new Token(TokenType.Multiply, "*", startLine, startColumn),
            '/' => new Token(TokenType.Divide, "/", startLine, startColumn),
            '^' => new Token(TokenType.Power, "^", startLine, startColumn),
            '(' => new Token(TokenType.LeftParen, "(", startLine, startColumn),
            ')' => new Token(TokenType.RightParen, ")", startLine, startColumn),
            '[' => new Token(TokenType.LeftBracket, "[", startLine, startColumn),
            ']' => new Token(TokenType.RightBracket, "]", startLine, startColumn),
            ',' => new Token(TokenType.Comma, ",", startLine, startColumn),
            ':' => new Token(TokenType.Colon, ":", startLine, startColumn),
            ';' => new Token(TokenType.Semicolon, ";", startLine, startColumn),
            '=' => new Token(TokenType.Equal, "=", startLine, startColumn),
            '<' => ScanLessThan(startLine, startColumn),
            '>' => ScanGreaterThan(startLine, startColumn),
            '"' => ScanString(startLine, startColumn),
            '\'' => ScanComment(startLine, startColumn),
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

    private Token ScanLessThan(int line, int column)
    {
        if (Match('=')) return new Token(TokenType.LessEqual, "<=", line, column);
        if (Match('>')) return new Token(TokenType.NotEqual, "<>", line, column);
        return new Token(TokenType.LessThan, "<", line, column);
    }

    private Token ScanGreaterThan(int line, int column)
    {
        if (Match('=')) return new Token(TokenType.GreaterEqual, ">=", line, column);
        return new Token(TokenType.GreaterThan, ">", line, column);
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
        // Skip everything until end of line (BASIC ' comment)
        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            Advance();
        }
        // Return null to skip comment, or handle newline
        if (!IsAtEnd())
        {
            return ScanToken()!;
        }
        return new Token(TokenType.Eof, "", _line, _column);
    }

    private Token ScanIC10Comment(int line, int column)
    {
        // IC10 style comment (# comment)
        // Skip everything until end of line
        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            Advance();
        }
        // Return null to skip comment, or handle newline
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
            return new Token(type, upperText, line, column);
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
