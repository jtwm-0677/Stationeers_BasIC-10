using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace BasicToMips.UI.Dashboard.Widgets;

public partial class LineCounterWidget : WidgetBase
{
    private const int MaxIc10Lines = 128;
    private int _basicLines = 0;
    private int _ic10Lines = 0;

    public LineCounterWidget()
    {
        Title = "Line Counter";
        InitializeComponent();
    }

    public override void Render()
    {
        if (ContentGrid == null)
            return;

        UpdateDisplay();
    }

    public void UpdateCounts(int basicLines, int ic10Lines)
    {
        Dispatcher.Invoke(() =>
        {
            _basicLines = basicLines;
            _ic10Lines = ic10Lines;
            UpdateDisplay();
        });
    }

    private void UpdateDisplay()
    {
        if (BasicLineText == null || Ic10LineText == null || ProgressBar == null || PercentageText == null || WarningIcon == null)
            return;

        // Update text displays
        BasicLineText.Text = $"BASIC: {_basicLines} lines";
        Ic10LineText.Text = $"IC10: {_ic10Lines} / {MaxIc10Lines} lines";

        // Calculate percentage
        double percentage = _ic10Lines > 0 ? (_ic10Lines / (double)MaxIc10Lines) * 100.0 : 0;
        PercentageText.Text = $"{percentage:F1}%";

        // Update progress bar
        var parentWidth = ActualWidth > 0 ? ActualWidth - 24 : 276; // Subtract margins
        var barWidth = Math.Min(percentage / 100.0 * parentWidth, parentWidth);
        ProgressBar.Width = Math.Max(0, barWidth);

        // Color-code based on usage
        if (_ic10Lines > 120)
        {
            // Red: Over limit
            ProgressBar.Background = new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C));
            WarningIcon.Visibility = Visibility.Visible;
            WarningIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF1, 0x4C, 0x4C));
            WarningIcon.ToolTip = "Exceeds IC10 line limit!";
        }
        else if (_ic10Lines >= 100)
        {
            // Yellow: Approaching limit
            ProgressBar.Background = new SolidColorBrush(Color.FromRgb(0xCC, 0xC9, 0x00));
            WarningIcon.Visibility = Visibility.Visible;
            WarningIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xC9, 0x00));
            WarningIcon.ToolTip = "Approaching IC10 line limit";
        }
        else
        {
            // Green: Safe
            ProgressBar.Background = new SolidColorBrush(Color.FromRgb(0x4E, 0xC9, 0xB0));
            WarningIcon.Visibility = Visibility.Collapsed;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateDisplay(); // Recalculate progress bar width
    }

    public override object SaveState()
    {
        return new
        {
            BasicLines = _basicLines,
            Ic10Lines = _ic10Lines
        };
    }

    public override void LoadState(object state)
    {
        try
        {
            var dict = state as Dictionary<string, object>;
            if (dict != null)
            {
                if (dict.TryGetValue("BasicLines", out var basicLines))
                    _basicLines = Convert.ToInt32(basicLines);

                if (dict.TryGetValue("Ic10Lines", out var ic10Lines))
                    _ic10Lines = Convert.ToInt32(ic10Lines);

                UpdateDisplay();
            }
        }
        catch
        {
            // Ignore state load errors
        }
    }
}
