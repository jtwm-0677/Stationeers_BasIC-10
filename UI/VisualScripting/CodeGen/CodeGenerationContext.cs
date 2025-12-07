using System;
using System.Collections.Generic;
using System.Text;

namespace BasicToMips.UI.VisualScripting.CodeGen
{
    /// <summary>
    /// Tracks state during code generation
    /// </summary>
    public class CodeGenerationContext
    {
        #region Properties

        /// <summary>
        /// Source map being built
        /// </summary>
        public SourceMap SourceMap { get; } = new();

        /// <summary>
        /// Generated code lines
        /// </summary>
        public List<string> Lines { get; } = new();

        /// <summary>
        /// Current indentation level (0 = no indent, 1 = 4 spaces, etc.)
        /// </summary>
        public int IndentLevel { get; set; } = 0;

        /// <summary>
        /// Spaces per indent level
        /// </summary>
        public int IndentSize { get; set; } = 4;

        /// <summary>
        /// Counter for generating unique temporary variable names
        /// </summary>
        public int TempVariableCounter { get; set; } = 0;

        /// <summary>
        /// Maps node IDs to their assigned variable names
        /// Used for nodes that produce values (like variables, constants, expressions)
        /// </summary>
        public Dictionary<Guid, string> NodeVariableNames { get; } = new();

        /// <summary>
        /// Maps node output pin IDs to their expression strings
        /// Used for inline expressions
        /// </summary>
        public Dictionary<Guid, string> PinExpressions { get; } = new();

        /// <summary>
        /// Current scope (main, subroutine name, etc.)
        /// </summary>
        public string CurrentScope { get; set; } = "main";

        /// <summary>
        /// Errors collected during generation
        /// </summary>
        public List<CodeGenerationError> Errors { get; } = new();

        /// <summary>
        /// Warnings collected during generation
        /// </summary>
        public List<CodeGenerationWarning> Warnings { get; } = new();

        /// <summary>
        /// Set of variable names that have been declared
        /// Used to prevent duplicate declarations
        /// </summary>
        public HashSet<string> DeclaredVariables { get; } = new();

        /// <summary>
        /// Set of label names that have been defined
        /// Used to prevent duplicate labels
        /// </summary>
        public HashSet<string> DeclaredLabels { get; } = new();

        /// <summary>
        /// Nodes that have been processed (to avoid duplicate generation)
        /// </summary>
        public HashSet<Guid> ProcessedNodes { get; } = new();

        #endregion

        #region Methods

        /// <summary>
        /// Add a line of code with current indentation
        /// </summary>
        /// <param name="nodeId">The node that generated this line</param>
        /// <param name="line">The line of code</param>
        public void AddLine(Guid nodeId, string line)
        {
            string indentedLine = GetIndent() + line;
            Lines.Add(indentedLine);

            int lineNumber = Lines.Count;
            SourceMap.AddMapping(nodeId, lineNumber, indentedLine);
        }

        /// <summary>
        /// Add multiple lines of code with current indentation
        /// </summary>
        /// <param name="nodeId">The node that generated these lines</param>
        /// <param name="lines">The lines of code</param>
        public void AddLines(Guid nodeId, params string[] lines)
        {
            foreach (var line in lines)
            {
                AddLine(nodeId, line);
            }
        }

        /// <summary>
        /// Add a blank line (not mapped to any node)
        /// </summary>
        public void AddBlankLine()
        {
            Lines.Add(string.Empty);
        }

        /// <summary>
        /// Add a comment line
        /// </summary>
        /// <param name="comment">Comment text (without the # prefix)</param>
        public void AddComment(string comment)
        {
            Lines.Add(GetIndent() + "# " + comment);
        }

        /// <summary>
        /// Add a section header comment
        /// </summary>
        /// <param name="sectionName">Name of the section</param>
        public void AddSectionHeader(string sectionName)
        {
            AddBlankLine();
            AddComment($"--- {sectionName} ---");
        }

        /// <summary>
        /// Increase indentation level
        /// </summary>
        public void Indent()
        {
            IndentLevel++;
        }

        /// <summary>
        /// Decrease indentation level
        /// </summary>
        public void Unindent()
        {
            if (IndentLevel > 0)
                IndentLevel--;
        }

        /// <summary>
        /// Get current indentation string
        /// </summary>
        public string GetIndent()
        {
            return new string(' ', IndentLevel * IndentSize);
        }

        /// <summary>
        /// Generate a unique temporary variable name
        /// </summary>
        public string GetTempVariable()
        {
            return $"_temp{++TempVariableCounter}";
        }

        /// <summary>
        /// Add an error
        /// </summary>
        public void AddError(Guid nodeId, string message)
        {
            Errors.Add(new CodeGenerationError
            {
                NodeId = nodeId,
                Message = message
            });
        }

        /// <summary>
        /// Add a warning
        /// </summary>
        public void AddWarning(Guid nodeId, string message)
        {
            Warnings.Add(new CodeGenerationWarning
            {
                NodeId = nodeId,
                Message = message
            });
        }

        /// <summary>
        /// Get the final generated code as a single string
        /// </summary>
        public string GetCode()
        {
            return string.Join(Environment.NewLine, Lines);
        }

        /// <summary>
        /// Check if generation was successful (no errors)
        /// </summary>
        public bool IsSuccessful => Errors.Count == 0;

        #endregion
    }

    /// <summary>
    /// Represents a code generation error
    /// </summary>
    public class CodeGenerationError
    {
        public Guid NodeId { get; set; }
        public string Message { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Error in node {NodeId}: {Message}";
        }
    }

    /// <summary>
    /// Represents a code generation warning
    /// </summary>
    public class CodeGenerationWarning
    {
        public Guid NodeId { get; set; }
        public string Message { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Warning in node {NodeId}: {Message}";
        }
    }
}
