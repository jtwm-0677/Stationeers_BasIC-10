using System.Text;
using System.Text.RegularExpressions;

namespace BasicToMips.Analysis;

/// <summary>
/// Formats BASIC code with proper indentation and spacing.
/// </summary>
public class CodeFormatter
{
    private readonly CodeFormatterOptions _options;

    public CodeFormatter() : this(new CodeFormatterOptions()) { }

    public CodeFormatter(CodeFormatterOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Format the given BASIC source code.
    /// </summary>
    public string Format(string source)
    {
        var lines = source.Split('\n');
        var result = new StringBuilder();
        int indentLevel = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();

            // Skip empty lines (but preserve them)
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                result.AppendLine();
                continue;
            }

            // Check if this line decreases indent BEFORE the line
            var decreaseBeforeLine = ShouldDecreaseIndentBefore(trimmed);
            if (decreaseBeforeLine)
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }

            // Format the line
            var formattedLine = FormatLine(trimmed);

            // Apply indentation
            var indent = new string(' ', indentLevel * _options.IndentSize);
            result.AppendLine(indent + formattedLine);

            // Check if this line increases indent for NEXT line
            if (ShouldIncreaseIndentAfter(trimmed))
            {
                indentLevel++;
            }

            // Check if this line decreases indent AFTER (for single-line constructs)
            if (ShouldDecreaseIndentAfter(trimmed))
            {
                indentLevel = Math.Max(0, indentLevel - 1);
            }
        }

        return result.ToString().TrimEnd() + Environment.NewLine;
    }

    private bool ShouldIncreaseIndentAfter(string line)
    {
        var upper = line.ToUpperInvariant();

        // Multi-line IF (IF...THEN with no statement after THEN)
        if (upper.StartsWith("IF ") && upper.Contains(" THEN"))
        {
            var thenIdx = upper.IndexOf(" THEN");
            var afterThen = line.Substring(thenIdx + 5).Trim();
            // Only increase if nothing after THEN (multi-line IF)
            if (string.IsNullOrEmpty(afterThen))
                return true;
        }

        // ELSE on its own line
        if (upper == "ELSE")
            return true;

        // FOR loop
        if (upper.StartsWith("FOR "))
            return true;

        // WHILE loop
        if (upper.StartsWith("WHILE ") || upper == "WHILE")
            return true;

        // DO loop
        if (upper.StartsWith("DO ") || upper == "DO")
            return true;

        // SUB definition
        if (upper.StartsWith("SUB "))
            return true;

        // FUNCTION definition
        if (upper.StartsWith("FUNCTION "))
            return true;

        // SELECT CASE
        if (upper.StartsWith("SELECT "))
            return true;

        // CASE clause (indent case body)
        if (upper.StartsWith("CASE ") && !upper.StartsWith("CASE ELSE"))
            return true;

        if (upper == "CASE ELSE")
            return true;

        return false;
    }

    private bool ShouldDecreaseIndentBefore(string line)
    {
        var upper = line.ToUpperInvariant();

        // ENDIF / END IF
        if (upper == "ENDIF" || upper == "END IF")
            return true;

        // ELSE
        if (upper == "ELSE" || upper.StartsWith("ELSEIF ") || upper.StartsWith("ELSE IF "))
            return true;

        // NEXT
        if (upper == "NEXT" || upper.StartsWith("NEXT "))
            return true;

        // WEND
        if (upper == "WEND")
            return true;

        // LOOP
        if (upper == "LOOP" || upper.StartsWith("LOOP "))
            return true;

        // END SUB
        if (upper == "ENDSUB" || upper == "END SUB")
            return true;

        // END FUNCTION
        if (upper == "ENDFUNCTION" || upper == "END FUNCTION")
            return true;

        // END SELECT
        if (upper == "END SELECT" || upper == "ENDSELECT")
            return true;

        // CASE (decrease from previous case body, then increase for new case)
        if (upper.StartsWith("CASE "))
            return true;

        return false;
    }

    private bool ShouldDecreaseIndentAfter(string line)
    {
        var upper = line.ToUpperInvariant();

        // ELSE decreases after (we increased before for the ELSE line itself)
        // Actually no - ELSE should increase for the else-body
        // ELSEIF should stay at same level as IF

        if (upper.StartsWith("ELSEIF ") || upper.StartsWith("ELSE IF "))
        {
            // ELSEIF increases indent for its body
            return false;
        }

        return false;
    }

    private string FormatLine(string line)
    {
        // Preserve comments as-is
        if (line.StartsWith("'") || line.StartsWith("#") ||
            line.ToUpperInvariant().StartsWith("REM "))
        {
            return line;
        }

        // Normalize spaces around operators
        line = NormalizeOperatorSpacing(line);

        // Normalize keyword casing if enabled
        if (_options.NormalizeKeywordCase)
        {
            line = NormalizeKeywords(line);
        }

        return line;
    }

    private string NormalizeOperatorSpacing(string line)
    {
        // Handle string literals - don't modify inside strings
        var parts = SplitByStrings(line);
        var result = new StringBuilder();

        foreach (var (text, isString) in parts)
        {
            if (isString)
            {
                result.Append(text);
            }
            else
            {
                var normalized = text;

                // IMPORTANT: Process compound operators FIRST before single-char operators
                // This prevents <= from becoming < = (fixes Issue #9)

                // Two-character comparison operators (must come before single-char < > =)
                normalized = Regex.Replace(normalized, @"(?<!\s)==(?!\s)", " == ");
                normalized = Regex.Replace(normalized, @"(?<!\s)!=(?!\s)", " != ");
                normalized = Regex.Replace(normalized, @"(?<!\s)<>(?!\s)", " <> ");
                normalized = Regex.Replace(normalized, @"(?<!\s)<=(?!\s)", " <= ");
                normalized = Regex.Replace(normalized, @"(?<!\s)>=(?!\s)", " >= ");

                // Single-character operators - use negative lookahead/lookbehind to avoid
                // matching when part of compound operators
                // < but not part of <= or <>
                normalized = Regex.Replace(normalized, @"(?<!\s)<(?![=>])(?!\s)", " < ");
                normalized = Regex.Replace(normalized, @"(?<![<])>(?![=])(?!\s)", " > ");

                // Assignment = (not == or != or <= or >=)
                normalized = Regex.Replace(normalized, @"(?<![=!<>])=(?!=)(?!\s)", " = ");
                normalized = Regex.Replace(normalized, @"(?<!\s)(?<![=!<>])=(?!=)", "= ");

                // Clean up multiple spaces
                normalized = Regex.Replace(normalized, @"  +", " ");

                result.Append(normalized);
            }
        }

        return result.ToString().Trim();
    }

    private string NormalizeKeywords(string line)
    {
        var keywords = new[]
        {
            "IF", "THEN", "ELSE", "ELSEIF", "ENDIF", "END IF",
            "FOR", "TO", "STEP", "NEXT",
            "WHILE", "WEND",
            "DO", "LOOP", "UNTIL",
            "SUB", "ENDSUB", "END SUB",
            "FUNCTION", "ENDFUNCTION", "END FUNCTION",
            "RETURN", "CALL",
            "DIM", "VAR", "LET", "CONST", "DEFINE",
            "ALIAS", "PRINT", "INPUT",
            "GOTO", "GOSUB",
            "SELECT", "CASE", "END SELECT",
            "AND", "OR", "NOT", "MOD",
            "TRUE", "FALSE",
            "BREAK", "CONTINUE", "END",
            "PUSH", "POP", "PEEK",
            "YIELD", "SLEEP"
        };

        var parts = SplitByStrings(line);
        var result = new StringBuilder();

        foreach (var (text, isString) in parts)
        {
            if (isString)
            {
                result.Append(text);
            }
            else
            {
                var normalized = text;
                foreach (var kw in keywords)
                {
                    // Match whole word only
                    normalized = Regex.Replace(
                        normalized,
                        $@"\b{Regex.Escape(kw)}\b",
                        kw,
                        RegexOptions.IgnoreCase);
                }
                result.Append(normalized);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Split a line into string and non-string parts.
    /// </summary>
    private List<(string Text, bool IsString)> SplitByStrings(string line)
    {
        var result = new List<(string, bool)>();
        var current = new StringBuilder();
        bool inString = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inString)
                {
                    current.Append(c);
                    result.Add((current.ToString(), true));
                    current.Clear();
                    inString = false;
                }
                else
                {
                    if (current.Length > 0)
                    {
                        result.Add((current.ToString(), false));
                        current.Clear();
                    }
                    current.Append(c);
                    inString = true;
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            result.Add((current.ToString(), inString));
        }

        return result;
    }
}

/// <summary>
/// Options for code formatting.
/// </summary>
public class CodeFormatterOptions
{
    /// <summary>
    /// Number of spaces per indent level.
    /// </summary>
    public int IndentSize { get; set; } = 4;

    /// <summary>
    /// Whether to normalize keyword casing to uppercase.
    /// </summary>
    public bool NormalizeKeywordCase { get; set; } = true;

    /// <summary>
    /// Whether to add spaces around operators.
    /// </summary>
    public bool SpaceAroundOperators { get; set; } = true;
}
