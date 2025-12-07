using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BasicToMips.UI.VisualScripting.Nodes;
using BasicToMips.UI.VisualScripting.Nodes.FlowControl;
using BasicToMips.UI.VisualScripting.Nodes.Subroutines;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Node palette for adding nodes to the visual scripting canvas
    /// </summary>
    public partial class NodePalette : System.Windows.Controls.UserControl
    {
        private readonly NodeFactory _nodeFactory;
        private List<NodeCategory> _allCategories = new();

        /// <summary>
        /// Event raised when a node type is selected for creation
        /// </summary>
        public event EventHandler<NodeTypeSelectedEventArgs>? NodeTypeSelected;

        public NodePalette()
        {
            InitializeComponent();

            _nodeFactory = new NodeFactory();
            RegisterAllNodeTypes();
            LoadCategories();
        }

        private void RegisterAllNodeTypes()
        {
            // Register all node types with the factory
            // Variables
            _nodeFactory.RegisterNodeType<VariableNode>();
            _nodeFactory.RegisterNodeType<ConstantNode>();
            _nodeFactory.RegisterNodeType<ConstNode>();
            _nodeFactory.RegisterNodeType<DefineNode>();

            // Devices
            _nodeFactory.RegisterNodeType<PinDeviceNode>();
            _nodeFactory.RegisterNodeType<NamedDeviceNode>();
            _nodeFactory.RegisterNodeType<ThisDeviceNode>();
            _nodeFactory.RegisterNodeType<ReadPropertyNode>();
            _nodeFactory.RegisterNodeType<WritePropertyNode>();
            _nodeFactory.RegisterNodeType<SlotReadNode>();
            _nodeFactory.RegisterNodeType<SlotWriteNode>();
            _nodeFactory.RegisterNodeType<BatchReadNode>();
            _nodeFactory.RegisterNodeType<BatchWriteNode>();

            // Math
            _nodeFactory.RegisterNodeType<AddNode>();
            _nodeFactory.RegisterNodeType<SubtractNode>();
            _nodeFactory.RegisterNodeType<MultiplyNode>();
            _nodeFactory.RegisterNodeType<DivideNode>();
            _nodeFactory.RegisterNodeType<ModuloNode>();
            _nodeFactory.RegisterNodeType<PowerNode>();
            _nodeFactory.RegisterNodeType<NegateNode>();
            _nodeFactory.RegisterNodeType<MinMaxNode>();
            _nodeFactory.RegisterNodeType<MathFunctionNode>();

            // Logic
            _nodeFactory.RegisterNodeType<CompareNode>();
            _nodeFactory.RegisterNodeType<AndNode>();
            _nodeFactory.RegisterNodeType<OrNode>();
            _nodeFactory.RegisterNodeType<NotNode>();

            // Bitwise
            _nodeFactory.RegisterNodeType<BitwiseNode>();
            _nodeFactory.RegisterNodeType<BitwiseNotNode>();
            _nodeFactory.RegisterNodeType<ShiftNode>();

            // Arrays
            _nodeFactory.RegisterNodeType<ArrayNode>();
            _nodeFactory.RegisterNodeType<ArrayAccessNode>();
            _nodeFactory.RegisterNodeType<ArrayAssignNode>();

            // Stack
            _nodeFactory.RegisterNodeType<PushNode>();
            _nodeFactory.RegisterNodeType<PopNode>();
            _nodeFactory.RegisterNodeType<PeekNode>();

            // Trigonometry
            _nodeFactory.RegisterNodeType<TrigNode>();
            _nodeFactory.RegisterNodeType<Atan2Node>();
            _nodeFactory.RegisterNodeType<ExpLogNode>();

            // Advanced
            _nodeFactory.RegisterNodeType<HashNode>();
            _nodeFactory.RegisterNodeType<IncrementNode>();
            _nodeFactory.RegisterNodeType<CompoundAssignNode>();

            // Comments
            _nodeFactory.RegisterNodeType<CommentNode>();

            // Flow Control
            _nodeFactory.RegisterNodeType<EntryPointNode>();
            _nodeFactory.RegisterNodeType<IfNode>();
            _nodeFactory.RegisterNodeType<WhileNode>();
            _nodeFactory.RegisterNodeType<ForNode>();
            _nodeFactory.RegisterNodeType<DoUntilNode>();
            _nodeFactory.RegisterNodeType<BreakNode>();
            _nodeFactory.RegisterNodeType<ContinueNode>();
            _nodeFactory.RegisterNodeType<LabelNode>();
            _nodeFactory.RegisterNodeType<GotoNode>();
            _nodeFactory.RegisterNodeType<GosubNode>();
            _nodeFactory.RegisterNodeType<ReturnNode>();
            _nodeFactory.RegisterNodeType<SelectCaseNode>();
            _nodeFactory.RegisterNodeType<YieldNode>();
            _nodeFactory.RegisterNodeType<SleepNode>();
            _nodeFactory.RegisterNodeType<EndNode>();

            // Subroutines
            _nodeFactory.RegisterNodeType<SubDefinitionNode>();
            _nodeFactory.RegisterNodeType<CallSubNode>();
            _nodeFactory.RegisterNodeType<ExitSubNode>();
            _nodeFactory.RegisterNodeType<FunctionDefinitionNode>();
            _nodeFactory.RegisterNodeType<CallFunctionNode>();
            _nodeFactory.RegisterNodeType<ExitFunctionNode>();
            _nodeFactory.RegisterNodeType<SetReturnValueNode>();
        }

        private void LoadCategories()
        {
            var nodesByCategory = _nodeFactory.GetNodeTypesByCategory();

            _allCategories = nodesByCategory
                .OrderBy(c => GetCategoryOrder(c.Key))
                .Select(c => new NodeCategory
                {
                    CategoryName = c.Key,
                    Nodes = c.Value.OrderBy(n => n.DisplayName).ToList()
                })
                .ToList();

            CategoriesPanel.ItemsSource = _allCategories;
        }

        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "Flow Control" => 0,
                "Variables" => 1,
                "Devices" => 2,
                "Basic Math" => 3,
                "Logic" => 4,
                "Comparison" => 5,
                "Subroutines" => 6,
                "Arrays" => 7,
                "Math Functions" => 8,
                "Stack" => 9,
                "Bitwise" => 10,
                "Trigonometry" => 11,
                "Advanced" => 12,
                "Comments" => 13,
                _ => 99
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                CategoriesPanel.ItemsSource = _allCategories;
                return;
            }

            // Filter categories and nodes
            var filtered = _allCategories
                .Select(c => new NodeCategory
                {
                    CategoryName = c.CategoryName,
                    Nodes = c.Nodes
                        .Where(n => n.DisplayName.ToLowerInvariant().Contains(searchText) ||
                                    n.TypeName.ToLowerInvariant().Contains(searchText))
                        .ToList()
                })
                .Where(c => c.Nodes.Count > 0)
                .ToList();

            CategoriesPanel.ItemsSource = filtered;
        }

        private void NodeItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is NodeTypeInfo nodeInfo)
            {
                NodeTypeSelected?.Invoke(this, new NodeTypeSelectedEventArgs(nodeInfo.TypeName, nodeInfo.DisplayName));
            }
        }

        private void NodeItem_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
            }
        }

        private void NodeItem_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = (Brush)FindResource("TertiaryBackgroundBrush");
            }
        }

        /// <summary>
        /// Create a node instance by type name
        /// </summary>
        public NodeBase? CreateNode(string typeName)
        {
            return _nodeFactory.CreateNode(typeName);
        }

        /// <summary>
        /// Get the node factory
        /// </summary>
        public NodeFactory Factory => _nodeFactory;
    }

    /// <summary>
    /// Category grouping for the palette
    /// </summary>
    public class NodeCategory
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<NodeTypeInfo> Nodes { get; set; } = new();
    }

    /// <summary>
    /// Event args for node type selection
    /// </summary>
    public class NodeTypeSelectedEventArgs : EventArgs
    {
        public string TypeName { get; }
        public string DisplayName { get; }

        public NodeTypeSelectedEventArgs(string typeName, string displayName)
        {
            TypeName = typeName;
            DisplayName = displayName;
        }
    }
}
