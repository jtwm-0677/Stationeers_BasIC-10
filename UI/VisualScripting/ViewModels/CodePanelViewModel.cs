using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BasicToMips.UI.VisualScripting.ViewModels
{
    /// <summary>
    /// ViewModel for the CodePanel - manages code display and view state
    /// </summary>
    public class CodePanelViewModel : INotifyPropertyChanged
    {
        private string _generatedCode = "";
        private string _ic10Code = "";
        private bool _showIC10 = false;
        private int _basicLineCount = 0;
        private int _ic10LineCount = 0;
        private Guid _selectedNodeId = Guid.Empty;
        private bool _hasErrors = false;
        private string _errorMessage = "";

        #region Properties

        /// <summary>
        /// The generated BASIC code from the visual graph
        /// </summary>
        public string GeneratedCode
        {
            get => _generatedCode;
            set
            {
                if (_generatedCode != value)
                {
                    _generatedCode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentCode)); // Update display when not showing IC10
                    UpdateLineCount();
                }
            }
        }

        /// <summary>
        /// The compiled IC10 code
        /// </summary>
        public string IC10Code
        {
            get => _ic10Code;
            set
            {
                if (_ic10Code != value)
                {
                    _ic10Code = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentCode)); // Update display when showing IC10
                    UpdateIC10LineCount();
                }
            }
        }

        /// <summary>
        /// Toggle between BASIC and IC10 view
        /// </summary>
        public bool ShowIC10
        {
            get => _showIC10;
            set
            {
                if (_showIC10 != value)
                {
                    _showIC10 = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentCode));
                    OnPropertyChanged(nameof(ViewModeText));
                }
            }
        }

        /// <summary>
        /// The currently displayed code (either BASIC or IC10)
        /// </summary>
        public string CurrentCode => _showIC10 ? _ic10Code : _generatedCode;

        /// <summary>
        /// Text for the view mode toggle button
        /// </summary>
        public string ViewModeText => _showIC10 ? "Show BASIC" : "Show IC10";

        /// <summary>
        /// Number of lines in the BASIC code
        /// </summary>
        public int BasicLineCount
        {
            get => _basicLineCount;
            private set
            {
                if (_basicLineCount != value)
                {
                    _basicLineCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineCountDisplay));
                }
            }
        }

        /// <summary>
        /// Number of lines in the IC10 code
        /// </summary>
        public int IC10LineCount
        {
            get => _ic10LineCount;
            private set
            {
                if (_ic10LineCount != value)
                {
                    _ic10LineCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LineCountDisplay));
                }
            }
        }

        /// <summary>
        /// Display text for line counts
        /// </summary>
        public string LineCountDisplay => $"BASIC: {_basicLineCount} lines | IC10: {_ic10LineCount}/128";

        /// <summary>
        /// The ID of the currently selected node (for highlight sync)
        /// </summary>
        public Guid SelectedNodeId
        {
            get => _selectedNodeId;
            set
            {
                if (_selectedNodeId != value)
                {
                    _selectedNodeId = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Whether there are compilation errors
        /// </summary>
        public bool HasErrors
        {
            get => _hasErrors;
            set
            {
                if (_hasErrors != value)
                {
                    _hasErrors = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The error message if there are errors
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clear all code
        /// </summary>
        public void Clear()
        {
            GeneratedCode = "";
            IC10Code = "";
            HasErrors = false;
            ErrorMessage = "";
        }

        /// <summary>
        /// Update the BASIC line count
        /// </summary>
        private void UpdateLineCount()
        {
            if (string.IsNullOrWhiteSpace(_generatedCode))
            {
                BasicLineCount = 0;
            }
            else
            {
                BasicLineCount = _generatedCode.Split('\n').Length;
            }
        }

        /// <summary>
        /// Update the IC10 line count
        /// </summary>
        private void UpdateIC10LineCount()
        {
            if (string.IsNullOrWhiteSpace(_ic10Code))
            {
                IC10LineCount = 0;
            }
            else
            {
                IC10LineCount = _ic10Code.Split('\n').Length;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
