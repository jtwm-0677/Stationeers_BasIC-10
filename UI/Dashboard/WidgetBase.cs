using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfGrid = System.Windows.Controls.Grid;
using WpfBorder = System.Windows.Controls.Border;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfButton = System.Windows.Controls.Button;

namespace BasicToMips.UI.Dashboard;

/// <summary>
/// Abstract base class for all dashboard widgets
/// </summary>
public abstract class WidgetBase : WpfUserControl
{
    // Properties for grid positioning
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "Widget";
    public int GridRow { get; set; }
    public int GridColumn { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;

    // Header controls
    protected WpfBorder? HeaderBorder;
    protected WpfTextBlock? TitleText;
    protected WpfButton? CloseButton;
    protected WpfGrid? ContentGrid;

    // Dragging state
    private bool _isDragging;
    private Point _dragStartPoint;

    protected WidgetBase()
    {
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildWidgetFrame();
    }

    /// <summary>
    /// Builds the standard widget frame with header and close button
    /// </summary>
    private void BuildWidgetFrame()
    {
        var mainGrid = new WpfGrid();
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto }); // Header
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

        // Header
        HeaderBorder = new WpfBorder
        {
            Background = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)),
            Padding = new Thickness(8, 6, 8, 6),
            Cursor = System.Windows.Input.Cursors.SizeAll
        };
        System.Windows.Controls.Grid.SetRow(HeaderBorder, 0);

        var headerGrid = new WpfGrid();
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

        TitleText = new WpfTextBlock
        {
            Text = Title,
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = Brushes.White
        };
        System.Windows.Controls.Grid.SetColumn(TitleText, 0);

        CloseButton = new WpfButton
        {
            Content = "✕",
            Width = 20,
            Height = 20,
            FontSize = 12,
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4)),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Close widget"
        };
        System.Windows.Controls.Grid.SetColumn(CloseButton, 1);
        CloseButton.Click += OnCloseButtonClick;

        // Hover effect for close button
        CloseButton.MouseEnter += (s, e) => CloseButton.Foreground = Brushes.White;
        CloseButton.MouseLeave += (s, e) => CloseButton.Foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));

        headerGrid.Children.Add(TitleText);
        headerGrid.Children.Add(CloseButton);
        HeaderBorder.Child = headerGrid;

        // Header drag events
        HeaderBorder.MouseLeftButtonDown += OnHeaderMouseDown;
        HeaderBorder.MouseLeftButtonUp += OnHeaderMouseUp;
        HeaderBorder.MouseMove += OnHeaderMouseMove;

        // Content area
        ContentGrid = new WpfGrid
        {
            Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D))
        };
        System.Windows.Controls.Grid.SetRow(ContentGrid, 1);

        mainGrid.Children.Add(HeaderBorder);
        mainGrid.Children.Add(ContentGrid);

        // Set main content
        Content = new WpfBorder
        {
            Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = mainGrid
        };

        // Call derived class to build content
        Render();
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            HeaderBorder?.CaptureMouse();
        }
    }

    private void OnHeaderMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && _isDragging)
        {
            _isDragging = false;
            HeaderBorder?.ReleaseMouseCapture();
        }
    }

    private void OnHeaderMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(Parent as UIElement);
            // The parent DashboardGrid will handle the actual repositioning
            DragMoved?.Invoke(this, currentPosition);
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Override to render widget content into ContentGrid
    /// </summary>
    public virtual void Render()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Called when widget data needs to be updated
    /// </summary>
    public virtual void OnUpdate()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Save widget-specific state
    /// </summary>
    public virtual object SaveState()
    {
        return new { }; // Override in derived classes
    }

    /// <summary>
    /// Load widget-specific state
    /// </summary>
    public virtual void LoadState(object state)
    {
        // Override in derived classes
    }

    // Events
    public event EventHandler? CloseRequested;
    public event EventHandler<Point>? DragMoved;
}
