using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class RegisterViewWidget : WidgetBase, ISimulationListener
{
    private readonly ObservableCollection<RegisterItem> _registers = new();
    private readonly Dictionary<string, double> _lastValues = new();
    private bool _useHexFormat = false;
    private readonly SimulationBridge _bridge;
    private int _currentInstructionLine = -1;

    private static readonly string[] RegisterNames = new[]
    {
        "r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7",
        "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
        "sp", "ra"
    };

    public RegisterViewWidget()
    {
        Title = "Register View";
        InitializeComponent();
        InitializeRegisters();

        _bridge = SimulationBridge.Instance;
        _bridge.RegisterListener(this);
    }

    private void InitializeRegisters()
    {
        foreach (var name in RegisterNames)
        {
            _registers.Add(new RegisterItem
            {
                Name = name,
                Value = 0,
                IsChanged = false
            });
            _lastValues[name] = 0;
        }
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        RegisterList.ItemsSource = _registers;
        UpdateFormat();
    }

    public void UpdateRegisters(Dictionary<string, double> registerValues)
    {
        Dispatcher.Invoke(() =>
        {
            if (registerValues == null || registerValues.Count == 0)
            {
                PlaceholderText.Visibility = Visibility.Visible;
                return;
            }

            PlaceholderText.Visibility = Visibility.Collapsed;

            foreach (var kvp in registerValues)
            {
                var register = _registers.FirstOrDefault(r => r.Name == kvp.Key);
                if (register != null)
                {
                    // Check if value changed
                    if (_lastValues.TryGetValue(kvp.Key, out var lastValue))
                    {
                        register.IsChanged = Math.Abs(lastValue - kvp.Value) > 0.0001;
                    }

                    register.Value = kvp.Value;
                    _lastValues[kvp.Key] = kvp.Value;

                    // Reset IsChanged after brief delay
                    if (register.IsChanged)
                    {
                        System.Threading.Tasks.Task.Delay(800).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() => register.IsChanged = false);
                        });
                    }
                }
            }

            UpdateFormat();
        });
    }

    private void FormatChanged(object sender, RoutedEventArgs e)
    {
        _useHexFormat = HexRadio?.IsChecked == true;
        UpdateFormat();
    }

    private void UpdateFormat()
    {
        foreach (var register in _registers)
        {
            if (_useHexFormat)
            {
                // Convert to hex (treating double as int for display)
                var intValue = (long)register.Value;
                register.FormattedValue = $"0x{intValue:X8}";
            }
            else
            {
                // Show as decimal
                if (register.Value == Math.Floor(register.Value))
                {
                    register.FormattedValue = register.Value.ToString("F0");
                }
                else
                {
                    register.FormattedValue = register.Value.ToString("F3");
                }
            }
        }
    }

    public override object SaveState()
    {
        return new
        {
            UseHex = _useHexFormat,
            Values = _lastValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    public override void LoadState(object state)
    {
        try
        {
            var dict = state as Dictionary<string, object>;
            if (dict != null)
            {
                if (dict.TryGetValue("UseHex", out var useHex))
                {
                    _useHexFormat = Convert.ToBoolean(useHex);
                    if (HexRadio != null && DecimalRadio != null)
                    {
                        HexRadio.IsChecked = _useHexFormat;
                        DecimalRadio.IsChecked = !_useHexFormat;
                    }
                }

                // Don't restore values - simulation state should be fresh
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
        Dispatcher.Invoke(() =>
        {
            PlaceholderText.Visibility = Visibility.Collapsed;
        });
    }

    public void OnSimulationStopped()
    {
        Dispatcher.Invoke(() =>
        {
            _currentInstructionLine = -1;
        });
    }

    public void OnSimulationStep(int lineNumber)
    {
        Dispatcher.Invoke(() =>
        {
            _currentInstructionLine = lineNumber;

            // Update all registers from simulation
            var registerValues = new Dictionary<string, double>();
            for (int i = 0; i < 16; i++)
            {
                registerValues[$"r{i}"] = _bridge.GetRegisterValue($"r{i}");
            }
            registerValues["sp"] = _bridge.GetRegisterValue("sp");
            registerValues["ra"] = _bridge.GetRegisterValue("ra");

            UpdateRegisters(registerValues);
        });
    }

    public void OnValueChanged(string name, double oldValue, double newValue)
    {
        Dispatcher.Invoke(() =>
        {
            var register = _registers.FirstOrDefault(r => r.Name == name);
            if (register != null)
            {
                register.IsChanged = true;
                register.Value = newValue;
                _lastValues[name] = newValue;
                UpdateFormat();

                // Reset IsChanged after brief delay
                System.Threading.Tasks.Task.Delay(800).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => register.IsChanged = false);
                });
            }
        });
    }
}

public class RegisterItem : INotifyPropertyChanged
{
    private string _name = "";
    private double _value;
    private string _formattedValue = "0";
    private bool _isChanged;

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

    public double Value
    {
        get => _value;
        set
        {
            if (Math.Abs(_value - value) > 0.0001)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public string FormattedValue
    {
        get => _formattedValue;
        set
        {
            if (_formattedValue != value)
            {
                _formattedValue = value;
                OnPropertyChanged(nameof(FormattedValue));
            }
        }
    }

    public bool IsChanged
    {
        get => _isChanged;
        set
        {
            if (_isChanged != value)
            {
                _isChanged = value;
                OnPropertyChanged(nameof(IsChanged));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
