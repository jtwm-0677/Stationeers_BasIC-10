using System.Collections.Generic;
using System.Linq;

namespace BasicToMips.Editor.Debugging;

/// <summary>
/// Manages breakpoints for the debugger.
/// </summary>
public class BreakpointManager
{
    private readonly HashSet<int> _breakpoints = new();
    private readonly Dictionary<int, string> _conditions = new();

    /// <summary>
    /// Event raised when breakpoints change.
    /// </summary>
    public event EventHandler? BreakpointsChanged;

    /// <summary>
    /// Get all breakpoint line numbers (1-based).
    /// </summary>
    public IReadOnlyCollection<int> Breakpoints => _breakpoints;

    /// <summary>
    /// Check if a line has a breakpoint.
    /// </summary>
    public bool HasBreakpoint(int line) => _breakpoints.Contains(line);

    /// <summary>
    /// Toggle breakpoint on a line.
    /// </summary>
    public bool ToggleBreakpoint(int line)
    {
        if (_breakpoints.Contains(line))
        {
            _breakpoints.Remove(line);
            _conditions.Remove(line);
            OnBreakpointsChanged();
            return false;
        }
        else
        {
            _breakpoints.Add(line);
            OnBreakpointsChanged();
            return true;
        }
    }

    /// <summary>
    /// Set a breakpoint on a line.
    /// </summary>
    public void SetBreakpoint(int line, string? condition = null)
    {
        _breakpoints.Add(line);
        if (!string.IsNullOrEmpty(condition))
        {
            _conditions[line] = condition;
        }
        OnBreakpointsChanged();
    }

    /// <summary>
    /// Remove a breakpoint from a line.
    /// </summary>
    public void RemoveBreakpoint(int line)
    {
        _breakpoints.Remove(line);
        _conditions.Remove(line);
        OnBreakpointsChanged();
    }

    /// <summary>
    /// Clear all breakpoints.
    /// </summary>
    public void ClearAll()
    {
        _breakpoints.Clear();
        _conditions.Clear();
        OnBreakpointsChanged();
    }

    /// <summary>
    /// Get the condition for a breakpoint (if any).
    /// </summary>
    public string? GetCondition(int line)
    {
        return _conditions.TryGetValue(line, out var condition) ? condition : null;
    }

    /// <summary>
    /// Set a condition for a breakpoint.
    /// </summary>
    public void SetCondition(int line, string condition)
    {
        if (_breakpoints.Contains(line))
        {
            _conditions[line] = condition;
            OnBreakpointsChanged();
        }
    }

    /// <summary>
    /// Check if execution should break at this line.
    /// </summary>
    public bool ShouldBreak(int line)
    {
        return _breakpoints.Contains(line);
    }

    /// <summary>
    /// Adjust breakpoint positions after text changes.
    /// </summary>
    public void AdjustForLineChange(int changedLine, int delta)
    {
        if (delta == 0) return;

        var toRemove = _breakpoints.Where(bp => bp >= changedLine).ToList();
        var conditions = toRemove.Where(bp => _conditions.ContainsKey(bp))
            .ToDictionary(bp => bp, bp => _conditions[bp]);

        foreach (var bp in toRemove)
        {
            _breakpoints.Remove(bp);
            _conditions.Remove(bp);
        }

        foreach (var bp in toRemove)
        {
            var newLine = bp + delta;
            if (newLine > 0)
            {
                _breakpoints.Add(newLine);
                if (conditions.TryGetValue(bp, out var cond))
                {
                    _conditions[newLine] = cond;
                }
            }
        }

        OnBreakpointsChanged();
    }

    private void OnBreakpointsChanged()
    {
        BreakpointsChanged?.Invoke(this, EventArgs.Empty);
    }
}
