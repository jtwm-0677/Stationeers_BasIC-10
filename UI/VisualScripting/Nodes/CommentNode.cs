using System;
using System.Collections.Generic;

namespace BasicToMips.UI.VisualScripting.Nodes
{
    /// <summary>
    /// A comment/annotation node for documentation
    /// </summary>
    public class CommentNode : NodeBase
    {
        public override string NodeType => "Comment";
        public override string Category => "Comments";
        public override string? Icon => "💬";

        private string _commentText = "Add your comment here...";

        /// <summary>
        /// Comment text content
        /// </summary>
        public string CommentText
        {
            get => _commentText;
            set
            {
                _commentText = value;
                OnPropertyValueChanged(nameof(CommentText), value);
            }
        }

        /// <summary>
        /// Comment color/theme
        /// </summary>
        public string CommentColor { get; set; } = "#808080"; // Gray

        public CommentNode()
        {
            Label = "Comment";
            Width = 200;
            Height = 100;

            // Comments don't have execution pins
            // They're purely for documentation
        }

        public override List<NodeProperty> GetEditableProperties()
        {
            return new List<NodeProperty>
            {
                new NodeProperty("Comment", nameof(CommentText), PropertyType.MultiLine, value => CommentText = value)
                {
                    Value = CommentText,
                    Placeholder = "Enter your comment...",
                    Tooltip = "Comment text (will be output as REM statements)"
                }
            };
        }

        public override void Initialize()
        {
            base.Initialize();

            // Comments don't need pins
            InputPins.Clear();
            OutputPins.Clear();
        }

        public override bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;
            return true; // Comments are always valid
        }

        public override string GenerateCode()
        {
            if (string.IsNullOrWhiteSpace(CommentText))
                return string.Empty;

            // Handle multi-line comments
            var lines = CommentText.Split('\n');
            var result = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var trimmedLine = line.TrimEnd('\r');
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    result.AppendLine($"# {trimmedLine}");
                }
            }
            return result.ToString().TrimEnd();
        }
    }
}
