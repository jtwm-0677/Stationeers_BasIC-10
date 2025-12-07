using System.Windows.Media;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Static color definitions for pin data types
    /// </summary>
    public static class PinColors
    {
        // Execution flow
        public static readonly Color Execution = Color.FromRgb(0xFF, 0xFF, 0xFF); // White

        // Data types
        public static readonly Color Number = Color.FromRgb(0x4A, 0x9E, 0xFF); // Blue
        public static readonly Color BooleanTrue = Color.FromRgb(0x44, 0xFF, 0x44); // Green
        public static readonly Color BooleanFalse = Color.FromRgb(0xFF, 0x44, 0x44); // Red
        public static readonly Color Device = Color.FromRgb(0xFF, 0xA5, 0x00); // Orange
        public static readonly Color String = Color.FromRgb(0xAA, 0x44, 0xFF); // Purple

        /// <summary>
        /// Get the color for a specific data type
        /// </summary>
        public static Color GetColor(DataType dataType)
        {
            return dataType switch
            {
                DataType.Execution => Execution,
                DataType.Number => Number,
                DataType.Boolean => BooleanTrue, // Default to green for boolean
                DataType.Device => Device,
                DataType.String => String,
                _ => Color.FromRgb(0x80, 0x80, 0x80) // Gray fallback
            };
        }

        /// <summary>
        /// Get the color for a boolean pin based on its value
        /// </summary>
        public static Color GetBooleanColor(bool value)
        {
            return value ? BooleanTrue : BooleanFalse;
        }

        /// <summary>
        /// Get a brush for the specified data type
        /// </summary>
        public static SolidColorBrush GetBrush(DataType dataType)
        {
            return new SolidColorBrush(GetColor(dataType));
        }

        /// <summary>
        /// Get a brush for a boolean value
        /// </summary>
        public static SolidColorBrush GetBooleanBrush(bool value)
        {
            return new SolidColorBrush(GetBooleanColor(value));
        }
    }
}
