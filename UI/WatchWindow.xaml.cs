using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using BasicToMips.Editor.Debugging;
using BasicToMips.Simulator;

namespace BasicToMips.UI;

public partial class WatchWindow : Window
{
    private readonly WatchManager _watchManager;
    private IC10Simulator? _simulator;
    private readonly ObservableCollection<WatchDisplayItem> _watchItems = new();

    public WatchWindow(WatchManager watchManager)
    {
        InitializeComponent();
        _watchManager = watchManager;
        WatchListPanel.ItemsSource = _watchItems;

        // Load existing watches from manager
        RefreshWatchList();
    }

    public void SetSimulator(IC10Simulator? simulator)
    {
        _simulator = simulator;
        RefreshValues();
    }

    public void RefreshValues()
    {
        if (_simulator == null) return;

        foreach (var item in _watchItems)
        {
            var oldValue = item.Value;
            item.Value = EvaluateExpression(item.Name);
            item.HasChanged = oldValue != item.Value;
        }
    }

    private void RefreshWatchList()
    {
        _watchItems.Clear();
        foreach (var watch in _watchManager.WatchItems)
        {
            _watchItems.Add(new WatchDisplayItem
            {
                Name = watch.Name,
                Value = "—",
                Type = watch.Type.ToString(),
                HasChanged = false
            });
        }
        RefreshValues();
    }

    private string EvaluateExpression(string expression)
    {
        if (_simulator == null) return "—";

        try
        {
            // Register expressions (r0-r15, sp, ra)
            if (expression.StartsWith("r", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(expression.Substring(1), out int regNum) && regNum >= 0 && regNum < 18)
                {
                    return _simulator.Registers[regNum].ToString("F4");
                }
            }

            if (expression.Equals("sp", StringComparison.OrdinalIgnoreCase))
            {
                return _simulator.StackPointer.ToString();
            }

            if (expression.Equals("ra", StringComparison.OrdinalIgnoreCase))
            {
                return _simulator.Registers[17].ToString("F4");
            }

            // Device property expressions (d0.Property)
            if (expression.Contains('.'))
            {
                var parts = expression.Split('.', 2);
                if (parts[0].StartsWith("d", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(parts[0].Substring(1), out int devNum) && devNum >= 0 && devNum < 6)
                    {
                        var device = _simulator.Devices[devNum];
                        var propName = parts[1];
                        if (device.Properties.TryGetValue(propName, out var value))
                        {
                            return value.ToString("F2");
                        }
                        return "N/A";
                    }
                }
            }

            return "?";
        }
        catch
        {
            return "Error";
        }
    }

    private void WatchExpressionInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddWatch_Click(sender, e);
        }
    }

    private void AddWatch_Click(object sender, RoutedEventArgs e)
    {
        var expression = WatchExpressionInput.Text.Trim();
        if (string.IsNullOrEmpty(expression)) return;

        var watchItem = _watchManager.AddWatch(expression);
        _watchItems.Add(new WatchDisplayItem
        {
            Name = expression,
            Value = EvaluateExpression(expression),
            Type = watchItem.Type.ToString(),
            HasChanged = false
        });

        WatchExpressionInput.Text = "";
    }

    private void RemoveWatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is WatchDisplayItem item)
        {
            // Find the corresponding WatchItem in the manager
            var managerItem = _watchManager.WatchItems.FirstOrDefault(w => w.Name == item.Name);
            if (managerItem != null)
            {
                _watchManager.RemoveWatch(managerItem);
            }
            _watchItems.Remove(item);
        }
    }

    private void ClearAllWatches_Click(object sender, RoutedEventArgs e)
    {
        _watchManager.ClearAll();
        _watchItems.Clear();
    }

    private void AddDefaultWatches_Click(object sender, RoutedEventArgs e)
    {
        // Add common registers
        var defaults = new[] { "r0", "r1", "r2", "r3", "sp", "ra" };
        foreach (var expr in defaults)
        {
            if (!_watchItems.Any(w => w.Name.Equals(expr, StringComparison.OrdinalIgnoreCase)))
            {
                var watchItem = _watchManager.AddWatch(expr);
                _watchItems.Add(new WatchDisplayItem
                {
                    Name = expr,
                    Value = EvaluateExpression(expr),
                    Type = watchItem.Type.ToString(),
                    HasChanged = false
                });
            }
        }
    }

    private void RefreshWatches_Click(object sender, RoutedEventArgs e)
    {
        RefreshValues();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Hide instead of close so we can reopen
        e.Cancel = true;
        Hide();
    }
}

public class WatchDisplayItem : INotifyPropertyChanged
{
    private string _name = "";
    private string _value = "";
    private string _type = "";
    private bool _hasChanged;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(nameof(Value)); }
    }

    public string Type
    {
        get => _type;
        set { _type = value; OnPropertyChanged(nameof(Type)); }
    }

    public bool HasChanged
    {
        get => _hasChanged;
        set { _hasChanged = value; OnPropertyChanged(nameof(HasChanged)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
