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
    private bool _isLooping;
    private bool _isInYieldPause;  // Track yield pause state explicitly
    private int _loopCount;

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
            if (!_simulator.Step() || _simulator.IsPaused)
            {
                _runTimer.Stop();
                if (_isLooping && _simulator.IsHalted)
                {
                    StopLooping();
                }
                break;
            }

            if (_simulator.IsYielding)
            {
                _runTimer.Stop();

                if (_isLooping)
                {
                    // In loop mode, schedule resume after yield pause using async delay
                    _loopCount++;
                    ScheduleLoopResume();
                }
                break;
            }
        }

        UpdateUI();
    }

    private async void ScheduleLoopResume()
    {
        // Calculate delay based on speed slider
        int delayMs = (int)GetLoopTickInterval();

        // Update UI to show yield pause state
        _isInYieldPause = true;
        UpdateUI();

        // Wait for the delay (ConfigureAwait(true) ensures we return to UI thread)
        await Task.Delay(delayMs).ConfigureAwait(true);

        // Clear yield pause state
        _isInYieldPause = false;

        // Check if we're still looping after the delay
        if (!_isLooping)
        {
            UpdateUI();
            return;
        }

        // Resume execution after yield
        _simulator.Resume();

        // Continue running
        _runTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(10, 110 - SpeedSlider.Value));
        _runTimer.Start();

        UpdateUI();
    }

    private double GetLoopTickInterval()
    {
        // Slider value 1-100 maps to 2000ms-100ms tick interval
        // Lower slider = slower (longer pause), Higher slider = faster (shorter pause)
        return 2100 - (SpeedSlider.Value * 20);
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
        StopLooping();
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
        StopLooping();
        _runTimer.Stop();
        _simulator.Reset();
        _simulator.LoadProgram(CodeEditor.Text);
        _loopCount = 0;
        UpdateUI();
    }

    private void Loop_Click(object sender, RoutedEventArgs e)
    {
        if (_isLooping)
        {
            StopLooping();
        }
        else
        {
            StartLooping();
        }
    }

    private void StartLooping()
    {
        _isLooping = true;
        _loopCount = 0;

        if (_simulator.IsHalted)
        {
            _simulator.Reset();
            _simulator.LoadProgram(CodeEditor.Text);
        }

        // If yielding, resume first
        if (_simulator.IsYielding)
        {
            _simulator.Resume();
        }

        // Start execution
        _runTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(10, 110 - SpeedSlider.Value));
        _runTimer.Start();

        UpdateUI();
    }

    private void StopLooping()
    {
        _isLooping = false;
        _isInYieldPause = false;
        _runTimer.Stop();
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
        else if (_isLooping && _isInYieldPause)
        {
            StatusText.Text = "Looping (yield pause)";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(197, 134, 192)); // Purple
        }
        else if (_isLooping)
        {
            StatusText.Text = "Looping";
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(197, 134, 192)); // Purple
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

        // Update loop button and counter
        LoopButtonText.Text = _isLooping ? " Stop Loop" : " Loop";
        LoopCountLabel.Visibility = _isLooping || _loopCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        LoopCountText.Visibility = _isLooping || _loopCount > 0 ? Visibility.Visible : Visibility.Collapsed;
        LoopCountText.Text = _loopCount.ToString();

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

        // Add virtual devices from registry (if not already in list)
        int deviceIndex = IC10Simulator.DeviceCount;
        var virtualDevices = _simulator.DeviceRegistry.GetAllDevices();

        // Remove old virtual devices and add new ones
        while (_devices.Count > IC10Simulator.DeviceCount)
        {
            _devices.RemoveAt(_devices.Count - 1);
        }

        foreach (var kvp in virtualDevices)
        {
            var virtualDev = kvp.Value;
            _devices.Add(new DeviceItem
            {
                Name = virtualDev.Alias,
                Alias = virtualDev.PrefabName,
                On = virtualDev.GetProperty("On"),
                Setting = virtualDev.GetProperty("Setting"),
                Temperature = virtualDev.GetProperty("Temperature")
            });
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
            var header = e.Column.Header.ToString();

            if (!double.TryParse(textBox.Text, out double value))
                return;

            // Check if it's a traditional d0-d5 device
            if (item.Name.StartsWith("d") && int.TryParse(item.Name.Substring(1), out int devIndex))
            {
                var device = _simulator.Devices[devIndex];
                switch (header)
                {
                    case "On": device.SetProperty("On", value); break;
                    case "Setting": device.SetProperty("Setting", value); break;
                    case "Temp": device.SetProperty("Temperature", value); break;
                }
            }
            else
            {
                // It's a virtual device from the registry
                var virtualDevice = _simulator.DeviceRegistry.GetDevice(item.Name);
                if (virtualDevice != null)
                {
                    switch (header)
                    {
                        case "On": virtualDevice.SetProperty("On", value); break;
                        case "Setting": virtualDevice.SetProperty("Setting", value); break;
                        case "Temp": virtualDevice.SetProperty("Temperature", value); break;
                    }
                }
            }
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _isLooping = false;
        _isInYieldPause = false;
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
