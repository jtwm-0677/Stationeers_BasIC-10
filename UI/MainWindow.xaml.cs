using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using BasicToMips.Lexer;
using BasicToMips.Parser;
using BasicToMips.CodeGen;
using BasicToMips.Editor.Highlighting;
using BasicToMips.Editor.Completion;
using BasicToMips.Editor.ErrorHighlighting;
using BasicToMips.Editor.Folding;
using BasicToMips.UI.Services;
using BasicToMips.UI.Dialogs;
using BasicToMips.Shared;
using BasicToMips.Refactoring;
using BasicToMips.Editor;
using BasicToMips.Editor.RetroEffects;
using ICSharpCode.AvalonEdit.Folding;
using BasicToMips.Services;

namespace BasicToMips.UI;

public partial class MainWindow : Window
{
    private string? _currentFilePath;
    private string? _workingDirectory;
    private string? _tempAutoSavePath;
    private bool _isModified;
    private CompletionWindow? _completionWindow;
    private readonly CompilerService _compiler;
    private readonly SettingsService _settings;
    private readonly DocumentationService _docs;
    private int _optimizationLevel = 1; // 0=None, 1=Basic, 2=Aggressive

    // Error highlighting
    private TextMarkerService? _textMarkerService;
    private readonly ErrorChecker _errorChecker = new();
    private DispatcherTimer? _errorCheckTimer;

    // Code folding
    private FoldingManager? _foldingManager;
    private readonly BasicFoldingStrategy _foldingStrategy = new();
    private DispatcherTimer? _foldingUpdateTimer;

    // Bidirectional editing sync
    private bool _suppressBasicUpdate = false;
    private bool _suppressMipsUpdate = false;
    private DispatcherTimer? _mipsSyncTimer;

    // Source map for debugging (IC10 line ↔ BASIC line mapping)
    private SourceMap? _sourceMap;

    // Breakpoint management
    private readonly BasicToMips.Editor.Debugging.BreakpointManager _breakpointManager = new();

    // Bookmark management
    private readonly BasicToMips.Editor.Debugging.BookmarkManager _bookmarkManager = new();

    // Refactoring service
    private readonly RefactoringService _refactoringService = new();

    // Watch variables management
    private readonly BasicToMips.Editor.Debugging.WatchManager _watchManager = new();

    // HTTP API Server for MCP integration
    private HttpApiServer? _httpApiServer;

    // Headless simulator for MCP integration
    private readonly BasicToMips.Simulator.IC10Simulator _mcpSimulator = new();

    // Autosave timer - saves every 30 seconds
    private DispatcherTimer? _autoSaveTimer;
    private DateTime _lastAutoSave = DateTime.MinValue;

    // Last compilation result for Problems Panel
    private CompilationResult? _lastCompilationResult;

    public MainWindow()
    {
        InitializeComponent();

        _compiler = new CompilerService();
        _settings = new SettingsService();
        _docs = new DocumentationService();

        InitializeTabManagement();
        SetupEditors();
        SetupAutoComplete();
        SetupTabKeyboardShortcuts();
        SetupF1Help();
        SetupSymbolsTimer();
        LoadSettings();
        PopulateDocumentation();
        PopulateSnippetsMenu();
        UpdateRecentFilesMenu();
        UpdateSymbolsList();
        SetupAutoSaveTimer();
        CheckForAutoSaveRecovery();
    }

    private void SetupEditors()
    {
        // Apply saved syntax colors before creating highlighting
        BasicHighlighting.SetColors(_settings.SyntaxColors);
        MipsHighlighting.SetColors(_settings.SyntaxColors);

        // Apply BASIC syntax highlighting
        BasicEditor.SyntaxHighlighting = BasicHighlighting.Create();
        MipsOutput.SyntaxHighlighting = MipsHighlighting.Create();

        // Editor events
        BasicEditor.TextArea.Caret.PositionChanged += (s, e) => UpdateCursorPosition();
        BasicEditor.TextArea.TextEntering += TextArea_TextEntering;
        BasicEditor.TextArea.TextEntered += TextArea_TextEntered;

        // Setup retro visual effects
        SetupRetroEffects();

        // Setup error highlighting
        SetupErrorHighlighting();

        // Setup code folding
        SetupCodeFolding();

        // Setup breakpoint margin
        SetupBreakpoints();

        // Setup bookmark margin
        SetupBookmarks();

        // Setup bidirectional editing
        SetupBidirectionalSync();

        // Setup source map navigation (click to jump between BASIC ↔ IC10)
        SetupSourceMapNavigation();

        // Set initial content
        BasicEditor.Text = GetWelcomeCode();
        UpdateLineCount();
    }

    private void SetupSourceMapNavigation()
    {
        // Ctrl+Click on IC10 output jumps to corresponding BASIC line
        MipsOutput.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && _sourceMap != null)
            {
                var pos = MipsOutput.GetPositionFromPoint(e.GetPosition(MipsOutput));
                if (pos.HasValue)
                {
                    var ic10Line = pos.Value.Line - 1; // 0-based
                    var basicLine = _sourceMap.GetBasicLine(ic10Line);
                    if (basicLine > 0)
                    {
                        // Jump to BASIC line
                        var line = BasicEditor.Document.GetLineByNumber(basicLine);
                        BasicEditor.ScrollToLine(basicLine);
                        BasicEditor.TextArea.Caret.Offset = line.Offset;
                        BasicEditor.TextArea.Caret.BringCaretToView();
                        BasicEditor.Focus();

                        // Flash highlight the line briefly
                        HighlightLine(BasicEditor, basicLine);
                        SetStatus($"IC10 line {ic10Line + 1} → BASIC line {basicLine}", true);
                        e.Handled = true;
                    }
                }
            }
        };

        // Ctrl+Click on BASIC editor jumps to corresponding IC10 line(s)
        BasicEditor.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && _sourceMap != null)
            {
                var pos = BasicEditor.GetPositionFromPoint(e.GetPosition(BasicEditor));
                if (pos.HasValue)
                {
                    var basicLine = pos.Value.Line; // 1-based
                    var ic10Lines = _sourceMap.GetIC10Lines(basicLine);
                    if (ic10Lines.Count > 0)
                    {
                        // Jump to first IC10 line
                        var ic10Line = ic10Lines[0] + 1; // 1-based for display
                        if (ic10Line <= MipsOutput.Document.LineCount)
                        {
                            var line = MipsOutput.Document.GetLineByNumber(ic10Line);
                            MipsOutput.ScrollToLine(ic10Line);
                            MipsOutput.TextArea.Caret.Offset = line.Offset;
                            MipsOutput.TextArea.Caret.BringCaretToView();
                            MipsOutput.Focus();

                            // Flash highlight
                            HighlightLine(MipsOutput, ic10Line);
                            var lineInfo = ic10Lines.Count > 1
                                ? $"BASIC line {basicLine} → IC10 lines {string.Join(", ", ic10Lines.Select(l => l + 1))}"
                                : $"BASIC line {basicLine} → IC10 line {ic10Line}";
                            SetStatus(lineInfo, true);
                            e.Handled = true;
                        }
                    }
                }
            }
        };
    }

    private void HighlightLine(ICSharpCode.AvalonEdit.TextEditor editor, int lineNumber)
    {
        // Temporarily highlight a line by selecting it
        if (lineNumber > 0 && lineNumber <= editor.Document.LineCount)
        {
            var line = editor.Document.GetLineByNumber(lineNumber);
            editor.Select(line.Offset, line.Length);

            // Clear selection after a brief moment
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                editor.SelectionLength = 0;
            };
            timer.Start();
        }
    }

    private void SetupRetroEffects()
    {
        // Block cursor (classic BASIC style)
        if (_settings.BlockCursorEnabled)
        {
            BlockCaretManager.EnableBlockCaret(BasicEditor.TextArea, Color.FromRgb(0, 200, 255));
        }

        // Current line highlight
        if (_settings.CurrentLineHighlightEnabled)
        {
            CurrentLineHighlighterManager.EnableHighlighter(BasicEditor.TextArea, Color.FromArgb(25, 100, 180, 255));
        }

        // Scanline overlay
        if (_settings.ScanlineOverlayEnabled)
        {
            ScanlineOverlayManager.SetEnabled(BasicEditor.TextArea, true);
        }

        // Screen glow
        if (_settings.ScreenGlowEnabled)
        {
            ScreenGlowManager.SetEnabled(BasicEditor, true);
        }

        // Retro font
        if (_settings.RetroFontEnabled)
        {
            RetroFontManager.SetEnabled(BasicEditor, true, _settings.RetroFontChoice);
            RetroFontManager.SetEnabled(MipsOutput, true, _settings.RetroFontChoice);
        }

        // Set font choice menu checkmarks
        UpdateFontMenuCheckmarks();

        // Startup beep
        if (_settings.StartupBeepEnabled)
        {
            StartupBeepManager.PlayStartupBeep();
        }
    }

    private void SetupBreakpoints()
    {
        // Add breakpoint margin to the left of the BASIC editor
        var breakpointMargin = new BasicToMips.Editor.Debugging.BreakpointMargin(BasicEditor, _breakpointManager);
        BasicEditor.TextArea.LeftMargins.Insert(0, breakpointMargin);

        // Setup F9 to toggle breakpoints
        BasicEditor.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.F9)
            {
                var line = BasicEditor.TextArea.Caret.Line;
                var wasSet = _breakpointManager.ToggleBreakpoint(line);
                SetStatus($"Breakpoint {(wasSet ? "set" : "removed")} at line {line}", true);
                e.Handled = true;
            }
        };

        // Track breakpoints when document changes (adjust line numbers)
        BasicEditor.Document.Changed += (s, e) =>
        {
            // Calculate line delta from the change
            if (e.InsertionLength > 0 || e.RemovalLength > 0)
            {
                var changeOffset = e.Offset;
                var changeLine = BasicEditor.Document.GetLineByOffset(changeOffset).LineNumber;

                // Count newlines in inserted/removed text
                var insertedNewlines = e.InsertedText.Text.Count(c => c == '\n');
                var removedNewlines = e.RemovedText.Text.Count(c => c == '\n');
                var delta = insertedNewlines - removedNewlines;

                if (delta != 0)
                {
                    _breakpointManager.AdjustForLineChange(changeLine, delta);
                }
            }
        };
    }

    private void SetupBookmarks()
    {
        // Add bookmark margin to the left of the BASIC editor (after breakpoint margin)
        var bookmarkMargin = new BasicToMips.Editor.Debugging.BookmarkMargin(BasicEditor, _bookmarkManager);
        BasicEditor.TextArea.LeftMargins.Insert(1, bookmarkMargin); // Index 1, after breakpoints

        // Setup Ctrl+B to toggle bookmarks
        BasicEditor.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
            {
                var line = BasicEditor.TextArea.Caret.Line;
                var wasSet = _bookmarkManager.ToggleBookmark(line);
                SetStatus($"Bookmark {(wasSet ? "set" : "removed")} at line {line}", true);
                e.Handled = true;
            }
            else if (e.Key == Key.F2 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Navigate to next bookmark (Ctrl+F2)
                NavigateToNextBookmark();
                e.Handled = true;
            }
            else if (e.Key == Key.F2 && (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)))
            {
                // Navigate to previous bookmark (Ctrl+Shift+F2)
                NavigateToPreviousBookmark();
                e.Handled = true;
            }
            else if (e.Key == Key.F2 && Keyboard.Modifiers == ModifierKeys.None)
            {
                // Rename symbol (F2)
                RenameSymbolAtCaret();
                e.Handled = true;
            }
        };

        // Track bookmarks when document changes (adjust line numbers)
        BasicEditor.Document.Changed += (s, e) =>
        {
            if (e.InsertionLength > 0 || e.RemovalLength > 0)
            {
                var changeOffset = e.Offset;
                var changeLine = BasicEditor.Document.GetLineByOffset(changeOffset).LineNumber;

                var insertedNewlines = e.InsertedText.Text.Count(c => c == '\n');
                var removedNewlines = e.RemovedText.Text.Count(c => c == '\n');
                var delta = insertedNewlines - removedNewlines;

                if (delta != 0)
                {
                    _bookmarkManager.AdjustForLineChange(changeLine, delta);
                }
            }
        };
    }

    private void NavigateToNextBookmark()
    {
        var currentLine = BasicEditor.TextArea.Caret.Line;
        var nextLine = _bookmarkManager.GetNextBookmark(currentLine);

        if (nextLine > 0)
        {
            JumpToLine(nextLine);
            SetStatus($"Bookmark at line {nextLine}", true);
        }
        else
        {
            SetStatus("No bookmarks set", false);
        }
    }

    private void NavigateToPreviousBookmark()
    {
        var currentLine = BasicEditor.TextArea.Caret.Line;
        var prevLine = _bookmarkManager.GetPreviousBookmark(currentLine);

        if (prevLine > 0)
        {
            JumpToLine(prevLine);
            SetStatus($"Bookmark at line {prevLine}", true);
        }
        else
        {
            SetStatus("No bookmarks set", false);
        }
    }

    private void RenameSymbolAtCaret()
    {
        // Get the word at the caret position
        var offset = BasicEditor.CaretOffset;
        var document = BasicEditor.Document;

        // Find word boundaries
        var wordStart = offset;
        var wordEnd = offset;

        while (wordStart > 0 && IsWordChar(document.GetCharAt(wordStart - 1)))
            wordStart--;

        while (wordEnd < document.TextLength && IsWordChar(document.GetCharAt(wordEnd)))
            wordEnd++;

        if (wordStart == wordEnd)
        {
            SetStatus("No symbol at cursor position", false);
            return;
        }

        var symbolName = document.GetText(wordStart, wordEnd - wordStart);

        // Find all occurrences
        var occurrences = _refactoringService.FindSymbolOccurrences(BasicEditor.Text, symbolName);

        if (occurrences.Count == 0)
        {
            SetStatus($"Symbol '{symbolName}' not found", false);
            return;
        }

        // Show rename dialog
        var dialog = new RenameDialog(symbolName, occurrences.Count)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true)
        {
            var newName = dialog.NewName;
            var result = _refactoringService.Rename(BasicEditor.Text, symbolName, newName);

            if (result.Success)
            {
                BasicEditor.Text = result.NewSource;
                SetStatus($"Renamed '{symbolName}' to '{newName}' ({result.RenamedCount} occurrences)", true);
            }
            else
            {
                MessageBox.Show(result.ErrorMessage, "Rename Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '$';
    }

    private void SetupCodeFolding()
    {
        _foldingManager = FoldingManager.Install(BasicEditor.TextArea);

        // Setup timer for delayed folding updates
        _foldingUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000)
        };
        _foldingUpdateTimer.Tick += (s, e) =>
        {
            _foldingUpdateTimer.Stop();
            UpdateFoldings();
        };
    }

    private void UpdateFoldings()
    {
        if (_foldingManager != null)
        {
            _foldingStrategy.UpdateFoldings(_foldingManager, BasicEditor.Document);
        }
    }

    private void RestartFoldingUpdateTimer()
    {
        _foldingUpdateTimer?.Stop();
        _foldingUpdateTimer?.Start();
    }

    private void SetupBidirectionalSync()
    {
        // Setup timer for delayed IC10 → BASIC sync (avoid re-compiling on every keystroke)
        _mipsSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(800)
        };
        _mipsSyncTimer.Tick += (s, e) =>
        {
            _mipsSyncTimer.Stop();
            // Don't auto-decompile - let user click the button
        };
    }

    private void SetupErrorHighlighting()
    {
        // Create and install the text marker service
        _textMarkerService = new TextMarkerService(BasicEditor.Document);
        BasicEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
        BasicEditor.TextArea.TextView.LineTransformers.Add(_textMarkerService);

        // Setup timer for delayed error checking (500ms after typing stops)
        _errorCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _errorCheckTimer.Tick += (s, e) =>
        {
            _errorCheckTimer.Stop();
            CheckForErrors();
        };

        // Setup tooltip for error markers
        BasicEditor.TextArea.TextView.MouseHover += TextView_MouseHover;
        BasicEditor.TextArea.TextView.MouseHoverStopped += TextView_MouseHoverStopped;
    }

    private System.Windows.Controls.ToolTip? _errorToolTip;

    private void TextView_MouseHover(object sender, MouseEventArgs e)
    {
        var pos = BasicEditor.TextArea.TextView.GetPositionFloor(e.GetPosition(BasicEditor.TextArea.TextView) + BasicEditor.TextArea.TextView.ScrollOffset);
        if (pos.HasValue && _textMarkerService != null)
        {
            int offset = BasicEditor.Document.GetOffset(pos.Value.Line, pos.Value.Column);
            var markers = _textMarkerService.GetMarkersAtOffset(offset);
            var marker = markers.FirstOrDefault();

            if (marker != null && !string.IsNullOrEmpty(marker.ToolTip))
            {
                _errorToolTip = new System.Windows.Controls.ToolTip
                {
                    Content = marker.ToolTip,
                    IsOpen = true,
                    PlacementTarget = BasicEditor,
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                    Padding = new Thickness(8, 4, 8, 4)
                };
            }
        }
    }

    private void TextView_MouseHoverStopped(object sender, MouseEventArgs e)
    {
        if (_errorToolTip != null)
        {
            _errorToolTip.IsOpen = false;
            _errorToolTip = null;
        }
    }

    private void CheckForErrors()
    {
        if (_textMarkerService == null) return;

        // Clear existing markers
        _textMarkerService.Clear();
        BasicEditor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);

        // Check for errors
        var errors = _errorChecker.Check(BasicEditor.Text);

        foreach (var error in errors)
        {
            try
            {
                var line = BasicEditor.Document.GetLineByNumber(Math.Min(error.Line, BasicEditor.Document.LineCount));
                int startOffset = line.Offset + Math.Max(0, error.Column - 1);
                int length = Math.Min(error.Length, line.EndOffset - startOffset);

                if (length <= 0) length = Math.Max(1, line.Length);

                var marker = _textMarkerService.Create(startOffset, length);
                if (marker != null)
                {
                    marker.MarkerType = TextMarkerType.SquigglyUnderline;
                    marker.MarkerColor = error.Severity switch
                    {
                        ErrorChecker.ErrorSeverity.Error => Colors.Red,
                        ErrorChecker.ErrorSeverity.Warning => Colors.Orange,
                        ErrorChecker.ErrorSeverity.Info => Colors.LightBlue,
                        _ => Colors.Red
                    };
                    marker.ToolTip = $"{error.Severity}: {error.Message}";
                }
            }
            catch
            {
                // Ignore marker creation errors
            }
        }

        BasicEditor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);

        // Update Problems Panel if visible
        if (ProblemsPanel.Visibility == Visibility.Visible)
        {
            UpdateProblemsList();
        }
    }

    private void RestartErrorCheckTimer()
    {
        _errorCheckTimer?.Stop();
        _errorCheckTimer?.Start();
    }

    private void FormatDocument_Click(object sender, RoutedEventArgs e) => FormatDocument();

    private void FormatDocument()
    {
        try
        {
            // Only format BASIC code, not IC10
            var language = LanguageDetector.Detect(BasicEditor.Text);
            if (language == LanguageType.IC10)
            {
                SetStatus("Cannot format IC10 code (only BASIC)", false);
                return;
            }

            var formatter = new BasicToMips.Analysis.CodeFormatter();
            var formatted = formatter.Format(BasicEditor.Text);

            // Preserve cursor position approximately
            var caretOffset = BasicEditor.CaretOffset;
            var caretLine = BasicEditor.Document.GetLineByOffset(caretOffset).LineNumber;

            BasicEditor.Document.Text = formatted;

            // Restore cursor to same line if possible
            if (caretLine <= BasicEditor.Document.LineCount)
            {
                var line = BasicEditor.Document.GetLineByNumber(caretLine);
                BasicEditor.CaretOffset = Math.Min(line.EndOffset, line.Offset + line.Length);
            }

            SetStatus("Document formatted (Ctrl+Shift+F)", true);
        }
        catch (Exception ex)
        {
            SetStatus($"Format error: {ex.Message}", false);
        }
    }

    private void DisplayAnalysisWarnings(List<BasicToMips.Analysis.AnalysisWarning> warnings)
    {
        if (_textMarkerService == null || warnings.Count == 0) return;

        // Note: Don't clear existing markers - they may contain real errors from ErrorChecker
        // Just add warning markers

        foreach (var warning in warnings)
        {
            try
            {
                if (warning.Line <= 0 || warning.Line > BasicEditor.Document.LineCount) continue;

                var line = BasicEditor.Document.GetLineByNumber(warning.Line);
                int startOffset = line.Offset;
                int length = line.Length;

                // Try to find the specific identifier on the line
                var lineText = BasicEditor.Document.GetText(line);
                var match = System.Text.RegularExpressions.Regex.Match(
                    warning.Message, @"'([^']+)'");
                if (match.Success)
                {
                    var identifier = match.Groups[1].Value;
                    var idx = lineText.IndexOf(identifier, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        startOffset = line.Offset + idx;
                        length = identifier.Length;
                    }
                }

                var marker = _textMarkerService.Create(startOffset, length);
                if (marker != null)
                {
                    marker.MarkerType = TextMarkerType.SquigglyUnderline;
                    marker.MarkerColor = warning.Type switch
                    {
                        BasicToMips.Analysis.WarningType.UnusedVariable => Colors.Gold,
                        BasicToMips.Analysis.WarningType.UnusedLabel => Colors.Gold,
                        BasicToMips.Analysis.WarningType.UnreachableCode => Colors.Gray,
                        _ => Colors.Orange
                    };
                    marker.ToolTip = $"Warning: {warning.Message}";
                }
            }
            catch
            {
                // Ignore marker creation errors
            }
        }

        BasicEditor.TextArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
    }

    private void SetupAutoComplete()
    {
        BasicEditor.TextArea.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ShowCompletionWindow();
                e.Handled = true;
            }
        };
    }

    private void LoadSettings()
    {
        _settings.Load();

        if (!string.IsNullOrEmpty(_settings.StationeersPath))
        {
            StationeersPathText.Text = $"Scripts: {_settings.StationeersPath}";
        }

        ShowDocsMenu.IsChecked = _settings.ShowDocumentation;
        if (!_settings.ShowDocumentation)
        {
            DocsPanelColumn.Width = new GridLength(0);
            DocsSplitter.Visibility = Visibility.Collapsed;
            DocsPanel.Visibility = Visibility.Collapsed;
        }

        AutoCompileMenu.IsChecked = _settings.AutoCompile;
        AutoCompleteMenu.IsChecked = _settings.AutoCompleteEnabled;
        AutoCompileCheckBox.IsChecked = _settings.AutoCompile;
        AutoCompleteCheckBox.IsChecked = _settings.AutoCompleteEnabled;

        // Retro effects menu states
        BlockCursorMenu.IsChecked = _settings.BlockCursorEnabled;
        CurrentLineHighlightMenu.IsChecked = _settings.CurrentLineHighlightEnabled;
        ScanlineOverlayMenu.IsChecked = _settings.ScanlineOverlayEnabled;
        ScreenGlowMenu.IsChecked = _settings.ScreenGlowEnabled;
        RetroFontMenu.IsChecked = _settings.RetroFontEnabled;
        StartupBeepMenu.IsChecked = _settings.StartupBeepEnabled;

        // Apply custom editor background color if set
        ApplySyntaxColors();
    }

    private string GetWelcomeCode()
    {
        return @"# ══════════════════════════════════════════════════════════════
# BASIC-10 Compiler for Stationeers
# Press F5 to compile | F1 for docs | Ctrl+Space for autocomplete
# ══════════════════════════════════════════════════════════════

# ── Device Aliases (Pin-based: d0-d5) ──────────────────────────
ALIAS sensor d0          # Gas sensor for atmosphere readings
ALIAS display d1         # LED display for status output
ALIAS alarm d2           # Warning light for alerts

# ── Named Device Reference (bypasses 6-pin limit) ──────────────
# DEVICE roomSensor ""StructureGasSensor""
# DEVICE allLights ""StructureWallLight""

# ── Constants ──────────────────────────────────────────────────
CONST MIN_PRESSURE = 80      # kPa - low pressure warning
CONST MAX_PRESSURE = 120     # kPa - high pressure warning
CONST MIN_OXYGEN = 0.18      # 18% - minimum safe O2
CONST TARGET_TEMP = 293.15   # 20°C in Kelvin

# ── Variables ──────────────────────────────────────────────────
VAR pressure = 0
VAR oxygen = 0
VAR temp = 0
VAR status = 0    # 0=danger, 1=warning, 2=safe

# ══════════════════════════════════════════════════════════════
# MAIN LOOP - Runs continuously
# ══════════════════════════════════════════════════════════════
main:
    GOSUB ReadSensors
    GOSUB CheckStatus
    GOSUB UpdateDisplay

    YIELD    # Required - lets game process
    GOTO main

# ── Subroutine: Read all sensor values ─────────────────────────
ReadSensors:
    pressure = sensor.Pressure
    oxygen = sensor.RatioOxygen
    temp = sensor.Temperature
    RETURN

# ── Subroutine: Determine atmosphere status ────────────────────
CheckStatus:
    status = 2    # Assume safe

    # Check pressure
    IF pressure < MIN_PRESSURE OR pressure > MAX_PRESSURE THEN
        status = 0
    ENDIF

    # Check oxygen
    IF oxygen < MIN_OXYGEN THEN
        status = 0
    ENDIF

    # Check temperature (warning if outside comfort range)
    IF temp < TARGET_TEMP - 10 OR temp > TARGET_TEMP + 10 THEN
        IF status > 0 THEN status = 1
    ENDIF

    RETURN

# ── Subroutine: Update display and alarm ───────────────────────
UpdateDisplay:
    display.Setting = pressure

    IF status = 0 THEN
        alarm.On = 1
        alarm.Color = Red
    ELSEIF status = 1 THEN
        alarm.On = 1
        alarm.Color = Yellow
    ELSE
        alarm.On = 1
        alarm.Color = Green
    ENDIF

    RETURN

END
";
    }

    #region File Operations

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        // Create new tab instead of replacing current content
        CreateNewTab();
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckSaveChanges()) return;

        // Get scripts folder path
        var scriptsFolder = GetScriptsFolder();
        if (!string.IsNullOrEmpty(scriptsFolder))
        {
            // Show script selection dialog
            var dialog = new Dialogs.ScriptSelectDialog(scriptsFolder);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedScriptPath))
            {
                OpenFile(dialog.SelectedScriptPath);
                return;
            }

            // User clicked Browse - fall through to legacy browser
            if (!dialog.BrowseRequested)
            {
                return;
            }
        }

        // Fall back to folder browser dialog
        OpenFileLegacy();
    }

    private void OpenFileLegacy()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select script folder (will auto-load .bas file)",
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true
        };

        // Set initial directory to last working directory or user's documents
        if (!string.IsNullOrEmpty(_workingDirectory) && Directory.Exists(_workingDirectory))
        {
            dialog.SelectedPath = _workingDirectory;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OpenFolder(dialog.SelectedPath);
        }
    }

    private void OpenFolder(string folderPath)
    {
        try
        {
            // Find .bas or .basic files in the folder
            var basFiles = Directory.GetFiles(folderPath, "*.bas")
                .Concat(Directory.GetFiles(folderPath, "*.basic"))
                .ToArray();

            if (basFiles.Length == 0)
            {
                MessageBox.Show(
                    "No .bas or .basic files found in the selected folder.\n\nPlease select a folder containing a BASIC script.",
                    "No Script Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string fileToOpen;
            if (basFiles.Length == 1)
            {
                fileToOpen = basFiles[0];
            }
            else
            {
                // Multiple files found - let user choose
                var fileNames = basFiles.Select(f => Path.GetFileName(f)!).ToArray();
                var result = ShowFileSelectionDialog(basFiles, fileNames);
                if (result == null) return;
                fileToOpen = result;
            }

            OpenFile(fileToOpen);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening folder:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string? ShowFileSelectionDialog(string[] filePaths, string[] fileNames)
    {
        // Simple dialog to select from multiple .bas files
        var dialog = new Window
        {
            Title = "Select Script File",
            Width = 350,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var listBox = new ListBox
        {
            Margin = new Thickness(10),
            ItemsSource = fileNames
        };
        listBox.SelectedIndex = 0;

        var okButton = new Button
        {
            Content = "Open",
            Width = 80,
            Height = 28,
            Margin = new Thickness(5),
            IsDefault = true
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Width = 80,
            Height = 28,
            Margin = new Thickness(5),
            IsCancel = true
        };

        var buttonPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(10, 0, 10, 10)
        };
        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        var mainPanel = new DockPanel();
        DockPanel.SetDock(buttonPanel, Dock.Bottom);
        mainPanel.Children.Add(buttonPanel);
        mainPanel.Children.Add(listBox);

        dialog.Content = mainPanel;

        string? selectedFile = null;
        okButton.Click += (s, e) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                selectedFile = filePaths[listBox.SelectedIndex];
                dialog.DialogResult = true;
            }
        };

        listBox.MouseDoubleClick += (s, e) =>
        {
            if (listBox.SelectedIndex >= 0)
            {
                selectedFile = filePaths[listBox.SelectedIndex];
                dialog.DialogResult = true;
            }
        };

        dialog.ShowDialog();
        return selectedFile;
    }

    private void OpenFile(string path)
    {
        try
        {
            BasicEditor.Text = File.ReadAllText(path);
            _currentFilePath = path;
            _workingDirectory = Path.GetDirectoryName(path);
            _isModified = false;
            _settings.AddRecentFile(path);
            UpdateTitle();
            UpdateWorkingDirectoryDisplay();
            UpdateRecentFilesMenu();
            SetStatus("File opened", true);

            if (AutoCompileMenu.IsChecked)
            {
                Compile();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateWorkingDirectoryDisplay()
    {
        if (string.IsNullOrEmpty(_workingDirectory))
        {
            WorkingDirectoryText.Text = "Unsaved script";
            WorkingDirectoryText.FontStyle = FontStyles.Italic;
            WorkingDirectoryText.Opacity = 0.8;
        }
        else
        {
            // Show folder name with path tooltip
            var folderName = Path.GetFileName(_workingDirectory);
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = _workingDirectory; // Root directory
            }
            WorkingDirectoryText.Text = folderName;
            WorkingDirectoryText.ToolTip = _workingDirectory;
            WorkingDirectoryText.FontStyle = FontStyles.Normal;
            WorkingDirectoryText.Opacity = 1.0;
        }
    }

    private void SaveFile_Click(object sender, RoutedEventArgs e)
    {
        SaveFile();
    }

    private bool SaveFile()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            return SaveFileAs();
        }

        return SaveToFile(_currentFilePath);
    }

    private void SaveFileAs_Click(object sender, RoutedEventArgs e)
    {
        SaveFileAs();
    }

    private bool SaveFileAs()
    {
        // Get scripts folder path
        var scriptsFolder = GetScriptsFolder();
        if (string.IsNullOrEmpty(scriptsFolder))
        {
            // Fall back to standard save dialog if no scripts folder configured
            return SaveFileAsLegacy();
        }

        // Get default name from current file or empty
        string? defaultName = null;
        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            defaultName = Path.GetFileNameWithoutExtension(_currentFilePath);
        }

        // Show script name dialog
        var dialog = new Dialogs.ScriptNameDialog(defaultName);
        dialog.Owner = this;

        if (dialog.ShowDialog() == true)
        {
            var scriptName = dialog.ScriptName;
            return SaveScriptToFolder(scriptsFolder, scriptName);
        }

        return false;
    }

    private bool SaveFileAsLegacy()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "BASIC Files (*.bas)|*.bas|All Files (*.*)|*.*",
            Title = "Save BASIC File",
            DefaultExt = ".bas"
        };

        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            dialog.FileName = Path.GetFileName(_currentFilePath);
            dialog.InitialDirectory = Path.GetDirectoryName(_currentFilePath);
        }

        if (dialog.ShowDialog() == true)
        {
            return SaveToFile(dialog.FileName);
        }

        return false;
    }

    private string? GetScriptsFolder()
    {
        if (!string.IsNullOrEmpty(_settings.StationeersPath))
        {
            var scriptsPath = Path.Combine(_settings.StationeersPath, "scripts");
            if (Directory.Exists(scriptsPath))
            {
                return scriptsPath;
            }
            // Try to create it
            try
            {
                Directory.CreateDirectory(scriptsPath);
                return scriptsPath;
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private bool SaveScriptToFolder(string scriptsFolder, string scriptName)
    {
        try
        {
            // Create script folder
            var scriptFolder = Path.Combine(scriptsFolder, scriptName);
            if (!Directory.Exists(scriptFolder))
            {
                Directory.CreateDirectory(scriptFolder);
            }

            // Save .bas file
            var basPath = Path.Combine(scriptFolder, $"{scriptName}.bas");
            File.WriteAllText(basPath, BasicEditor.Text);

            // Compile the code
            var result = _compiler.Compile(BasicEditor.Text, _optimizationLevel);
            string ic10Code = result.Success ? (result.Output ?? "") : "";

            // Extract author from meta comments if present
            string author = ExtractMetaAuthor(BasicEditor.Text);

            // Generate and save instruction.xml (singular - matches game format)
            var instructionPath = Path.Combine(scriptFolder, "instruction.xml");
            GenerateInstructionXml(instructionPath, scriptName, ic10Code, author);

            // Update state
            _currentFilePath = basPath;
            _workingDirectory = scriptFolder;
            _isModified = false;
            _settings.AddRecentFile(basPath);
            UpdateTitle();
            UpdateWorkingDirectoryDisplay();
            UpdateRecentFilesMenu();
            CleanupTempAutoSave();

            // Update MIPS output panel
            if (result.Success)
            {
                _suppressMipsUpdate = true;
                MipsOutput.Text = result.Output;
                _suppressMipsUpdate = false;
                _sourceMap = result.SourceMap;
                _watchManager.SetSourceMap(_sourceMap);
                UpdateLineCount();
            }

            SetStatus($"Script saved: {scriptName}", true);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving script:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private string ExtractMetaAuthor(string source)
    {
        foreach (var line in source.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("##Meta:Author=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring("##Meta:Author=".Length).Trim();
            }
        }
        return "";
    }

    private void GenerateInstructionXml(string path, string scriptName, string ic10Code, string author)
    {
        // Generate timestamp in game format (ticks)
        var ticks = DateTime.UtcNow.Ticks;

        // Escape the IC10 code for XML
        var escapedCode = System.Security.SecurityElement.Escape(ic10Code);

        // Use settings author first, then meta comment author, then default
        var displayAuthor = !string.IsNullOrWhiteSpace(_settings.ScriptAuthor)
            ? _settings.ScriptAuthor
            : (!string.IsNullOrWhiteSpace(author) ? author : "Unknown");

        // Build description: user's custom description + "Built in Basic-10"
        var userDesc = _settings.ScriptDescription?.Trim() ?? "";
        var description = string.IsNullOrEmpty(userDesc)
            ? "Built in Basic-10"
            : $"{userDesc}\n\nBuilt in Basic-10";

        var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<InstructionData xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DateTime>{ticks}</DateTime>
  <GameVersion>0.2</GameVersion>
  <Title>{System.Security.SecurityElement.Escape(scriptName)}</Title>
  <Description>{System.Security.SecurityElement.Escape(description)}</Description>
  <Author>{System.Security.SecurityElement.Escape(displayAuthor)}</Author>
  <WorkshopFileHandle>0</WorkshopFileHandle>
  <Instructions>{escapedCode}</Instructions>
</InstructionData>";
        File.WriteAllText(path, xml);
    }

    private bool SaveToFile(string path)
    {
        try
        {
            File.WriteAllText(path, BasicEditor.Text);
            _currentFilePath = path;
            _workingDirectory = Path.GetDirectoryName(path);
            _isModified = false;
            _settings.AddRecentFile(path);
            UpdateTitle();
            UpdateWorkingDirectoryDisplay();
            UpdateRecentFilesMenu();

            // Clear temp auto-save since we now have a real file
            CleanupTempAutoSave();

            // Compile and update instruction.xml
            var result = _compiler.Compile(BasicEditor.Text, _optimizationLevel);
            if (result.Success)
            {
                _suppressMipsUpdate = true;
                MipsOutput.Text = result.Output ?? "";
                _suppressMipsUpdate = false;
                _sourceMap = result.SourceMap;
                _watchManager.SetSourceMap(_sourceMap);
                UpdateLineCount();

                // Update instruction.xml in the same folder
                // Use folder name as script title (Stationeers displays folder name in-game)
                var folder = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                {
                    var instructionPath = Path.Combine(folder, "instruction.xml");
                    var scriptName = Path.GetFileName(folder); // Use folder name, not file name
                    var author = ExtractMetaAuthor(BasicEditor.Text);
                    GenerateInstructionXml(instructionPath, scriptName, result.Output ?? "", author);
                }
            }

            SetStatus("File saved", true);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void CleanupTempAutoSave()
    {
        try
        {
            // Clean up timestamped autosave
            if (!string.IsNullOrEmpty(_tempAutoSavePath) && File.Exists(_tempAutoSavePath))
            {
                File.Delete(_tempAutoSavePath);
                _tempAutoSavePath = null;
            }

            // Clean up recovery backup files
            var tempDir = Path.Combine(Path.GetTempPath(), "BasicToMips_AutoSave");
            var backupPath = Path.Combine(tempDir, "recovery_backup.bas");
            var metaPath = Path.Combine(tempDir, "recovery_meta.txt");

            if (File.Exists(backupPath)) File.Delete(backupPath);
            if (File.Exists(metaPath)) File.Delete(metaPath);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void AutoSaveToTemp()
    {
        if (string.IsNullOrWhiteSpace(BasicEditor.Text)) return;

        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BasicToMips_AutoSave");
            Directory.CreateDirectory(tempDir);

            // Always save to a consistent backup file for recovery
            var backupPath = Path.Combine(tempDir, "recovery_backup.bas");
            File.WriteAllText(backupPath, BasicEditor.Text);

            // Also store original file path if known
            var metaPath = Path.Combine(tempDir, "recovery_meta.txt");
            File.WriteAllText(metaPath, _currentFilePath ?? "UNSAVED");

            _lastAutoSave = DateTime.Now;

            // Also keep a timestamped version for unsaved scripts
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                if (string.IsNullOrEmpty(_tempAutoSavePath))
                {
                    _tempAutoSavePath = Path.Combine(tempDir, $"autosave_{DateTime.Now:yyyyMMdd_HHmmss}.bas");
                }
                File.WriteAllText(_tempAutoSavePath, BasicEditor.Text);
            }
        }
        catch
        {
            // Silently ignore auto-save errors
        }
    }

    private void SetupAutoSaveTimer()
    {
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(30) // Save every 30 seconds
        };
        _autoSaveTimer.Tick += (s, e) =>
        {
            if (_isModified && !string.IsNullOrWhiteSpace(BasicEditor.Text))
            {
                AutoSaveToTemp();
            }
        };
        _autoSaveTimer.Start();
    }

    private void CheckForAutoSaveRecovery()
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "BasicToMips_AutoSave");
            var backupPath = Path.Combine(tempDir, "recovery_backup.bas");
            var metaPath = Path.Combine(tempDir, "recovery_meta.txt");

            if (!File.Exists(backupPath)) return;

            var backupInfo = new FileInfo(backupPath);
            // Only offer recovery if backup is less than 24 hours old
            if (backupInfo.LastWriteTime < DateTime.Now.AddHours(-24))
            {
                // Old backup, delete it
                File.Delete(backupPath);
                if (File.Exists(metaPath)) File.Delete(metaPath);
                return;
            }

            var backupContent = File.ReadAllText(backupPath);
            if (string.IsNullOrWhiteSpace(backupContent)) return;

            var originalPath = File.Exists(metaPath) ? File.ReadAllText(metaPath).Trim() : "UNSAVED";
            var timeAgo = DateTime.Now - backupInfo.LastWriteTime;
            var timeStr = timeAgo.TotalMinutes < 60
                ? $"{(int)timeAgo.TotalMinutes} minutes ago"
                : $"{(int)timeAgo.TotalHours} hours ago";

            var message = originalPath == "UNSAVED"
                ? $"Recovered unsaved work from {timeStr}.\n\nWould you like to restore it?"
                : $"Recovered work from:\n{originalPath}\n\nLast saved: {timeStr}\n\nWould you like to restore it?";

            var result = MessageBox.Show(
                message,
                "Recover Unsaved Work",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BasicEditor.Text = backupContent;
                _isModified = true;
                ModifiedIndicator.Visibility = Visibility.Visible;
                UpdateTitle();
                StatusText.Text = "Recovered from autosave backup";
            }

            // Delete recovery files after offering
            File.Delete(backupPath);
            if (File.Exists(metaPath)) File.Delete(metaPath);
        }
        catch
        {
            // Silently ignore recovery errors
        }
    }

    private void ExportIC10_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MipsOutput.Text))
        {
            MessageBox.Show("No IC10 code to export. Please compile first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "IC10 Files (*.ic10)|*.ic10|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Export IC10 Code",
            DefaultExt = ".ic10"
        };

        if (!string.IsNullOrEmpty(_currentFilePath))
        {
            dialog.FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + ".ic10";
        }

        if (dialog.ShowDialog() == true)
        {
            try
            {
                File.WriteAllText(dialog.FileName, MipsOutput.Text);
                SetStatus("IC10 code exported", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool CheckSaveChanges()
    {
        if (!_isModified) return true;

        var result = MessageBox.Show(
            "Do you want to save changes?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => SaveFile(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();

        if (_settings.RecentFiles.Count == 0)
        {
            var emptyItem = new MenuItem { Header = "(No recent files)", IsEnabled = false };
            RecentFilesMenu.Items.Add(emptyItem);
            return;
        }

        foreach (var file in _settings.RecentFiles.Take(10))
        {
            var item = new MenuItem { Header = file, Tag = file };
            item.Click += (s, e) =>
            {
                if (CheckSaveChanges())
                {
                    OpenFile((string)((MenuItem)s).Tag);
                }
            };
            RecentFilesMenu.Items.Add(item);
        }

        RecentFilesMenu.Items.Add(new Separator());
        var clearItem = new MenuItem { Header = "Clear Recent Files" };
        clearItem.Click += (s, e) =>
        {
            _settings.RecentFiles.Clear();
            _settings.Save();
            UpdateRecentFilesMenu();
        };
        RecentFilesMenu.Items.Add(clearItem);
    }

    private void ImportIC10_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckSaveChanges()) return;

        var dialog = new OpenFileDialog
        {
            Filter = "IC10 Files (*.ic10)|*.ic10|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Import IC10 File"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var ic10Code = File.ReadAllText(dialog.FileName);
                ImportAndDecompileIC10(ic10Code);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportIC10FromClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckSaveChanges()) return;

        try
        {
            if (Clipboard.ContainsText())
            {
                var ic10Code = Clipboard.GetText();
                ImportAndDecompileIC10(ic10Code);
            }
            else
            {
                MessageBox.Show("Clipboard does not contain text.", "Import IC10", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error reading clipboard:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportAndDecompileIC10(string ic10Code)
    {
        try
        {
            var result = _compiler.Decompile(ic10Code);

            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                BasicEditor.Text = result.Output;
                _currentFilePath = null;
                _workingDirectory = null;
                _isModified = true;
                UpdateTitle();
                UpdateWorkingDirectoryDisplay();
                SetStatus("Imported and decompiled IC10", true);
            }
            else
            {
                MessageBox.Show(
                    $"Decompilation failed:\n{result.ErrorMessage}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error during decompilation:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Edit Operations

    private void Undo_Click(object sender, RoutedEventArgs e) => BasicEditor.Undo();
    private void Redo_Click(object sender, RoutedEventArgs e) => BasicEditor.Redo();
    private void Cut_Click(object sender, RoutedEventArgs e) => BasicEditor.Cut();
    private void Copy_Click(object sender, RoutedEventArgs e) => BasicEditor.Copy();
    private void Paste_Click(object sender, RoutedEventArgs e) => BasicEditor.Paste();

    private FindReplaceWindow? _findReplaceWindow;

    private void Find_Click(object sender, RoutedEventArgs e)
    {
        OpenFindReplace();
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        OpenFindReplace();
    }

    private void OpenFindReplace()
    {
        if (_findReplaceWindow != null && _findReplaceWindow.IsVisible)
        {
            _findReplaceWindow.Focus();
            return;
        }

        _findReplaceWindow = new FindReplaceWindow(BasicEditor);
        _findReplaceWindow.Owner = this;
        _findReplaceWindow.Closed += (s, e) => _findReplaceWindow = null;
        _findReplaceWindow.Show();
    }

    #endregion

    #region Compilation

    private void Compile_Click(object sender, RoutedEventArgs e)
    {
        Compile();
    }

    private void CompileAndCopy_Click(object sender, RoutedEventArgs e)
    {
        if (Compile())
        {
            Clipboard.SetText(MipsOutput.Text);
            SetStatus("Compiled and copied to clipboard", true);
        }
    }

    private void RunSimulator_Click(object sender, RoutedEventArgs e)
    {
        // Compile first if needed
        if (string.IsNullOrEmpty(MipsOutput.Text))
        {
            if (!Compile())
            {
                MessageBox.Show("Please fix compilation errors before running the simulator.",
                    "Compilation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var simulator = new SimulatorWindow(MipsOutput.Text);
        simulator.Owner = this;
        simulator.Show();
    }

    private void CopyIC10_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MipsOutput.Text))
        {
            Clipboard.SetText(MipsOutput.Text);
            SetStatus("IC10 code copied to clipboard", true);
        }
    }

    private bool Compile()
    {
        try
        {
            ClearError();
            SetStatus("Compiling...", false);

            var result = _compiler.Compile(BasicEditor.Text, _optimizationLevel);
            _lastCompilationResult = result;

            if (result.Success)
            {
                _suppressMipsUpdate = true;
                MipsOutput.Text = result.Output;
                _suppressMipsUpdate = false;

                // Store source map for debugging
                _sourceMap = result.SourceMap;

                // Update watch manager with source map for BASIC variable resolution
                _watchManager.SetSourceMap(_sourceMap);

                UpdateLineCount();

                // Display static analysis warnings
                DisplayAnalysisWarnings(result.Warnings);

                // Update problems panel with all errors and warnings
                UpdateProblemsList();

                // Show detected language in status
                var langInfo = result.DetectedLanguage switch
                {
                    LanguageType.IC10 => " (IC10 passthrough)",
                    LanguageType.Basic => " (BASIC)",
                    _ => ""
                };

                var warningInfo = result.Warnings.Count > 0 ? $", {result.Warnings.Count} warning(s)" : "";
                SetStatus($"Compiled successfully ({result.LineCount} lines{warningInfo}){langInfo}", true);
                return true;
            }
            else
            {
                // Update problems panel with compilation error
                UpdateProblemsList();

                ShowError(result.ErrorMessage ?? "Unknown error", result.ErrorLine);
                SetStatus("Compilation failed", false);
                return false;
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message, null);
            SetStatus("Compilation failed", false);
            return false;
        }
    }

    private void OptLevel_Click(object sender, RoutedEventArgs e)
    {
        OptNone.IsChecked = sender == OptNone;
        OptBasic.IsChecked = sender == OptBasic;
        OptAggressive.IsChecked = sender == OptAggressive;

        _optimizationLevel = sender == OptNone ? 0 : sender == OptBasic ? 1 : 2;
    }

    private void Optimize_Click(object sender, RoutedEventArgs e)
    {
        var previousLevel = _optimizationLevel;
        _optimizationLevel = 2; // Aggressive
        Compile();
        _optimizationLevel = previousLevel;
    }

    #endregion

    #region Stationeers Integration

    private void SetStationeersDir_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Stationeers save directory (contains 'scripts' folder)",
            ShowNewFolderButton = false
        };

        // Try to find default Stationeers scripts save path
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "My Games", "Stationeers");

        if (Directory.Exists(defaultPath))
        {
            dialog.SelectedPath = defaultPath;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _settings.StationeersPath = dialog.SelectedPath;
            _settings.Save();
            StationeersPathText.Text = $"Scripts: {dialog.SelectedPath}";
            SetStatus("Stationeers scripts path configured", true);
        }
    }

    private void OpenScriptsFolder_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_settings.StationeersPath))
        {
            MessageBox.Show("Please configure the Stationeers directory first.", "Not Configured", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var scriptsPath = Path.Combine(_settings.StationeersPath, "scripts");
        if (!Directory.Exists(scriptsPath))
        {
            Directory.CreateDirectory(scriptsPath);
        }

        System.Diagnostics.Process.Start("explorer.exe", scriptsPath);
    }

    private void SaveAndDeploy_Click(object sender, RoutedEventArgs e)
    {
        // SaveFile handles everything: creates folder, saves .bas, compiles, and generates instruction.xml
        SaveFile();
    }

    #endregion

    #region View Operations

    private void ToggleDocs_Click(object sender, RoutedEventArgs e)
    {
        var show = ShowDocsMenu.IsChecked;
        _settings.ShowDocumentation = show;
        _settings.Save();

        if (show)
        {
            DocsPanelColumn.Width = new GridLength(300);
            DocsSplitter.Visibility = Visibility.Visible;
            DocsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            DocsPanelColumn.Width = new GridLength(0);
            DocsSplitter.Visibility = Visibility.Collapsed;
            DocsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseDocs_Click(object sender, RoutedEventArgs e)
    {
        ShowDocsMenu.IsChecked = false;
        ToggleDocs_Click(sender, e);
    }

    private void ToggleLineNumbers_Click(object sender, RoutedEventArgs e)
    {
        var show = ShowLineNumbersMenu.IsChecked;
        BasicEditor.ShowLineNumbers = show;
        MipsOutput.ShowLineNumbers = show;
    }

    private void ToggleWordWrap_Click(object sender, RoutedEventArgs e)
    {
        var wrap = WordWrapMenu.IsChecked;
        BasicEditor.WordWrap = wrap;
        MipsOutput.WordWrap = wrap;
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        BasicEditor.FontSize = Math.Min(BasicEditor.FontSize + 2, 32);
        MipsOutput.FontSize = BasicEditor.FontSize;
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        BasicEditor.FontSize = Math.Max(BasicEditor.FontSize - 2, 8);
        MipsOutput.FontSize = BasicEditor.FontSize;
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        BasicEditor.FontSize = 14;
        MipsOutput.FontSize = 14;
    }

    // Retro Effects Toggles
    private void ToggleBlockCursor_Click(object sender, RoutedEventArgs e)
    {
        _settings.BlockCursorEnabled = BlockCursorMenu.IsChecked;
        _settings.Save();
        // Note: Block cursor requires restart to toggle (complex to remove at runtime)
        MessageBox.Show("Block cursor change will take effect on next launch.", "Retro Effects", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ToggleCurrentLineHighlight_Click(object sender, RoutedEventArgs e)
    {
        _settings.CurrentLineHighlightEnabled = CurrentLineHighlightMenu.IsChecked;
        _settings.Save();
        CurrentLineHighlighterManager.SetEnabled(BasicEditor.TextArea, _settings.CurrentLineHighlightEnabled);
    }

    private void ToggleScanlineOverlay_Click(object sender, RoutedEventArgs e)
    {
        _settings.ScanlineOverlayEnabled = ScanlineOverlayMenu.IsChecked;
        _settings.Save();
        ScanlineOverlayManager.SetEnabled(BasicEditor.TextArea, _settings.ScanlineOverlayEnabled);
    }

    private void ToggleScreenGlow_Click(object sender, RoutedEventArgs e)
    {
        _settings.ScreenGlowEnabled = ScreenGlowMenu.IsChecked;
        _settings.Save();
        ScreenGlowManager.SetEnabled(BasicEditor, _settings.ScreenGlowEnabled);
    }

    private void ToggleRetroFont_Click(object sender, RoutedEventArgs e)
    {
        _settings.RetroFontEnabled = RetroFontMenu.IsChecked;
        _settings.Save();
        RetroFontManager.SetEnabled(BasicEditor, _settings.RetroFontEnabled, _settings.RetroFontChoice);
        RetroFontManager.SetEnabled(MipsOutput, _settings.RetroFontEnabled, _settings.RetroFontChoice);
    }

    private void FontStyle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is string fontChoice)
        {
            _settings.RetroFontChoice = fontChoice;
            _settings.Save();
            UpdateFontMenuCheckmarks();

            // Apply font change if retro font is enabled
            if (_settings.RetroFontEnabled)
            {
                RetroFontManager.SetEnabled(BasicEditor, true, _settings.RetroFontChoice);
                RetroFontManager.SetEnabled(MipsOutput, true, _settings.RetroFontChoice);
            }
        }
    }

    private void UpdateFontMenuCheckmarks()
    {
        FontDefaultMenu.IsChecked = _settings.RetroFontChoice == "Default";
        FontAppleMenu.IsChecked = _settings.RetroFontChoice == "Apple";
        FontTRS80Menu.IsChecked = _settings.RetroFontChoice == "TRS80";
    }

    private void ToggleStartupBeep_Click(object sender, RoutedEventArgs e)
    {
        _settings.StartupBeepEnabled = StartupBeepMenu.IsChecked;
        _settings.Save();
        // Play a test beep when enabled
        if (_settings.StartupBeepEnabled)
        {
            StartupBeepManager.PlaySimpleBeep();
        }
    }

    private void AutoCompileCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return; // Guard against initialization
        _settings.AutoCompile = AutoCompileCheckBox.IsChecked == true;
        _settings.Save();
        // Keep menu in sync
        AutoCompileMenu.IsChecked = AutoCompileCheckBox.IsChecked == true;
    }

    private void AutoCompleteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_settings == null) return; // Guard against initialization
        _settings.AutoCompleteEnabled = AutoCompleteCheckBox.IsChecked == true;
        _settings.Save();
        // Keep menu in sync
        AutoCompleteMenu.IsChecked = AutoCompleteCheckBox.IsChecked == true;
    }

    private void AutoCompileMenu_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoCompile = AutoCompileMenu.IsChecked == true;
        _settings.Save();
        // Keep checkbox in sync
        AutoCompileCheckBox.IsChecked = AutoCompileMenu.IsChecked == true;
    }

    private void AutoCompleteMenu_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoCompleteEnabled = AutoCompleteMenu.IsChecked == true;
        _settings.Save();
        // Keep checkbox in sync
        AutoCompleteCheckBox.IsChecked = AutoCompleteMenu.IsChecked == true;
    }

    #endregion

    #region Help and Documentation

    private void ShowDocs_Click(object sender, RoutedEventArgs e)
    {
        // Open comprehensive documentation window (all docs combined)
        var dialog = new DocumentationWindow("Basic-10 Documentation", _docs.PopulateComprehensiveDocs);
        dialog.Owner = this;
        dialog.Show();
    }

    private void QuickStart_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DocumentationWindow("Quick Start Guide", _docs.PopulateStartHere);
        dialog.Owner = this;
        dialog.Show();
    }

    private void LangRef_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DocumentationWindow("Language Reference", _docs.PopulateSyntax);
        dialog.Owner = this;
        dialog.Show();
    }

    private void Examples_Click(object sender, RoutedEventArgs e)
    {
        LoadExample_Click(sender, e);
    }

    private void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("You are using the latest version.", "Updates", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.6.3";

        MessageBox.Show(
            "Stationeers Basic-10 Compiler\n\n" +
            $"Version {versionStr}\n\n" +
            "A BASIC to IC10 compiler that generates optimized\n" +
            "MIPS assembly code for Stationeers.\n\n" +
            "Developed by Dog Tired Studios\n" +
            "Authors: ThunderDuck & DrGoNzO1489\n\n" +
            "Features:\n" +
            "- Syntax highlighting and auto-complete\n" +
            "- Real-time error checking\n" +
            "- Automatic code optimization\n" +
            "- Direct deployment to Stationeers\n" +
            "- Comprehensive documentation",
            "About Stationeers Basic-10",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            LoadSettings();
        }
    }

    private void DeviceDatabase_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DeviceDatabaseWindow();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedHash))
        {
            // Insert the hash at the cursor position
            var insertText = dialog.SelectedHash;
            if (!string.IsNullOrEmpty(dialog.SelectedName))
            {
                insertText = $"{dialog.SelectedHash} ' {dialog.SelectedName}";
            }
            BasicEditor.Document.Insert(BasicEditor.CaretOffset, insertText);
            SetStatus($"Inserted hash for {dialog.SelectedName}", true);
        }
    }

    private void PopulateDocumentation()
    {
        _docs.PopulateStartHere(StartHerePanel);
        _docs.PopulateSyntax(SyntaxPanel);
        _docs.PopulateFunctions(FunctionsPanel);
        _docs.PopulateDevices(DevicesPanel);
        _docs.PopulateIC10Reference(IC10Panel);
        _docs.PopulatePatterns(PatternsPanel);
        _docs.PopulateExamples(ExamplesPanel, LoadExampleCode);
    }

    private void LoadExampleCode(string code)
    {
        if (!CheckSaveChanges()) return;
        BasicEditor.Text = code;
        _currentFilePath = null;
        _isModified = false;
        UpdateTitle();
        if (AutoCompileMenu.IsChecked) Compile();
    }

    private void LoadExample_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ExamplesWindow(_docs);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.SelectedCode))
        {
            LoadExampleCode(dialog.SelectedCode);
        }
    }

    private void PopulateSnippetsMenu()
    {
        var snippets = _docs.GetSnippets();
        foreach (var snippet in snippets)
        {
            var item = new MenuItem { Header = snippet.Name, Tag = snippet.Code };
            item.Click += (s, e) =>
            {
                var code = (string)((MenuItem)s).Tag;
                BasicEditor.Document.Insert(BasicEditor.CaretOffset, code);
            };
            SnippetsMenu.Items.Add(item);
        }
    }

    #endregion

    #region Auto-Complete

    private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (_completionWindow != null && !string.IsNullOrEmpty(e.Text))
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
            {
                _completionWindow.CompletionList.RequestInsertion(e);
            }
        }
    }

    private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        // Safety check for empty text
        if (string.IsNullOrEmpty(e.Text)) return;

        // Auto-complete brackets for .Name[""]
        // When user types [" after .Name, auto-insert "] and position cursor inside
        if (e.Text == "\"")
        {
            var offset = BasicEditor.CaretOffset;
            var document = BasicEditor.Document;

            // Check if we just typed [" (offset is after the ")
            if (offset >= 2)
            {
                var prevChar = document.GetCharAt(offset - 2);
                if (prevChar == '[')
                {
                    // Check if .Name precedes the ["
                    var lineStart = document.GetLineByOffset(offset).Offset;
                    var textBefore = document.GetText(lineStart, offset - lineStart - 2);
                    if (textBefore.TrimEnd().EndsWith(".Name", StringComparison.OrdinalIgnoreCase))
                    {
                        // Insert "] after cursor
                        document.Insert(offset, "\"]");
                        // Cursor stays between the quotes (user can type device name)
                        return;
                    }
                }
            }
        }

        // Trigger auto-complete on certain characters
        if (char.IsLetter(e.Text[0]) || e.Text[0] == '.')
        {
            var word = GetWordBeforeCaret();
            if (word.Length >= 2 || e.Text[0] == '.')
            {
                ShowCompletionWindow();
            }
        }
    }

    private void ShowCompletionWindow()
    {
        // Check if auto-complete is enabled
        if (AutoCompleteMenu.IsChecked != true) return;

        var completionData = BasicCompletionData.GetCompletionData(BasicEditor, GetWordBeforeCaret());

        if (completionData.Count == 0) return;

        _completionWindow = new CompletionWindow(BasicEditor.TextArea);
        var data = _completionWindow.CompletionList.CompletionData;

        foreach (var item in completionData)
        {
            data.Add(item);
        }

        _completionWindow.Show();
        _completionWindow.Closed += (s, e) => _completionWindow = null;
    }

    private string GetWordBeforeCaret()
    {
        var offset = BasicEditor.CaretOffset;
        var document = BasicEditor.Document;

        int start = offset;
        while (start > 0)
        {
            var c = document.GetCharAt(start - 1);
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                break;
            start--;
        }

        return document.GetText(start, offset - start);
    }

    #endregion

    #region UI Updates

    private void BasicEditor_TextChanged(object sender, EventArgs e)
    {
        _isModified = true;
        UpdateTitle();
        UpdateLineCount();
        ModifiedIndicator.Visibility = Visibility.Visible;
        RestartErrorCheckTimer();
        RestartFoldingUpdateTimer();
        RestartSymbolsUpdateTimer();

        // Auto-save unsaved scripts to temp directory
        AutoSaveToTemp();

        // Auto-compile if not being suppressed (prevents infinite loop)
        if (!_suppressBasicUpdate && AutoCompileMenu.IsChecked)
        {
            _suppressMipsUpdate = true;
            Compile();
            _suppressMipsUpdate = false;
        }
    }

    private void MipsOutput_TextChanged(object sender, EventArgs e)
    {
        if (_suppressMipsUpdate) return;

        UpdateLineCount();
        // Mark as modified when user edits IC10 directly
        _isModified = true;
        ModifiedIndicator.Visibility = Visibility.Visible;
        UpdateTitle();
    }

    private void Decompile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MipsOutput.Text))
        {
            SetStatus("No IC10 code to decompile", false);
            return;
        }

        try
        {
            var result = _compiler.Decompile(MipsOutput.Text);

            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                var response = MessageBox.Show(
                    "Replace BASIC source with decompiled code?\n\nThis will overwrite your current BASIC code.",
                    "Confirm Decompile",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (response == MessageBoxResult.Yes)
                {
                    _suppressBasicUpdate = true;
                    BasicEditor.Text = result.Output;
                    _suppressBasicUpdate = false;
                    SetStatus("IC10 decompiled to BASIC successfully", true);
                }
            }
            else
            {
                MessageBox.Show(
                    $"Decompilation failed:\n{result.ErrorMessage}",
                    "Decompile Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error during decompilation:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void UpdateTitle()
    {
        var fileName = string.IsNullOrEmpty(_currentFilePath)
            ? "Untitled"
            : Path.GetFileName(_currentFilePath);

        var modified = _isModified ? "*" : "";
        Title = $"{fileName}{modified} - BASIC to IC10 Compiler";
        BasicFileNameText.Text = string.IsNullOrEmpty(_currentFilePath) ? "" : $"- {fileName}";
    }

    private void UpdateLineCount()
    {
        var basicLines = BasicEditor.LineCount;
        var mipsLines = string.IsNullOrEmpty(MipsOutput.Text) ? 0 : MipsOutput.LineCount;

        BasicLineCountText.Text = $"Lines: {basicLines}";
        MipsLineCountText.Text = $"Lines: {mipsLines} / 128";

        // Update warning badges
        LineWarningBadge.Visibility = mipsLines >= 100 && mipsLines <= 128 ? Visibility.Visible : Visibility.Collapsed;
        LineErrorBadge.Visibility = mipsLines > 128 ? Visibility.Visible : Visibility.Collapsed;

        // Update line budget indicator in status bar with color coding
        LineBudgetText.Text = $"{mipsLines}/128";
        if (mipsLines > 128)
        {
            MipsLineCountText.Foreground = (Brush)FindResource("ErrorBrush");
            LineBudgetIndicator.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
        }
        else if (mipsLines >= 100)
        {
            MipsLineCountText.Foreground = (Brush)FindResource("WarningBrush");
            LineBudgetIndicator.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
        }
        else
        {
            MipsLineCountText.Foreground = (Brush)FindResource("SecondaryTextBrush");
            LineBudgetIndicator.Background = new SolidColorBrush(Color.FromRgb(0, 200, 83)); // Green
        }
    }

    private void UpdateCursorPosition()
    {
        var line = BasicEditor.TextArea.Caret.Line;
        var col = BasicEditor.TextArea.Caret.Column;
        CursorPositionText.Text = $"Ln {line}, Col {col}";
    }

    private void SetStatus(string message, bool success)
    {
        StatusText.Text = message;
        StatusIcon.Text = success ? "\uE73E" : "\uE7BA";
    }

    private void ShowError(string message, int? line)
    {
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorText.Text = line.HasValue ? $"Line {line}: {message}" : message;
        ErrorIcon.Foreground = (Brush)FindResource("ErrorBrush");
    }

    private void ClearError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
    }

    private void ClearOutput()
    {
        MipsOutput.Text = "";
        UpdateLineCount();
        ClearError();
    }

    #endregion

    #region Window Events

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Set version display
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.6.3";
        VersionText.Text = $"Stationeers Basic-10 v{versionStr}";

        BasicEditor.Focus();

        // Start HTTP API server for MCP integration
        StartHttpApiServer();

        // Try to auto-detect Stationeers scripts folder
        if (string.IsNullOrEmpty(_settings.StationeersPath))
        {
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "Stationeers");

            if (Directory.Exists(defaultPath))
            {
                _settings.StationeersPath = defaultPath;
                _settings.Save();
                StationeersPathText.Text = $"Scripts: {defaultPath}";
            }
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!CheckSaveChanges())
        {
            e.Cancel = true;
        }
        else
        {
            // Stop HTTP API server
            StopHttpApiServer();
            _settings.Save();
        }
    }

    #endregion

    #region Output Mode

    private string _outputMode = "Readable";

    private void OutputMode_Click(object sender, RoutedEventArgs e)
    {
        OutputModeReadable.IsChecked = sender == OutputModeReadable;
        OutputModeCompact.IsChecked = sender == OutputModeCompact;
        OutputModeDebug.IsChecked = sender == OutputModeDebug;

        _outputMode = sender == OutputModeReadable ? "Readable" :
                      sender == OutputModeCompact ? "Compact" : "Debug";

        // Re-compile with new output mode
        if (!string.IsNullOrEmpty(BasicEditor.Text))
        {
            Compile();
        }
    }

    #endregion

    #region Panels Toggle

    private void ToggleSymbols_Click(object sender, RoutedEventArgs e)
    {
        var show = ShowSymbolsMenu.IsChecked;
        if (show)
        {
            SymbolsPanel.Visibility = Visibility.Visible;
            SymbolsSplitter.Visibility = Visibility.Visible;
            UpdateSymbolsList();
        }
        else
        {
            SymbolsPanel.Visibility = Visibility.Collapsed;
            SymbolsSplitter.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseSymbols_Click(object sender, RoutedEventArgs e)
    {
        ShowSymbolsMenu.IsChecked = false;
        ToggleSymbols_Click(sender, e);
    }

    private void ToggleSnippets_Click(object sender, RoutedEventArgs e)
    {
        if (ShowSnippetsMenu.IsChecked)
        {
            PopulateSnippetsPanel();
            SnippetsPopup.Visibility = Visibility.Visible;
        }
        else
        {
            SnippetsPopup.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseSnippetsPanel_Click(object sender, RoutedEventArgs e)
    {
        ShowSnippetsMenu.IsChecked = false;
        SnippetsPopup.Visibility = Visibility.Collapsed;
    }

    private void PopulateSnippetsPanel()
    {
        SnippetsPanelList.Children.Clear();
        var snippets = _docs.GetSnippets();

        foreach (var snippet in snippets)
        {
            var btn = new Button
            {
                Style = (Style)FindResource("ModernButtonStyle"),
                Content = snippet.Name,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 4),
                Padding = new Thickness(8, 6, 8, 6),
                Tag = snippet.Code
            };
            btn.Click += (s, ev) =>
            {
                var code = (string)((Button)s).Tag;
                InsertSnippetAtCursor(code);
            };
            SnippetsPanelList.Children.Add(btn);
        }
    }

    private void InsertSnippetAtCursor(string code)
    {
        var offset = BasicEditor.CaretOffset;
        BasicEditor.Document.Insert(offset, code);
        BasicEditor.CaretOffset = offset + code.Length;
        BasicEditor.Focus();
        SetStatus("Snippet inserted", true);
    }

    private void ToggleSimulator_Click(object sender, RoutedEventArgs e)
    {
        if (ShowSimulatorMenu.IsChecked)
        {
            ShowSimulatorWindow();
        }
        else
        {
            _simulatorWindow?.Hide();
        }
    }

    private void ShowSimulatorWindow()
    {
        if (_simulatorWindow == null)
        {
            _simulatorWindow = new SimulatorWindow(MipsOutput.Text);
            _simulatorWindow.Owner = this;
            _simulatorWindow.Closed += (s, e) =>
            {
                _simulatorWindow = null;
                ShowSimulatorMenu.IsChecked = false;
            };

            // Position to the right of the main window
            _simulatorWindow.Left = Left + Width + 10;
            _simulatorWindow.Top = Top;
        }
        else
        {
            // Update code in existing window
            _simulatorWindow.LoadCode(MipsOutput.Text);
        }

        _simulatorWindow.Show();
        _simulatorWindow.Activate();
    }

    private void CloseSimulatorPanel_Click(object sender, RoutedEventArgs e)
    {
        ShowSimulatorMenu.IsChecked = false;
        _simulatorWindow?.Hide();
    }

    private BasicToMips.Simulator.IC10Simulator? _simulator;

    // Floating tool windows
    private SimulatorWindow? _simulatorWindow;
    private WatchWindow? _watchWindow;
    private VariableInspectorWindow? _variableInspectorWindow;

    private void InitializeSimulator()
    {
        if (_simulator == null)
        {
            _simulator = new BasicToMips.Simulator.IC10Simulator();
            _simulator.StateChanged += (s, e) => Dispatcher.Invoke(() =>
            {
                UpdateWatchValues();
            });
        }

        // Load current IC10 code
        var ic10Code = MipsOutput.Text;
        if (!string.IsNullOrWhiteSpace(ic10Code))
        {
            _simulator.LoadProgram(ic10Code);
        }
    }

    // Problems Panel
    private void ToggleProblems_Click(object sender, RoutedEventArgs e)
    {
        if (ShowProblemsMenu.IsChecked)
        {
            ProblemsPanel.Visibility = Visibility.Visible;
            UpdateProblemsList();
        }
        else
        {
            ProblemsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void CloseProblemsPanel_Click(object sender, RoutedEventArgs e)
    {
        ShowProblemsMenu.IsChecked = false;
        ProblemsPanel.Visibility = Visibility.Collapsed;
    }

    private void ProblemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProblemsList.SelectedItem is ProblemItem problem && problem.Line > 0)
        {
            // Navigate to the line in the editor
            var line = Math.Min(problem.Line, BasicEditor.Document.LineCount);
            var docLine = BasicEditor.Document.GetLineByNumber(line);
            BasicEditor.CaretOffset = docLine.Offset;
            BasicEditor.ScrollToLine(line);
            BasicEditor.Focus();
        }
    }

    private void UpdateProblemsList()
    {
        var problems = new List<ProblemItem>();
        var errors = _errorChecker.Check(BasicEditor.Text);

        int errorCount = 0;
        int warningCount = 0;

        // Add syntax errors from error checker
        foreach (var error in errors)
        {
            var isError = error.Severity == Editor.ErrorHighlighting.ErrorChecker.ErrorSeverity.Error;
            var isWarning = error.Severity == Editor.ErrorHighlighting.ErrorChecker.ErrorSeverity.Warning;

            if (isError) errorCount++;
            if (isWarning) warningCount++;

            problems.Add(new ProblemItem
            {
                Line = error.Line,
                Column = error.Column,
                Message = error.Message,
                Severity = error.Severity,
                Icon = isError ? "\uEA39" : (isWarning ? "\uE7BA" : "\uE946"),
                Color = isError ? (Brush)FindResource("ErrorBrush") :
                        (isWarning ? (Brush)FindResource("WarningBrush") : (Brush)FindResource("InfoBrush")),
                Location = $"Ln {error.Line}"
            });
        }

        // Add compilation errors and warnings
        if (_lastCompilationResult != null)
        {
            // Add compilation error if present
            if (!_lastCompilationResult.Success && !string.IsNullOrEmpty(_lastCompilationResult.ErrorMessage))
            {
                errorCount++;
                problems.Add(new ProblemItem
                {
                    Line = _lastCompilationResult.ErrorLine ?? 0,
                    Column = 1,
                    Message = _lastCompilationResult.ErrorMessage,
                    Severity = Editor.ErrorHighlighting.ErrorChecker.ErrorSeverity.Error,
                    Icon = "\uEA39",
                    Color = (Brush)FindResource("ErrorBrush"),
                    Location = _lastCompilationResult.ErrorLine.HasValue ? $"Ln {_lastCompilationResult.ErrorLine}" : "—"
                });
            }

            // Add analysis/compilation warnings
            foreach (var warning in _lastCompilationResult.Warnings)
            {
                warningCount++;
                problems.Add(new ProblemItem
                {
                    Line = warning.Line,
                    Column = 1,
                    Message = warning.Message,
                    Severity = Editor.ErrorHighlighting.ErrorChecker.ErrorSeverity.Warning,
                    Icon = "\uE7BA",
                    Color = (Brush)FindResource("WarningBrush"),
                    Location = warning.Line > 0 ? $"Ln {warning.Line}" : "—"
                });
            }
        }

        ProblemsList.ItemsSource = problems;

        // Update badges
        ErrorCountText.Text = errorCount.ToString();
        ErrorCountBadge.Visibility = errorCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        WarningCountText.Text = warningCount.ToString();
        WarningCountBadge.Visibility = warningCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Auto-show panel if there are errors
        if (errorCount > 0 && ProblemsPanel.Visibility == Visibility.Collapsed)
        {
            ShowProblemsMenu.IsChecked = true;
            ProblemsPanel.Visibility = Visibility.Visible;
        }
    }

    #endregion

    #region Symbols List

    private DispatcherTimer? _symbolsUpdateTimer;

    private void SetupSymbolsTimer()
    {
        _symbolsUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _symbolsUpdateTimer.Tick += (s, e) =>
        {
            _symbolsUpdateTimer.Stop();
            UpdateSymbolsList();
        };
    }

    private void RestartSymbolsUpdateTimer()
    {
        _symbolsUpdateTimer?.Stop();
        _symbolsUpdateTimer?.Start();
    }

    private void UpdateSymbolsList()
    {
        if (SymbolsPanel.Visibility != Visibility.Visible) return;

        var text = BasicEditor.Text;
        var lines = text.Split('\n');

        var variables = new List<(string Name, int Line)>();
        var constants = new List<(string Name, int Line)>();
        var labels = new List<(string Name, int Line)>();
        var aliases = new List<(string Name, int Line)>();
        var arrays = new List<(string Name, int Line)>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineNum = i + 1;

            // Skip comments
            if (line.StartsWith("'") || line.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
                continue;

            // VAR or LET declaration
            if (line.StartsWith("VAR ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("LET ", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:VAR|LET)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                    variables.Add((match.Groups[1].Value, lineNum));
            }
            // CONST or DEFINE
            else if (line.StartsWith("CONST ", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("DEFINE ", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:CONST|DEFINE)\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                    constants.Add((match.Groups[1].Value, lineNum));
            }
            // ALIAS
            else if (line.StartsWith("ALIAS ", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"ALIAS\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                    aliases.Add((match.Groups[1].Value, lineNum));
            }
            // DEVICE
            else if (line.StartsWith("DEVICE ", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"DEVICE\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                    aliases.Add((match.Groups[1].Value, lineNum));
            }
            // DIM (array declaration)
            else if (line.StartsWith("DIM ", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"DIM\s+(\w+)\s*\(", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                    arrays.Add((match.Groups[1].Value, lineNum));
            }
            // Label (word followed by colon at start of line)
            else if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^(\w+):"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^(\w+):");
                if (match.Success)
                    labels.Add((match.Groups[1].Value, lineNum));
            }
        }

        // Update UI
        PopulateSymbolList(VariablesListPanel, variables, "VAR");
        PopulateSymbolList(ConstantsListPanel, constants, "CONST");
        PopulateSymbolList(LabelsListPanel, labels, "LABEL");
        PopulateSymbolList(AliasesListPanel, aliases, "ALIAS");
        PopulateSymbolList(ArraysListPanel, arrays, "ARRAY");
    }

    private void PopulateSymbolList(StackPanel panel, List<(string Name, int Line)> symbols, string type)
    {
        panel.Children.Clear();

        if (symbols.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "(none)",
                FontStyle = FontStyles.Italic,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
                FontSize = 10,
                Margin = new Thickness(8, 2, 0, 2)
            });
            return;
        }

        foreach (var (name, line) in symbols)
        {
            var btn = new Button
            {
                Content = $"{name} (:{line})",
                Style = (Style)FindResource("IconButtonStyle"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 2, 4, 2),
                FontSize = 11,
                Tag = line
            };
            btn.Click += (s, e) =>
            {
                var targetLine = (int)((Button)s).Tag;
                GoToLine(targetLine);
            };
            panel.Children.Add(btn);
        }
    }

    private void GoToLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > BasicEditor.LineCount)
            return;

        var line = BasicEditor.Document.GetLineByNumber(lineNumber);
        BasicEditor.CaretOffset = line.Offset;
        BasicEditor.ScrollToLine(lineNumber);
        BasicEditor.Focus();
    }

    #endregion

    #region Split View

    private void SplitView_Click(object sender, RoutedEventArgs e)
    {
        // Update checkmarks
        SplitHorizontalMenu.IsChecked = sender == SplitHorizontalMenu;
        SplitVerticalMenu.IsChecked = sender == SplitVerticalMenu;
        SplitEditorOnlyMenu.IsChecked = sender == SplitEditorOnlyMenu;

        if (sender == SplitHorizontalMenu)
        {
            ApplySplitView("Horizontal");
        }
        else if (sender == SplitVerticalMenu)
        {
            ApplySplitView("Vertical");
        }
        else if (sender == SplitEditorOnlyMenu)
        {
            ApplySplitView("EditorOnly");
        }
    }

    private void ApplySplitView(string mode)
    {
        // Get the parent Border elements
        var basicEditorBorder = BasicEditor.Parent as Grid;
        var basicPanel = basicEditorBorder?.Parent as Border;
        var mipsPanel = MipsOutputPanel;

        switch (mode)
        {
            case "Horizontal":
                // Horizontal split = horizontal divider = top/bottom layout (default)
                // Reset to row-based layout
                EditorGrid.ColumnDefinitions.Clear();
                EditorGrid.RowDefinitions.Clear();
                EditorGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 150 });
                EditorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                EditorGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 150 });

                // Set elements to rows
                if (basicPanel != null)
                {
                    Grid.SetRow(basicPanel, 0);
                    Grid.SetColumn(basicPanel, 0);
                }
                Grid.SetRow(EditorSplitter, 1);
                Grid.SetColumn(EditorSplitter, 0);
                Grid.SetRow(mipsPanel, 2);
                Grid.SetColumn(mipsPanel, 0);

                // Configure splitter for horizontal orientation
                EditorSplitter.Height = 6;
                EditorSplitter.Width = double.NaN;
                EditorSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                EditorSplitter.VerticalAlignment = VerticalAlignment.Center;
                EditorSplitter.Cursor = System.Windows.Input.Cursors.SizeNS;

                MipsOutputPanel.Visibility = Visibility.Visible;
                EditorSplitter.Visibility = Visibility.Visible;
                SetStatus("Split view: Horizontal (Top/Bottom)", true);
                break;

            case "Vertical":
                // Vertical split = vertical divider = side-by-side layout
                // Change to column-based layout
                EditorGrid.RowDefinitions.Clear();
                EditorGrid.ColumnDefinitions.Clear();
                EditorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 });
                EditorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                EditorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 200 });

                // Set elements to columns
                if (basicPanel != null)
                {
                    Grid.SetRow(basicPanel, 0);
                    Grid.SetColumn(basicPanel, 0);
                }
                Grid.SetRow(EditorSplitter, 0);
                Grid.SetColumn(EditorSplitter, 1);
                Grid.SetRow(mipsPanel, 0);
                Grid.SetColumn(mipsPanel, 2);

                // Configure splitter for vertical orientation
                EditorSplitter.Width = 6;
                EditorSplitter.Height = double.NaN;
                EditorSplitter.HorizontalAlignment = HorizontalAlignment.Center;
                EditorSplitter.VerticalAlignment = VerticalAlignment.Stretch;
                EditorSplitter.Cursor = System.Windows.Input.Cursors.SizeWE;

                MipsOutputPanel.Visibility = Visibility.Visible;
                EditorSplitter.Visibility = Visibility.Visible;
                SetStatus("Split view: Vertical (Side by Side)", true);
                break;

            case "EditorOnly":
                // Hide IC10 output and expand editor to fill space
                EditorGrid.RowDefinitions.Clear();
                EditorGrid.ColumnDefinitions.Clear();
                EditorGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                // Position editor in full space
                if (basicPanel != null)
                {
                    Grid.SetRow(basicPanel, 0);
                    Grid.SetColumn(basicPanel, 0);
                }

                MipsOutputPanel.Visibility = Visibility.Collapsed;
                EditorSplitter.Visibility = Visibility.Collapsed;
                SetStatus("Split view: Editor Only", true);
                break;
        }
    }

    #endregion

    #region Settings Handlers

    private void SyntaxColors_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SyntaxColorsWindow(_settings);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.ColorsChanged)
        {
            // Refresh syntax highlighting with new colors
            ApplySyntaxColors();
        }
    }

    private void ApplySyntaxColors()
    {
        BasicHighlighting.SetColors(_settings.SyntaxColors);
        MipsHighlighting.SetColors(_settings.SyntaxColors);
        BasicEditor.SyntaxHighlighting = BasicHighlighting.Create();
        MipsOutput.SyntaxHighlighting = MipsHighlighting.Create();

        // Apply editor background color
        var bgColor = _settings.SyntaxColors.GetEditorBackgroundColor();
        var bgBrush = new System.Windows.Media.SolidColorBrush(bgColor);
        BasicEditor.Background = bgBrush;
        MipsOutput.Background = bgBrush;
    }

    #endregion

    #region Contextual F1 Help

    private void SetupF1Help()
    {
        // Window-level key handler for F1 (works anywhere in the app)
        this.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.F1)
            {
                ShowContextualHelp();
                e.Handled = true;
            }
            else if (e.Key == Key.F8)
            {
                // Toggle watch panel
                ShowWatchMenu.IsChecked = !ShowWatchMenu.IsChecked;
                ToggleWatch_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                // Format document
                FormatDocument();
                e.Handled = true;
            }
            else if (e.Key == Key.F9)
            {
                // Toggle Variable Inspector
                ShowVariableInspectorMenu.IsChecked = !ShowVariableInspectorMenu.IsChecked;
                ToggleVariableInspector_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.None)
            {
                // Open Simulator for stepping - F10 shows simulator
                ShowSimulatorMenu.IsChecked = true;
                ShowSimulatorWindow();
                e.Handled = true;
            }
            else if (e.Key == Key.F11 && Keyboard.Modifiers == ModifierKeys.None)
            {
                // Open Simulator for stepping - F11 shows simulator
                ShowSimulatorMenu.IsChecked = true;
                ShowSimulatorWindow();
                e.Handled = true;
            }
        };

        // Editor-specific keys (F12 for Go to Definition)
        BasicEditor.PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.F12)
            {
                GoToDefinition();
                e.Handled = true;
            }
        };
    }

    private void GoToDefinition()
    {
        var word = GetWordAtCaret();
        if (string.IsNullOrEmpty(word)) return;

        // First check source map symbols (from last compilation)
        if (_sourceMap != null)
        {
            var symbol = _sourceMap.GetSymbol(word);
            if (symbol != null)
            {
                JumpToLine(symbol.Line);
                SetStatus($"Go to definition: {word} ({symbol.Kind})", true);
                return;
            }

            // Try case-insensitive match
            var matchingSymbol = _sourceMap.Symbols
                .FirstOrDefault(kvp => kvp.Key.Equals(word, StringComparison.OrdinalIgnoreCase));
            if (matchingSymbol.Value != null)
            {
                JumpToLine(matchingSymbol.Value.Line);
                SetStatus($"Go to definition: {matchingSymbol.Key} ({matchingSymbol.Value.Kind})", true);
                return;
            }
        }

        // Fallback: search for definition patterns in the source
        var patterns = new[]
        {
            $@"^\s*SUB\s+{word}\s*",           // SUB definition
            $@"^\s*FUNCTION\s+{word}\s*",      // FUNCTION definition
            $@"^\s*{word}\s*:",                // Label
            $@"^\s*ALIAS\s+{word}\s*",         // ALIAS
            $@"^\s*CONST\s+{word}\s*",         // CONST
            $@"^\s*DEFINE\s+{word}\s*",        // DEFINE
            $@"^\s*VAR\s+{word}\s*",           // VAR
            $@"^\s*DIM\s+{word}\s*"            // DIM array
        };

        var text = BasicEditor.Text;
        var lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            foreach (var pattern in patterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(lines[i], pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    JumpToLine(i + 1);
                    SetStatus($"Go to definition: {word}", true);
                    return;
                }
            }
        }

        SetStatus($"Definition not found: {word}", false);
    }

    private void JumpToLine(int lineNumber)
    {
        if (lineNumber > 0 && lineNumber <= BasicEditor.Document.LineCount)
        {
            var line = BasicEditor.Document.GetLineByNumber(lineNumber);
            BasicEditor.ScrollToLine(lineNumber);
            BasicEditor.TextArea.Caret.Offset = line.Offset;
            BasicEditor.TextArea.Caret.BringCaretToView();
            BasicEditor.Focus();
            HighlightLine(BasicEditor, lineNumber);
        }
    }

    private void ShowContextualHelp()
    {
        // Get word under cursor
        var word = GetWordAtCaret().ToUpperInvariant();

        // Show docs panel if hidden
        if (!ShowDocsMenu.IsChecked)
        {
            ShowDocsMenu.IsChecked = true;
            ToggleDocs_Click(this, new RoutedEventArgs());
        }

        // Navigate to appropriate tab based on keyword
        int tabIndex = GetHelpTabForKeyword(word);
        DocsTabControl.SelectedIndex = tabIndex;

        SetStatus($"Help: {(string.IsNullOrEmpty(word) ? "General" : word)}", true);
    }

    private string GetWordAtCaret()
    {
        var offset = BasicEditor.CaretOffset;
        var document = BasicEditor.Document;

        if (offset <= 0 || offset > document.TextLength)
            return "";

        int start = offset;
        int end = offset;

        // Find start of word
        while (start > 0)
        {
            var c = document.GetCharAt(start - 1);
            if (!char.IsLetterOrDigit(c) && c != '_')
                break;
            start--;
        }

        // Find end of word
        while (end < document.TextLength)
        {
            var c = document.GetCharAt(end);
            if (!char.IsLetterOrDigit(c) && c != '_')
                break;
            end++;
        }

        if (end <= start)
            return "";

        return document.GetText(start, end - start);
    }

    private int GetHelpTabForKeyword(string keyword)
    {
        // Tab indices: 0=Start, 1=Syntax, 2=Funcs, 3=Devices, 4=IC10, 5=Tips, 6=Examples, 7=Wiki

        // Control flow keywords -> Syntax tab
        var syntaxKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IF", "THEN", "ELSE", "ELSEIF", "ENDIF", "END",
            "WHILE", "WEND", "FOR", "TO", "STEP", "NEXT",
            "DO", "LOOP", "UNTIL", "BREAK", "CONTINUE",
            "SELECT", "CASE", "DEFAULT", "GOTO", "GOSUB", "RETURN",
            "SUB", "FUNCTION", "CALL", "EXIT", "VAR", "LET",
            "CONST", "DEFINE", "ALIAS", "DEVICE", "REM",
            "AND", "OR", "NOT", "MOD", "YIELD", "SLEEP", "WAIT"
        };

        // Built-in functions -> Funcs tab
        var funcKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ABS", "SQRT", "MIN", "MAX", "CEIL", "FLOOR", "ROUND", "TRUNC", "SGN",
            "SIN", "COS", "TAN", "ASIN", "ACOS", "ATAN", "ATAN2", "LOG", "EXP",
            "RND", "RAND", "PUSH", "POP", "PEEK", "BATCHREAD", "BATCHWRITE"
        };

        // Device properties -> Devices tab
        var deviceKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TEMPERATURE", "PRESSURE", "RATIO", "ON", "SETTING", "MODE", "OPEN", "LOCK",
            "POWER", "CHARGE", "PREFABHASH", "REFERENCEID", "OCCUPIED", "OCCUPANTHASH",
            "QUANTITY", "SLOT", "HORIZONTAL", "VERTICAL", "SOLARANGLE", "D0", "D1", "D2",
            "D3", "D4", "D5", "DB", "THIS", "RATIOOXYGEN", "RATIOCARBONDIOXIDE"
        };

        // IC10/MIPS keywords -> IC10 tab
        var ic10Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ADD", "SUB", "MUL", "DIV", "MOD", "SLT", "SGT", "SEQ", "SNE", "SLE", "SGE",
            "J", "JR", "JAL", "BEQ", "BNE", "BLT", "BGT", "BLE", "BGE", "MOVE",
            "L", "S", "LS", "SS", "LB", "SB", "LBN", "SBN", "R0", "R1", "R2", "R3",
            "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15", "SP", "RA"
        };

        if (string.IsNullOrEmpty(keyword))
            return 0; // Start tab

        if (syntaxKeywords.Contains(keyword))
            return 1; // Syntax tab

        if (funcKeywords.Contains(keyword))
            return 2; // Funcs tab

        if (deviceKeywords.Contains(keyword))
            return 3; // Devices tab

        if (ic10Keywords.Contains(keyword))
            return 4; // IC10 tab

        return 0; // Default to Start tab
    }

    #endregion

    #region Watch Variables Panel

    private void ToggleWatch_Click(object sender, RoutedEventArgs e)
    {
        if (ShowWatchMenu.IsChecked)
        {
            ShowWatchWindow();
        }
        else
        {
            _watchWindow?.Hide();
        }
    }

    private void ShowWatchWindow()
    {
        if (_watchWindow == null)
        {
            _watchWindow = new WatchWindow(_watchManager);
            _watchWindow.Owner = this;
            _watchWindow.Closing += (s, e) =>
            {
                ShowWatchMenu.IsChecked = false;
            };

            // Position to the right of main window, below simulator if open
            _watchWindow.Left = Left + Width + 10;
            _watchWindow.Top = Top + 300;
        }

        _watchWindow.SetSimulator(_simulator);
        _watchWindow.Show();
        _watchWindow.Activate();
    }

    private void CloseWatchPanel_Click(object sender, RoutedEventArgs e)
    {
        ShowWatchMenu.IsChecked = false;
        _watchWindow?.Hide();
    }

    private void UpdateWatchValues()
    {
        // Update watch values in floating window
        _watchWindow?.RefreshValues();
        _variableInspectorWindow?.RefreshValues();
    }

    #endregion

    #region Variable Inspector

    private void ToggleVariableInspector_Click(object sender, RoutedEventArgs e)
    {
        if (ShowVariableInspectorMenu.IsChecked)
        {
            ShowVariableInspectorWindow();
        }
        else
        {
            _variableInspectorWindow?.Hide();
        }
    }

    private void ShowVariableInspectorWindow()
    {
        if (_variableInspectorWindow == null)
        {
            _variableInspectorWindow = new VariableInspectorWindow();
            _variableInspectorWindow.Owner = this;
            _variableInspectorWindow.Closing += (s, e) =>
            {
                ShowVariableInspectorMenu.IsChecked = false;
            };

            // Position to the right of main window
            _variableInspectorWindow.Left = Left + Width + 10;
            _variableInspectorWindow.Top = Top + 150;
        }

        // Load variables from current code
        _variableInspectorWindow.LoadFromBasicCode(BasicEditor.Text, MipsOutput.Text);
        _variableInspectorWindow.SetSimulator(_simulator);
        _variableInspectorWindow.Show();
        _variableInspectorWindow.Activate();
    }

    #endregion

    #region Wiki Browser

    private const string WikiHomeUrl = "https://stationeers-wiki.com/IC10";

    private void WikiBack_Click(object sender, RoutedEventArgs e)
    {
        if (WikiBrowser.CanGoBack)
        {
            WikiBrowser.GoBack();
        }
    }

    private void WikiForward_Click(object sender, RoutedEventArgs e)
    {
        if (WikiBrowser.CanGoForward)
        {
            WikiBrowser.GoForward();
        }
    }

    private void WikiRefresh_Click(object sender, RoutedEventArgs e)
    {
        WikiBrowser.Reload();
    }

    private void WikiHome_Click(object sender, RoutedEventArgs e)
    {
        WikiBrowser.Source = new Uri(WikiHomeUrl);
        WikiAddressBar.Text = WikiHomeUrl;
    }

    private void WikiAddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateToWikiUrl();
            e.Handled = true;
        }
    }

    private void WikiGo_Click(object sender, RoutedEventArgs e)
    {
        NavigateToWikiUrl();
    }

    private void NavigateToWikiUrl()
    {
        var url = WikiAddressBar.Text?.Trim();
        if (string.IsNullOrEmpty(url)) return;

        // Add https:// if no protocol specified
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        try
        {
            WikiBrowser.Source = new Uri(url);
        }
        catch (UriFormatException)
        {
            SetStatus("Invalid URL format", false);
        }
    }

    private void WikiBrowser_NavigationCompleted(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
    {
        // Update address bar with current URL
        if (WikiBrowser.Source != null)
        {
            WikiAddressBar.Text = WikiBrowser.Source.ToString();
        }
    }

    #endregion

    #region API Bridge Methods

    /// <summary>
    /// Start the HTTP API server for MCP integration.
    /// </summary>
    private void StartHttpApiServer()
    {
        if (!_settings.ApiServerEnabled)
        {
            System.Diagnostics.Debug.WriteLine("HTTP API server disabled in settings");
            return;
        }

        try
        {
            var bridge = new EditorBridgeService(this);
            _httpApiServer = new HttpApiServer(bridge, _settings.ApiServerPort);
            _httpApiServer.Start();
            System.Diagnostics.Debug.WriteLine($"HTTP API server started on port {_settings.ApiServerPort}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start HTTP API server: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop the HTTP API server.
    /// </summary>
    private void StopHttpApiServer()
    {
        try
        {
            _httpApiServer?.Stop();
            _httpApiServer = null;
            System.Diagnostics.Debug.WriteLine("HTTP API server stopped");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping HTTP API server: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the current BASIC source code from the editor.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public string GetEditorCode()
    {
        return BasicEditor.Text;
    }

    /// <summary>
    /// Set the BASIC source code in the editor.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public void SetEditorCode(string code)
    {
        BasicEditor.Text = code;
        _isModified = true;
        UpdateTitle();
        ModifiedIndicator.Visibility = Visibility.Visible;
        if (_settings.AutoCompile)
        {
            Compile();
        }
    }

    /// <summary>
    /// Insert code at the current cursor position or at a specific line.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public void InsertCode(string code, int? atLine = null)
    {
        if (atLine.HasValue && atLine.Value > 0 && atLine.Value <= BasicEditor.Document.LineCount)
        {
            var line = BasicEditor.Document.GetLineByNumber(atLine.Value);
            BasicEditor.Document.Insert(line.Offset, code + Environment.NewLine);
        }
        else
        {
            var offset = BasicEditor.CaretOffset;
            BasicEditor.Document.Insert(offset, code);
            BasicEditor.CaretOffset = offset + code.Length;
        }
        _isModified = true;
        UpdateTitle();
        ModifiedIndicator.Visibility = Visibility.Visible;
        if (_settings.AutoCompile)
        {
            Compile();
        }
    }

    /// <summary>
    /// Get the current IC10 output from the output panel.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public string GetIc10Output()
    {
        return MipsOutput.Text;
    }

    /// <summary>
    /// Set the IC10 output in the output panel.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public void SetIc10Output(string output)
    {
        _suppressMipsUpdate = true;
        MipsOutput.Text = output;
        _suppressMipsUpdate = false;
        UpdateLineCount();
    }

    /// <summary>
    /// Get the current cursor position in the editor.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public BasicToMips.Services.CursorPosition GetCursorPosition()
    {
        var caret = BasicEditor.TextArea.Caret;
        return new BasicToMips.Services.CursorPosition
        {
            Line = caret.Line,
            Column = caret.Column,
            Offset = caret.Offset
        };
    }

    /// <summary>
    /// Set the cursor position in the editor.
    /// Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public void SetCursorPosition(int line, int column)
    {
        if (line > 0 && line <= BasicEditor.Document.LineCount)
        {
            BasicEditor.ScrollToLine(line);
            var lineObj = BasicEditor.Document.GetLineByNumber(line);
            var maxCol = lineObj.Length;
            var targetCol = Math.Min(Math.Max(1, column), maxCol + 1);
            BasicEditor.TextArea.Caret.Line = line;
            BasicEditor.TextArea.Caret.Column = targetCol;
            BasicEditor.TextArea.Caret.BringCaretToView();
            BasicEditor.Focus();
        }
    }

    // ==================== TAB MANAGEMENT API ====================

    /// <summary>
    /// Create a new tab. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public int ApiCreateNewTab(string? name = null)
    {
        CreateNewTab();
        var tabIndex = _tabs.Count - 1;

        // If a name was provided, we could use it for display (future enhancement)
        return tabIndex;
    }

    /// <summary>
    /// Get list of all open tabs. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public List<TabInfo> ApiGetTabs()
    {
        var result = new List<TabInfo>();
        for (int i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            result.Add(new TabInfo
            {
                Index = i,
                Name = tab.FileName,
                FilePath = tab.FilePath,
                IsModified = tab.IsModified,
                IsActive = tab == _currentTab
            });
        }
        return result;
    }

    /// <summary>
    /// Switch to a specific tab by index. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public bool ApiSwitchTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= _tabs.Count) return false;

        SwitchToTab(_tabs[tabIndex]);
        return true;
    }

    /// <summary>
    /// Switch to a specific tab by name. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public bool ApiSwitchTabByName(string name)
    {
        var tab = _tabs.FirstOrDefault(t =>
            t.FileName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            (t.FilePath != null && Path.GetFileNameWithoutExtension(t.FilePath).Equals(name, StringComparison.OrdinalIgnoreCase)));

        if (tab == null) return false;

        SwitchToTab(tab);
        return true;
    }

    /// <summary>
    /// Close a specific tab by index. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    /// <param name="tabIndex">Index of the tab to close</param>
    /// <param name="force">If true, close without prompting to save unsaved changes</param>
    public bool ApiCloseTab(int tabIndex, bool force = false)
    {
        if (tabIndex < 0 || tabIndex >= _tabs.Count) return false;

        CloseTab(_tabs[tabIndex], promptToSave: !force);
        return true;
    }

    /// <summary>
    /// Save current script to a folder by name. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public SaveScriptResult ApiSaveScript(string scriptName)
    {
        var scriptsFolder = GetScriptsFolder();
        if (string.IsNullOrEmpty(scriptsFolder))
        {
            return new SaveScriptResult { Success = false, Error = "Scripts folder not configured" };
        }

        try
        {
            // Sync current editor to tab before saving
            SyncEditorToTab();

            var success = SaveScriptToFolder(scriptsFolder, scriptName);
            if (success)
            {
                return new SaveScriptResult
                {
                    Success = true,
                    ScriptName = scriptName,
                    FolderPath = Path.Combine(scriptsFolder, scriptName)
                };
            }
            return new SaveScriptResult { Success = false, Error = "Failed to save script" };
        }
        catch (Exception ex)
        {
            return new SaveScriptResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Load a script from a folder by name. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public LoadScriptResult ApiLoadScript(string scriptName, bool newTab = false)
    {
        var scriptsFolder = GetScriptsFolder();
        if (string.IsNullOrEmpty(scriptsFolder))
        {
            return new LoadScriptResult { Success = false, Error = "Scripts folder not configured" };
        }

        var scriptFolder = Path.Combine(scriptsFolder, scriptName);
        if (!Directory.Exists(scriptFolder))
        {
            return new LoadScriptResult { Success = false, Error = $"Script folder not found: {scriptName}" };
        }

        // Find the .bas file
        var basFile = Path.Combine(scriptFolder, $"{scriptName}.bas");
        if (!File.Exists(basFile))
        {
            // Try to find any .bas file in the folder
            var basFiles = Directory.GetFiles(scriptFolder, "*.bas");
            if (basFiles.Length == 0)
            {
                return new LoadScriptResult { Success = false, Error = $"No .bas file found in script folder: {scriptName}" };
            }
            basFile = basFiles[0];
        }

        try
        {
            if (newTab)
            {
                OpenFileInNewTab(basFile);
            }
            else
            {
                // Load into current tab
                var content = File.ReadAllText(basFile);
                BasicEditor.Text = content;
                _currentFilePath = basFile;
                _workingDirectory = scriptFolder;
                _isModified = false;
                if (_currentTab != null)
                {
                    _currentTab.FilePath = basFile;
                    _currentTab.Content = content;
                    _currentTab.IsModified = false;
                }
                UpdateTitle();
                UpdateWorkingDirectoryDisplay();
                _settings.AddRecentFile(basFile);
                UpdateRecentFilesMenu();
            }

            return new LoadScriptResult
            {
                Success = true,
                ScriptName = scriptName,
                FilePath = basFile,
                CodePreview = File.ReadAllText(basFile).Substring(0, Math.Min(500, File.ReadAllText(basFile).Length))
            };
        }
        catch (Exception ex)
        {
            return new LoadScriptResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// List all available scripts in the scripts folder. Called by EditorBridgeService from the HTTP API.
    /// </summary>
    public List<ScriptInfo> ApiListScripts()
    {
        var result = new List<ScriptInfo>();
        var scriptsFolder = GetScriptsFolder();

        if (string.IsNullOrEmpty(scriptsFolder) || !Directory.Exists(scriptsFolder))
        {
            return result;
        }

        foreach (var dir in Directory.GetDirectories(scriptsFolder))
        {
            var scriptName = Path.GetFileName(dir);
            var basFile = Path.Combine(dir, $"{scriptName}.bas");
            var instructionFile = Path.Combine(dir, "instruction.xml");

            if (File.Exists(basFile) || File.Exists(instructionFile))
            {
                result.Add(new ScriptInfo
                {
                    Name = scriptName,
                    FolderPath = dir,
                    HasBasFile = File.Exists(basFile),
                    HasInstructionXml = File.Exists(instructionFile)
                });
            }
        }

        return result;
    }

    #region Simulator API Methods

    /// <summary>
    /// Initialize and load code into the headless simulator.
    /// </summary>
    public SimulatorStateResult ApiSimulatorStart(string? ic10Code = null)
    {
        // If no code provided, use the current compiled output
        var code = ic10Code ?? MipsOutput.Text;
        if (string.IsNullOrEmpty(code))
        {
            return new SimulatorStateResult { Success = false, Error = "No IC10 code to simulate" };
        }

        _mcpSimulator.LoadProgram(code);
        return GetSimulatorState();
    }

    /// <summary>
    /// Stop and reset the simulator.
    /// </summary>
    public SimulatorStateResult ApiSimulatorStop()
    {
        _mcpSimulator.Stop();
        return GetSimulatorState();
    }

    /// <summary>
    /// Reset the simulator to initial state.
    /// </summary>
    public SimulatorStateResult ApiSimulatorReset()
    {
        _mcpSimulator.Reset();
        return GetSimulatorState();
    }

    /// <summary>
    /// Execute a single instruction.
    /// </summary>
    public SimulatorStateResult ApiSimulatorStep()
    {
        _mcpSimulator.Step();
        return GetSimulatorState();
    }

    /// <summary>
    /// Run until breakpoint, halt, or yield.
    /// </summary>
    public SimulatorStateResult ApiSimulatorRun(int maxInstructions = 10000)
    {
        int count = 0;
        while (!_mcpSimulator.IsHalted && !_mcpSimulator.IsPaused && !_mcpSimulator.IsYielding && count < maxInstructions)
        {
            if (_mcpSimulator.Breakpoints.Contains(_mcpSimulator.ProgramCounter))
            {
                break;
            }
            if (!_mcpSimulator.Step()) break;
            count++;
        }
        return GetSimulatorState();
    }

    /// <summary>
    /// Get current simulator state.
    /// </summary>
    public SimulatorStateResult ApiSimulatorGetState()
    {
        return GetSimulatorState();
    }

    private SimulatorStateResult GetSimulatorState()
    {
        var registers = new Dictionary<string, double>();
        for (int i = 0; i < 16; i++)
        {
            registers[$"r{i}"] = _mcpSimulator.Registers[i];
        }
        registers["sp"] = _mcpSimulator.StackPointer;
        registers["ra"] = _mcpSimulator.Registers[17];

        var devices = new List<SimulatorDeviceInfo>();
        for (int i = 0; i < BasicToMips.Simulator.IC10Simulator.DeviceCount; i++)
        {
            var dev = _mcpSimulator.Devices[i];
            devices.Add(new SimulatorDeviceInfo
            {
                Index = i,
                Name = dev.Name,
                Alias = dev.Alias,
                Properties = new Dictionary<string, double>(dev.Properties)
            });
        }

        return new SimulatorStateResult
        {
            Success = true,
            ProgramCounter = _mcpSimulator.ProgramCounter,
            InstructionCount = _mcpSimulator.InstructionCount,
            IsRunning = _mcpSimulator.IsRunning,
            IsPaused = _mcpSimulator.IsPaused,
            IsHalted = _mcpSimulator.IsHalted,
            IsYielding = _mcpSimulator.IsYielding,
            ErrorMessage = _mcpSimulator.ErrorMessage,
            Registers = registers,
            Stack = _mcpSimulator.Stack.Take(_mcpSimulator.StackPointer).ToArray(),
            Devices = devices,
            Breakpoints = _mcpSimulator.Breakpoints.ToList()
        };
    }

    /// <summary>
    /// Set a register value.
    /// </summary>
    public SimulatorStateResult ApiSimulatorSetRegister(string register, double value)
    {
        var reg = register.ToLowerInvariant();
        if (reg.StartsWith("r") && int.TryParse(reg.Substring(1), out int regNum) && regNum >= 0 && regNum < 16)
        {
            _mcpSimulator.Registers[regNum] = value;
        }
        else if (reg == "ra")
        {
            _mcpSimulator.Registers[17] = value;
        }
        else
        {
            return new SimulatorStateResult { Success = false, Error = $"Invalid register: {register}" };
        }
        return GetSimulatorState();
    }

    /// <summary>
    /// Add a breakpoint at a line number.
    /// </summary>
    public SimulatorStateResult ApiSimulatorAddBreakpoint(int line)
    {
        _mcpSimulator.Breakpoints.Add(line);
        return GetSimulatorState();
    }

    /// <summary>
    /// Remove a breakpoint.
    /// </summary>
    public SimulatorStateResult ApiSimulatorRemoveBreakpoint(int line)
    {
        _mcpSimulator.Breakpoints.Remove(line);
        return GetSimulatorState();
    }

    /// <summary>
    /// Clear all breakpoints.
    /// </summary>
    public SimulatorStateResult ApiSimulatorClearBreakpoints()
    {
        _mcpSimulator.Breakpoints.Clear();
        return GetSimulatorState();
    }

    /// <summary>
    /// Set a simulated device property.
    /// </summary>
    public SimulatorStateResult ApiSimulatorSetDeviceProperty(int deviceIndex, string property, double value)
    {
        if (deviceIndex < 0 || deviceIndex >= BasicToMips.Simulator.IC10Simulator.DeviceCount)
        {
            return new SimulatorStateResult { Success = false, Error = $"Invalid device index: {deviceIndex}" };
        }
        _mcpSimulator.Devices[deviceIndex].SetProperty(property, value);
        return GetSimulatorState();
    }

    /// <summary>
    /// Get a simulated device property.
    /// </summary>
    public double ApiSimulatorGetDeviceProperty(int deviceIndex, string property)
    {
        if (deviceIndex < 0 || deviceIndex >= BasicToMips.Simulator.IC10Simulator.DeviceCount)
        {
            return 0;
        }
        return _mcpSimulator.Devices[deviceIndex].GetProperty(property);
    }

    /// <summary>
    /// Set a simulated device slot property.
    /// </summary>
    public SimulatorStateResult ApiSimulatorSetDeviceSlotProperty(int deviceIndex, int slot, string property, double value)
    {
        if (deviceIndex < 0 || deviceIndex >= BasicToMips.Simulator.IC10Simulator.DeviceCount)
        {
            return new SimulatorStateResult { Success = false, Error = $"Invalid device index: {deviceIndex}" };
        }
        _mcpSimulator.Devices[deviceIndex].SetSlotProperty(slot, property, value);
        return GetSimulatorState();
    }

    #endregion

    #region Debugging API Methods

    /// <summary>
    /// Add a watch expression.
    /// </summary>
    public WatchInfo ApiAddWatch(string expression)
    {
        var item = _watchManager.AddWatch(expression);
        // Update values from current simulator state
        _watchManager.UpdateValues(_mcpSimulator);
        return new WatchInfo
        {
            Name = item.Name,
            Value = item.Value,
            Type = item.Type.ToString()
        };
    }

    /// <summary>
    /// Remove a watch by name.
    /// </summary>
    public bool ApiRemoveWatch(string name)
    {
        var item = _watchManager.WatchItems.FirstOrDefault(w => w.Name == name);
        if (item != null)
        {
            _watchManager.RemoveWatch(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get all watch values.
    /// </summary>
    public List<WatchInfo> ApiGetWatches()
    {
        // Update values first
        _watchManager.SetSourceMap(_sourceMap);
        _watchManager.UpdateValues(_mcpSimulator);

        return _watchManager.WatchItems.Select(w => new WatchInfo
        {
            Name = w.Name,
            Value = w.Value,
            Type = w.Type.ToString(),
            HasChanged = w.HasChanged
        }).ToList();
    }

    /// <summary>
    /// Clear all watches.
    /// </summary>
    public void ApiClearWatches()
    {
        _watchManager.ClearAll();
    }

    /// <summary>
    /// Add a BASIC editor breakpoint.
    /// </summary>
    public List<int> ApiAddBreakpoint(int line)
    {
        _breakpointManager.SetBreakpoint(line);
        return _breakpointManager.Breakpoints.ToList();
    }

    /// <summary>
    /// Remove a BASIC editor breakpoint.
    /// </summary>
    public List<int> ApiRemoveBreakpoint(int line)
    {
        _breakpointManager.RemoveBreakpoint(line);
        return _breakpointManager.Breakpoints.ToList();
    }

    /// <summary>
    /// Toggle a BASIC editor breakpoint.
    /// </summary>
    public BreakpointToggleResult ApiToggleBreakpoint(int line)
    {
        var isSet = _breakpointManager.ToggleBreakpoint(line);
        return new BreakpointToggleResult
        {
            Line = line,
            IsSet = isSet,
            AllBreakpoints = _breakpointManager.Breakpoints.ToList()
        };
    }

    /// <summary>
    /// Get all BASIC editor breakpoints.
    /// </summary>
    public List<int> ApiGetBreakpoints()
    {
        return _breakpointManager.Breakpoints.ToList();
    }

    /// <summary>
    /// Clear all BASIC editor breakpoints.
    /// </summary>
    public void ApiClearBreakpoints()
    {
        _breakpointManager.ClearAll();
    }

    /// <summary>
    /// Get the source map (BASIC to IC10 line mapping).
    /// </summary>
    public SourceMapInfo ApiGetSourceMap()
    {
        if (_sourceMap == null)
        {
            return new SourceMapInfo { HasMap = false };
        }

        return new SourceMapInfo
        {
            HasMap = true,
            BasicToIc10 = _sourceMap.BasicToIC10.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()),
            Ic10ToBasic = new Dictionary<int, int>(_sourceMap.IC10ToBasic),
            VariableRegisters = new Dictionary<string, string>(_sourceMap.VariableRegisters),
            AliasDevices = new Dictionary<string, string>(_sourceMap.AliasDevices)
        };
    }

    /// <summary>
    /// Get the current compilation errors and warnings.
    /// </summary>
    public List<ErrorInfo> ApiGetErrors()
    {
        var errors = new List<ErrorInfo>();

        // Get errors from the error checker
        var basicCode = BasicEditor.Text;
        var result = _compiler.Compile(basicCode);

        if (!result.Success && result.ErrorMessage != null)
        {
            errors.Add(new ErrorInfo
            {
                Line = result.ErrorLine ?? 0,
                Message = result.ErrorMessage,
                Severity = "error"
            });
        }

        foreach (var warning in result.Warnings)
        {
            errors.Add(new ErrorInfo
            {
                Line = warning.Line,
                Message = warning.Message,
                Severity = "warning"
            });
        }

        return errors;
    }

    /// <summary>
    /// Navigate to a specific line in the editor.
    /// </summary>
    public void ApiGoToLine(int line)
    {
        var doc = BasicEditor.Document;
        if (line < 1) line = 1;
        if (line > doc.LineCount) line = doc.LineCount;

        var docLine = doc.GetLineByNumber(line);
        BasicEditor.CaretOffset = docLine.Offset;
        BasicEditor.ScrollToLine(line);
        BasicEditor.Focus();
    }

    #endregion

    #region Settings API

    /// <summary>
    /// Get all current settings.
    /// </summary>
    public SettingsSnapshot ApiGetSettings()
    {
        return new SettingsSnapshot
        {
            Theme = _settings.Theme,
            FontSize = _settings.FontSize,
            AutoCompile = _settings.AutoCompile,
            AutoCompleteEnabled = _settings.AutoCompleteEnabled,
            WordWrap = _settings.WordWrap,
            OptimizationLevel = _settings.OptimizationLevel,
            AutoSaveEnabled = _settings.AutoSaveEnabled,
            AutoSaveIntervalSeconds = _settings.AutoSaveIntervalSeconds,
            SplitViewMode = _settings.SplitViewMode,
            ApiServerEnabled = _settings.ApiServerEnabled,
            ApiServerPort = _settings.ApiServerPort,
            ScriptAuthor = _settings.ScriptAuthor
        };
    }

    /// <summary>
    /// Update a setting value.
    /// </summary>
    public SettingsUpdateResult ApiUpdateSetting(string name, object value)
    {
        try
        {
            bool requiresRestart = false;

            switch (name.ToLowerInvariant())
            {
                case "theme":
                    var theme = value.ToString()!;
                    if (theme != "Dark" && theme != "Light")
                        return new SettingsUpdateResult { Success = false, Error = "Theme must be 'Dark' or 'Light'" };
                    _settings.Theme = theme;
                    Services.ThemeManager.ApplyTheme(theme);
                    break;
                case "fontsize":
                    var fontSize = Convert.ToDouble(value);
                    _settings.FontSize = fontSize;
                    BasicEditor.FontSize = fontSize;
                    MipsOutput.FontSize = fontSize;
                    break;
                case "autocompile":
                    _settings.AutoCompile = Convert.ToBoolean(value);
                    break;
                case "autocompleteenabled":
                    _settings.AutoCompleteEnabled = Convert.ToBoolean(value);
                    break;
                case "wordwrap":
                    _settings.WordWrap = Convert.ToBoolean(value);
                    BasicEditor.WordWrap = _settings.WordWrap;
                    break;
                case "optimizationlevel":
                    _settings.OptimizationLevel = Convert.ToInt32(value);
                    break;
                case "autosaveenabled":
                    _settings.AutoSaveEnabled = Convert.ToBoolean(value);
                    break;
                case "autosaveintervalseconds":
                    _settings.AutoSaveIntervalSeconds = Convert.ToInt32(value);
                    break;
                case "splitviewmode":
                    var mode = value.ToString()!;
                    if (mode != "Vertical" && mode != "Horizontal" && mode != "EditorOnly")
                        return new SettingsUpdateResult { Success = false, Error = "SplitViewMode must be 'Vertical', 'Horizontal', or 'EditorOnly'" };
                    _settings.SplitViewMode = mode;
                    requiresRestart = true; // Split view mode requires restart to apply
                    break;
                case "scriptauthor":
                    _settings.ScriptAuthor = value.ToString()!;
                    break;
                default:
                    return new SettingsUpdateResult { Success = false, Error = $"Unknown setting: {name}" };
            }

            _settings.Save();

            if (requiresRestart)
                return new SettingsUpdateResult { Success = true, Name = name, Error = "Setting saved. Restart required to apply." };

            return new SettingsUpdateResult { Success = true, Name = name };
        }
        catch (Exception ex)
        {
            return new SettingsUpdateResult { Success = false, Error = ex.Message };
        }
    }

    #endregion

    #region Code Analysis API

    /// <summary>
    /// Find all references to a symbol in the code.
    /// </summary>
    public List<SymbolReference> ApiFindReferences(string symbolName)
    {
        var occurrences = _refactoringService.FindSymbolOccurrences(BasicEditor.Text, symbolName);
        return occurrences.Select(o => new SymbolReference
        {
            Line = o.Line,
            Column = o.Column,
            Length = o.Length,
            Kind = o.SymbolKind.ToString()
        }).ToList();
    }

    /// <summary>
    /// Get code metrics for the current code.
    /// </summary>
    public CodeMetrics ApiGetCodeMetrics()
    {
        var code = BasicEditor.Text;
        var lines = code.Split('\n');
        var result = _compiler.Compile(code);

        var metrics = new CodeMetrics
        {
            TotalLines = lines.Length,
            CodeLines = lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("'")),
            CommentLines = lines.Count(l => l.TrimStart().StartsWith("'")),
            BlankLines = lines.Count(string.IsNullOrWhiteSpace),
            Ic10Lines = result.LineCount,
            CompilationSuccess = result.Success,
            WarningCount = result.Warnings.Count,
            ErrorCount = result.Success ? 0 : 1
        };

        if (result.Metadata != null)
        {
            metrics.VariableCount = result.Metadata.Variables.Count;
            metrics.ConstantCount = result.Metadata.Constants.Count;
            metrics.LabelCount = result.Metadata.Labels.Count;
            metrics.FunctionCount = result.Metadata.Functions.Count;
            metrics.AliasCount = result.Metadata.DeviceTypes.Count;
        }

        return metrics;
    }

    #endregion

    #endregion
}

/// <summary>
/// Watch variable info for MCP integration.
/// </summary>
public class WatchInfo
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Type { get; set; } = "";
    public bool HasChanged { get; set; }
}

/// <summary>
/// Breakpoint toggle result.
/// </summary>
public class BreakpointToggleResult
{
    public int Line { get; set; }
    public bool IsSet { get; set; }
    public List<int> AllBreakpoints { get; set; } = new();
}

/// <summary>
/// Source map information.
/// </summary>
public class SourceMapInfo
{
    public bool HasMap { get; set; }
    /// <summary>
    /// Maps BASIC line number to list of IC10 line numbers (one BASIC line can generate multiple IC10 lines).
    /// </summary>
    public Dictionary<int, List<int>> BasicToIc10 { get; set; } = new();
    /// <summary>
    /// Maps IC10 line number to BASIC line number.
    /// </summary>
    public Dictionary<int, int> Ic10ToBasic { get; set; } = new();
    public Dictionary<string, string> VariableRegisters { get; set; } = new();
    public Dictionary<string, string> AliasDevices { get; set; } = new();
}

/// <summary>
/// Error/warning information.
/// </summary>
public class ErrorInfo
{
    public int Line { get; set; }
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "error";
}

/// <summary>
/// Simulator state result for MCP integration.
/// </summary>
public class SimulatorStateResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int ProgramCounter { get; set; }
    public int InstructionCount { get; set; }
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool IsHalted { get; set; }
    public bool IsYielding { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, double> Registers { get; set; } = new();
    public double[] Stack { get; set; } = Array.Empty<double>();
    public List<SimulatorDeviceInfo> Devices { get; set; } = new();
    public List<int> Breakpoints { get; set; } = new();
}

/// <summary>
/// Simulated device information.
/// </summary>
public class SimulatorDeviceInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string? Alias { get; set; }
    public Dictionary<string, double> Properties { get; set; } = new();
}

/// <summary>
/// Information about an open tab.
/// </summary>
public class TabInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string? FilePath { get; set; }
    public bool IsModified { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Result of saving a script.
/// </summary>
public class SaveScriptResult
{
    public bool Success { get; set; }
    public string? ScriptName { get; set; }
    public string? FolderPath { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of loading a script.
/// </summary>
public class LoadScriptResult
{
    public bool Success { get; set; }
    public string? ScriptName { get; set; }
    public string? FilePath { get; set; }
    public string? CodePreview { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Information about a script in the scripts folder.
/// </summary>
public class ScriptInfo
{
    public string Name { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public bool HasBasFile { get; set; }
    public bool HasInstructionXml { get; set; }
}

/// <summary>
/// Snapshot of all user-accessible settings.
/// </summary>
public class SettingsSnapshot
{
    public string Theme { get; set; } = "Dark";
    public double FontSize { get; set; }
    public bool AutoCompile { get; set; }
    public bool AutoCompleteEnabled { get; set; }
    public bool WordWrap { get; set; }
    public int OptimizationLevel { get; set; }
    public bool AutoSaveEnabled { get; set; }
    public int AutoSaveIntervalSeconds { get; set; }
    public string SplitViewMode { get; set; } = "Vertical";
    public bool ApiServerEnabled { get; set; }
    public int ApiServerPort { get; set; }
    public string ScriptAuthor { get; set; } = "";
}

/// <summary>
/// Result of updating a setting.
/// </summary>
public class SettingsUpdateResult
{
    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// A reference to a symbol in the code.
/// </summary>
public class SymbolReference
{
    public int Line { get; set; }
    public int Column { get; set; }
    public int Length { get; set; }
    public string Kind { get; set; } = "";
}

/// <summary>
/// Code metrics for analysis.
/// </summary>
public class CodeMetrics
{
    public int TotalLines { get; set; }
    public int CodeLines { get; set; }
    public int CommentLines { get; set; }
    public int BlankLines { get; set; }
    public int Ic10Lines { get; set; }
    public bool CompilationSuccess { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public int VariableCount { get; set; }
    public int ConstantCount { get; set; }
    public int LabelCount { get; set; }
    public int FunctionCount { get; set; }
    public int AliasCount { get; set; }
}

/// <summary>
/// A problem (error/warning) item for the Problems Panel.
/// </summary>
public class ProblemItem
{
    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = "";
    public BasicToMips.Editor.ErrorHighlighting.ErrorChecker.ErrorSeverity Severity { get; set; }
    public string Icon { get; set; } = "";
    public System.Windows.Media.Brush? Color { get; set; }
    public string Location { get; set; } = "";
}
