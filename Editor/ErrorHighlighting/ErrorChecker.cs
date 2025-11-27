using BasicToMips.Lexer;
using BasicToMips.Parser;

namespace BasicToMips.Editor.ErrorHighlighting;

public class ErrorChecker
{
    public class ErrorInfo
    {
        public int Line { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Message { get; set; } = "";
        public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    }

    public enum ErrorSeverity
    {
        Error,
        Warning,
        Info
    }

    public List<ErrorInfo> Check(string code)
    {
        var errors = new List<ErrorInfo>();

        if (string.IsNullOrWhiteSpace(code))
            return errors;

        try
        {
            // Run lexer
            var lexer = new BasicLexer();
            var tokens = lexer.Tokenize(code);

            // Check for unclosed strings or other lexer issues
            CheckLexerIssues(code, errors);

            // Run parser
            var parser = new BasicParser();
            try
            {
                var ast = parser.Parse(tokens);

                // Semantic checks
                CheckSemantics(ast, errors);
            }
            catch (ParseException ex)
            {
                errors.Add(new ErrorInfo
                {
                    Line = ex.Line,
                    Column = ex.Column,
                    Length = 10, // Approximate
                    Message = ex.Message,
                    Severity = ErrorSeverity.Error
                });
            }
        }
        catch (Exception ex)
        {
            // General parsing error
            errors.Add(new ErrorInfo
            {
                Line = 1,
                Column = 1,
                Length = 1,
                Message = ex.Message,
                Severity = ErrorSeverity.Error
            });
        }

        // Check for common issues
        CheckCommonIssues(code, errors);

        return errors;
    }

    private void CheckLexerIssues(string code, List<ErrorInfo> errors)
    {
        var lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // Check for unclosed strings
            int quoteCount = line.Count(c => c == '"');
            if (quoteCount % 2 != 0)
            {
                errors.Add(new ErrorInfo
                {
                    Line = i + 1,
                    Column = line.IndexOf('"') + 1,
                    Length = line.Length - line.IndexOf('"'),
                    Message = "Unclosed string literal",
                    Severity = ErrorSeverity.Error
                });
            }

            // Check for unclosed parentheses on single line
            int openParen = line.Count(c => c == '(');
            int closeParen = line.Count(c => c == ')');

            // Only warn if there's a significant mismatch on a single line
            if (Math.Abs(openParen - closeParen) > 2)
            {
                if (openParen > closeParen)
                {
                    errors.Add(new ErrorInfo
                    {
                        Line = i + 1,
                        Column = 1,
                        Length = line.Length,
                        Message = $"Possible missing {openParen - closeParen} closing parenthesis",
                        Severity = ErrorSeverity.Warning
                    });
                }
            }
        }
    }

    private void CheckSemantics(BasicToMips.AST.ProgramNode ast, List<ErrorInfo> errors)
    {
        var declaredVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var definedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaredAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stmt in ast.Statements)
        {
            CollectDeclarations(stmt, declaredVars, definedLabels, declaredAliases);
        }

        foreach (var stmt in ast.Statements)
        {
            CheckStatement(stmt, declaredVars, usedLabels, definedLabels, declaredAliases, errors);
        }

        // Check for undefined labels
        foreach (var label in usedLabels)
        {
            if (!definedLabels.Contains(label))
            {
                errors.Add(new ErrorInfo
                {
                    Line = 1,
                    Column = 1,
                    Length = label.Length,
                    Message = $"Undefined label: {label}",
                    Severity = ErrorSeverity.Error
                });
            }
        }
    }

    private void CollectDeclarations(BasicToMips.AST.StatementNode stmt,
        HashSet<string> vars, HashSet<string> labels, HashSet<string> aliases)
    {
        switch (stmt)
        {
            case BasicToMips.AST.LetStatement let:
                vars.Add(let.VariableName);
                break;
            case BasicToMips.AST.VarStatement var:
                vars.Add(var.VariableName);
                break;
            case BasicToMips.AST.DimStatement dim:
                vars.Add(dim.VariableName);
                break;
            case BasicToMips.AST.LabelStatement label:
                labels.Add(label.Name);
                break;
            case BasicToMips.AST.AliasStatement alias:
                aliases.Add(alias.AliasName);
                break;
            case BasicToMips.AST.ForStatement forStmt:
                vars.Add(forStmt.VariableName);
                foreach (var bodyStmt in forStmt.Body)
                    CollectDeclarations(bodyStmt, vars, labels, aliases);
                break;
            case BasicToMips.AST.WhileStatement whileStmt:
                foreach (var bodyStmt in whileStmt.Body)
                    CollectDeclarations(bodyStmt, vars, labels, aliases);
                break;
            case BasicToMips.AST.IfStatement ifStmt:
                foreach (var bodyStmt in ifStmt.ThenBranch)
                    CollectDeclarations(bodyStmt, vars, labels, aliases);
                foreach (var bodyStmt in ifStmt.ElseBranch)
                    CollectDeclarations(bodyStmt, vars, labels, aliases);
                break;
        }
    }

    private void CheckStatement(BasicToMips.AST.StatementNode stmt,
        HashSet<string> declaredVars,
        HashSet<string> usedLabels,
        HashSet<string> definedLabels,
        HashSet<string> declaredAliases,
        List<ErrorInfo> errors)
    {
        switch (stmt)
        {
            case BasicToMips.AST.GotoStatement gotoStmt:
                if (!string.IsNullOrEmpty(gotoStmt.TargetLabel))
                    usedLabels.Add(gotoStmt.TargetLabel);
                break;

            case BasicToMips.AST.GosubStatement gosubStmt:
                if (!string.IsNullOrEmpty(gosubStmt.TargetLabel))
                    usedLabels.Add(gosubStmt.TargetLabel);
                break;

            case BasicToMips.AST.ForStatement forStmt:
                foreach (var bodyStmt in forStmt.Body)
                    CheckStatement(bodyStmt, declaredVars, usedLabels, definedLabels, declaredAliases, errors);
                break;

            case BasicToMips.AST.WhileStatement whileStmt:
                foreach (var bodyStmt in whileStmt.Body)
                    CheckStatement(bodyStmt, declaredVars, usedLabels, definedLabels, declaredAliases, errors);
                break;

            case BasicToMips.AST.IfStatement ifStmt:
                foreach (var bodyStmt in ifStmt.ThenBranch)
                    CheckStatement(bodyStmt, declaredVars, usedLabels, definedLabels, declaredAliases, errors);
                foreach (var bodyStmt in ifStmt.ElseBranch)
                    CheckStatement(bodyStmt, declaredVars, usedLabels, definedLabels, declaredAliases, errors);
                break;
        }
    }

    private void CheckCommonIssues(string code, List<ErrorInfo> errors)
    {
        var lines = code.Split('\n');

        // Track control structure depth
        int ifDepth = 0;
        int forDepth = 0;
        int whileDepth = 0;
        int subDepth = 0;
        int funcDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim().ToUpperInvariant();

            // Skip comments
            if (line.StartsWith("'") || line.StartsWith("REM "))
                continue;

            // Track IF blocks
            if (line.StartsWith("IF ") && (line.Contains(" THEN") && !line.Contains(" THEN ") && !line.EndsWith(" THEN")))
            {
                // Single-line IF - ignore
            }
            else if (line.StartsWith("IF ") && line.Contains(" THEN"))
            {
                // Could be multi-line, track it
                if (!HasStatementAfterThen(line))
                    ifDepth++;
            }
            else if (line == "ENDIF" || line == "END IF")
            {
                ifDepth--;
                if (ifDepth < 0)
                {
                    errors.Add(new ErrorInfo
                    {
                        Line = i + 1,
                        Column = 1,
                        Length = line.Length,
                        Message = "ENDIF without matching IF",
                        Severity = ErrorSeverity.Error
                    });
                    ifDepth = 0;
                }
            }

            // Track FOR loops
            if (line.StartsWith("FOR "))
                forDepth++;
            else if (line.StartsWith("NEXT"))
            {
                forDepth--;
                if (forDepth < 0)
                {
                    errors.Add(new ErrorInfo
                    {
                        Line = i + 1,
                        Column = 1,
                        Length = line.Length,
                        Message = "NEXT without matching FOR",
                        Severity = ErrorSeverity.Error
                    });
                    forDepth = 0;
                }
            }

            // Track WHILE loops
            if (line.StartsWith("WHILE ") || line == "WHILE")
                whileDepth++;
            else if (line == "WEND")
            {
                whileDepth--;
                if (whileDepth < 0)
                {
                    errors.Add(new ErrorInfo
                    {
                        Line = i + 1,
                        Column = 1,
                        Length = line.Length,
                        Message = "WEND without matching WHILE",
                        Severity = ErrorSeverity.Error
                    });
                    whileDepth = 0;
                }
            }

            // Track SUBs
            if (line.StartsWith("SUB "))
                subDepth++;
            else if (line == "ENDSUB" || line == "END SUB")
            {
                subDepth--;
                if (subDepth < 0)
                {
                    errors.Add(new ErrorInfo
                    {
                        Line = i + 1,
                        Column = 1,
                        Length = line.Length,
                        Message = "END SUB without matching SUB",
                        Severity = ErrorSeverity.Error
                    });
                    subDepth = 0;
                }
            }

            // Track FUNCTIONs
            if (line.StartsWith("FUNCTION "))
                funcDepth++;
            else if (line == "ENDFUNCTION" || line == "END FUNCTION")
            {
                funcDepth--;
                if (funcDepth < 0)
                {
                    errors.Add(new ErrorInfo
                    {
                        Line = i + 1,
                        Column = 1,
                        Length = line.Length,
                        Message = "END FUNCTION without matching FUNCTION",
                        Severity = ErrorSeverity.Error
                    });
                    funcDepth = 0;
                }
            }

            // Check for deprecated or invalid constructs
            if (line.Contains("GOTO") && !line.StartsWith("'"))
            {
                // GOTO warning (not error)
                errors.Add(new ErrorInfo
                {
                    Line = i + 1,
                    Column = line.IndexOf("GOTO", StringComparison.OrdinalIgnoreCase) + 1,
                    Length = 4,
                    Message = "Consider using structured control flow instead of GOTO",
                    Severity = ErrorSeverity.Info
                });
            }
        }

        // Check for unclosed blocks
        if (ifDepth > 0)
        {
            errors.Add(new ErrorInfo
            {
                Line = lines.Length,
                Column = 1,
                Length = 1,
                Message = $"Missing {ifDepth} ENDIF statement(s)",
                Severity = ErrorSeverity.Error
            });
        }
        if (forDepth > 0)
        {
            errors.Add(new ErrorInfo
            {
                Line = lines.Length,
                Column = 1,
                Length = 1,
                Message = $"Missing {forDepth} NEXT statement(s)",
                Severity = ErrorSeverity.Error
            });
        }
        if (whileDepth > 0)
        {
            errors.Add(new ErrorInfo
            {
                Line = lines.Length,
                Column = 1,
                Length = 1,
                Message = $"Missing {whileDepth} WEND statement(s)",
                Severity = ErrorSeverity.Error
            });
        }
        if (subDepth > 0)
        {
            errors.Add(new ErrorInfo
            {
                Line = lines.Length,
                Column = 1,
                Length = 1,
                Message = $"Missing {subDepth} END SUB statement(s)",
                Severity = ErrorSeverity.Error
            });
        }
        if (funcDepth > 0)
        {
            errors.Add(new ErrorInfo
            {
                Line = lines.Length,
                Column = 1,
                Length = 1,
                Message = $"Missing {funcDepth} END FUNCTION statement(s)",
                Severity = ErrorSeverity.Error
            });
        }
    }

    private bool HasStatementAfterThen(string line)
    {
        var thenIndex = line.IndexOf(" THEN", StringComparison.OrdinalIgnoreCase);
        if (thenIndex < 0) return false;

        var afterThen = line.Substring(thenIndex + 5).Trim();
        return !string.IsNullOrEmpty(afterThen);
    }
}
