using System;
using System.Windows;
using System.Windows.Media;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class SimulationControlWidget : WidgetBase, ISimulationListener
{
    private readonly SimulationBridge _bridge;
    private bool _isRunning;

    public SimulationControlWidget()
    {
        Title = "Simulation Control";
        InitializeComponent();

        _bridge = SimulationBridge.Instance;
        _bridge.RegisterListener(this);

        // Subscribe to bridge events for UI updates
        _bridge.SimulationStarted += OnBridgeSimulationStarted;
        _bridge.SimulationStopped += OnBridgeSimulationStopped;
        _bridge.SimulationStep += OnBridgeSimulationStep;
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        UpdateDisplay();
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            // Pause
            _bridge.Stop();
        }
        else
        {
            // Play
            _bridge.Start();
        }
    }

    private void StepButton_Click(object sender, RoutedEventArgs e)
    {
        _bridge.Step();
        UpdateDisplay();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _bridge.Reset();
        UpdateDisplay();
    }

    private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Convert slider value to delay (inverted: higher value = slower)
        var delayMs = (int)e.NewValue;
        _bridge.SetSpeed(delayMs);
    }

    private void UpdateDisplay()
    {
        Dispatcher.Invoke(() =>
        {
            var state = _bridge.GetState();

            // Update status text and color
            if (state.IsHalted)
            {
                StatusText.Text = state.ErrorMessage != null ? $"Error: {state.ErrorMessage}" : "Halted";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C)); // Red
            }
            else if (state.IsRunning)
            {
                StatusText.Text = "Running";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0)); // Green
            }
            else if (state.IsPaused)
            {
                StatusText.Text = "Paused";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xC9, 0x00)); // Yellow
            }
            else
            {
                StatusText.Text = "Stopped";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0x85, 0x85, 0x85)); // Gray
            }

            // Update current line
            CurrentLineText.Text = state.ProgramCounter.ToString();

            // Update instruction count
            InstructionCountText.Text = state.InstructionCount.ToString();

            // Update breakpoint count
            BreakpointCountText.Text = state.BreakpointCount.ToString();

            // Update button states
            if (state.IsRunning)
            {
                PlayPauseIcon.Text = "⏸";
                PlayPauseText.Text = "Pause";
                StepButton.IsEnabled = false;
            }
            else
            {
                PlayPauseIcon.Text = "▶";
                PlayPauseText.Text = "Run";
                StepButton.IsEnabled = !state.IsHalted;
            }

            PlayPauseButton.IsEnabled = !state.IsHalted;
        });
    }

    private void OnBridgeSimulationStarted(object? sender, EventArgs e)
    {
        UpdateDisplay();
    }

    private void OnBridgeSimulationStopped(object? sender, EventArgs e)
    {
        UpdateDisplay();
    }

    private void OnBridgeSimulationStep(object? sender, int lineNumber)
    {
        UpdateDisplay();
    }

    // ISimulationListener implementation
    public void OnSimulationStarted()
    {
        Dispatcher.Invoke(() =>
        {
            _isRunning = true;
            UpdateDisplay();
        });
    }

    public void OnSimulationStopped()
    {
        Dispatcher.Invoke(() =>
        {
            _isRunning = false;
            UpdateDisplay();
        });
    }

    public void OnSimulationStep(int lineNumber)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateDisplay();
        });
    }

    public void OnValueChanged(string name, double oldValue, double newValue)
    {
        // Not used by this widget
    }

    public override object SaveState()
    {
        return new
        {
            Speed = (int)SpeedSlider.Value
        };
    }

    public override void LoadState(object state)
    {
        try
        {
            var dict = state as System.Collections.Generic.Dictionary<string, object>;
            if (dict != null && dict.TryGetValue("Speed", out var speed))
            {
                SpeedSlider.Value = Convert.ToDouble(speed);
            }
        }
        catch
        {
            // Ignore state load errors
        }
    }
}
