using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Wires;
using WpfFlowDirection = System.Windows.FlowDirection;

namespace BasicToMips.UI.VisualScripting.Animations
{
    /// <summary>
    /// Represents a floating value popup
    /// </summary>
    public class ValuePopup
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; } = "";
        public DateTime StartTime { get; set; }
        public double OldValue { get; set; }
        public double NewValue { get; set; }
        public double Opacity { get; set; } = 1.0;
        public bool IsIncreasing { get; set; }
    }

    /// <summary>
    /// Shows floating value change popups near node outputs
    /// </summary>
    public class ValueChangeVisualizer
    {
        private readonly List<ValuePopup> _activePopups = new();

        // Animation timing (in milliseconds)
        private const double FadeInDuration = 150;
        private const double HoldDuration = 500;
        private const double FadeOutDuration = 200;
        private const double TotalDuration = FadeInDuration + HoldDuration + FadeOutDuration;

        // Visual properties
        private const double FloatDistance = 30; // How far up the popup floats
        private const double FontSize = 12;

        /// <summary>
        /// Animation settings
        /// </summary>
        public AnimationSettings Settings { get; set; } = new AnimationSettings();

        /// <summary>
        /// Show a value change popup
        /// </summary>
        /// <param name="x">X position (near node output)</param>
        /// <param name="y">Y position (near node output)</param>
        /// <param name="oldValue">Previous value</param>
        /// <param name="newValue">New value</param>
        public void ShowValueChange(double x, double y, double oldValue, double newValue)
        {
            if (!Settings.EnableValuePopups || !Settings.EnableAnimations)
                return;

            // Format the text based on value change
            string text;
            bool isIncreasing = newValue > oldValue;
            double delta = newValue - oldValue;

            if (Math.Abs(delta) < 0.01)
                return; // Ignore very small changes

            // Format value with appropriate precision
            if (Math.Abs(newValue) < 1000)
            {
                text = $"{newValue:F2}";
            }
            else
            {
                text = $"{newValue:F0}";
            }

            // Add delta indicator
            if (Math.Abs(delta) >= 0.01)
            {
                string deltaSign = isIncreasing ? "+" : "";
                text += $" ({deltaSign}{delta:F2})";
            }

            var popup = new ValuePopup
            {
                X = x,
                Y = y,
                Text = text,
                StartTime = DateTime.Now,
                OldValue = oldValue,
                NewValue = newValue,
                IsIncreasing = isIncreasing,
                Opacity = 0
            };

            _activePopups.Add(popup);
        }

        /// <summary>
        /// Update all active popups
        /// </summary>
        /// <param name="deltaTime">Time since last update in seconds</param>
        public void Update(double deltaTime)
        {
            if (!Settings.EnableValuePopups || !Settings.EnableAnimations)
            {
                _activePopups.Clear();
                return;
            }

            var now = DateTime.Now;

            // Update and remove expired popups
            _activePopups.RemoveAll(popup =>
            {
                double elapsed = (now - popup.StartTime).TotalMilliseconds;
                if (elapsed >= TotalDuration)
                    return true;

                // Calculate opacity based on phase
                if (elapsed < FadeInDuration)
                {
                    // Fade in
                    double progress = elapsed / FadeInDuration;
                    popup.Opacity = EasingFunctions.EaseOutCubic(progress);
                }
                else if (elapsed < FadeInDuration + HoldDuration)
                {
                    // Hold at full opacity
                    popup.Opacity = 1.0;
                }
                else
                {
                    // Fade out
                    double fadeOutElapsed = elapsed - (FadeInDuration + HoldDuration);
                    double progress = fadeOutElapsed / FadeOutDuration;
                    popup.Opacity = 1.0 - EasingFunctions.EaseInOutQuad(progress);
                }

                return false;
            });
        }

        /// <summary>
        /// Render all active value popups
        /// </summary>
        /// <param name="context">Drawing context</param>
        public void Render(DrawingContext context)
        {
            if (!Settings.EnableValuePopups || !Settings.EnableAnimations)
                return;

            var now = DateTime.Now;

            foreach (var popup in _activePopups)
            {
                double elapsed = (now - popup.StartTime).TotalMilliseconds;
                double progress = Math.Min(1.0, elapsed / TotalDuration);

                // Calculate vertical offset (floats upward)
                double yOffset = -FloatDistance * EasingFunctions.EaseOutCubic(progress);
                double renderY = popup.Y + yOffset;

                // Choose color based on value direction
                Color textColor = popup.IsIncreasing
                    ? Color.FromRgb(0x44, 0xFF, 0x44) // Green for increase
                    : Color.FromRgb(0xFF, 0x44, 0x44); // Red for decrease

                // Apply opacity
                byte alpha = (byte)(popup.Opacity * 255);
                var brush = new SolidColorBrush(Color.FromArgb(alpha, textColor.R, textColor.G, textColor.B));

                // Create formatted text
                var formattedText = new FormattedText(
                    popup.Text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    WpfFlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    FontSize,
                    brush,
                    VisualTreeHelper.GetDpi(new System.Windows.Controls.Canvas()).PixelsPerDip);

                // Draw text with shadow for better visibility
                if (popup.Opacity > 0.3)
                {
                    var shadowBrush = new SolidColorBrush(Color.FromArgb((byte)(alpha * 0.5), 0, 0, 0));
                    context.DrawText(
                        new FormattedText(
                            popup.Text,
                            System.Globalization.CultureInfo.CurrentCulture,
                            WpfFlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            FontSize,
                            shadowBrush,
                            VisualTreeHelper.GetDpi(new System.Windows.Controls.Canvas()).PixelsPerDip),
                        new Point(popup.X + 1 - formattedText.Width / 2, renderY + 1));
                }

                // Draw main text (centered above the point)
                context.DrawText(formattedText, new Point(popup.X - formattedText.Width / 2, renderY));
            }
        }

        /// <summary>
        /// Clear all active popups
        /// </summary>
        public void Clear()
        {
            _activePopups.Clear();
        }

        /// <summary>
        /// Get the number of active popups
        /// </summary>
        public int ActiveCount => _activePopups.Count;
    }
}
