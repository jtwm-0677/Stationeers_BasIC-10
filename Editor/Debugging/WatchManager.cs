using System.Collections.ObjectModel;
using BasicToMips.Simulator;
using BasicToMips.Shared;

namespace BasicToMips.Editor.Debugging;

/// <summary>
/// Represents an item being watched in the debugger.
/// </summary>
public class WatchItem : System.ComponentModel.INotifyPropertyChanged
{
    private string _name = "";
    private string _value = "";
    private WatchItemType _type;
    private bool _hasChanged;

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                HasChanged = _value != "" && _value != value;
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public WatchItemType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
    }

    /// <summary>
    /// Indicates if the value changed since last update.
    /// </summary>
    public bool HasChanged
    {
        get => _hasChanged;
        set
        {
            if (_hasChanged != value)
            {
                _hasChanged = value;
                OnPropertyChanged(nameof(HasChanged));
            }
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

public enum WatchItemType
{
    Register,       // r0-r15, sp, ra
    Variable,       // BASIC variable (mapped to register)
    Device,         // Device property (e.g., d0.Temperature)
    StackValue,     // Stack[n]
    Expression      // Computed expression
}

/// <summary>
/// Manages the watch list for debugging.
/// </summary>
public class WatchManager
{
    private readonly ObservableCollection<WatchItem> _watchItems = new();

    /// <summary>
    /// The collection of watched items.
    /// </summary>
    public ObservableCollection<WatchItem> WatchItems => _watchItems;

    /// <summary>
    /// Event raised when any watch value changes.
    /// </summary>
    public event EventHandler? WatchValuesChanged;

    /// <summary>
    /// Add a watch expression.
    /// </summary>
    public WatchItem AddWatch(string expression)
    {
        var item = new WatchItem
        {
            Name = expression,
            Type = DetermineType(expression),
            Value = "?"
        };
        _watchItems.Add(item);
        return item;
    }

    /// <summary>
    /// Remove a watch item.
    /// </summary>
    public void RemoveWatch(WatchItem item)
    {
        _watchItems.Remove(item);
    }

    /// <summary>
    /// Clear all watch items.
    /// </summary>
    public void ClearAll()
    {
        _watchItems.Clear();
    }

    /// <summary>
    /// Current source map for resolving BASIC variable names.
    /// </summary>
    private SourceMap? _sourceMap;

    /// <summary>
    /// Set the source map for resolving BASIC variable names to registers.
    /// </summary>
    public void SetSourceMap(SourceMap? sourceMap)
    {
        _sourceMap = sourceMap;
    }

    /// <summary>
    /// Update all watch values from the simulator state.
    /// </summary>
    public void UpdateValues(IC10Simulator simulator)
    {
        foreach (var item in _watchItems)
        {
            item.Value = EvaluateExpression(item.Name, simulator);
        }
        WatchValuesChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clear the HasChanged flag on all items.
    /// </summary>
    public void ClearChangedFlags()
    {
        foreach (var item in _watchItems)
        {
            item.HasChanged = false;
        }
    }

    private WatchItemType DetermineType(string expression)
    {
        expression = expression.Trim().ToLowerInvariant();

        // Register (r0-r15, sp, ra)
        if (expression == "sp" || expression == "ra" ||
            (expression.StartsWith("r") && int.TryParse(expression.Substring(1), out int regNum) && regNum >= 0 && regNum <= 15))
        {
            return WatchItemType.Register;
        }

        // Device property (d0.Property or device.Property)
        if (expression.Contains('.'))
        {
            var parts = expression.Split('.');
            if (parts[0].StartsWith("d") && int.TryParse(parts[0].Substring(1), out _))
            {
                return WatchItemType.Device;
            }
        }

        // Stack value (stack[n])
        if (expression.StartsWith("stack[") && expression.EndsWith("]"))
        {
            return WatchItemType.StackValue;
        }

        // Assume it's a variable name (BASIC variable mapped to register)
        return WatchItemType.Variable;
    }

    private string EvaluateExpression(string expression, IC10Simulator simulator)
    {
        try
        {
            expression = expression.Trim();
            var lowerExpr = expression.ToLowerInvariant();

            // First, check if this is a BASIC variable name from source map
            if (_sourceMap != null)
            {
                // Check BASIC variable -> register mapping
                if (_sourceMap.VariableRegisters.TryGetValue(expression, out var register))
                {
                    // Resolve the register value
                    if (register.StartsWith("r") && int.TryParse(register.Substring(1), out int varRegNum) && varRegNum >= 0 && varRegNum < 16)
                    {
                        return simulator.Registers[varRegNum].ToString("F2");
                    }
                }

                // Check BASIC alias -> device property (e.g., "sensor.Temperature")
                if (expression.Contains('.'))
                {
                    var parts = expression.Split('.', 2);
                    if (_sourceMap.AliasDevices.TryGetValue(parts[0], out var device))
                    {
                        // Resolve device property
                        if (device.StartsWith("d") && int.TryParse(device.Substring(1), out int devIdx) && devIdx >= 0 && devIdx < IC10Simulator.DeviceCount)
                        {
                            var value = simulator.Devices[devIdx].GetProperty(parts[1]);
                            return value.ToString("F2");
                        }
                    }
                }

                // Check BASIC alias alone (returns device name)
                if (_sourceMap.AliasDevices.TryGetValue(expression, out var aliasDevice))
                {
                    return aliasDevice;
                }
            }

            // Register (r0-r15, sp, ra)
            if (lowerExpr == "sp")
            {
                return simulator.StackPointer.ToString();
            }
            if (lowerExpr == "ra")
            {
                return simulator.Registers[17].ToString("F2");
            }
            if (lowerExpr.StartsWith("r") && int.TryParse(lowerExpr.Substring(1), out int regNum) && regNum >= 0 && regNum < 16)
            {
                return simulator.Registers[regNum].ToString("F2");
            }

            // Device property (d0.Property)
            if (expression.Contains('.'))
            {
                var parts = expression.Split('.', 2);
                if (parts[0].ToLowerInvariant().StartsWith("d") &&
                    int.TryParse(parts[0].Substring(1), out int devIndex) &&
                    devIndex >= 0 && devIndex < IC10Simulator.DeviceCount)
                {
                    var value = simulator.Devices[devIndex].GetProperty(parts[1]);
                    return value.ToString("F2");
                }
            }

            // Stack value (stack[n] or stack:n)
            if (lowerExpr.StartsWith("stack[") && lowerExpr.EndsWith("]"))
            {
                var indexStr = lowerExpr.Substring(6, lowerExpr.Length - 7);
                if (int.TryParse(indexStr, out int stackIndex) && stackIndex >= 0 && stackIndex < simulator.StackPointer)
                {
                    return simulator.Stack[stackIndex].ToString("F2");
                }
                return "(empty)";
            }

            // Try to find as a variable/alias name in the simulator
            // Check if it matches a device alias
            for (int i = 0; i < IC10Simulator.DeviceCount; i++)
            {
                if (simulator.Devices[i].Alias?.Equals(expression, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return $"d{i}";
                }
            }

            return "?";
        }
        catch
        {
            return "Error";
        }
    }

    /// <summary>
    /// Get a list of suggested watch expressions based on program.
    /// </summary>
    public static List<string> GetSuggestedWatches()
    {
        return new List<string>
        {
            "r0", "r1", "r2", "r3", "r4", "r5",
            "sp", "ra",
            "d0.Temperature", "d0.Pressure", "d0.On",
            "d1.Temperature", "d1.Pressure", "d1.On",
            "stack[0]"
        };
    }
}
