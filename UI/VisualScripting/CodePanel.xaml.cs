using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using BasicToMips.UI.VisualScripting.ViewModels;
using BasicToMips.Editor.Highlighting;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Code panel that displays generated BASIC or IC10 code alongside the visual canvas
    /// </summary>
    public partial class CodePanel : System.Windows.Controls.UserControl
    {
        private readonly CodePanelViewModel _viewModel;
        private readonly HighlightedLineBackgroundRenderer _lineHighlighter;

        /// <summary>
        /// Gets the view model for this code panel
        /// </summary>
        public CodePanelViewModel ViewModel => _viewModel;

        /// <summary>
        /// Event raised when a line is clicked in the editor
        /// </summary>
        public event EventHandler<LineClickedEventArgs>? LineClicked;

        /// <summary>
        /// Whether to show the IC10 toggle button
        /// </summary>
        public bool ShowIC10Toggle
        {
            get => ToggleViewButton.Visibility == Visibility.Visible;
            set => ToggleViewButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Whether to show line numbers in the editor
        /// </summary>
        public bool ShowLineNumbers
        {
            get => CodeEditor.ShowLineNumbers;
            set => CodeEditor.ShowLineNumbers = value;
        }

        /// <summary>
        /// Gets the current BASIC code (convenience property for API access)
        /// </summary>
        public string BasicCode => _viewModel.GeneratedCode;

        /// <summary>
        /// Gets the current IC10 code (convenience property for API access)
        /// </summary>
        public string Ic10Code => _viewModel.IC10Code;

        public CodePanel()
        {
            InitializeComponent();

            _viewModel = new CodePanelViewModel();
            DataContext = _viewModel;

            // Setup syntax highlighting for BASIC (with error handling)
            try
            {
                CodeEditor.SyntaxHighlighting = BasicHighlighting.Create();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize syntax highlighting: {ex.Message}");
                // Continue without syntax highlighting
            }

            // Setup line highlighter for selection sync
            _lineHighlighter = new HighlightedLineBackgroundRenderer(CodeEditor);
            CodeEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlighter);

            // Subscribe to property changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // Subscribe to click events in the editor
            CodeEditor.TextArea.TextView.MouseDown += TextView_MouseDown;

            // Bind the text
            UpdateEditorText();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CodePanelViewModel.CurrentCode))
            {
                UpdateEditorText();
            }
            else if (e.PropertyName == nameof(CodePanelViewModel.ShowIC10))
            {
                UpdateSyntaxHighlighting();
                ToggleViewText.Text = _viewModel.ViewModeText;
            }
            else if (e.PropertyName == nameof(CodePanelViewModel.LineCountDisplay))
            {
                LineCountText.Text = _viewModel.LineCountDisplay;
            }
            else if (e.PropertyName == nameof(CodePanelViewModel.HasErrors))
            {
                ErrorPanel.Visibility = _viewModel.HasErrors ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.PropertyName == nameof(CodePanelViewModel.ErrorMessage))
            {
                ErrorText.Text = _viewModel.ErrorMessage;
            }
        }

        private void UpdateEditorText()
        {
            var currentOffset = CodeEditor.CaretOffset;
            CodeEditor.Document.Text = _viewModel.CurrentCode;

            // Restore caret position if possible
            if (currentOffset <= CodeEditor.Document.TextLength)
            {
                CodeEditor.CaretOffset = currentOffset;
            }
        }

        private void UpdateSyntaxHighlighting()
        {
            if (_viewModel.ShowIC10)
            {
                CodeEditor.SyntaxHighlighting = MipsHighlighting.Create();
            }
            else
            {
                CodeEditor.SyntaxHighlighting = BasicHighlighting.Create();
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_viewModel.CurrentCode);
                // Could show a brief success notification here
            }
            catch (Exception)
            {
                // Handle clipboard access errors silently
            }
        }

        private void ToggleViewButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowIC10 = !_viewModel.ShowIC10;
        }

        private void TextView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Get the line number at the click position
            var position = CodeEditor.TextArea.TextView.GetPositionFloor(e.GetPosition(CodeEditor.TextArea.TextView) + CodeEditor.TextArea.TextView.ScrollOffset);
            if (position.HasValue)
            {
                var line = position.Value.Line;
                LineClicked?.Invoke(this, new LineClickedEventArgs(line));
            }
        }

        /// <summary>
        /// Highlight specific lines in the editor
        /// </summary>
        /// <param name="lineNumbers">Line numbers to highlight (1-based)</param>
        public void HighlightLines(params int[] lineNumbers)
        {
            _lineHighlighter.SetHighlightedLines(lineNumbers);
            CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        /// <summary>
        /// Clear all line highlights
        /// </summary>
        public void ClearHighlights()
        {
            _lineHighlighter.ClearHighlightedLines();
            CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        /// <summary>
        /// Scroll to a specific line
        /// </summary>
        /// <param name="lineNumber">Line number (1-based)</param>
        public void ScrollToLine(int lineNumber)
        {
            if (lineNumber > 0 && lineNumber <= CodeEditor.Document.LineCount)
            {
                CodeEditor.ScrollToLine(lineNumber);
            }
        }
    }

    /// <summary>
    /// Event args for line click events
    /// </summary>
    public class LineClickedEventArgs : EventArgs
    {
        public int LineNumber { get; }

        public LineClickedEventArgs(int lineNumber)
        {
            LineNumber = lineNumber;
        }
    }

    /// <summary>
    /// Background renderer for highlighting specific lines
    /// </summary>
    public class HighlightedLineBackgroundRenderer : IBackgroundRenderer
    {
        private readonly TextEditor _editor;
        private readonly HashSet<int> _highlightedLines = new();
        private static readonly Color HighlightColor = Color.FromArgb(128, 255, 255, 136); // Yellow with 50% opacity

        public HighlightedLineBackgroundRenderer(TextEditor editor)
        {
            _editor = editor;
        }

        public KnownLayer Layer => KnownLayer.Background;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_highlightedLines.Count == 0)
                return;

            textView.EnsureVisualLines();

            foreach (var lineNumber in _highlightedLines)
            {
                if (lineNumber < 1 || lineNumber > _editor.Document.LineCount)
                    continue;

                var line = _editor.Document.GetLineByNumber(lineNumber);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
                {
                    var brush = new SolidColorBrush(HighlightColor);
                    brush.Freeze();
                    drawingContext.DrawRectangle(brush, null, rect);
                }
            }
        }

        public void SetHighlightedLines(params int[] lineNumbers)
        {
            _highlightedLines.Clear();
            foreach (var line in lineNumbers)
            {
                _highlightedLines.Add(line);
            }
        }

        public void ClearHighlightedLines()
        {
            _highlightedLines.Clear();
        }
    }
}
