using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Types of editable properties for nodes
    /// </summary>
    public enum PropertyType
    {
        Text,       // Single-line text input
        MultiLine,  // Multi-line text input (for comments)
        Number,     // Numeric input
        Boolean,    // Checkbox
        Dropdown    // Dropdown selection
    }

    /// <summary>
    /// Represents an editable property on a node
    /// </summary>
    public class NodeProperty : INotifyPropertyChanged
    {
        private string _value = string.Empty;
        private readonly Action<string>? _onValueChanged;

        /// <summary>
        /// Display name for the property
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Property name on the node (for binding)
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Type of input control to display
        /// </summary>
        public PropertyType Type { get; }

        /// <summary>
        /// Current value as string
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                    _onValueChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Placeholder text for text inputs
        /// </summary>
        public string Placeholder { get; set; } = string.Empty;

        /// <summary>
        /// Tooltip/help text
        /// </summary>
        public string Tooltip { get; set; } = string.Empty;

        /// <summary>
        /// Options for dropdown type
        /// </summary>
        public string[]? Options { get; set; }

        /// <summary>
        /// Minimum value for number type
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// Maximum value for number type
        /// </summary>
        public double? MaxValue { get; set; }

        /// <summary>
        /// Whether the property is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public NodeProperty(string name, string propertyName, PropertyType type, Action<string>? onValueChanged = null)
        {
            Name = name;
            PropertyName = propertyName;
            Type = type;
            _onValueChanged = onValueChanged;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
