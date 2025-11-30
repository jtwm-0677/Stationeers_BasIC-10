using System.Text.RegularExpressions;
using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.AST;

namespace BasicToMips.Refactoring;

/// <summary>
/// Service for code refactoring operations.
/// </summary>
public class RefactoringService
{
    /// <summary>
    /// Find all occurrences of a symbol in the source code.
    /// </summary>
    /// <param name="source">The source code to search.</param>
    /// <param name="symbolName">The symbol name to find.</param>
    /// <returns>List of symbol occurrences with location info.</returns>
    public List<SymbolOccurrence> FindSymbolOccurrences(string source, string symbolName)
    {
        var occurrences = new List<SymbolOccurrence>();

        try
        {
            // Parse the source
            var lexer = new Lexer.Lexer(source, true);
            var tokens = lexer.Tokenize();
            var parser = new Parser.Parser(tokens);
            var program = parser.Parse();

            // Determine what kind of symbol this is
            var symbolInfo = IdentifySymbol(program, symbolName);

            // Find all occurrences in tokens
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Identifier &&
                    token.Value.Equals(symbolName, StringComparison.OrdinalIgnoreCase))
                {
                    occurrences.Add(new SymbolOccurrence
                    {
                        Line = token.Line,
                        Column = token.Column,
                        Length = token.Value.Length,
                        IsDefinition = IsDefinitionLocation(program, token.Line, symbolName),
                        SymbolKind = symbolInfo.Kind,
                        OriginalName = token.Value
                    });
                }
            }
        }
        catch
        {
            // If parsing fails, fall back to text-based search
            occurrences = FindTextOccurrences(source, symbolName);
        }

        return occurrences;
    }

    /// <summary>
    /// Rename all occurrences of a symbol.
    /// </summary>
    /// <param name="source">The source code.</param>
    /// <param name="oldName">The current symbol name.</param>
    /// <param name="newName">The new symbol name.</param>
    /// <returns>The modified source code with renames applied.</returns>
    public RenameResult Rename(string source, string oldName, string newName)
    {
        var result = new RenameResult();

        // Validate new name
        if (string.IsNullOrWhiteSpace(newName))
        {
            result.Success = false;
            result.ErrorMessage = "New name cannot be empty.";
            return result;
        }

        if (!IsValidIdentifier(newName))
        {
            result.Success = false;
            result.ErrorMessage = "New name is not a valid identifier.";
            return result;
        }

        // Check if new name is a reserved keyword
        if (IsReservedKeyword(newName))
        {
            result.Success = false;
            result.ErrorMessage = $"'{newName}' is a reserved keyword.";
            return result;
        }

        // Find all occurrences
        var occurrences = FindSymbolOccurrences(source, oldName);

        if (occurrences.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = $"Symbol '{oldName}' not found.";
            return result;
        }

        // Check if new name already exists
        var existingOccurrences = FindSymbolOccurrences(source, newName);
        if (existingOccurrences.Count > 0)
        {
            result.Success = false;
            result.ErrorMessage = $"Symbol '{newName}' already exists.";
            return result;
        }

        // Apply renames (from end to start to preserve positions)
        var lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        var sortedOccurrences = occurrences.OrderByDescending(o => o.Line).ThenByDescending(o => o.Column).ToList();

        foreach (var occurrence in sortedOccurrences)
        {
            var lineIndex = occurrence.Line - 1;
            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                var line = lines[lineIndex];
                var colIndex = occurrence.Column - 1;
                if (colIndex >= 0 && colIndex + occurrence.Length <= line.Length)
                {
                    // Replace the occurrence, preserving case pattern if desired
                    var replacement = MatchCase(occurrence.OriginalName, newName);
                    lines[lineIndex] = line.Substring(0, colIndex) + replacement + line.Substring(colIndex + occurrence.Length);
                }
            }
        }

        result.Success = true;
        result.NewSource = string.Join(Environment.NewLine, lines);
        result.RenamedCount = occurrences.Count;
        result.Occurrences = occurrences;

        return result;
    }

    /// <summary>
    /// Get a preview of rename changes.
    /// </summary>
    public List<RenamePreview> PreviewRename(string source, string oldName, string newName)
    {
        var previews = new List<RenamePreview>();
        var occurrences = FindSymbolOccurrences(source, oldName);
        var lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var occurrence in occurrences)
        {
            var lineIndex = occurrence.Line - 1;
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                var originalLine = lines[lineIndex];
                var colIndex = occurrence.Column - 1;
                var replacement = MatchCase(occurrence.OriginalName, newName);
                var newLine = originalLine.Substring(0, colIndex) + replacement +
                             originalLine.Substring(colIndex + occurrence.Length);

                previews.Add(new RenamePreview
                {
                    Line = occurrence.Line,
                    OriginalText = originalLine,
                    NewText = newLine,
                    IsDefinition = occurrence.IsDefinition
                });
            }
        }

        return previews;
    }

    private SymbolInfo IdentifySymbol(ProgramNode program, string name)
    {
        var info = new SymbolInfo { Name = name, Kind = SymbolKind.Unknown };

        foreach (var stmt in program.Statements)
        {
            switch (stmt)
            {
                case LetStatement let when let.VariableName.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Variable;
                    info.DefinitionLine = let.Line;
                    return info;

                case VarStatement varStmt when varStmt.VariableName.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Variable;
                    info.DefinitionLine = varStmt.Line;
                    return info;

                case DimStatement dim when dim.VariableName.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Variable;
                    info.DefinitionLine = dim.Line;
                    return info;

                case ConstStatement constStmt when constStmt.ConstantName.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Constant;
                    info.DefinitionLine = constStmt.Line;
                    return info;

                case DefineStatement def when def.ConstantName.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Constant;
                    info.DefinitionLine = def.Line;
                    return info;

                case LabelStatement label when label.Name.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Label;
                    info.DefinitionLine = label.Line;
                    return info;

                case AliasStatement alias when alias.AliasName.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Alias;
                    info.DefinitionLine = alias.Line;
                    return info;

                case SubDefinition sub when sub.Name.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Subroutine;
                    info.DefinitionLine = sub.Line;
                    return info;

                case FunctionDefinition func when func.Name.Equals(name, StringComparison.OrdinalIgnoreCase):
                    info.Kind = SymbolKind.Function;
                    info.DefinitionLine = func.Line;
                    return info;
            }
        }

        return info;
    }

    private bool IsDefinitionLocation(ProgramNode program, int line, string name)
    {
        foreach (var stmt in program.Statements)
        {
            if (stmt.Line != line) continue;

            switch (stmt)
            {
                case LetStatement let when let.VariableName.Equals(name, StringComparison.OrdinalIgnoreCase):
                case VarStatement varStmt when varStmt.VariableName.Equals(name, StringComparison.OrdinalIgnoreCase):
                case DimStatement dim when dim.VariableName.Equals(name, StringComparison.OrdinalIgnoreCase):
                case ConstStatement constStmt when constStmt.ConstantName.Equals(name, StringComparison.OrdinalIgnoreCase):
                case DefineStatement def when def.ConstantName.Equals(name, StringComparison.OrdinalIgnoreCase):
                case LabelStatement label when label.Name.Equals(name, StringComparison.OrdinalIgnoreCase):
                case AliasStatement alias when alias.AliasName.Equals(name, StringComparison.OrdinalIgnoreCase):
                case SubDefinition sub when sub.Name.Equals(name, StringComparison.OrdinalIgnoreCase):
                case FunctionDefinition func when func.Name.Equals(name, StringComparison.OrdinalIgnoreCase):
                    return true;
            }
        }
        return false;
    }

    private List<SymbolOccurrence> FindTextOccurrences(string source, string symbolName)
    {
        var occurrences = new List<SymbolOccurrence>();
        var lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var pattern = new Regex($@"\b{Regex.Escape(symbolName)}\b", RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Length; i++)
        {
            var matches = pattern.Matches(lines[i]);
            foreach (Match match in matches)
            {
                occurrences.Add(new SymbolOccurrence
                {
                    Line = i + 1,
                    Column = match.Index + 1,
                    Length = match.Length,
                    IsDefinition = false,
                    SymbolKind = SymbolKind.Unknown,
                    OriginalName = match.Value
                });
            }
        }

        return occurrences;
    }

    private bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '$');
    }

    private bool IsReservedKeyword(string name)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LET", "PRINT", "INPUT", "IF", "THEN", "ELSE", "ELSEIF", "ENDIF", "END",
            "FOR", "TO", "STEP", "NEXT", "WHILE", "WEND", "DO", "LOOP", "UNTIL",
            "GOTO", "GOSUB", "RETURN", "REM", "DIM", "AS", "INTEGER", "SINGLE",
            "AND", "OR", "NOT", "MOD", "DEF", "FN", "SUB", "ENDSUB", "FUNCTION",
            "ENDFUNCTION", "CALL", "EXIT", "SLEEP", "WAIT", "YIELD", "DEVICE",
            "ALIAS", "DEFINE", "VAR", "CONST", "BREAK", "CONTINUE", "SELECT",
            "CASE", "DEFAULT", "ENDSELECT", "PUSH", "POP", "PEEK", "TRUE", "FALSE",
            "SHL", "SHR", "BAND", "BOR", "BXOR", "BNOT", "INCLUDE", "ON", "DATA",
            "READ", "RESTORE"
        };
        return keywords.Contains(name);
    }

    private string MatchCase(string original, string newName)
    {
        // Preserve the case style of the original
        if (original.All(char.IsUpper))
            return newName.ToUpper();
        if (original.All(char.IsLower))
            return newName.ToLower();
        if (char.IsUpper(original[0]) && original.Skip(1).All(char.IsLower))
            return char.ToUpper(newName[0]) + newName.Substring(1).ToLower();
        return newName;
    }
}

/// <summary>
/// Represents an occurrence of a symbol in the source code.
/// </summary>
public class SymbolOccurrence
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Length { get; set; }
    public bool IsDefinition { get; set; }
    public SymbolKind SymbolKind { get; set; }
    public string OriginalName { get; set; } = string.Empty;
}

/// <summary>
/// Kind of symbol.
/// </summary>
public enum SymbolKind
{
    Unknown,
    Variable,
    Constant,
    Label,
    Alias,
    Subroutine,
    Function
}

/// <summary>
/// Information about a symbol.
/// </summary>
public class SymbolInfo
{
    public string Name { get; set; } = string.Empty;
    public SymbolKind Kind { get; set; }
    public int DefinitionLine { get; set; }
}

/// <summary>
/// Result of a rename operation.
/// </summary>
public class RenameResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NewSource { get; set; }
    public int RenamedCount { get; set; }
    public List<SymbolOccurrence> Occurrences { get; set; } = new();
}

/// <summary>
/// Preview of a rename change on a single line.
/// </summary>
public class RenamePreview
{
    public int Line { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
    public bool IsDefinition { get; set; }
}
