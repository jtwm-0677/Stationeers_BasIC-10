using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class VariableWatchWidget : WidgetBase, ISimulationListener
{
    private readonly ObservableCollection<WatchedVariable> _variables = new();
    private readonly Dictionary<string, string> _lastValues = new();
    private readonly SimulationBridge _bridge;

    public VariableWatchWidget()
    {
        Title = "Variable Watch";
        InitializeComponent();

        _bridge = SimulationBridge.Instance;
        _bridge.RegisterListener(this);
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        VariableList.ItemsSource = _variables;
        _variables.CollectionChanged += (s, e) => UpdateEmptyState();
        UpdateEmptyState();
    }

    public void AddVariable(string name, string register = "", string value = "", string line = "")
    {
        Dispatcher.Invoke(() =>
        {
            // Don't add duplicates
            if (_variables.Any(v => v.Name == name))
                return;

            _variables.Add(new WatchedVariable
            {
                Name = name,
                Register = register,
                Value = value,
                Line = line,
                IsChanged = false
            });

            _lastValues[name] = value;
            UpdateEmptyState();
        });
    }

    public void UpdateVariable(string name, string value, string register = "", string line = "")
    {
        Dispatcher.Invoke(() =>
        {
            var variable = _variables.FirstOrDefault(v => v.Name == name);
            if (variable != null)
            {
                // Check if value changed
                if (_lastValues.TryGetValue(name, out var lastValue))
                {
                    variable.IsChanged = lastValue != value;
                }

                variable.Value = value;
                if (!string.IsNullOrEmpty(register))
                    variable.Register = register;
                if (!string.IsNullOrEmpty(line))
                    variable.Line = line;

                _lastValues[name] = value;

                // Reset IsChanged after delay
                if (variable.IsChanged)
                {
                    System.Threading.Tasks.Task.Delay(600).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() => variable.IsChanged = false);
                    });
                }
            }
        });
    }

    public void RemoveVariable(string name)
    {
        Dispatcher.Invoke(() =>
        {
            var variable = _variables.FirstOrDefault(v => v.Name == name);
            if (variable != null)
            {
                _variables.Remove(variable);
                _lastValues.Remove(name);
            }
        });
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        AddVariableFromInput();
    }

    private void AddVariableTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddVariableFromInput();
            e.Handled = true;
        }
    }

    private void AddVariableFromInput()
    {
        var name = AddVariableTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            AddVariable(name, "-", "-", "-");
            AddVariableTextBox.Clear();
            AddVariableTextBox.Focus();
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is WatchedVariable variable)
        {
            _variables.Remove(variable);
            _lastValues.Remove(variable.Name);
        }
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        _variables.Clear();
        _lastValues.Clear();
    }

    private void Value_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && sender is TextBlock textBlock)
        {
            // Double-click to edit value (during pause)
            if (textBlock.DataContext is WatchedVariable variable)
            {
                EditValue(variable, textBlock);
            }
        }
    }

    private void EditValue(WatchedVariable variable, TextBlock textBlock)
    {
        // Create inline editor
        var textBox = new TextBox
        {
            Text = variable.Value,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 12,
            FontFamily = new System.Windows.Media.FontFamily("Consolas, Cascadia Code, Courier New"),
            CaretBrush = System.Windows.Media.Brushes.White
        };

        // Replace TextBlock with TextBox temporarily
        var parent = textBlock.Parent as Grid;
        if (parent == null)
            return;

        var column = Grid.GetColumn(textBlock);
        parent.Children.Remove(textBlock);
        Grid.SetColumn(textBox, column);
        parent.Children.Add(textBox);

        textBox.Focus();
        textBox.SelectAll();

        // Save on Enter or focus lost
        void SaveEdit()
        {
            var newValue = textBox.Text.Trim();
            if (!string.IsNullOrEmpty(newValue) && double.TryParse(newValue, out var numericValue))
            {
                variable.Value = newValue;
                _lastValues[variable.Name] = newValue;

                // Notify simulator of value change if this is a register
                if (variable.Name.StartsWith("r") || variable.Name == "sp" || variable.Name == "ra")
                {
                    _bridge.SetRegisterValue(variable.Name, numericValue);
                }
                else
                {
                    _bridge.SetVariableValue(variable.Name, numericValue);
                }
            }

            parent.Children.Remove(textBox);
            Grid.SetColumn(textBlock, column);
            parent.Children.Add(textBlock);
        }

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SaveEdit();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                parent.Children.Remove(textBox);
                Grid.SetColumn(textBlock, column);
                parent.Children.Add(textBlock);
                e.Handled = true;
            }
        };

        textBox.LostFocus += (s, e) => SaveEdit();
    }

    private void UpdateEmptyState()
    {
        EmptyMessage.Visibility = _variables.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public override object SaveState()
    {
        return new
        {
            Variables = _variables.Select(v => new
            {
                v.Name,
                v.Register,
                v.Value,
                v.Line
            }).ToList()
        };
    }

    public override void LoadState(object state)
    {
        try
        {
            var dict = state as Dictionary<string, object>;
            if (dict != null && dict.TryGetValue("Variables", out var varsObj))
            {
                // Variables will be populated during runtime
                // This just preserves the watch list
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
            // Update all watched variables to show "Running" state
            foreach (var variable in _variables)
            {
                if (variable.Value == "-")
                {
                    variable.Value = "0";
                }
            }
        });
    }

    public void OnSimulationStopped()
    {
        Dispatcher.Invoke(() =>
        {
            // No special action needed on stop
        });
    }

    public void OnSimulationStep(int lineNumber)
    {
        Dispatcher.Invoke(() =>
        {
            // Update all watched variables with current values from simulation
            foreach (var variable in _variables)
            {
                double value = 0;

                // Check if it's a register
                if (variable.Name.StartsWith("r") || variable.Name == "sp" || variable.Name == "ra")
                {
                    value = _bridge.GetRegisterValue(variable.Name);
                }
                else
                {
                    value = _bridge.GetVariableValue(variable.Name);
                }

                var valueStr = FormatValue(value);
                if (variable.Value != valueStr)
                {
                    variable.IsChanged = true;
                    variable.Value = valueStr;
                    _lastValues[variable.Name] = valueStr;

                    // Reset IsChanged after delay
                    System.Threading.Tasks.Task.Delay(600).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() => variable.IsChanged = false);
                    });
                }
            }
        });
    }

    public void OnValueChanged(string name, double oldValue, double newValue)
    {
        Dispatcher.Invoke(() =>
        {
            // Update specific variable if being watched
            var variable = _variables.FirstOrDefault(v => v.Name == name);
            if (variable != null)
            {
                var valueStr = FormatValue(newValue);
                variable.IsChanged = true;
                variable.Value = valueStr;
                _lastValues[name] = valueStr;

                // Reset IsChanged after delay
                System.Threading.Tasks.Task.Delay(600).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => variable.IsChanged = false);
                });
            }
        });
    }

    private string FormatValue(double value)
    {
        if (double.IsNaN(value))
            return "NaN";
        if (double.IsInfinity(value))
            return value > 0 ? "Infinity" : "-Infinity";
        if (value == Math.Floor(value))
            return value.ToString("F0");
        return value.ToString("F3");
    }
}

public class WatchedVariable : INotifyPropertyChanged
{
    private string _name = "";
    private string _register = "-";
    private string _value = "-";
    private string _line = "-";
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

    public string Register
    {
        get => _register;
        set
        {
            if (_register != value)
            {
                _register = value;
                OnPropertyChanged(nameof(Register));
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

    public string Line
    {
        get => _line;
        set
        {
            if (_line != value)
            {
                _line = value;
                OnPropertyChanged(nameof(Line));
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
