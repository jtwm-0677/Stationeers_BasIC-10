using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;

namespace BasicToMips.UI.Services;

public class CompilationResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ErrorLine { get; set; }
    public int LineCount { get; set; }
}

public class CompilerService
{
    public CompilationResult Compile(string source, int optimizationLevel = 1)
    {
        try
        {
            // Lexical analysis
            var lexer = new Lexer.Lexer(source);
            var tokens = lexer.Tokenize();

            // Parsing
            var parser = new Parser.Parser(tokens);
            var program = parser.Parse();

            // Code generation
            var generator = new MipsGenerator();
            var output = generator.Generate(program);

            // Apply optimization
            if (optimizationLevel > 0)
            {
                output = Optimize(output, optimizationLevel);
            }

            var lineCount = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"));

            return new CompilationResult
            {
                Success = true,
                Output = output,
                LineCount = lineCount
            };
        }
        catch (LexerException ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorLine = ex.Line
            };
        }
        catch (ParserException ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorLine = ex.Line
            };
        }
        catch (Exception ex)
        {
            return new CompilationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
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
