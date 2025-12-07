using System;
using System.Collections.Generic;
using BasicToMips.UI.Services;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Singleton manager for experience mode system
    /// Controls UI complexity and available features based on user expertise
    /// </summary>
    public class ExperienceModeManager
    {
        private static ExperienceModeManager? _instance;
        private static readonly object _lock = new object();

        private ExperienceLevel _currentMode;
        private ExperienceModeSettings _currentSettings;
        private readonly Dictionary<ExperienceLevel, ExperienceModeSettings> _presetSettings;
        private ExperienceModeSettings? _customSettings;

        /// <summary>
        /// Event fired when the experience mode changes
        /// </summary>
        public event EventHandler<ModeChangedEventArgs>? ModeChanged;

        /// <summary>
        /// Get the singleton instance
        /// </summary>
        public static ExperienceModeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ExperienceModeManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Current experience mode
        /// </summary>
        public ExperienceLevel CurrentMode
        {
            get => _currentMode;
            private set
            {
                if (_currentMode != value)
                {
                    var oldMode = _currentMode;
                    _currentMode = value;
                    _currentSettings = GetSettings(value);
                    OnModeChanged(oldMode, value);
                }
            }
        }

        /// <summary>
        /// Current settings for the active mode
        /// </summary>
        public ExperienceModeSettings CurrentSettings => _currentSettings;

        private ExperienceModeManager()
        {
            _currentMode = ExperienceLevel.Beginner;
            _presetSettings = new Dictionary<ExperienceLevel, ExperienceModeSettings>
            {
                { ExperienceLevel.Beginner, ExperienceModeSettings.CreateBeginnerSettings() },
                { ExperienceLevel.Intermediate, ExperienceModeSettings.CreateIntermediateSettings() },
                { ExperienceLevel.Expert, ExperienceModeSettings.CreateExpertSettings() }
            };
            _currentSettings = _presetSettings[ExperienceLevel.Beginner];
        }

        /// <summary>
        /// Get settings for a specific experience level
        /// </summary>
        public ExperienceModeSettings GetSettings(ExperienceLevel mode)
        {
            if (mode == ExperienceLevel.Custom)
            {
                return _customSettings ?? ExperienceModeSettings.CreateBeginnerSettings();
            }

            return _presetSettings.TryGetValue(mode, out var settings)
                ? settings
                : ExperienceModeSettings.CreateBeginnerSettings();
        }

        /// <summary>
        /// Set the experience mode
        /// </summary>
        public void SetMode(ExperienceLevel mode)
        {
            CurrentMode = mode;
        }

        /// <summary>
        /// Set custom mode settings
        /// </summary>
        public void SetCustomSettings(ExperienceModeSettings settings)
        {
            _customSettings = settings.Clone();
            if (_currentMode == ExperienceLevel.Custom)
            {
                _currentSettings = _customSettings;
                OnModeChanged(ExperienceLevel.Custom, ExperienceLevel.Custom);
            }
        }

        /// <summary>
        /// Get custom mode settings (or create default if none exist)
        /// </summary>
        public ExperienceModeSettings GetCustomSettings()
        {
            if (_customSettings == null)
            {
                _customSettings = ExperienceModeSettings.CreateIntermediateSettings();
            }
            return _customSettings.Clone();
        }

        /// <summary>
        /// Load settings from SettingsService
        /// </summary>
        public void LoadFromSettings(SettingsService settings)
        {
            // Load experience mode
            _currentMode = settings.ExperienceMode;

            // Load custom settings if they exist
            if (settings.CustomModeSettings != null)
            {
                _customSettings = settings.CustomModeSettings.Clone();
            }

            // Apply current settings
            _currentSettings = GetSettings(_currentMode);
        }

        /// <summary>
        /// Save settings to SettingsService
        /// </summary>
        public void SaveToSettings(SettingsService settings)
        {
            settings.ExperienceMode = _currentMode;
            settings.CustomModeSettings = _customSettings;
        }

        /// <summary>
        /// Check if a category is available in current mode
        /// </summary>
        public bool IsCategoryAvailable(string category)
        {
            // If no categories specified, all are available
            if (_currentSettings.AvailableNodeCategories.Count == 0)
            {
                return true;
            }

            return _currentSettings.AvailableNodeCategories.Contains(category);
        }

        /// <summary>
        /// Get user-friendly name for a mode
        /// </summary>
        public static string GetModeName(ExperienceLevel mode)
        {
            return mode switch
            {
                ExperienceLevel.Beginner => "Beginner",
                ExperienceLevel.Intermediate => "Intermediate",
                ExperienceLevel.Expert => "Expert",
                ExperienceLevel.Custom => "Custom",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get icon for a mode
        /// </summary>
        public static string GetModeIcon(ExperienceLevel mode)
        {
            return mode switch
            {
                ExperienceLevel.Beginner => "🎓",
                ExperienceLevel.Intermediate => "📊",
                ExperienceLevel.Expert => "⚡",
                ExperienceLevel.Custom => "⚙️",
                _ => "?"
            };
        }

        /// <summary>
        /// Get description for a mode
        /// </summary>
        public static string GetModeDescription(ExperienceLevel mode)
        {
            return mode switch
            {
                ExperienceLevel.Beginner => "Simple interface with friendly labels and essential nodes",
                ExperienceLevel.Intermediate => "Balanced interface with code preview and most features",
                ExperienceLevel.Expert => "Full interface with IC10 code, all nodes, and optimization hints",
                ExperienceLevel.Custom => "Customize your own experience settings",
                _ => ""
            };
        }

        private void OnModeChanged(ExperienceLevel oldMode, ExperienceLevel newMode)
        {
            ModeChanged?.Invoke(this, new ModeChangedEventArgs(oldMode, newMode, _currentSettings));
        }
    }

    /// <summary>
    /// Event args for mode changed event
    /// </summary>
    public class ModeChangedEventArgs : EventArgs
    {
        public ExperienceLevel OldMode { get; }
        public ExperienceLevel NewMode { get; }
        public ExperienceModeSettings Settings { get; }

        public ModeChangedEventArgs(ExperienceLevel oldMode, ExperienceLevel newMode, ExperienceModeSettings settings)
        {
            OldMode = oldMode;
            NewMode = newMode;
            Settings = settings;
        }
    }
}
