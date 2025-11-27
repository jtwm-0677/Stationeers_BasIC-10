using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;
using BasicToMips.IC10;
using BasicToMips.Shared;

namespace BasicToMips.UI.Services;

public class CompilationResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ErrorLine { get; set; }
    public int LineCount { get; set; }
    public LanguageType DetectedLanguage { get; set; }
}

public class CompilerService
{
    /// <summary>
    /// Compile BASIC to IC10, or pass through raw IC10
    /// </summary>
    public CompilationResult Compile(string source, int optimizationLevel = 1)
    {
        try
        {
            // Detect the language
            var language = LanguageDetector.Detect(source);

            string output;
            if (language == LanguageType.IC10)
            {
                // It's already IC10 - pass through with validation
                output = ValidateAndCleanIC10(source);
            }
            else
            {
                // It's BASIC - compile it
                // Lexical analysis
                var lexer = new Lexer.Lexer(source);
                var tokens = lexer.Tokenize();

                // Parsing
                var parser = new Parser.Parser(tokens);
                var program = parser.Parse();

                // Code generation
                var generator = new MipsGenerator();
                output = generator.Generate(program);

                // Apply optimization
                if (optimizationLevel > 0)
                {
                    output = Optimize(output, optimizationLevel);
                }
            }

            var lineCount = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"));

            return new CompilationResult
            {
                Success = true,
                Output = output,
                LineCount = lineCount,
                DetectedLanguage = language
            };
        }
        catch (LexerException ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorLine = ex.Line,
                DetectedLanguage = LanguageType.Basic
            };
        }
        catch (ParserException ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorLine = ex.Line,
                DetectedLanguage = LanguageType.Basic
            };
        }
        catch (Exception ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                DetectedLanguage = LanguageType.Unknown
            };
        }
    }

    /// <summary>
    /// Decompile IC10 back to BASIC
    /// </summary>
    public CompilationResult Decompile(string ic10Source)
    {
        try
        {
            var parser = new IC10Parser();
            var program = parser.Parse(ic10Source);

            var decompiler = new IC10Decompiler(program);
            var output = decompiler.Decompile();

            return new CompilationResult
            {
                Success = true,
                Output = output,
                DetectedLanguage = LanguageType.IC10
            };
        }
        catch (Exception ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = $"Decompilation error: {ex.Message}",
                DetectedLanguage = LanguageType.IC10
            };
        }
    }

    /// <summary>
    /// Validate and clean IC10 code
    /// </summary>
    private string ValidateAndCleanIC10(string source)
    {
        var lines = source.Split('\n');
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return string.Join("\n", result);
    }

    private string Optimize(string code, int level)
    {
        var lines = code.Split('\n').ToList();
        var result = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines in aggressive mode
            if (level >= 2 && string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Remove comments in aggressive mode
            if (level >= 2 && trimmed.StartsWith("#"))
                continue;

            // Remove inline comments in aggressive mode
            if (level >= 2 && trimmed.Contains(" #"))
            {
                trimmed = trimmed.Substring(0, trimmed.IndexOf(" #")).TrimEnd();
            }

            result.Add(trimmed);
        }

        // Additional optimizations for level 2
        if (level >= 2)
        {
            result = OptimizeRegisterUsage(result);
            result = RemoveDeadCode(result);
            result = CombineOperations(result);
        }

        return string.Join("\n", result);
    }

    private List<string> OptimizeRegisterUsage(List<string> lines)
    {
        // TODO: Implement register allocation optimization
        return lines;
    }

    private List<string> RemoveDeadCode(List<string> lines)
    {
        // TODO: Implement dead code elimination
        return lines;
    }

    private List<string> CombineOperations(List<string> lines)
    {
        // TODO: Implement operation combining (e.g., move followed by add)
        return lines;
    }
}
