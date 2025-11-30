using System.Text;
using System.Text.RegularExpressions;

namespace BasicToMips.Preprocessing;

/// <summary>
/// Preprocessor that handles INCLUDE directives before lexing.
/// </summary>
public class Preprocessor
{
    private readonly HashSet<string> _includedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PreprocessorError> _errors = new();
    private string? _baseDirectory;

    // Regex to match INCLUDE "filename" or INCLUDE 'filename'
    private static readonly Regex IncludePattern = new(
        @"^\s*INCLUDE\s+[""']([^""']+)[""']\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Errors encountered during preprocessing.
    /// </summary>
    public IReadOnlyList<PreprocessorError> Errors => _errors;

    /// <summary>
    /// Files that were included during preprocessing.
    /// </summary>
    public IReadOnlySet<string> IncludedFiles => _includedFiles;

    /// <summary>
    /// Process source code, resolving all INCLUDE directives.
    /// </summary>
    /// <param name="source">The source code to process.</param>
    /// <param name="sourceFilePath">Optional path to the source file (for relative includes).</param>
    /// <returns>The processed source with all includes resolved.</returns>
    public PreprocessorResult Process(string source, string? sourceFilePath = null)
    {
        _errors.Clear();
        _includedFiles.Clear();

        // Set base directory for relative includes
        if (!string.IsNullOrEmpty(sourceFilePath))
        {
            _baseDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath));
            _includedFiles.Add(Path.GetFullPath(sourceFilePath));
        }
        else
        {
            _baseDirectory = Environment.CurrentDirectory;
        }

        var result = ProcessIncludes(source, sourceFilePath ?? "<input>", 1);

        return new PreprocessorResult
        {
            ProcessedSource = result.Source,
            SourceMappings = result.Mappings,
            Errors = _errors.ToList(),
            IncludedFiles = _includedFiles.ToList()
        };
    }

    private (string Source, List<SourceMapping> Mappings) ProcessIncludes(
        string source, string fileName, int depth)
    {
        if (depth > 10)
        {
            _errors.Add(new PreprocessorError(
                "Include depth exceeded (max 10 levels). Possible circular include.",
                fileName, 0));
            return (source, new List<SourceMapping>());
        }

        var result = new StringBuilder();
        var mappings = new List<SourceMapping>();
        var lines = source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int outputLine = 1;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;
            var match = IncludePattern.Match(line);

            if (match.Success)
            {
                var includePath = match.Groups[1].Value;
                var fullPath = ResolveIncludePath(includePath, fileName);

                if (fullPath == null)
                {
                    _errors.Add(new PreprocessorError(
                        $"Include file not found: {includePath}",
                        fileName, lineNumber));

                    // Keep the include line as a comment
                    result.AppendLine($"' ERROR: Include file not found: {includePath}");
                    mappings.Add(new SourceMapping(outputLine++, fileName, lineNumber));
                }
                else if (_includedFiles.Contains(fullPath))
                {
                    // Already included - skip to prevent circular includes
                    result.AppendLine($"' Already included: {includePath}");
                    mappings.Add(new SourceMapping(outputLine++, fileName, lineNumber));
                }
                else
                {
                    _includedFiles.Add(fullPath);

                    try
                    {
                        var includeContent = File.ReadAllText(fullPath);

                        // Add marker comment
                        result.AppendLine($"' BEGIN INCLUDE: {includePath}");
                        mappings.Add(new SourceMapping(outputLine++, fileName, lineNumber));

                        // Recursively process the included file
                        var (processedContent, childMappings) = ProcessIncludes(
                            includeContent, fullPath, depth + 1);

                        // Add the processed content
                        var includeLines = processedContent.Split(
                            new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        for (int j = 0; j < includeLines.Length; j++)
                        {
                            if (j < includeLines.Length - 1 || !string.IsNullOrEmpty(includeLines[j]))
                            {
                                result.AppendLine(includeLines[j]);

                                // Find mapping for this line
                                var childMapping = childMappings.FirstOrDefault(m => m.OutputLine == j + 1);
                                if (childMapping != null)
                                {
                                    mappings.Add(new SourceMapping(outputLine++,
                                        childMapping.SourceFile, childMapping.SourceLine));
                                }
                                else
                                {
                                    mappings.Add(new SourceMapping(outputLine++, fullPath, j + 1));
                                }
                            }
                        }

                        // Add end marker comment
                        result.AppendLine($"' END INCLUDE: {includePath}");
                        mappings.Add(new SourceMapping(outputLine++, fileName, lineNumber));
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(new PreprocessorError(
                            $"Error reading include file {includePath}: {ex.Message}",
                            fileName, lineNumber));

                        result.AppendLine($"' ERROR: Could not read: {includePath}");
                        mappings.Add(new SourceMapping(outputLine++, fileName, lineNumber));
                    }
                }
            }
            else
            {
                // Regular line - pass through
                result.AppendLine(line);
                mappings.Add(new SourceMapping(outputLine++, fileName, lineNumber));
            }
        }

        return (result.ToString(), mappings);
    }

    private string? ResolveIncludePath(string includePath, string currentFile)
    {
        // Try relative to current file first
        var currentDir = Path.GetDirectoryName(Path.GetFullPath(currentFile)) ?? _baseDirectory!;
        var relativePath = Path.Combine(currentDir, includePath);

        if (File.Exists(relativePath))
        {
            return Path.GetFullPath(relativePath);
        }

        // Try relative to base directory
        if (_baseDirectory != null)
        {
            var basePath = Path.Combine(_baseDirectory, includePath);
            if (File.Exists(basePath))
            {
                return Path.GetFullPath(basePath);
            }
        }

        // Try as absolute path
        if (Path.IsPathRooted(includePath) && File.Exists(includePath))
        {
            return Path.GetFullPath(includePath);
        }

        return null;
    }
}

/// <summary>
/// Result of preprocessing.
/// </summary>
public class PreprocessorResult
{
    public string ProcessedSource { get; set; } = string.Empty;
    public List<SourceMapping> SourceMappings { get; set; } = new();
    public List<PreprocessorError> Errors { get; set; } = new();
    public List<string> IncludedFiles { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// Maps a line in the processed output back to its original source.
/// </summary>
public class SourceMapping
{
    public int OutputLine { get; }
    public string SourceFile { get; }
    public int SourceLine { get; }

    public SourceMapping(int outputLine, string sourceFile, int sourceLine)
    {
        OutputLine = outputLine;
        SourceFile = sourceFile;
        SourceLine = sourceLine;
    }
}

/// <summary>
/// Error encountered during preprocessing.
/// </summary>
public class PreprocessorError
{
    public string Message { get; }
    public string File { get; }
    public int Line { get; }

    public PreprocessorError(string message, string file, int line)
    {
        Message = message;
        File = file;
        Line = line;
    }

    public override string ToString() => $"{File}({Line}): {Message}";
}
