using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BasicToMips.Simulator;

namespace BasicToMips.UI;

public partial class VariableInspectorWindow : Window
{
    private readonly ObservableCollection<VariableItem> _variables = new();
    private readonly ObservableCollection<ConstantItem> _constants = new();
    private IC10Simulator? _simulator;
    private string _basicCode = "";
    private Dictionary<string, int> _variableToRegister = new();

    public VariableInspectorWindow()
    {
        InitializeComponent();
        VariablesGrid.ItemsSource = _variables;
        ConstantsGrid.ItemsSource = _constants;
    }

    public void SetSimulator(IC10Simulator? simulator)
    {
        _simulator = simulator;
        RefreshValues();
    }

    public void LoadFromBasicCode(string basicCode, string ic10Code)
    {
        _basicCode = basicCode;
        ParseVariables(basicCode, ic10Code);
        RefreshValues();
    }

    private void ParseVariables(string basicCode, string ic10Code)
    {
        _variables.Clear();
        _constants.Clear();
        _variableToRegister.Clear();

        var lines = basicCode.Split('\n');

        // First pass: find all VAR declarations
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var lineNum = i + 1;

            // Skip comments
            if (line.StartsWith("'") || line.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
                continue;

            // VAR or LET declaration
            if (line.StartsWith("VAR ", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("LET ", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"(?:VAR|LET)\s+(\w+)(?:\s*=\s*(.+))?", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var varName = match.Groups[1].Value;
                    var initialValue = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "0";

                    _variables.Add(new VariableItem
                    {
                        Name = varName,
                        Register = "?",
                        Value = 0,
                        Line = lineNum,
                        InitialValue = initialValue
                    });
                }
            }
            // CONST or DEFINE
            else if (line.StartsWith("CONST ", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("DEFINE ", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"(?:CONST|DEFINE)\s+(\w+)\s*=\s*(.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    _constants.Add(new ConstantItem
                    {
                        Name = match.Groups[1].Value,
                        Value = match.Groups[2].Value.Trim()
                    });
                }
            }
        }

        // Second pass: try to map variables to registers from IC10 code
        // Look for comments like "# varName -> r0" or patterns in alias statements
        ParseRegisterMappings(ic10Code);
    }

    private void ParseRegisterMappings(string ic10Code)
    {
        var ic10Lines = ic10Code.Split('\n');

        // Look for alias statements or comments indicating variable mappings
        foreach (var line in ic10Lines)
        {
            // Pattern: alias varName r0
            var aliasMatch = Regex.Match(line, @"alias\s+(\w+)\s+(r\d+)", RegexOptions.IgnoreCase);
            if (aliasMatch.Success)
            {
                var varName = aliasMatch.Groups[1].Value;
                var register = aliasMatch.Groups[2].Value;

                var varItem = _variables.FirstOrDefault(v =>
                    v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                if (varItem != null)
                {
                    varItem.Register = register;
                    _variableToRegister[varName.ToLower()] = int.Parse(register.Substring(1));
                }
            }

            // Pattern: # varName -> r0 (comment mapping)
            var commentMatch = Regex.Match(line, @"#\s*(\w+)\s*->\s*(r\d+)", RegexOptions.IgnoreCase);
            if (commentMatch.Success)
            {
                var varName = commentMatch.Groups[1].Value;
                var register = commentMatch.Groups[2].Value;

                var varItem = _variables.FirstOrDefault(v =>
                    v.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                if (varItem != null && varItem.Register == "?")
                {
                    varItem.Register = register;
                    _variableToRegister[varName.ToLower()] = int.Parse(register.Substring(1));
                }
            }
        }

        // For variables without register mappings, try to infer from usage
        // This is a simple heuristic - assign sequential registers starting from r0
        int nextReg = 0;
        foreach (var varItem in _variables.Where(v => v.Register == "?"))
        {
            while (_variableToRegister.ContainsValue(nextReg) && nextReg < 16)
                nextReg++;

            if (nextReg < 16)
            {
                varItem.Register = $"r{nextReg}";
                _variableToRegister[varItem.Name.ToLower()] = nextReg;
                nextReg++;
            }
        }
    }

    public void RefreshValues()
    {
        if (_simulator == null) return;

        foreach (var varItem in _variables)
        {
            if (varItem.Register != "?" && varItem.Register.StartsWith("r"))
            {
                if (int.TryParse(varItem.Register.Substring(1), out int regNum) && regNum >= 0 && regNum < 16)
                {
                    var oldValue = varItem.Value;
                    varItem.Value = _simulator.Registers[regNum];
                    varItem.HasChanged = Math.Abs(oldValue - varItem.Value) > 0.0001;
                }
            }
        }

        VariablesGrid.Items.Refresh();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        RefreshValues();
        StatusText.Text = "Values refreshed from simulator";
    }

    private void VariablesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column.Header.ToString() == "Value" && e.EditingElement is TextBox textBox)
        {
            if (double.TryParse(textBox.Text, out double value))
            {
                var item = (VariableItem)e.Row.Item;
                if (_simulator != null && item.Register.StartsWith("r"))
                {
                    if (int.TryParse(item.Register.Substring(1), out int regNum) && regNum >= 0 && regNum < 16)
                    {
                        _simulator.Registers[regNum] = value;
                        StatusText.Text = $"Set {item.Name} ({item.Register}) = {value}";
                    }
                }
            }
            else
            {
                StatusText.Text = "Invalid number format";
            }
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Hide instead of close so we can reopen
        e.Cancel = true;
        Hide();
    }
}

public class VariableItem : INotifyPropertyChanged
{
    private string _name = "";
    private string _register = "";
    private double _value;
    private int _line;
    private string _initialValue = "";
    private bool _hasChanged;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string Register
    {
        get => _register;
        set { _register = value; OnPropertyChanged(nameof(Register)); }
    }

    public double Value
    {
        get => _value;
        set { _value = value; OnPropertyChanged(nameof(Value)); }
    }

    public int Line
    {
        get => _line;
        set { _line = value; OnPropertyChanged(nameof(Line)); }
    }

    public string InitialValue
    {
        get => _initialValue;
        set { _initialValue = value; OnPropertyChanged(nameof(InitialValue)); }
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

public class ConstantItem
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
