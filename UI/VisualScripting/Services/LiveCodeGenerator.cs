using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using BasicToMips.UI.VisualScripting.CodeGen;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Wires;
using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;

namespace BasicToMips.UI.VisualScripting.Services
{
    /// <summary>
    /// Manages real-time code generation from the visual graph
    /// Debounces rapid changes and compiles BASIC to IC10
    /// </summary>
    public class LiveCodeGenerator
    {
        private readonly CodePanel _codePanel;
        private readonly DispatcherTimer _debounceTimer;
        private bool _needsRegeneration = false;
        private List<NodeBase> _nodes = new();
        private List<Wire> _wires = new();

        private const int DebounceDelayMs = 200; // Wait 200ms after last change

        /// <summary>
        /// Event raised when code is successfully generated
        /// </summary>
        public event EventHandler<CodeGeneratedEventArgs>? CodeGenerated;

        /// <summary>
        /// Event raised when code generation fails
        /// </summary>
        public event EventHandler<CodeGenerationErrorEventArgs>? GenerationFailed;

        /// <summary>
        /// Gets the last generated source map
        /// </summary>
        public SourceMap? LastSourceMap { get; private set; }

        public LiveCodeGenerator(CodePanel codePanel)
        {
            _codePanel = codePanel;

            // Setup debounce timer
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DebounceDelayMs)
            };
            _debounceTimer.Tick += DebounceTimer_Tick;
        }

        /// <summary>
        /// Notify that the graph has changed and code needs regeneration
        /// </summary>
        /// <param name="nodes">Current nodes in the graph</param>
        /// <param name="wires">Current wires in the graph</param>
        public void NotifyGraphChanged(List<NodeBase> nodes, List<Wire> wires)
        {
            _nodes = nodes;
            _wires = wires;
            _needsRegeneration = true;

            // Restart debounce timer
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        /// <summary>
        /// Force immediate regeneration of code (bypass debounce)
        /// </summary>
        public void RegenerateNow()
        {
            _debounceTimer.Stop();
            PerformCodeGeneration();
        }

        private void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();

            if (_needsRegeneration)
            {
                PerformCodeGeneration();
            }
        }

        private void PerformCodeGeneration()
        {
            _needsRegeneration = false;

            try
            {
                // Generate BASIC code from visual graph
                var generator = new GraphToBasicGenerator(_nodes, _wires);
                var (basicCode, sourceMap) = generator.GenerateWithSourceMap();

                LastSourceMap = sourceMap;

                // Check for generation errors
                if (!generator.IsSuccessful)
                {
                    var errors = generator.GetErrors();
                    var errorMessage = string.Join("\n", errors.Select(e => e.Message));

                    _codePanel.ViewModel.HasErrors = true;
                    _codePanel.ViewModel.ErrorMessage = errorMessage;
                    _codePanel.ViewModel.GeneratedCode = basicCode; // Show partial code
                    _codePanel.ViewModel.IC10Code = "";

                    GenerationFailed?.Invoke(this, new CodeGenerationErrorEventArgs(errorMessage));
                    return;
                }

                // Update BASIC code in panel
                _codePanel.ViewModel.GeneratedCode = basicCode;
                _codePanel.ViewModel.HasErrors = false;

                // Compile BASIC to IC10
                string ic10Code = CompileToIC10(basicCode);
                _codePanel.ViewModel.IC10Code = ic10Code;

                // Raise success event
                CodeGenerated?.Invoke(this, new CodeGeneratedEventArgs(basicCode, ic10Code, sourceMap));
            }
            catch (Exception ex)
            {
                _codePanel.ViewModel.HasErrors = true;
                _codePanel.ViewModel.ErrorMessage = $"Code generation error: {ex.Message}";

                GenerationFailed?.Invoke(this, new CodeGenerationErrorEventArgs(ex.Message));
            }
        }

        private string CompileToIC10(string basicCode)
        {
            try
            {
                // Use the existing compiler pipeline
                var lexer = new BasicToMips.Lexer.Lexer(basicCode, preserveComments: false);
                var tokens = lexer.Tokenize();

                var parser = new BasicToMips.Parser.Parser(tokens);
                var ast = parser.Parse();

                var mipsGen = new MipsGenerator();
                var ic10Code = mipsGen.Generate(ast);

                return ic10Code;
            }
            catch (Exception ex)
            {
                // Return error comment in IC10 format
                return $"# Compilation Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Clear all generated code
        /// </summary>
        public void Clear()
        {
            _debounceTimer.Stop();
            _needsRegeneration = false;
            _nodes.Clear();
            _wires.Clear();
            LastSourceMap = null;
            _codePanel.ViewModel.Clear();
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _debounceTimer.Stop();
        }
    }

    /// <summary>
    /// Event args for successful code generation
    /// </summary>
    public class CodeGeneratedEventArgs : EventArgs
    {
        public string BasicCode { get; }
        public string IC10Code { get; }
        public SourceMap SourceMap { get; }

        public CodeGeneratedEventArgs(string basicCode, string ic10Code, SourceMap sourceMap)
        {
            BasicCode = basicCode;
            IC10Code = ic10Code;
            SourceMap = sourceMap;
        }
    }

    /// <summary>
    /// Event args for code generation errors
    /// </summary>
    public class CodeGenerationErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }

        public CodeGenerationErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
