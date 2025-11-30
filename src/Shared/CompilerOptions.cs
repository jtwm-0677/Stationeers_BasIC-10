namespace BasicToMips.Shared;

/// <summary>
/// Output formatting mode for generated IC10 code.
/// </summary>
public enum OutputMode
{
    /// <summary>
    /// Readable mode - preserves comments and formatting (default).
    /// </summary>
    Readable = 0,

    /// <summary>
    /// Compact mode - strips all comments and minimizes whitespace for smallest output.
    /// </summary>
    Compact = 1,

    /// <summary>
    /// Debug mode - includes source line numbers as comments on each instruction.
    /// </summary>
    Debug = 2
}

/// <summary>
/// Compiler configuration options.
/// Can be set programmatically or via ##Meta: directives in source.
/// </summary>
public class CompilerOptions
{
    /// <summary>
    /// Whether to preserve comments in the output IC10 code.
    /// Default: true (comments are preserved)
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// Whether to emit debug comments showing variable assignments.
    /// Default: false
    /// </summary>
    public bool EmitDebugComments { get; set; } = false;

    /// <summary>
    /// Optimization level (0 = none, 1 = basic, 2 = aggressive).
    /// </summary>
    public int OptimizationLevel { get; set; } = 1;

    /// <summary>
    /// Whether to use inline hash values instead of define statements.
    /// Default: true (matches working compiler behavior)
    /// </summary>
    public bool UseInlineHashes { get; set; } = true;

    /// <summary>
    /// Whether to append source line numbers as comments on each IC10 instruction.
    /// Example: "s d0 On 1 # 15" where 15 is the BASIC source line.
    /// Default: false
    /// </summary>
    public bool EmitSourceLineComments { get; set; } = false;

    /// <summary>
    /// Output formatting mode - controls comment generation and formatting.
    /// Default: Readable (preserves comments)
    /// </summary>
    public OutputMode OutputMode { get; set; } = OutputMode.Readable;

    /// <summary>
    /// Path to the source file. Used for resolving INCLUDE paths.
    /// When null, includes are relative to current directory.
    /// </summary>
    public string? SourceFilePath { get; set; }

    /// <summary>
    /// Create default options.
    /// </summary>
    public static CompilerOptions Default => new();

    /// <summary>
    /// Clone options for modification.
    /// </summary>
    public CompilerOptions Clone() => new()
    {
        PreserveComments = PreserveComments,
        EmitDebugComments = EmitDebugComments,
        OptimizationLevel = OptimizationLevel,
        UseInlineHashes = UseInlineHashes,
        EmitSourceLineComments = EmitSourceLineComments,
        OutputMode = OutputMode,
        SourceFilePath = SourceFilePath
    };
}

/// <summary>
/// Maps IC10 output lines back to BASIC source lines for debugging.
/// </summary>
public class SourceMap
{
    /// <summary>
    /// Maps IC10 line number (0-based) to BASIC source line number (1-based).
    /// </summary>
    public Dictionary<int, int> IC10ToBasic { get; } = new();

    /// <summary>
    /// Maps BASIC source line number (1-based) to list of IC10 line numbers (0-based).
    /// One BASIC line can generate multiple IC10 lines.
    /// </summary>
    public Dictionary<int, List<int>> BasicToIC10 { get; } = new();

    /// <summary>
    /// Symbol definitions with their source locations.
    /// Maps symbol name to (line, column, kind).
    /// </summary>
    public Dictionary<string, SymbolLocation> Symbols { get; } = new();

    /// <summary>
    /// Maps BASIC variable names to their assigned IC10 registers.
    /// Used for watch panel to resolve BASIC names to register values.
    /// </summary>
    public Dictionary<string, string> VariableRegisters { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps BASIC alias names to their device pins (d0-d5).
    /// Used for watch panel to resolve device references.
    /// </summary>
    public Dictionary<string, string> AliasDevices { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Add a mapping from IC10 line to BASIC source line.
    /// </summary>
    public void AddMapping(int ic10Line, int basicLine)
    {
        if (basicLine <= 0) return; // Skip invalid lines

        IC10ToBasic[ic10Line] = basicLine;

        if (!BasicToIC10.TryGetValue(basicLine, out var ic10Lines))
        {
            ic10Lines = new List<int>();
            BasicToIC10[basicLine] = ic10Lines;
        }
        if (!ic10Lines.Contains(ic10Line))
        {
            ic10Lines.Add(ic10Line);
        }
    }

    /// <summary>
    /// Add a symbol definition location.
    /// </summary>
    public void AddSymbol(string name, int line, int column, SymbolKind kind)
    {
        Symbols[name] = new SymbolLocation(line, column, kind);
    }

    /// <summary>
    /// Get the BASIC line for an IC10 line, or -1 if not mapped.
    /// </summary>
    public int GetBasicLine(int ic10Line)
    {
        return IC10ToBasic.TryGetValue(ic10Line, out var basicLine) ? basicLine : -1;
    }

    /// <summary>
    /// Get all IC10 lines generated from a BASIC line.
    /// </summary>
    public IReadOnlyList<int> GetIC10Lines(int basicLine)
    {
        return BasicToIC10.TryGetValue(basicLine, out var lines) ? lines : Array.Empty<int>();
    }

    /// <summary>
    /// Get symbol location by name.
    /// </summary>
    public SymbolLocation? GetSymbol(string name)
    {
        return Symbols.TryGetValue(name, out var loc) ? loc : null;
    }
}

/// <summary>
/// Location of a symbol definition in source code.
/// </summary>
public record SymbolLocation(int Line, int Column, SymbolKind Kind);

/// <summary>
/// Kind of symbol for go-to-definition.
/// </summary>
public enum SymbolKind
{
    Variable,
    Constant,
    Label,
    Subroutine,
    Function,
    Alias,
    Define
}

/// <summary>
/// Metadata extracted from ##Meta: directives in source code.
/// Used to feed autocomplete hints and configure the compiler.
/// </summary>
public class SourceMetadata
{
    /// <summary>
    /// Device type hints for autocomplete (from ##Meta:DeviceType or ALIAS statements).
    /// Maps alias name to device type name.
    /// </summary>
    public Dictionary<string, string> DeviceTypes { get; } = new();

    /// <summary>
    /// Property hints for autocomplete (from ##Meta:Properties).
    /// Maps device type to list of valid properties.
    /// </summary>
    public Dictionary<string, List<string>> DeviceProperties { get; } = new();

    /// <summary>
    /// Custom constants defined in source (from CONST/DEFINE statements).
    /// </summary>
    public Dictionary<string, double> Constants { get; } = new();

    /// <summary>
    /// Variables declared in source.
    /// </summary>
    public HashSet<string> Variables { get; } = new();

    /// <summary>
    /// Labels declared in source.
    /// </summary>
    public HashSet<string> Labels { get; } = new();

    /// <summary>
    /// User-defined functions/subs declared in source.
    /// </summary>
    public Dictionary<string, int> Functions { get; } = new(); // Name -> parameter count

    /// <summary>
    /// Compiler options extracted from ##Meta:Option directives.
    /// </summary>
    public CompilerOptions Options { get; set; } = new();
}
