using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class DeviceMonitorWidget : WidgetBase, ISimulationListener
{
    private readonly ObservableCollection<MonitoredDevice> _devices = new();
    private readonly SimulationBridge _bridge;
    private readonly Dictionary<string, Dictionary<string, double>> _lastDeviceValues = new();

    // Common device properties to monitor
    private static readonly string[] DefaultProperties = new[]
    {
        "On", "Setting", "Temperature", "Pressure"
    };

    public DeviceMonitorWidget()
    {
        Title = "Device Monitor";
        InitializeComponent();

        _bridge = SimulationBridge.Instance;
        _bridge.RegisterListener(this);
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        DeviceList.ItemsSource = _devices;
        _devices.CollectionChanged += (s, e) => UpdateEmptyState();
        UpdateEmptyState();
    }

    public void AddDevice(string nameOrPin, string deviceType = "Unknown")
    {
        Dispatcher.Invoke(() =>
        {
            // Don't add duplicates
            if (_devices.Any(d => d.Name == nameOrPin))
                return;

            var device = new MonitoredDevice
            {
                Name = nameOrPin,
                DeviceType = deviceType,
                TypeColor = GetDeviceColor(deviceType)
            };

            // Add default properties
            foreach (var prop in DefaultProperties)
            {
                device.Properties.Add(new DeviceProperty
                {
                    Name = prop,
                    Value = "-"
                });
            }

            _devices.Add(device);
            UpdateEmptyState();
        });
    }

    public void UpdateDeviceProperty(string nameOrPin, string propertyName, string value)
    {
        Dispatcher.Invoke(() =>
        {
            var device = _devices.FirstOrDefault(d => d.Name == nameOrPin);
            if (device != null)
            {
                var property = device.Properties.FirstOrDefault(p => p.Name == propertyName);
                if (property != null)
                {
                    property.Value = value;
                }
                else
                {
                    // Add new property if not exists
                    device.Properties.Add(new DeviceProperty
                    {
                        Name = propertyName,
                        Value = value
                    });
                }
            }
        });
    }

    public void RemoveDevice(string nameOrPin)
    {
        Dispatcher.Invoke(() =>
        {
            var device = _devices.FirstOrDefault(d => d.Name == nameOrPin);
            if (device != null)
            {
                _devices.Remove(device);
            }
        });
    }

    private Brush GetDeviceColor(string deviceType)
    {
        // Color-code by device type
        return deviceType.ToLowerInvariant() switch
        {
            "pump" => new SolidColorBrush(Color.FromRgb(0x4A, 0x9E, 0xFF)), // Blue
            "furnace" => new SolidColorBrush(Color.FromRgb(0xFF, 0x8C, 0x00)), // Orange
            "sensor" => new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)), // Green
            "valve" => new SolidColorBrush(Color.FromRgb(0x9C, 0x76, 0xD9)), // Purple
            "logic" => new SolidColorBrush(Color.FromRgb(0xCC, 0xC9, 0x00)), // Yellow
            _ => new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85)) // Gray
        };
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        AddDeviceFromInput();
    }

    private void AddDeviceTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddDeviceFromInput();
            e.Handled = true;
        }
    }

    private void AddDeviceFromInput()
    {
        var input = AddDeviceTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(input))
        {
            // Parse input - could be alias or pin reference
            string deviceType = "Unknown";

            if (input.StartsWith("d", StringComparison.OrdinalIgnoreCase) &&
                (input.Length == 2 || input.Equals("db", StringComparison.OrdinalIgnoreCase)))
            {
                deviceType = "Device Pin";
            }

            AddDevice(input, deviceType);
            AddDeviceTextBox.Clear();
            AddDeviceTextBox.Focus();
        }
    }

    private void RemoveDeviceButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is MonitoredDevice device)
        {
            _devices.Remove(device);
        }
    }

    private void UpdateEmptyState()
    {
        EmptyMessage.Visibility = _devices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public override object SaveState()
    {
        return new
        {
            Devices = _devices.Select(d => new
            {
                d.Name,
                d.DeviceType,
                Properties = d.Properties.Select(p => new { p.Name, p.Value }).ToList()
            }).ToList()
        };
    }

    public override void LoadState(object state)
    {
        try
        {
            var dict = state as Dictionary<string, object>;
            if (dict != null && dict.TryGetValue("Devices", out var devicesObj))
            {
                // Devices will be populated during runtime
                // This just preserves the device list structure
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
            // Initialize device tracking
            _lastDeviceValues.Clear();
        });
    }

    public void OnSimulationStopped()
    {
        Dispatcher.Invoke(() =>
        {
            // No special action on stop
        });
    }

    public void OnSimulationStep(int lineNumber)
    {
        Dispatcher.Invoke(() =>
        {
            // Update all monitored devices
            foreach (var device in _devices)
            {
                UpdateDeviceProperties(device);
            }
        });
    }

    public void OnValueChanged(string name, double oldValue, double newValue)
    {
        // Not directly used by device monitor
    }

    private void UpdateDeviceProperties(MonitoredDevice device)
    {
        // Get all current properties for this device
        var currentProps = _bridge.GetAllDeviceProperties(device.Name);

        if (!_lastDeviceValues.ContainsKey(device.Name))
        {
            _lastDeviceValues[device.Name] = new Dictionary<string, double>();
        }

        foreach (var property in device.Properties)
        {
            if (currentProps.TryGetValue(property.Name, out var value))
            {
                var valueStr = FormatDeviceValue(value);

                // Check if value changed
                bool changed = false;
                if (_lastDeviceValues[device.Name].TryGetValue(property.Name, out var lastValue))
                {
                    changed = Math.Abs(value - lastValue) > 0.0001;
                }
                else
                {
                    changed = true;
                }

                if (changed || property.Value == "-")
                {
                    property.Value = valueStr;
                    _lastDeviceValues[device.Name][property.Name] = value;

                    // Could add visual indication of change here if desired
                }
            }
        }
    }

    private string FormatDeviceValue(double value)
    {
        if (double.IsNaN(value))
            return "NaN";
        if (double.IsInfinity(value))
            return value > 0 ? "Infinity" : "-Infinity";
        if (value == Math.Floor(value))
            return value.ToString("F0");
        return value.ToString("F2");
    }
}

public class MonitoredDevice : INotifyPropertyChanged
{
    private string _name = "";
    private string _deviceType = "Unknown";
    private Brush _typeColor = Brushes.Gray;

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

    public string DeviceType
    {
        get => _deviceType;
        set
        {
            if (_deviceType != value)
            {
                _deviceType = value;
                OnPropertyChanged(nameof(DeviceType));
            }
        }
    }

    public Brush TypeColor
    {
        get => _typeColor;
        set
        {
            if (_typeColor != value)
            {
                _typeColor = value;
                OnPropertyChanged(nameof(TypeColor));
            }
        }
    }

    public ObservableCollection<DeviceProperty> Properties { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DeviceProperty : INotifyPropertyChanged
{
    private string _name = "";
    private string _value = "-";

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
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
