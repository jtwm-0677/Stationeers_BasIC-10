using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;
using BasicToMips.IC10;
using BasicToMips.Shared;
using BasicToMips.Analysis;
using BasicToMips.Preprocessing;

namespace BasicToMips.UI.Services;

public class CompilationResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ErrorLine { get; set; }
    public int LineCount { get; set; }
    public LanguageType DetectedLanguage { get; set; }
    public SourceMetadata? Metadata { get; set; }
    public SourceMap? SourceMap { get; set; }
    public List<AnalysisWarning> Warnings { get; set; } = new();
    public List<string> IncludedFiles { get; set; } = new();
    public List<PreprocessorError> PreprocessorErrors { get; set; } = new();
}

public class CompilerService
{
    /// <summary>
    /// Compile BASIC to IC10, or pass through raw IC10
    /// </summary>
    public CompilationResult Compile(string source, int optimizationLevel = 1)
        => Compile(source, new CompilerOptions { OptimizationLevel = optimizationLevel });

    /// <summary>
    /// Compile BASIC to IC10 with explicit options
    /// </summary>
    public CompilationResult Compile(string source, CompilerOptions options)
    {
        try
        {
            // Detect the language
            var language = LanguageDetector.Detect(source);
            var metadata = new SourceMetadata { Options = options };
            SourceMap? sourceMap = null;
            var warnings = new List<AnalysisWarning>();
            var includedFiles = new List<string>();
            var preprocessorErrors = new List<PreprocessorError>();

            string output;
            if (language == LanguageType.IC10)
            {
                // It's already IC10 - pass through with validation
                output = ValidateAndCleanIC10(source);
            }
            else
            {
                // It's BASIC - preprocess and compile it
                // Preprocessing - resolve INCLUDE directives
                var preprocessor = new Preprocessor();
                var preprocessResult = preprocessor.Process(source, options.SourceFilePath);
                includedFiles = preprocessResult.IncludedFiles;
                preprocessorErrors = preprocessResult.Errors;

                // Use preprocessed source for compilation
                var processedSource = preprocessResult.ProcessedSource;

                // Lexical analysis - pass PreserveComments option to lexer
                var lexer = new Lexer.Lexer(processedSource, options.PreserveComments);
                var tokens = lexer.Tokenize();

                // Parsing
                var parser = new Parser.Parser(tokens);
                var program = parser.Parse();

                // Static analysis for warnings
                var analyzer = new StaticAnalyzer();
                warnings = analyzer.Analyze(program);

                // Extract metadata from AST for autocomplete
                ExtractMetadata(program, metadata);

                // Parse ##Meta: directives to update options
                ParseMetaDirectives(program, metadata);

                // Code generation with options and source map
                var generator = new MipsGenerator();
                var genResult = generator.GenerateWithSourceMap(program, metadata.Options);
                output = genResult.Code;
                sourceMap = genResult.SourceMap;

                // Add generator warnings (like line limit exceeded)
                foreach (var w in genResult.Warnings)
                {
                    warnings.Add(new AnalysisWarning(WarningType.PossibleError, w, 0));
                }

                // Apply optimization
                if (metadata.Options.OptimizationLevel > 0)
                {
                    output = Optimize(output, metadata.Options.OptimizationLevel);
                }
            }

            var lineCount = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"));

            return new CompilationResult
            {
                Success = true,
                Output = output,
                LineCount = lineCount,
                DetectedLanguage = language,
                Metadata = metadata,
                SourceMap = sourceMap,
                Warnings = warnings,
                IncludedFiles = includedFiles,
                PreprocessorErrors = preprocessorErrors
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
    /// Extract metadata from AST for autocomplete hints
    /// </summary>
    private void ExtractMetadata(BasicToMips.AST.ProgramNode program, SourceMetadata metadata)
    {
        foreach (var stmt in program.Statements)
        {
            switch (stmt)
            {
                case BasicToMips.AST.AliasStatement alias:
                    // Extract device type from ALIAS statements
                    if (alias.DeviceReference != null)
                    {
                        var devRef = alias.DeviceReference;
                        if (devRef.DeviceHash is BasicToMips.AST.VariableExpression varHash)
                        {
                            // Device type name stored in hash (e.g., "StructureGasSensor")
                            metadata.DeviceTypes[alias.AliasName] = varHash.Name;
                        }
                        else if (devRef.DeviceHash is BasicToMips.AST.StringLiteral strHash)
                        {
                            metadata.DeviceTypes[alias.AliasName] = strHash.Value;
                        }
                    }
                    break;

                case BasicToMips.AST.ConstStatement constStmt:
                    if (constStmt.Value is BasicToMips.AST.NumberLiteral numVal)
                    {
                        metadata.Constants[constStmt.ConstantName] = numVal.Value;
                    }
                    break;

                case BasicToMips.AST.DefineStatement define:
                    if (define.Value is BasicToMips.AST.NumberLiteral defVal)
                    {
                        metadata.Constants[define.ConstantName] = defVal.Value;
                    }
                    break;

                case BasicToMips.AST.LetStatement let:
                    metadata.Variables.Add(let.VariableName);
                    break;

                case BasicToMips.AST.VarStatement varStmt:
                    metadata.Variables.Add(varStmt.VariableName);
                    break;

                case BasicToMips.AST.LabelStatement label:
                    metadata.Labels.Add(label.Name);
                    break;

                case BasicToMips.AST.SubDefinition sub:
                    metadata.Functions[sub.Name] = sub.Parameters.Count;
                    break;

                case BasicToMips.AST.FunctionDefinition func:
                    metadata.Functions[func.Name] = func.Parameters.Count;
                    break;
            }
        }
    }

    /// <summary>
    /// Parse ##Meta: directives to configure compiler options
    /// </summary>
    private void ParseMetaDirectives(BasicToMips.AST.ProgramNode program, SourceMetadata metadata)
    {
        foreach (var stmt in program.Statements)
        {
            if (stmt is BasicToMips.AST.CommentStatement comment && comment.IsMetaComment)
            {
                var text = comment.Text;
                if (text.StartsWith("#Meta:", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(6).Trim(); // Remove "#Meta:" prefix
                }
                else if (text.StartsWith("Meta:", StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(5).Trim(); // Remove "Meta:" prefix
                }

                // Parse directive: Key=Value or Key:Value
                var parts = text.Split(new[] { '=', ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim().ToUpperInvariant();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "PRESERVECOMMENTS":
                            metadata.Options.PreserveComments = value.Equals("true", StringComparison.OrdinalIgnoreCase)
                                || value == "1";
                            break;

                        case "DEBUGCOMMENTS":
                            metadata.Options.EmitDebugComments = value.Equals("true", StringComparison.OrdinalIgnoreCase)
                                || value == "1";
                            break;

                        case "OPTIMIZATION":
                            if (int.TryParse(value, out var optLevel))
                            {
                                metadata.Options.OptimizationLevel = optLevel;
                            }
                            break;

                        case "SOURCELINECOMMENTS":
                        case "LINECOMMENTS":
                            metadata.Options.EmitSourceLineComments = value.Equals("true", StringComparison.OrdinalIgnoreCase)
                                || value == "1";
                            break;

                        case "DEVICETYPE":
                            // Format: DeviceType: aliasName=TypeName
                            var typeParts = value.Split('=', 2);
                            if (typeParts.Length == 2)
                            {
                                metadata.DeviceTypes[typeParts[0].Trim()] = typeParts[1].Trim();
                            }
                            break;

                        case "PROPERTY":
                            // Format: Property: TypeName=Prop1,Prop2,Prop3
                            var propParts = value.Split('=', 2);
                            if (propParts.Length == 2)
                            {
                                var typeName = propParts[0].Trim();
                                var props = propParts[1].Split(',').Select(p => p.Trim()).ToList();
                                metadata.DeviceProperties[typeName] = props;
                            }
                            break;
                    }
                }
            }
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
