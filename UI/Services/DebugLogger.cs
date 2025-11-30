using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace BasicToMips.UI.Services;

/// <summary>
/// Debug logger for diagnosing performance issues.
/// Logs to both a file and an in-memory buffer for display.
/// </summary>
public static class DebugLogger
{
    private static readonly ConcurrentQueue<LogEntry> _logBuffer = new();
    private static readonly object _fileLock = new();
    private static string? _logFilePath;
    private static bool _isEnabled = true;
    private static readonly Stopwatch _appStopwatch = Stopwatch.StartNew();
    private const int MaxBufferSize = 500;

    public static event EventHandler<LogEntry>? LogAdded;

    public static bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public static void Initialize()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logDir = Path.Combine(appData, "BasicToMips", "Logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        Log("DebugLogger", "Initialized");
        Log("DebugLogger", $"Log file: {_logFilePath}");
    }

    public static void Log(string source, string message)
    {
        if (!_isEnabled) return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            ElapsedMs = _appStopwatch.ElapsedMilliseconds,
            ThreadId = Environment.CurrentManagedThreadId,
            Source = source,
            Message = message
        };

        // Add to buffer
        _logBuffer.Enqueue(entry);
        while (_logBuffer.Count > MaxBufferSize)
        {
            _logBuffer.TryDequeue(out _);
        }

        // Write to file (non-blocking)
        Task.Run(() => WriteToFile(entry));

        // Notify listeners
        LogAdded?.Invoke(null, entry);
    }

    public static void LogTiming(string source, string operation, Action action)
    {
        if (!_isEnabled)
        {
            action();
            return;
        }

        var sw = Stopwatch.StartNew();
        Log(source, $"START: {operation}");
        try
        {
            action();
        }
        finally
        {
            sw.Stop();
            Log(source, $"END: {operation} ({sw.ElapsedMilliseconds}ms)");
        }
    }

    public static async Task LogTimingAsync(string source, string operation, Func<Task> action)
    {
        if (!_isEnabled)
        {
            await action();
            return;
        }

        var sw = Stopwatch.StartNew();
        Log(source, $"START: {operation}");
        try
        {
            await action();
        }
        finally
        {
            sw.Stop();
            Log(source, $"END: {operation} ({sw.ElapsedMilliseconds}ms)");
        }
    }

    public static T LogTiming<T>(string source, string operation, Func<T> func)
    {
        if (!_isEnabled)
        {
            return func();
        }

        var sw = Stopwatch.StartNew();
        Log(source, $"START: {operation}");
        try
        {
            return func();
        }
        finally
        {
            sw.Stop();
            Log(source, $"END: {operation} ({sw.ElapsedMilliseconds}ms)");
        }
    }

    public static IEnumerable<LogEntry> GetRecentLogs(int count = 100)
    {
        return _logBuffer.TakeLast(count);
    }

    public static void Clear()
    {
        while (_logBuffer.TryDequeue(out _)) { }
    }

    private static void WriteToFile(LogEntry entry)
    {
        if (string.IsNullOrEmpty(_logFilePath)) return;

        try
        {
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath,
                    $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.ElapsedMs,8}ms] [T{entry.ThreadId,2}] [{entry.Source,-20}] {entry.Message}\n");
            }
        }
        catch
        {
            // Ignore file write errors
        }
    }
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public long ElapsedMs { get; set; }
    public int ThreadId { get; set; }
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss.fff}] [T{ThreadId}] [{Source}] {Message}";
    }
}
