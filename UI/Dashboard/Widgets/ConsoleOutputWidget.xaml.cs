using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class ConsoleOutputWidget : WidgetBase, ISimulationListener
{
    private readonly List<ConsoleMessage> _messages = new();
    private const int MaxLines = 1000;
    private bool _showTimestamps = true;
    private bool _verboseMode = false;
    private readonly SimulationBridge _bridge;

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    public ConsoleOutputWidget()
    {
        Title = "Console Output";
        InitializeComponent();

        _bridge = SimulationBridge.Instance;
        _bridge.RegisterListener(this);
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        UpdateDisplay();
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        Dispatcher.Invoke(() =>
        {
            _messages.Add(new ConsoleMessage
            {
                Text = message,
                Level = level,
                Timestamp = DateTime.Now
            });

            // Enforce max line limit
            while (_messages.Count > MaxLines)
            {
                _messages.RemoveAt(0);
            }

            UpdateDisplay();

            // Auto-scroll if enabled
            if (AutoScrollCheckBox?.IsChecked == true)
            {
                ConsoleScroller?.ScrollToEnd();
            }
        });
    }

    public void Clear()
    {
        Dispatcher.Invoke(() =>
        {
            _messages.Clear();
            UpdateDisplay();
        });
    }

    private void UpdateDisplay()
    {
        if (ConsoleTextBlock == null || EmptyMessage == null)
            return;

        if (_messages.Count == 0)
        {
            ConsoleTextBlock.Inlines.Clear();
            EmptyMessage.Visibility = Visibility.Visible;
            return;
        }

        EmptyMessage.Visibility = Visibility.Collapsed;
        ConsoleTextBlock.Inlines.Clear();

        foreach (var msg in _messages)
        {
            var line = new Run();

            if (_showTimestamps)
            {
                line.Text = $"[{msg.Timestamp:HH:mm:ss}] {msg.Text}\n";
            }
            else
            {
                line.Text = $"{msg.Text}\n";
            }

            line.Foreground = msg.Level switch
            {
                LogLevel.Info => Brushes.White,
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(0xCC, 0xC9, 0x00)),
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C)),
                LogLevel.Success => new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)),
                _ => Brushes.White
            };

            ConsoleTextBlock.Inlines.Add(line);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        Clear();
    }

    private void CopyAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (_messages.Count == 0)
            return;

        var allText = string.Join("\n", _messages.Select(m =>
        {
            if (_showTimestamps)
                return $"[{m.Timestamp:HH:mm:ss}] {m.Text}";
            else
                return m.Text;
        }));

        try
        {
            Clipboard.SetText(allText);
            Log("Copied to clipboard", LogLevel.Success);
        }
        catch (Exception ex)
        {
            Log($"Failed to copy: {ex.Message}", LogLevel.Error);
        }
    }

    private void TimestampCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _showTimestamps = TimestampCheckBox?.IsChecked == true;
        UpdateDisplay();
    }

    public override object SaveState()
    {
        return new
        {
            ShowTimestamps = _showTimestamps,
            AutoScroll = AutoScrollCheckBox?.IsChecked ?? true,
            Messages = _messages.TakeLast(100).ToList() // Save last 100 messages
        };
    }

    public override void LoadState(object state)
    {
        try
        {
            var dict = state as Dictionary<string, object>;
            if (dict != null)
            {
                if (dict.TryGetValue("ShowTimestamps", out var showTimestamps))
                {
                    _showTimestamps = Convert.ToBoolean(showTimestamps);
                    if (TimestampCheckBox != null)
                        TimestampCheckBox.IsChecked = _showTimestamps;
                }

                if (dict.TryGetValue("AutoScroll", out var autoScroll))
                {
                    if (AutoScrollCheckBox != null)
                        AutoScrollCheckBox.IsChecked = Convert.ToBoolean(autoScroll);
                }

                // Note: Messages not restored from state to avoid clutter
            }
        }
        catch
        {
            // Ignore state load errors
        }
    }

    // ISimulationListener implementation
    public void OnSimulationStarted()
    {
        Log("Simulation started", LogLevel.Success);
    }

    public void OnSimulationStopped()
    {
        var state = _bridge.GetState();
        if (state.IsHalted && state.ErrorMessage != null)
        {
            Log($"Simulation stopped with error: {state.ErrorMessage}", LogLevel.Error);
        }
        else if (state.IsHalted)
        {
            Log("Simulation halted", LogLevel.Info);
        }
        else
        {
            Log("Simulation stopped", LogLevel.Info);
        }
    }

    public void OnSimulationStep(int lineNumber)
    {
        // Only log steps in verbose mode to avoid flooding
        if (_verboseMode)
        {
            Log($"Step: Line {lineNumber}", LogLevel.Info);
        }
    }

    public void OnValueChanged(string name, double oldValue, double newValue)
    {
        // Log value changes in verbose mode
        if (_verboseMode)
        {
            Log($"Value changed: {name} = {newValue:F3} (was {oldValue:F3})", LogLevel.Info);
        }
    }

    /// <summary>
    /// Enable or disable verbose logging
    /// </summary>
    public void SetVerboseMode(bool enabled)
    {
        _verboseMode = enabled;
        if (enabled)
        {
            Log("Verbose mode enabled", LogLevel.Info);
        }
        else
        {
            Log("Verbose mode disabled", LogLevel.Info);
        }
    }

    private class ConsoleMessage
    {
        public string Text { get; set; } = "";
        public LogLevel Level { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
