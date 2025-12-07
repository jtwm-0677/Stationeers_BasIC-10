using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicToMips.Simulator;

namespace BasicToMips.UI.Dashboard;

/// <summary>
/// Singleton bridge connecting dashboard widgets to the IC10 simulator
/// </summary>
public class SimulationBridge
{
    private static SimulationBridge? _instance;
    private static readonly object _lock = new();

    public static SimulationBridge Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new SimulationBridge();
                }
            }
            return _instance;
        }
    }

    private IC10Simulator? _simulator;
    private readonly List<ISimulationListener> _listeners = new();
    private readonly Dictionary<string, double> _lastRegisterValues = new();
    private CancellationTokenSource? _runCancellation;
    private bool _isRunning;
    private int _stepDelayMs = 100; // Default speed

    // Events
    public event EventHandler? SimulationStarted;
    public event EventHandler? SimulationStopped;
    public event EventHandler<int>? SimulationStep;
    public event EventHandler<ValueChangedEventArgs>? ValueChanged;

    private SimulationBridge()
    {
    }

    /// <summary>
    /// Attach to a simulator instance
    /// </summary>
    public void AttachSimulator(IC10Simulator simulator)
    {
        if (_simulator != null)
        {
            _simulator.StateChanged -= OnSimulatorStateChanged;
        }

        _simulator = simulator;
        _simulator.StateChanged += OnSimulatorStateChanged;

        // Initialize last values
        _lastRegisterValues.Clear();
        for (int i = 0; i < IC10Simulator.NumericRegisterCount; i++)
        {
            _lastRegisterValues[$"r{i}"] = 0;
        }
        _lastRegisterValues["sp"] = 0;
        _lastRegisterValues["ra"] = 0;
    }

    /// <summary>
    /// Register a widget to receive simulation events
    /// </summary>
    public void RegisterListener(ISimulationListener listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    /// <summary>
    /// Unregister a widget from receiving simulation events
    /// </summary>
    public void UnregisterListener(ISimulationListener listener)
    {
        _listeners.Remove(listener);
    }

    /// <summary>
    /// Start continuous simulation
    /// </summary>
    public void Start()
    {
        if (_simulator == null || _isRunning) return;

        _isRunning = true;
        _runCancellation = new CancellationTokenSource();

        SimulationStarted?.Invoke(this, EventArgs.Empty);
        NotifyListeners(l => l.OnSimulationStarted());

        Task.Run(async () =>
        {
            while (_isRunning && !_runCancellation.Token.IsCancellationRequested)
            {
                if (_simulator.IsHalted || _simulator.IsPaused)
                {
                    Stop();
                    break;
                }

                var success = Step();
                if (!success)
                {
                    Stop();
                    break;
                }

                try
                {
                    await Task.Delay(_stepDelayMs, _runCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, _runCancellation.Token);
    }

    /// <summary>
    /// Stop simulation
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _runCancellation?.Cancel();
        _runCancellation?.Dispose();
        _runCancellation = null;

        SimulationStopped?.Invoke(this, EventArgs.Empty);
        NotifyListeners(l => l.OnSimulationStopped());
    }

    /// <summary>
    /// Execute a single simulation step
    /// </summary>
    public bool Step()
    {
        if (_simulator == null) return false;

        var lineNumber = _simulator.ProgramCounter;
        var success = _simulator.Step();

        if (success)
        {
            SimulationStep?.Invoke(this, lineNumber);
            NotifyListeners(l => l.OnSimulationStep(lineNumber));

            // Check for register changes
            CheckForValueChanges();
        }

        return success;
    }

    /// <summary>
    /// Reset simulation to initial state
    /// </summary>
    public void Reset()
    {
        Stop();
        _simulator?.Reset();

        _lastRegisterValues.Clear();
        for (int i = 0; i < IC10Simulator.NumericRegisterCount; i++)
        {
            _lastRegisterValues[$"r{i}"] = 0;
        }
        _lastRegisterValues["sp"] = 0;
        _lastRegisterValues["ra"] = 0;

        SimulationStopped?.Invoke(this, EventArgs.Empty);
        NotifyListeners(l => l.OnSimulationStopped());
    }

    /// <summary>
    /// Set simulation speed
    /// </summary>
    /// <param name="delayMs">Delay between steps in milliseconds</param>
    public void SetSpeed(int delayMs)
    {
        _stepDelayMs = Math.Max(0, Math.Min(delayMs, 2000));
    }

    /// <summary>
    /// Get current register value
    /// </summary>
    public double GetRegisterValue(string registerName)
    {
        if (_simulator == null) return 0;

        if (registerName.StartsWith("r") && int.TryParse(registerName.Substring(1), out int regNum))
        {
            if (regNum >= 0 && regNum < IC10Simulator.NumericRegisterCount)
            {
                return _simulator.Registers[regNum];
            }
        }
        else if (registerName == "sp")
        {
            return _simulator.StackPointer;
        }
        else if (registerName == "ra")
        {
            return _simulator.Registers[17];
        }

        return 0;
    }

    /// <summary>
    /// Set register value (for debugging/editing)
    /// </summary>
    public void SetRegisterValue(string registerName, double value)
    {
        if (_simulator == null) return;

        if (registerName.StartsWith("r") && int.TryParse(registerName.Substring(1), out int regNum))
        {
            if (regNum >= 0 && regNum < IC10Simulator.NumericRegisterCount)
            {
                var oldValue = _simulator.Registers[regNum];
                _simulator.Registers[regNum] = value;
                NotifyValueChange(registerName, oldValue, value);
            }
        }
        else if (registerName == "sp")
        {
            // Stack pointer is read-only in this context
        }
        else if (registerName == "ra")
        {
            var oldValue = _simulator.Registers[17];
            _simulator.Registers[17] = value;
            NotifyValueChange(registerName, oldValue, value);
        }
    }

    /// <summary>
    /// Get variable value (mapped to register)
    /// </summary>
    public double GetVariableValue(string variableName)
    {
        // Variables would need to be mapped to registers
        // This is a placeholder for future variable tracking
        return 0;
    }

    /// <summary>
    /// Set variable value (mapped to register)
    /// </summary>
    public void SetVariableValue(string variableName, double value)
    {
        // Variables would need to be mapped to registers
        // This is a placeholder for future variable tracking
    }

    /// <summary>
    /// Get device property value
    /// </summary>
    public double GetDeviceProperty(int deviceIndex, string propertyName)
    {
        if (_simulator == null || deviceIndex < 0 || deviceIndex >= IC10Simulator.DeviceCount)
            return 0;

        return _simulator.Devices[deviceIndex].GetProperty(propertyName);
    }

    /// <summary>
    /// Get device property value by name or alias
    /// </summary>
    public double GetDeviceProperty(string deviceName, string propertyName)
    {
        if (_simulator == null) return 0;

        // Try to parse as device index (d0-d5)
        if (deviceName.StartsWith("d") && int.TryParse(deviceName.Substring(1), out int index))
        {
            return GetDeviceProperty(index, propertyName);
        }

        // Try to find by alias
        for (int i = 0; i < IC10Simulator.DeviceCount; i++)
        {
            if (_simulator.Devices[i].Alias?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return GetDeviceProperty(i, propertyName);
            }
        }

        return 0;
    }

    /// <summary>
    /// Get all device properties for a device
    /// </summary>
    public Dictionary<string, double> GetAllDeviceProperties(string deviceName)
    {
        if (_simulator == null) return new Dictionary<string, double>();

        // Try to parse as device index (d0-d5)
        int deviceIndex = -1;
        if (deviceName.StartsWith("d") && int.TryParse(deviceName.Substring(1), out int index))
        {
            deviceIndex = index;
        }
        else
        {
            // Try to find by alias
            for (int i = 0; i < IC10Simulator.DeviceCount; i++)
            {
                if (_simulator.Devices[i].Alias?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    deviceIndex = i;
                    break;
                }
            }
        }

        if (deviceIndex >= 0 && deviceIndex < IC10Simulator.DeviceCount)
        {
            return new Dictionary<string, double>(_simulator.Devices[deviceIndex].Properties);
        }

        return new Dictionary<string, double>();
    }

    /// <summary>
    /// Get current simulator state
    /// </summary>
    public SimulatorState GetState()
    {
        if (_simulator == null)
        {
            return new SimulatorState
            {
                IsRunning = false,
                IsPaused = false,
                IsHalted = false,
                ProgramCounter = 0,
                InstructionCount = 0,
                ErrorMessage = null
            };
        }

        return new SimulatorState
        {
            IsRunning = _isRunning,
            IsPaused = _simulator.IsPaused,
            IsHalted = _simulator.IsHalted,
            ProgramCounter = _simulator.ProgramCounter,
            InstructionCount = _simulator.InstructionCount,
            ErrorMessage = _simulator.ErrorMessage,
            BreakpointCount = _simulator.Breakpoints.Count
        };
    }

    private void OnSimulatorStateChanged(object? sender, EventArgs e)
    {
        // Simulator state changed externally
        CheckForValueChanges();
    }

    private void CheckForValueChanges()
    {
        if (_simulator == null) return;

        // Check all registers for changes
        for (int i = 0; i < IC10Simulator.NumericRegisterCount; i++)
        {
            var regName = $"r{i}";
            var currentValue = _simulator.Registers[i];
            if (_lastRegisterValues.TryGetValue(regName, out var lastValue))
            {
                if (Math.Abs(currentValue - lastValue) > 0.0001)
                {
                    NotifyValueChange(regName, lastValue, currentValue);
                    _lastRegisterValues[regName] = currentValue;
                }
            }
            else
            {
                _lastRegisterValues[regName] = currentValue;
            }
        }

        // Check sp
        var spValue = (double)_simulator.StackPointer;
        if (_lastRegisterValues.TryGetValue("sp", out var lastSp) && Math.Abs(spValue - lastSp) > 0.0001)
        {
            NotifyValueChange("sp", lastSp, spValue);
            _lastRegisterValues["sp"] = spValue;
        }

        // Check ra
        var raValue = _simulator.Registers[17];
        if (_lastRegisterValues.TryGetValue("ra", out var lastRa) && Math.Abs(raValue - lastRa) > 0.0001)
        {
            NotifyValueChange("ra", lastRa, raValue);
            _lastRegisterValues["ra"] = raValue;
        }
    }

    private void NotifyValueChange(string name, double oldValue, double newValue)
    {
        var args = new ValueChangedEventArgs(name, oldValue, newValue);
        ValueChanged?.Invoke(this, args);
        NotifyListeners(l => l.OnValueChanged(name, oldValue, newValue));
    }

    private void NotifyListeners(Action<ISimulationListener> action)
    {
        // Create a copy to avoid modification during iteration
        var listeners = _listeners.ToList();
        foreach (var listener in listeners)
        {
            try
            {
                action(listener);
            }
            catch
            {
                // Ignore errors in listeners
            }
        }
    }
}

/// <summary>
/// Event args for value changes
/// </summary>
public class ValueChangedEventArgs : EventArgs
{
    public string Name { get; }
    public double OldValue { get; }
    public double NewValue { get; }

    public ValueChangedEventArgs(string name, double oldValue, double newValue)
    {
        Name = name;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

/// <summary>
/// Current simulator state
/// </summary>
public class SimulatorState
{
    public bool IsRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool IsHalted { get; set; }
    public int ProgramCounter { get; set; }
    public int InstructionCount { get; set; }
    public string? ErrorMessage { get; set; }
    public int BreakpointCount { get; set; }
}
