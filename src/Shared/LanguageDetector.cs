namespace BasicToMips.Shared;

/// <summary>
/// Detects whether source code is BASIC or IC10 MIPS assembly
/// </summary>
public static class LanguageDetector
{
    // IC10 instruction opcodes
    private static readonly HashSet<string> IC10Instructions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Arithmetic
        "add", "sub", "mul", "div", "mod",
        // Math
        "sqrt", "abs", "floor", "ceil", "round", "trunc",
        "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
        "log", "exp", "max", "min", "rand",
        // Comparison
        "seq", "sne", "slt", "sgt", "sle", "sge",
        "seqz", "snez", "sgtz", "sltz", "sgez", "slez",
        "snan", "snaz", "sap", "sapz",
        // Branching
        "j", "jr", "jal",
        "beq", "bne", "blt", "bgt", "ble", "bge",
        "beqz", "bnez", "bgtz", "bltz", "bgez", "blez",
        "bnan", "bnaz", "bap", "bapz",
        "beqal", "bneal", "bltal", "bgtal", "bleal", "bgeal",
        // Bitwise
        "and", "or", "xor", "nor", "not",
        "sll", "srl", "sra",
        // Stack
        "push", "pop", "peek",
        // Device I/O
        "l", "s", "ls", "ss", "lr",
        "lb", "sb", "lbn", "sbn", "lbs", "sbs", "lbns", "sbns",
        // Misc
        "move", "alias", "define", "select",
        "yield", "sleep", "hcf",
        "sdse", "sdns", "bdse", "bdns"
    };

    // BASIC keywords that don't exist in IC10
    private static readonly HashSet<string> BasicKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "LET", "PRINT", "INPUT", "IF", "THEN", "ELSE", "ELSEIF", "ENDIF",
        "FOR", "TO", "STEP", "NEXT", "WHILE", "WEND", "DO", "LOOP", "UNTIL",
        "GOTO", "GOSUB", "RETURN", "END", "REM", "DIM", "AS", "INTEGER", "SINGLE",
        "AND", "OR", "NOT", "MOD", "DEF", "FN", "SUB", "ENDSUB", "FUNCTION",
        "ENDFUNCTION", "CALL", "EXIT", "SLEEP", "YIELD", "DEVICE", "VAR", "CONST"
    };

    /// <summary>
    /// Detect the language of the given source code
    /// </summary>
    public static LanguageType Detect(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return LanguageType.Unknown;

        var lines = source.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        int basicScore = 0;
        int ic10Score = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // IC10 comment style (# at start)
            if (line.StartsWith("#"))
            {
                ic10Score += 3;
                continue;
            }

            // BASIC comment style (' at start or REM)
            if (line.StartsWith("'") || line.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
            {
                basicScore += 3;
                continue;
            }

            // Check for labels (name:) - both languages have these
            if (line.EndsWith(":") && !line.Contains(" "))
            {
                // Neutral - both have labels
                continue;
            }

            // Get first word (potential instruction/keyword)
            var firstWord = GetFirstWord(line);
            if (string.IsNullOrEmpty(firstWord)) continue;

            // Check for IC10 instruction
            if (IC10Instructions.Contains(firstWord))
            {
                ic10Score += 2;

                // Strong IC10 indicators: register references like r0, r1, d0, etc.
                if (HasRegisterReference(line))
                {
                    ic10Score += 2;
                }
            }

            // Check for BASIC keyword
            if (BasicKeywords.Contains(firstWord))
            {
                basicScore += 2;
            }

            // BASIC-specific patterns
            if (line.Contains("THEN") || line.Contains("ENDIF") || line.Contains("WEND"))
            {
                basicScore += 3;
            }

            // BASIC assignment with LET or without
            if (firstWord.Equals("LET", StringComparison.OrdinalIgnoreCase))
            {
                basicScore += 3;
            }

            // BASIC variable patterns (variableName = expression)
            if (line.Contains(" = ") && !line.StartsWith("move", StringComparison.OrdinalIgnoreCase)
                && !line.StartsWith("seq", StringComparison.OrdinalIgnoreCase))
            {
                basicScore += 2;
            }

            // IC10 device access pattern: s d0 Property value
            if ((firstWord == "s" || firstWord == "l") && line.Contains(" d"))
            {
                ic10Score += 3;
            }

            // BASIC property access: device.Property
            if (line.Contains(".") && !line.Contains(" d") && !line.Contains("0.") && !line.Contains("1."))
            {
                basicScore += 2;
            }
        }

        // Determine result based on scores
        if (basicScore == 0 && ic10Score == 0)
            return LanguageType.Unknown;

        if (basicScore > ic10Score * 1.5)
            return LanguageType.Basic;

        if (ic10Score > basicScore * 1.5)
            return LanguageType.IC10;

        // Close scores - check for definitive markers
        if (source.Contains("ENDIF") || source.Contains("WEND") || source.Contains("GOSUB"))
            return LanguageType.Basic;

        if (HasRegisterReference(source) && !source.Contains("'"))
            return LanguageType.IC10;

        // Default to BASIC if uncertain (more forgiving)
        return basicScore >= ic10Score ? LanguageType.Basic : LanguageType.IC10;
    }

    private static string GetFirstWord(string line)
    {
        var trimmed = line.TrimStart();
        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex > 0 ? trimmed.Substring(0, spaceIndex) : trimmed;
    }

    private static bool HasRegisterReference(string line)
    {
        // Look for r0-r15 or d0-d5 patterns
        for (int i = 0; i < line.Length - 1; i++)
        {
            char c = line[i];
            char next = line[i + 1];

            if ((c == 'r' || c == 'R') && char.IsDigit(next))
            {
                // Check it's not part of a larger word
                if (i == 0 || !char.IsLetterOrDigit(line[i - 1]))
                    return true;
            }
            if ((c == 'd' || c == 'D') && char.IsDigit(next))
            {
                if (i == 0 || !char.IsLetterOrDigit(line[i - 1]))
                {
                    // Make sure it's d0-d5, not a decimal like 0.5d
                    if (i == 0 || line[i - 1] != '.')
                        return true;
                }
            }
        }
        return false;
    }
}

public enum LanguageType
{
    Unknown,
    Basic,
    IC10
}
