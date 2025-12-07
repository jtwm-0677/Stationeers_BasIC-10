using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Abstract base class for all visual scripting nodes
    /// </summary>
    public abstract class NodeBase
    {
        #region Events

        /// <summary>
        /// Event raised when a property value changes (for UI updates)
        /// </summary>
        public event EventHandler<NodePropertyChangedEventArgs>? PropertyValueChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Type identifier for serialization and factory creation
        /// </summary>
        public abstract string NodeType { get; }

        /// <summary>
        /// X position on canvas
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y position on canvas
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Width of the node (can be overridden)
        /// </summary>
        public virtual double Width { get; set; } = 200;

        /// <summary>
        /// Height of the node (calculated from content)
        /// </summary>
        public virtual double Height { get; set; } = 100;

        /// <summary>
        /// Whether the user has manually resized this node.
        /// When true, auto-sizing will not override the dimensions.
        /// </summary>
        public bool UserResized { get; set; } = false;

        /// <summary>
        /// Display label for the node
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Input pins for this node
        /// </summary>
        public List<NodePin> InputPins { get; set; } = new();

        /// <summary>
        /// Output pins for this node
        /// </summary>
        public List<NodePin> OutputPins { get; set; } = new();

        /// <summary>
        /// Whether this node is currently selected
        /// </summary>
        [JsonIgnore]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Category for grouping in palette
        /// </summary>
        public abstract string Category { get; }

        /// <summary>
        /// Icon identifier (optional, for visual representation)
        /// </summary>
        public virtual string? Icon { get; } = null;

        #endregion

        #region Constructor

        protected NodeBase()
        {
            Id = Guid.NewGuid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the node's pins and default values
        /// Called after construction or deserialization
        /// </summary>
        public virtual void Initialize()
        {
            // Set parent references for all pins
            foreach (var pin in InputPins)
            {
                pin.ParentNode = this;
            }
            foreach (var pin in OutputPins)
            {
                pin.ParentNode = this;
            }
        }

        /// <summary>
        /// Get the bounding rectangle for this node
        /// </summary>
        public Rect GetBounds()
        {
            return new Rect(X, Y, Width, Height);
        }

        /// <summary>
        /// Test if a point hits this node
        /// </summary>
        /// <param name="x">X coordinate in canvas space</param>
        /// <param name="y">Y coordinate in canvas space</param>
        /// <returns>True if the point is inside the node</returns>
        public virtual bool HitTest(double x, double y)
        {
            return x >= X && x <= X + Width && y >= Y && y <= Y + Height;
        }

        /// <summary>
        /// Test if a point hits a specific pin
        /// </summary>
        /// <param name="x">X coordinate in canvas space</param>
        /// <param name="y">Y coordinate in canvas space</param>
        /// <param name="hitPin">The pin that was hit, if any</param>
        /// <returns>True if a pin was hit</returns>
        public virtual bool HitTestPin(double x, double y, out NodePin? hitPin)
        {
            const double hitRadius = 10.0; // Slightly larger for easier clicking

            // Test input pins
            for (int i = 0; i < InputPins.Count; i++)
            {
                var (pinX, pinY) = InputPins[i].GetPosition(i);
                double dx = x - (X + pinX);
                double dy = y - (Y + pinY);
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= hitRadius * hitRadius)
                {
                    hitPin = InputPins[i];
                    return true;
                }
            }

            // Test output pins
            for (int i = 0; i < OutputPins.Count; i++)
            {
                var (pinX, pinY) = OutputPins[i].GetPosition(i);
                double dx = x - (X + pinX);
                double dy = y - (Y + pinY);
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared <= hitRadius * hitRadius)
                {
                    hitPin = OutputPins[i];
                    return true;
                }
            }

            hitPin = null;
            return false;
        }

        /// <summary>
        /// Calculate the minimum height needed for this node
        /// based on the number of pins and body content
        /// </summary>
        protected virtual double CalculateMinHeight()
        {
            const double headerHeight = 32.0;
            const double pinSpacing = 24.0;
            const double bottomPadding = 16.0;

            int maxPins = Math.Max(InputPins.Count, OutputPins.Count);
            double pinsHeight = maxPins > 0 ? pinSpacing + (maxPins * pinSpacing) : 0;

            return headerHeight + pinsHeight + bottomPadding;
        }

        /// <summary>
        /// Add an input pin to this node
        /// </summary>
        protected NodePin AddInputPin(string name, DataType dataType)
        {
            var pin = new NodePin(name, PinType.Input, dataType)
            {
                ParentNode = this
            };
            InputPins.Add(pin);
            return pin;
        }

        /// <summary>
        /// Add an output pin to this node
        /// </summary>
        protected NodePin AddOutputPin(string name, DataType dataType)
        {
            var pin = new NodePin(name, PinType.Output, dataType)
            {
                ParentNode = this
            };
            OutputPins.Add(pin);
            return pin;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Validate the node's configuration
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public abstract bool Validate(out string errorMessage);

        /// <summary>
        /// Generate BASIC code for this node
        /// </summary>
        /// <returns>Generated BASIC code string</returns>
        public abstract string GenerateCode();

        #endregion

        #region Editable Properties

        /// <summary>
        /// Get the list of editable properties for this node.
        /// Override in derived classes to expose editable properties.
        /// </summary>
        /// <returns>List of editable properties</returns>
        public virtual List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>();
        }

        /// <summary>
        /// Raise the PropertyValueChanged event
        /// </summary>
        protected void OnPropertyValueChanged(string propertyName, string newValue)
        {
            PropertyValueChanged?.Invoke(this, new NodePropertyChangedEventArgs(propertyName, newValue));
        }

        #endregion
    }

    /// <summary>
    /// Event args for node property changes
    /// </summary>
    public class NodePropertyChangedEventArgs : EventArgs
    {
        public string PropertyName { get; }
        public string NewValue { get; }

        public NodePropertyChangedEventArgs(string propertyName, string newValue)
        {
            PropertyName = propertyName;
            NewValue = newValue;
        }
    }
}
