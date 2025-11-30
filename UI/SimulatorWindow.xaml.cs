using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BasicToMips.Simulator;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace BasicToMips.UI;

public partial class SimulatorWindow : Window
{
    private readonly IC10Simulator _simulator = new();
    private readonly DispatcherTimer _runTimer = new();
    private readonly ObservableCollection<RegisterItem> _registers = new();
    private readonly ObservableCollection<DeviceItem> _devices = new();
    private CurrentLineHighlighter? _lineHighlighter;

    public SimulatorWindow(string ic10Code)
    {
        InitializeComponent();

        // Setup code editor
        CodeEditor.Text = ic10Code;
        _lineHighlighter = new CurrentLineHighlighter();
        CodeEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlighter);

        // Setup timer for continuous execution
        _runTimer.Tick += RunTimer_Tick;

        // Setup data bindings
        RegistersGrid.ItemsSource = _registers;
        DevicesGrid.ItemsSource = _devices;

        // Initialize registers display
        for (int i = 0; i < 16; i++)
        {
            _registers.Add(new RegisterItem { Name = $"r{i}", Value = 0 });
        }
        _registers.Add(new RegisterItem { Name = "sp", Value = 0 });
        _registers.Add(new RegisterItem { Name = "ra", Value = 0 });

        // Initialize devices display
        for (int i = 0; i < IC10Simulator.DeviceCount; i++)
        {
            _devices.Add(new DeviceItem { Name = $"d{i}" });
        }

        // Load program
        _simulator.LoadProgram(ic10Code);
        _simulator.StateChanged += Simulator_StateChanged;

        UpdateUI();
    }

    private void RunTimer_Tick(object? sender, EventArgs e)
    {
        // Execute multiple steps per timer tick based on speed
        int stepsPerTick = (int)(SpeedSlider.Value / 10) + 1;

        for (int i = 0; i < stepsPerTick; i++)
        {
            if (!_simulator.Step() || _simulator.IsPaused || _simulator.IsYielding)
            {
                _runTimer.Stop();
                break;
            }
        }

        UpdateUI();
    }

    private void Simulator_StateChanged(object? sender, EventArgs e)
    {
        // UI updates happen in the timer or step handlers
    }

    private void Run_Click(object sender, RoutedEventArgs e)
    {
        if (_simulator.IsHalted)
        {
            _simulator.Reset();
            _simulator.LoadProgram(CodeEditor.Text);
        }

        _runTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(10, 110 - SpeedSlider.Value));
        _runTimer.Start();
        UpdateUI();
    }

    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        _runTimer.Stop();
        _simulator.Pause();
        UpdateUI();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _runTimer.Stop();
        _simulator.Stop();
        UpdateUI();
    }

    private void Step_Click(object sender, RoutedEventArgs e)
    {
        _runTimer.Stop();

        if (_simulator.IsHalted)
        {
            _simulator.Reset();
            _simulator.LoadProgram(CodeEditor.Text);
        }

        _simulator.Step();
        UpdateUI();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _runTimer.Stop();
        _simulator.Reset();
        _simulator.LoadProgram(CodeEditor.Text);
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Update status
        if (_simulator.IsHalted)
        {
            StatusText.Text = string.IsNullOrEmpty(_simulator.ErrorMessage) ? "Halted" : "Error";
            StatusText.Foreground = string.IsNullOrEmpty(_simulator.ErrorMessage) ? Brushes.White : Brushes.LightCoral;
        }
        else if (_simulator.IsYielding)
        {
            StatusText.Text = "Yielding";
            StatusText.Foreground = Brushes.LightYellow;
        }
        else if (_runTimer.IsEnabled)
        {
            StatusText.Text = "Running";
            StatusText.Foreground = Brushes.LightGreen;
        }
        else
        {
            StatusText.Text = "Paused";
            StatusText.Foreground = Brushes.White;
        }

        ErrorText.Text = _simulator.ErrorMessage ?? "";

        // Update counters
        PCText.Text = _simulator.ProgramCounter.ToString();
        InstructionCountText.Text = _simulator.InstructionCount.ToString();
        SPText.Text = _simulator.StackPointer.ToString();

        // Update registers
        for (int i = 0; i < 16; i++)
        {
            _registers[i].Value = _simulator.Registers[i];
        }
        _registers[16].Value = _simulator.StackPointer;
        _registers[17].Value = _simulator.Registers[17];

        RegistersGrid.Items.Refresh();

        // Update devices
        for (int i = 0; i < IC10Simulator.DeviceCount; i++)
        {
            var dev = _simulator.Devices[i];
            _devices[i].Alias = dev.Alias;
            _devices[i].On = dev.GetProperty("On");
            _devices[i].Setting = dev.GetProperty("Setting");
            _devices[i].Temperature = dev.GetProperty("Temperature");
        }

        DevicesGrid.Items.Refresh();

        // Update stack
        StackList.Items.Clear();
        for (int i = _simulator.StackPointer - 1; i >= 0; i--)
        {
            StackList.Items.Add($"[{i}] = {_simulator.Stack[i]:F4}");
        }
        StackCountText.Text = $" ({_simulator.StackPointer} items)";

        // Highlight current line
        _lineHighlighter?.SetLine(_simulator.ProgramCounter);
        CodeEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);

        // Scroll to current line
        if (_simulator.ProgramCounter >= 0 && _simulator.ProgramCounter < CodeEditor.Document.LineCount)
        {
            var line = CodeEditor.Document.GetLineByNumber(_simulator.ProgramCounter + 1);
            CodeEditor.ScrollTo(_simulator.ProgramCounter + 1, 0);
        }

        // Update button states
        RunButton.IsEnabled = !_runTimer.IsEnabled;
        PauseButton.IsEnabled = _runTimer.IsEnabled;
        StepButton.IsEnabled = !_runTimer.IsEnabled;
    }

    private void RegistersGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column.Header.ToString() == "Value" && e.EditingElement is TextBox textBox)
        {
            if (double.TryParse(textBox.Text, out double value))
            {
                var item = (RegisterItem)e.Row.Item;
                if (item.Name.StartsWith("r") && int.TryParse(item.Name.Substring(1), out int regNum))
                {
                    _simulator.Registers[regNum] = value;
                }
                else if (item.Name == "ra")
                {
                    _simulator.Registers[17] = value;
                }
            }
        }
    }

    private void DevicesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditingElement is TextBox textBox)
        {
            var item = (DeviceItem)e.Row.Item;
            if (item.Name.StartsWith("d") && int.TryParse(item.Name.Substring(1), out int devIndex))
            {
                var device = _simulator.Devices[devIndex];
                var header = e.Column.Header.ToString();

                if (double.TryParse(textBox.Text, out double value))
                {
                    switch (header)
                    {
                        case "On": device.SetProperty("On", value); break;
                        case "Setting": device.SetProperty("Setting", value); break;
                        case "Temp": device.SetProperty("Temperature", value); break;
                    }
                }
            }
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _runTimer.Stop();
    }

    /// <summary>
    /// Load new IC10 code into the simulator
    /// </summary>
    public void LoadCode(string ic10Code)
    {
        _runTimer.Stop();
        CodeEditor.Text = ic10Code;
        _simulator.Reset();
        _simulator.LoadProgram(ic10Code);
        UpdateUI();
    }
}

public class RegisterItem
{
    public string Name { get; set; } = "";
    public double Value { get; set; }
}

public class DeviceItem
{
    public string Name { get; set; } = "";
    public string? Alias { get; set; }
    public double On { get; set; }
    public double Setting { get; set; }
    public double Temperature { get; set; } = 293.15;
}

public class CurrentLineHighlighter : IBackgroundRenderer
{
    private int _currentLine = -1;

    public KnownLayer Layer => KnownLayer.Background;

    public void SetLine(int line)
    {
        _currentLine = line;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_currentLine < 0 || _currentLine >= textView.Document.LineCount)
            return;

        textView.EnsureVisualLines();

        var line = textView.Document.GetLineByNumber(_currentLine + 1);
        var segment = new TextSegment { StartOffset = line.Offset, Length = line.Length };

        foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
        {
            var fullRect = new Rect(0, rect.Top, textView.ActualWidth, rect.Height);
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(60, 255, 255, 0)),
                null,
                fullRect);
        }
    }
}

public class TextSegment : ICSharpCode.AvalonEdit.Document.ISegment
{
    public int Offset { get; set; }
    public int StartOffset { get; set; }
    public int Length { get; set; }
    public int EndOffset => StartOffset + Length;
}
