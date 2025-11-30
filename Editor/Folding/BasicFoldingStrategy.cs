using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace BasicToMips.Editor.Folding;

/// <summary>
/// Folding strategy for BASIC-IC10 code blocks
/// Supports case-insensitive folding for all BASIC block structures
/// </summary>
public class BasicFoldingStrategy
{
    /// <summary>
    /// Block pair definitions for BASIC code
    /// Format: (startKeyword, endKeyword, requiresMatch)
    /// </summary>
    private readonly (string Start, string End, bool CaseSensitive)[] _blockPairs = new[]
    {
        // Control flow blocks
        ("IF", "ENDIF", false),
        ("IF", "END IF", false),
        ("WHILE", "WEND", false),
        ("FOR", "NEXT", false),
        ("DO", "LOOP", false),
        ("SELECT", "END SELECT", false),

        // Subroutine/function blocks
        ("SUB", "ENDSUB", false),
        ("SUB", "END SUB", false),
        ("FUNCTION", "ENDFUNCTION", false),
        ("FUNCTION", "END FUNCTION", false)
    };

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
        var foldings = CreateNewFoldings(document);
        manager.UpdateFoldings(foldings, -1);
    }

    public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document)
    {
        var foldings = new List<NewFolding>();

        if (document == null || document.TextLength == 0)
            return foldings;

        var lines = document.Lines.ToList();
        var blockStack = new Stack<(int LineNumber, int StartOffset, string StartKeyword, string EndKeyword)>();

        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineText = document.GetText(line.Offset, line.Length).Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(lineText) || lineText.StartsWith("'") || lineText.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
                continue;

            // Remove inline comments
            int commentPos = lineText.IndexOf('\'');
            if (commentPos >= 0)
                lineText = lineText.Substring(0, commentPos).Trim();

            // Check for block start keywords
            foreach (var (start, end, caseSensitive) in _blockPairs)
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                // Check if line starts with the keyword (must be at start or after whitespace)
                if (StartsWithKeyword(lineText, start, comparison))
                {
                    blockStack.Push((lineIndex, line.Offset, start, end));
                    break; // Only one start keyword per line
                }
            }

            // Check for block end keywords
            foreach (var (start, end, caseSensitive) in _blockPairs)
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                if (StartsWithKeyword(lineText, end, comparison))
                {
                    // Find matching start block
                    if (blockStack.Count > 0)
                    {
                        var (startLine, startOffset, startKeyword, endKeyword) = blockStack.Peek();

                        // Check if this end matches the current start
                        if (endKeyword.Equals(end, comparison))
                        {
                            blockStack.Pop();

                            // Create folding from start to end
                            // Don't create foldings for single-line blocks
                            if (lineIndex > startLine)
                            {
                                var endOffset = line.EndOffset;
                                var folding = new NewFolding(startOffset, endOffset)
                                {
                                    Name = GetFoldingName(startKeyword, document, startOffset),
                                    DefaultClosed = false
                                };
                                foldings.Add(folding);
                            }
                            break; // Only one end keyword per line
                        }
                    }
                    break;
                }
            }
        }

        // Sort foldings by start offset (required by AvalonEdit)
        foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

        return foldings;
    }

    /// <summary>
    /// Checks if text starts with a keyword (as a whole word, not part of another word)
    /// </summary>
    private bool StartsWithKeyword(string text, string keyword, StringComparison comparison)
    {
        if (!text.StartsWith(keyword, comparison))
            return false;

        // Check that keyword is followed by whitespace, end of line, or special character
        if (text.Length > keyword.Length)
        {
            char nextChar = text[keyword.Length];
            // Allow space, tab, colon, parenthesis, equals, etc. after keyword
            if (char.IsLetterOrDigit(nextChar) || nextChar == '_')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a friendly name for the folding (shown when collapsed)
    /// </summary>
    private string GetFoldingName(string keyword, TextDocument document, int offset)
    {
        // Get the full first line for context
        var line = document.GetLineByOffset(offset);
        var lineText = document.GetText(line.Offset, line.Length).Trim();

        // Remove comments
        int commentPos = lineText.IndexOf('\'');
        if (commentPos >= 0)
            lineText = lineText.Substring(0, commentPos).Trim();

        // Truncate if too long
        if (lineText.Length > 50)
            lineText = lineText.Substring(0, 47) + "...";

        return lineText;
    }
}
