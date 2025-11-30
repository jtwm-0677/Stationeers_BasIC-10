using System.Windows;
using System.Windows.Threading;
using BasicToMips.UI.Services;

namespace BasicToMips.UI;

public partial class DebugConsoleWindow : Window
{
    private readonly DispatcherTimer _updateTimer;

    public DebugConsoleWindow()
    {
        InitializeComponent();

        // Subscribe to new log entries
        DebugLogger.LogAdded += OnLogAdded;

        // Load existing logs
        foreach (var entry in DebugLogger.GetRecentLogs(500))
        {
            LogList.Items.Add(entry.ToString());
        }
        UpdateLogCount();

        // Setup periodic UI update timer (batches updates for performance)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Start();

        Closed += (s, e) =>
        {
            DebugLogger.LogAdded -= OnLogAdded;
            _updateTimer.Stop();
        };
    }

    private void OnLogAdded(object? sender, LogEntry entry)
    {
        Dispatcher.BeginInvoke(() =>
        {
            LogList.Items.Add(entry.ToString());

            // Keep list manageable
            while (LogList.Items.Count > 1000)
            {
                LogList.Items.RemoveAt(0);
            }

            UpdateLogCount();

            if (AutoScrollCheck.IsChecked == true && LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            }
        }, DispatcherPriority.Background);
    }

    private void UpdateLogCount()
    {
        LogCountText.Text = $"{LogList.Items.Count} entries";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        LogList.Items.Clear();
        DebugLogger.Clear();
        UpdateLogCount();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var text = string.Join("\n", LogList.Items.Cast<string>());
        Clipboard.SetText(text);
        DebugLogger.Log("DebugConsole", "Copied all logs to clipboard");
    }

    private void EnableLoggingCheck_Changed(object sender, RoutedEventArgs e)
    {
        DebugLogger.IsEnabled = EnableLoggingCheck.IsChecked == true;
    }
}
