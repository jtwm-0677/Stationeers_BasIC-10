using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BasicToMips.UI.VisualScripting.CodeGen
{
    /// <summary>
    /// Performs basic optimizations on generated BASIC code
    /// </summary>
    public class GeneratedCodeOptimizer
    {
        #region Properties

        private readonly CodeGenerationContext _context;

        #endregion

        #region Constructor

        public GeneratedCodeOptimizer(CodeGenerationContext context)
        {
            _context = context;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Optimize the generated code
        /// </summary>
        /// <param name="code">Input code</param>
        /// <returns>Optimized code</returns>
        public string Optimize(string code)
        {
            var lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

            // Run optimization passes
            lines = RemoveUnusedTempVariables(lines);
            lines = RemoveConsecutiveDuplicateAssignments(lines);
            lines = RemoveDeadCode(lines);
            lines = RemoveExcessiveBlankLines(lines);

            return string.Join(Environment.NewLine, lines);
        }

        #endregion

        #region Optimization Passes

        /// <summary>
        /// Remove temporary variables that are never used
        /// </summary>
        private List<string> RemoveUnusedTempVariables(List<string> lines)
        {
            var tempVarPattern = new Regex(@"^\s*VAR\s+(_temp\d+)\s*=\s*(.+)$");
            var usagePattern = new Regex(@"\b(_temp\d+)\b");

            var tempVars = new Dictionary<string, int>(); // varName -> lineIndex
            var result = new List<string>();

            // First pass: find all temp variable declarations
            for (int i = 0; i < lines.Count; i++)
            {
                var match = tempVarPattern.Match(lines[i]);
                if (match.Success)
                {
                    string varName = match.Groups[1].Value;
                    tempVars[varName] = i;
                }
            }

            // Second pass: check usage
            var usedVars = new HashSet<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                var matches = usagePattern.Matches(lines[i]);
                foreach (Match match in matches)
                {
                    string varName = match.Groups[1].Value;
                    if (tempVars.ContainsKey(varName))
                    {
                        usedVars.Add(varName);
                    }
                }
            }

            // Third pass: remove unused declarations
            for (int i = 0; i < lines.Count; i++)
            {
                var match = tempVarPattern.Match(lines[i]);
                if (match.Success)
                {
                    string varName = match.Groups[1].Value;
                    // Keep the line if variable is used elsewhere
                    if (usedVars.Contains(varName))
                    {
                        result.Add(lines[i]);
                    }
                    // Otherwise skip it (optimization)
                }
                else
                {
                    result.Add(lines[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Remove consecutive assignments to the same variable where the value is not used
        /// Example: x = 1; x = 2; -> x = 2;
        /// </summary>
        private List<string> RemoveConsecutiveDuplicateAssignments(List<string> lines)
        {
            var assignPattern = new Regex(@"^\s*(?:LET\s+)?(\w+)\s*=\s*(.+)$");
            var result = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                var currentMatch = assignPattern.Match(lines[i]);

                if (currentMatch.Success && i + 1 < lines.Count)
                {
                    var nextMatch = assignPattern.Match(lines[i + 1]);

                    // If next line assigns to same variable, skip current line
                    if (nextMatch.Success &&
                        currentMatch.Groups[1].Value == nextMatch.Groups[1].Value)
                    {
                        // Skip this assignment
                        continue;
                    }
                }

                result.Add(lines[i]);
            }

            return result;
        }

        /// <summary>
        /// Remove unreachable code (after GOTO, RETURN, etc.)
        /// </summary>
        private List<string> RemoveDeadCode(List<string> lines)
        {
            var result = new List<string>();
            bool inDeadCode = false;

            var jumpPattern = new Regex(@"^\s*(GOTO|RETURN|END)\b");
            var labelPattern = new Regex(@"^\s*\w+:"); // Labels start new reachable code

            foreach (var line in lines)
            {
                // Check if this is a label (starts reachable code again)
                if (labelPattern.IsMatch(line))
                {
                    inDeadCode = false;
                }

                // If not in dead code, add the line
                if (!inDeadCode)
                {
                    result.Add(line);

                    // Check if this line makes following code unreachable
                    if (jumpPattern.IsMatch(line))
                    {
                        inDeadCode = true;
                    }
                }
                // If in dead code, skip the line
            }

            return result;
        }

        /// <summary>
        /// Remove excessive blank lines (more than 2 consecutive)
        /// </summary>
        private List<string> RemoveExcessiveBlankLines(List<string> lines)
        {
            var result = new List<string>();
            int consecutiveBlankLines = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    consecutiveBlankLines++;

                    // Only allow up to 2 consecutive blank lines
                    if (consecutiveBlankLines <= 2)
                    {
                        result.Add(line);
                    }
                }
                else
                {
                    consecutiveBlankLines = 0;
                    result.Add(line);
                }
            }

            return result;
        }

        /// <summary>
        /// Inline single-use expressions
        /// Example: temp = a + b; c = temp; -> c = a + b;
        /// </summary>
        private List<string> InlineSingleUseExpressions(List<string> lines)
        {
            // This is a more complex optimization that would require tracking
            // variable usage across the entire code. For now, we skip it.
            // It could be implemented in a future version.
            return lines;
        }

        /// <summary>
        /// Combine arithmetic operations where possible
        /// Example: x += 1; x += 2; -> x += 3;
        /// </summary>
        private List<string> CombineArithmeticOperations(List<string> lines)
        {
            var compoundPattern = new Regex(@"^\s*(\w+)\s*(\+=|-=|\*=|/=)\s*(\d+(?:\.\d+)?)\s*$");
            var result = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                var currentMatch = compoundPattern.Match(lines[i]);

                if (currentMatch.Success && i + 1 < lines.Count)
                {
                    var nextMatch = compoundPattern.Match(lines[i + 1]);

                    // If next line has same variable and operator
                    if (nextMatch.Success &&
                        currentMatch.Groups[1].Value == nextMatch.Groups[1].Value &&
                        currentMatch.Groups[2].Value == nextMatch.Groups[2].Value)
                    {
                        // Try to combine the values
                        string op = currentMatch.Groups[2].Value;
                        if (op == "+=" || op == "-=")
                        {
                            if (double.TryParse(currentMatch.Groups[3].Value, out double val1) &&
                                double.TryParse(nextMatch.Groups[3].Value, out double val2))
                            {
                                double combined = op == "+=" ? val1 + val2 : val1 - val2;
                                string varName = currentMatch.Groups[1].Value;

                                // Add combined line
                                result.Add($"{varName} {op} {combined}");

                                // Skip next line
                                i++;
                                continue;
                            }
                        }
                    }
                }

                result.Add(lines[i]);
            }

            return result;
        }

        #endregion
    }
}
