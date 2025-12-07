using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using BasicToMips.UI.VisualScripting.Services;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// Visual control for rendering a node on the canvas
    /// </summary>
    public partial class NodeControl : System.Windows.Controls.UserControl
    {
        private NodeBase? _node;
        private bool _isDragging;
        private Point _dragStartPoint;
        private readonly Dictionary<string, FrameworkElement> _propertyControls = new();

        public NodeBase? Node
        {
            get => _node;
            set
            {
                // Unsubscribe from previous node
                if (_node != null)
                {
                    _node.PropertyValueChanged -= Node_PropertyValueChanged;
                }

                _node = value;
                if (_node != null)
                {
                    _node.PropertyValueChanged += Node_PropertyValueChanged;
                    Render();
                }
            }
        }

        public event EventHandler<NodeDragEventArgs>? NodeDragStarted;
        public event EventHandler<NodeDragEventArgs>? NodeDragging;
        public event EventHandler<NodeDragEventArgs>? NodeDragEnded;
        public event EventHandler<PinClickEventArgs>? PinClicked;
        public event EventHandler? PropertyChanged;

        public NodeControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Render the node based on its data
        /// </summary>
        private void Render()
        {
            if (_node == null) return;

            // Set label
            LabelText.Text = _node.Label;

            // Set header color based on category
            HeaderBorder.Background = GetCategoryColor(_node.Category);

            // Set icon if present
            if (!string.IsNullOrEmpty(_node.Icon))
            {
                IconText.Text = _node.Icon;
                IconText.Visibility = Visibility.Visible;
            }

            // Apply selection effect
            MainBorder.Effect = _node.IsSelected ? (System.Windows.Media.Effects.Effect)FindResource("SelectionGlow") : null;
            MainBorder.BorderThickness = _node.IsSelected ? new Thickness(2) : new Thickness(1);
            MainBorder.BorderBrush = _node.IsSelected
                ? new SolidColorBrush(Color.FromRgb(0x4A, 0x9E, 0xFF))
                : new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D));

            // Render pins
            RenderPins();

            // Render editable properties
            RenderEditableProperties();

            // Set size after rendering content
            Width = _node.Width;
            Height = _node.Height;
        }

        /// <summary>
        /// Render input and output pins
        /// </summary>
        private void RenderPins()
        {
            if (_node == null) return;

            // Clear existing pins
            InputPinsPanel.Children.Clear();
            OutputPinsPanel.Children.Clear();

            // Render input pins
            for (int i = 0; i < _node.InputPins.Count; i++)
            {
                var pin = _node.InputPins[i];
                var pinControl = CreatePinControl(pin, i, true);
                InputPinsPanel.Children.Add(pinControl);
            }

            // Render output pins
            for (int i = 0; i < _node.OutputPins.Count; i++)
            {
                var pin = _node.OutputPins[i];
                var pinControl = CreatePinControl(pin, i, false);
                OutputPinsPanel.Children.Add(pinControl);
            }
        }

        /// <summary>
        /// Create a visual control for a pin
        /// </summary>
        private FrameworkElement CreatePinControl(NodePin pin, int index, bool isInput)
        {
            var container = new Grid
            {
                Height = 24,
                Margin = new Thickness(0, index == 0 ? 0 : 0, 0, 0)
            };

            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Pin circle
            var circle = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = PinColors.GetBrush(pin.DataType),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = pin.IsConnected ? 2 : 1,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = isInput ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pin
            };

            circle.MouseDown += PinCircle_MouseDown;

            // Pin label
            var label = new TextBlock
            {
                Text = pin.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(isInput ? 4 : 0, 0, isInput ? 0 : 4, 0),
                TextAlignment = isInput ? TextAlignment.Left : TextAlignment.Right
            };

            if (isInput)
            {
                Grid.SetColumn(circle, 0);
                Grid.SetColumn(label, 1);
            }
            else
            {
                Grid.SetColumn(label, 0);
                Grid.SetColumn(circle, 1);
            }

            container.Children.Add(circle);
            container.Children.Add(label);

            return container;
        }

        /// <summary>
        /// Render editable property controls in the body area
        /// </summary>
        private void RenderEditableProperties()
        {
            if (_node == null) return;

            _propertyControls.Clear();
            var properties = _node.GetEditableProperties();

            if (properties.Count == 0)
            {
                BodyContent.Content = null;
                return;
            }

            var panel = new StackPanel { Margin = new Thickness(0) };

            foreach (var prop in properties)
            {
                var control = CreatePropertyControl(prop);
                if (control != null)
                {
                    _propertyControls[prop.PropertyName] = control;
                    panel.Children.Add(control);
                }
            }

            BodyContent.Content = panel;

            // Only auto-size height if user hasn't manually resized
            if (!_node.UserResized)
            {
                // Recalculate node height based on content (shorter header = 24px)
                var baseHeight = 24 + 12; // Header + padding
                var pinsHeight = Math.Max(_node.InputPins.Count, _node.OutputPins.Count) * 20 + 8;
                var propertiesHeight = properties.Count * 44 + 8; // Each property ~44px (label + input)
                var contentHeight = Math.Max(pinsHeight, propertiesHeight);

                // Set minimum height but allow for scrolling if needed
                _node.Height = Math.Max(80, baseHeight + contentHeight);
            }
        }

        /// <summary>
        /// Create an input control for a property
        /// </summary>
        private FrameworkElement? CreatePropertyControl(NodeProperty prop)
        {
            var container = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Margin = new Thickness(0, 2, 0, 2)
            };

            // Add label for the property
            var label = new TextBlock
            {
                Text = prop.Name,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                FontSize = 10,
                Margin = new Thickness(0, 0, 0, 2)
            };
            container.Children.Add(label);

            System.Windows.Controls.Control inputControl;

            switch (prop.Type)
            {
                case PropertyType.Text:
                    var textBox = new TextBox
                    {
                        Text = prop.Value,
                        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                        Foreground = new SolidColorBrush(Colors.White),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(4, 2, 4, 2),
                        FontSize = 11,
                        IsReadOnly = prop.IsReadOnly,
                        Focusable = true,
                        IsTabStop = true,
                        IsEnabled = true,
                        IsHitTestVisible = true,
                        CaretBrush = new SolidColorBrush(Colors.White)
                    };
                    // Handle mouse events to ensure focus and prevent canvas interference
                    textBox.PreviewMouseDown += TextBox_PreviewMouseDown;
                    textBox.MouseDown += TextBox_MouseDown; // Prevent bubbling to canvas
                    textBox.GotFocus += (s, e) => textBox.SelectAll();
                    textBox.TextChanged += (s, e) =>
                    {
                        prop.Value = textBox.Text;
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                    };
                    if (!string.IsNullOrEmpty(prop.Tooltip))
                        textBox.ToolTip = prop.Tooltip;
                    inputControl = textBox;
                    break;

                case PropertyType.MultiLine:
                    var multiLineBox = new TextBox
                    {
                        Text = prop.Value,
                        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                        Foreground = new SolidColorBrush(Colors.White),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(4, 2, 4, 2),
                        FontSize = 11,
                        AcceptsReturn = true,
                        TextWrapping = TextWrapping.Wrap,
                        MinHeight = 60,
                        MaxHeight = 400,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        IsReadOnly = prop.IsReadOnly,
                        Focusable = true,
                        IsTabStop = true,
                        IsEnabled = true,
                        IsHitTestVisible = true,
                        CaretBrush = new SolidColorBrush(Colors.White)
                    };
                    // Handle mouse events to ensure focus and prevent canvas interference
                    multiLineBox.PreviewMouseDown += TextBox_PreviewMouseDown;
                    multiLineBox.MouseDown += TextBox_MouseDown;
                    multiLineBox.TextChanged += (s, e) =>
                    {
                        prop.Value = multiLineBox.Text;
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                    };
                    if (!string.IsNullOrEmpty(prop.Tooltip))
                        multiLineBox.ToolTip = prop.Tooltip;
                    inputControl = multiLineBox;
                    break;

                case PropertyType.Number:
                    var numberBox = new TextBox
                    {
                        Text = prop.Value,
                        Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                        Foreground = new SolidColorBrush(Colors.White),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(4, 2, 4, 2),
                        FontSize = 11,
                        IsReadOnly = prop.IsReadOnly,
                        Focusable = true,
                        IsTabStop = true,
                        IsEnabled = true,
                        IsHitTestVisible = true,
                        CaretBrush = new SolidColorBrush(Colors.White)
                    };
                    // Handle mouse events to ensure focus and prevent canvas interference
                    numberBox.PreviewMouseDown += TextBox_PreviewMouseDown;
                    numberBox.MouseDown += TextBox_MouseDown;
                    numberBox.GotFocus += (s, e) => numberBox.SelectAll();
                    numberBox.PreviewTextInput += (s, e) =>
                    {
                        // Allow only numeric input (with decimal point and minus)
                        var text = numberBox.Text + e.Text;
                        e.Handled = !IsValidNumberInput(text);
                    };
                    numberBox.TextChanged += (s, e) =>
                    {
                        prop.Value = numberBox.Text;
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                    };
                    if (!string.IsNullOrEmpty(prop.Tooltip))
                        numberBox.ToolTip = prop.Tooltip;
                    inputControl = numberBox;
                    break;

                case PropertyType.Boolean:
                    var checkBox = new CheckBox
                    {
                        IsChecked = prop.Value.ToLower() == "true",
                        Foreground = new SolidColorBrush(Colors.White),
                        IsEnabled = !prop.IsReadOnly,
                        Focusable = true,
                        IsTabStop = true,
                        IsHitTestVisible = true
                    };
                    // Handle mouse events to prevent canvas interference
                    checkBox.PreviewMouseDown += CheckBox_PreviewMouseDown;
                    checkBox.MouseDown += (s, e) => e.Handled = true;
                    checkBox.Checked += (s, e) =>
                    {
                        prop.Value = "true";
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                    };
                    checkBox.Unchecked += (s, e) =>
                    {
                        prop.Value = "false";
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                    };
                    if (!string.IsNullOrEmpty(prop.Tooltip))
                        checkBox.ToolTip = prop.Tooltip;
                    inputControl = checkBox;
                    break;

                case PropertyType.Dropdown:
                    var comboBox = new ComboBox
                    {
                        // Light background with dark text for maximum readability
                        Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                        Foreground = new SolidColorBrush(Colors.Black),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
                        BorderThickness = new Thickness(1),
                        FontSize = 11,
                        IsEnabled = !prop.IsReadOnly,
                        Focusable = true,
                        IsTabStop = true,
                        IsHitTestVisible = true
                    };

                    // Style the dropdown items for readability (dark text on light background)
                    var itemStyle = new Style(typeof(ComboBoxItem));
                    itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))));
                    itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(Colors.Black)));
                    itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(4, 2, 4, 2)));
                    comboBox.ItemContainerStyle = itemStyle;

                    // Handle mouse events to prevent canvas interference
                    comboBox.PreviewMouseDown += ComboBox_PreviewMouseDown;
                    comboBox.MouseDown += (s, e) => e.Handled = true;
                    if (prop.Options != null)
                    {
                        foreach (var option in prop.Options)
                        {
                            comboBox.Items.Add(option);
                        }
                        comboBox.SelectedItem = prop.Value;
                    }
                    comboBox.SelectionChanged += (s, e) =>
                    {
                        if (comboBox.SelectedItem is string selected)
                        {
                            prop.Value = selected;
                            PropertyChanged?.Invoke(this, EventArgs.Empty);
                        }
                    };
                    if (!string.IsNullOrEmpty(prop.Tooltip))
                        comboBox.ToolTip = prop.Tooltip;
                    inputControl = comboBox;
                    break;

                default:
                    return null;
            }

            container.Children.Add(inputControl);
            return container;
        }

        /// <summary>
        /// Check if input is a valid number (allowing partial input like "-" or "3.")
        /// </summary>
        private bool IsValidNumberInput(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            if (text == "-" || text == ".") return true;
            return double.TryParse(text, out _);
        }

        /// <summary>
        /// Handle property value changes from the node
        /// </summary>
        private void Node_PropertyValueChanged(object? sender, NodePropertyChangedEventArgs e)
        {
            // Update the corresponding control if it exists
            if (_propertyControls.TryGetValue(e.PropertyName, out var container))
            {
                // Find the actual input control in the container
                if (container is StackPanel panel && panel.Children.Count > 1)
                {
                    var inputControl = panel.Children[1];
                    if (inputControl is TextBox textBox && textBox.Text != e.NewValue)
                    {
                        textBox.Text = e.NewValue;
                    }
                    else if (inputControl is CheckBox checkBox)
                    {
                        checkBox.IsChecked = e.NewValue.ToLower() == "true";
                    }
                    else if (inputControl is ComboBox comboBox)
                    {
                        comboBox.SelectedItem = e.NewValue;
                    }
                }
            }

            // Re-render to update label if needed
            if (_node != null)
            {
                LabelText.Text = _node.Label;
            }
        }

        /// <summary>
        /// Get color for a category based on current syntax color settings.
        /// Uses NodeColorProvider to respect user's accessibility preferences.
        /// </summary>
        private Brush GetCategoryColor(string category)
        {
            return NodeColorProvider.GetCategoryColor(category);
        }

        /// <summary>
        /// Update the header color to reflect current syntax color settings.
        /// Called when the color theme changes for accessibility.
        /// </summary>
        public void UpdateHeaderColor()
        {
            if (_node != null)
            {
                HeaderBorder.Background = GetCategoryColor(_node.Category);
            }
        }

        #region Event Handlers

        private void PinCircle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is NodePin pin)
            {
                PinClicked?.Invoke(this, new PinClickEventArgs(pin, _node!));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handle TextBox PreviewMouseDown to ensure it receives focus.
        /// Uses Dispatcher to handle focus after the event routing completes.
        /// </summary>
        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsReadOnly)
            {
                // First, activate this UserControl
                this.Focus();

                // Then focus the TextBox
                textBox.Focus();

                // Use Dispatcher to ensure keyboard focus after event routing completes
                // Don't manually set caret - let the TextBox handle click position naturally
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Keyboard.Focus(textBox);
                }), System.Windows.Threading.DispatcherPriority.Input);

                // DON'T mark as handled - let TextBox process click for proper caret placement
                // The bubbling MouseDown handler will still mark it handled for Canvas
            }
        }

        /// <summary>
        /// Handle TextBox MouseDown to prevent the event from bubbling to the Canvas.
        /// This stops the Canvas from capturing mouse and starting box selection.
        /// </summary>
        private void TextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Mark as handled to prevent Canvas from receiving this event
            e.Handled = true;
        }

        /// <summary>
        /// Handle CheckBox PreviewMouseDown to ensure it receives focus and toggles correctly.
        /// </summary>
        private void CheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.IsEnabled)
            {
                // Focus this UserControl first
                this.Focus();

                // Focus the CheckBox
                checkBox.Focus();

                // Use Dispatcher to ensure proper focus and toggle
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Keyboard.Focus(checkBox);
                }), System.Windows.Threading.DispatcherPriority.Input);

                // Don't mark as handled - let the CheckBox toggle naturally
            }
        }

        /// <summary>
        /// Handle ComboBox PreviewMouseDown to ensure it can open its dropdown.
        /// </summary>
        private void ComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.IsEnabled)
            {
                // Focus this UserControl first
                this.Focus();

                // Focus the ComboBox
                comboBox.Focus();

                // Use Dispatcher to ensure proper focus
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Keyboard.Focus(comboBox);
                }), System.Windows.Threading.DispatcherPriority.Input);

                // Don't mark as handled - let the ComboBox open naturally
            }
        }

        /// <summary>
        /// Handle mouse down on header - start dragging the node.
        /// Only the header is draggable, allowing body controls to work normally.
        /// </summary>
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _node != null)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                HeaderBorder.CaptureMouse();

                NodeDragStarted?.Invoke(this, new NodeDragEventArgs(_node, _dragStartPoint));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Find parent of a specific type in the visual tree
        /// </summary>
        private T? FindParentOfType<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T found)
                    return found;
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }

        /// <summary>
        /// Check if the element or any of its ancestors is an input control
        /// </summary>
        private bool IsInputControl(DependencyObject? element)
        {
            while (element != null)
            {
                if (element is TextBox || element is ComboBox || element is CheckBox ||
                    element is System.Windows.Controls.Primitives.TextBoxBase ||
                    element is System.Windows.Controls.Primitives.Selector)
                {
                    return true;
                }
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        /// <summary>
        /// Handle mouse move on header - drag the node.
        /// </summary>
        private void Header_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _node != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPoint = e.GetPosition(this);
                var delta = new Point(currentPoint.X - _dragStartPoint.X, currentPoint.Y - _dragStartPoint.Y);

                NodeDragging?.Invoke(this, new NodeDragEventArgs(_node, delta));
            }
        }

        /// <summary>
        /// Handle mouse up on header - end dragging.
        /// </summary>
        private void Header_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && e.ChangedButton == MouseButton.Left && _node != null)
            {
                _isDragging = false;
                HeaderBorder.ReleaseMouseCapture();

                NodeDragEnded?.Invoke(this, new NodeDragEventArgs(_node, new Point(0, 0)));
                e.Handled = true;
            }
        }

        private void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_node == null) return;

            // Calculate new size
            var newWidth = Math.Max(120, Width + e.HorizontalChange);
            var newHeight = Math.Max(60, Height + e.VerticalChange);

            // Update node and control size
            _node.Width = newWidth;
            _node.Height = newHeight;
            _node.UserResized = true; // Mark as user-resized to prevent auto-sizing
            Width = newWidth;
            Height = newHeight;

            // Notify that property changed (for wire updates)
            PropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        /// <summary>
        /// Update the visual state (e.g., selection)
        /// </summary>
        public void UpdateVisualState()
        {
            Render();
        }
    }

    #region Event Args Classes

    public class NodeDragEventArgs : EventArgs
    {
        public NodeBase Node { get; }
        public Point Delta { get; }

        public NodeDragEventArgs(NodeBase node, Point delta)
        {
            Node = node;
            Delta = delta;
        }
    }

    public class PinClickEventArgs : EventArgs
    {
        public NodePin Pin { get; }
        public NodeBase ParentNode { get; }

        public PinClickEventArgs(NodePin pin, NodeBase parentNode)
        {
            Pin = pin;
            ParentNode = parentNode;
        }
    }

    #endregion
}
