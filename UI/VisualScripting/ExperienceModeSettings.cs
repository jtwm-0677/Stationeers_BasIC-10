using System.Collections.Generic;
using BasicToMips.UI.VisualScripting.Animations;

namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Configuration settings for each experience mode
    /// Controls UI complexity and available features
    /// </summary>
    public class ExperienceModeSettings
    {
        /// <summary>
        /// Whether to show the code panel
        /// </summary>
        public bool ShowCodePanel { get; set; }

        /// <summary>
        /// Whether to show IC10 toggle button (requires ShowCodePanel = true)
        /// </summary>
        public bool ShowIC10Toggle { get; set; }

        /// <summary>
        /// Whether to show register allocation information
        /// </summary>
        public bool ShowRegisterInfo { get; set; }

        /// <summary>
        /// Whether to show line numbers in code panel
        /// </summary>
        public bool ShowLineNumbers { get; set; }

        /// <summary>
        /// Node label display style
        /// </summary>
        public NodeLabelStyle NodeLabelStyle { get; set; }

        /// <summary>
        /// List of available node categories (empty = all)
        /// </summary>
        public List<string> AvailableNodeCategories { get; set; }

        /// <summary>
        /// Whether to show execution pins (flow control)
        /// </summary>
        public bool ShowExecutionPins { get; set; }

        /// <summary>
        /// Whether to show data type indicators on pins
        /// </summary>
        public bool ShowDataTypes { get; set; }

        /// <summary>
        /// Whether to show optimization hints/warnings
        /// </summary>
        public bool ShowOptimizationHints { get; set; }

        /// <summary>
        /// Error message detail level
        /// </summary>
        public ErrorMessageStyle ErrorMessageStyle { get; set; }

        /// <summary>
        /// Whether to auto-compile on graph changes
        /// </summary>
        public bool AutoCompile { get; set; }

        /// <summary>
        /// Whether to show advanced node properties
        /// </summary>
        public bool ShowAdvancedProperties { get; set; }

        /// <summary>
        /// Whether to show grid snap controls
        /// </summary>
        public bool ShowGridSnap { get; set; }

        /// <summary>
        /// Maximum nodes shown in palette before "Show More" expander
        /// </summary>
        public int PaletteNodeLimit { get; set; }

        /// <summary>
        /// Animation settings for this experience mode
        /// </summary>
        public AnimationSettings? AnimationSettings { get; set; }

        public ExperienceModeSettings()
        {
            AvailableNodeCategories = new List<string>();
        }

        /// <summary>
        /// Create default settings for Beginner mode
        /// </summary>
        public static ExperienceModeSettings CreateBeginnerSettings()
        {
            return new ExperienceModeSettings
            {
                ShowCodePanel = false,
                ShowIC10Toggle = false,
                ShowRegisterInfo = false,
                ShowLineNumbers = false,
                NodeLabelStyle = NodeLabelStyle.Friendly,
                AvailableNodeCategories = new List<string>
                {
                    "Variables",
                    "Devices",
                    "Basic Math",
                    "Flow Control"
                },
                ShowExecutionPins = true, // Simplified but visible
                ShowDataTypes = false,
                ShowOptimizationHints = false,
                ErrorMessageStyle = ErrorMessageStyle.Simple,
                AutoCompile = true,
                ShowAdvancedProperties = false,
                ShowGridSnap = true,
                PaletteNodeLimit = 20,
                AnimationSettings = new AnimationSettings
                {
                    EnableAnimations = true,
                    AnimationSpeed = 0.8, // Slower for beginners
                    ParticleCount = ParticleDensity.Low, // Less visual clutter
                    EnableGlowEffects = true,
                    EnableValuePopups = true,
                    EnableExecutionHighlight = true,
                    EnableNodeHoverEffects = true,
                    EnableCanvasAnimations = true,
                    EnableErrorAnimations = true,
                    PerformanceMode = false
                }
            };
        }

        /// <summary>
        /// Create default settings for Intermediate mode
        /// </summary>
        public static ExperienceModeSettings CreateIntermediateSettings()
        {
            return new ExperienceModeSettings
            {
                ShowCodePanel = true,
                ShowIC10Toggle = false,
                ShowRegisterInfo = false,
                ShowLineNumbers = true,
                NodeLabelStyle = NodeLabelStyle.Mixed,
                AvailableNodeCategories = new List<string>
                {
                    "Variables",
                    "Devices",
                    "Basic Math",
                    "Flow Control",
                    "Math Functions",
                    "Logic",
                    "Arrays",
                    "Comparison"
                },
                ShowExecutionPins = true,
                ShowDataTypes = true,
                ShowOptimizationHints = false,
                ErrorMessageStyle = ErrorMessageStyle.Detailed,
                AutoCompile = true,
                ShowAdvancedProperties = true,
                ShowGridSnap = true,
                PaletteNodeLimit = 45,
                AnimationSettings = new AnimationSettings
                {
                    EnableAnimations = true,
                    AnimationSpeed = 1.0, // Normal speed
                    ParticleCount = ParticleDensity.Medium,
                    EnableGlowEffects = true,
                    EnableValuePopups = true,
                    EnableExecutionHighlight = true,
                    EnableNodeHoverEffects = true,
                    EnableCanvasAnimations = true,
                    EnableErrorAnimations = true,
                    PerformanceMode = false
                }
            };
        }

        /// <summary>
        /// Create default settings for Expert mode
        /// </summary>
        public static ExperienceModeSettings CreateExpertSettings()
        {
            return new ExperienceModeSettings
            {
                ShowCodePanel = true,
                ShowIC10Toggle = true,
                ShowRegisterInfo = true,
                ShowLineNumbers = true,
                NodeLabelStyle = NodeLabelStyle.Technical,
                AvailableNodeCategories = new List<string>(), // Empty = all categories
                ShowExecutionPins = true,
                ShowDataTypes = true,
                ShowOptimizationHints = true,
                ErrorMessageStyle = ErrorMessageStyle.Technical,
                AutoCompile = false, // Expert users prefer manual control
                ShowAdvancedProperties = true,
                ShowGridSnap = true,
                PaletteNodeLimit = 999, // Show all
                AnimationSettings = new AnimationSettings
                {
                    EnableAnimations = true,
                    AnimationSpeed = 1.2, // Faster for experts
                    ParticleCount = ParticleDensity.High,
                    EnableGlowEffects = true,
                    EnableValuePopups = false, // Less distraction for experts
                    EnableExecutionHighlight = true,
                    EnableNodeHoverEffects = false, // Minimize hover effects
                    EnableCanvasAnimations = true,
                    EnableErrorAnimations = false, // Experts don't need shake animations
                    PerformanceMode = false,
                    PerformanceModeThreshold = 100 // Higher threshold for experts
                }
            };
        }

        /// <summary>
        /// Create a copy of these settings
        /// </summary>
        public ExperienceModeSettings Clone()
        {
            return new ExperienceModeSettings
            {
                ShowCodePanel = ShowCodePanel,
                ShowIC10Toggle = ShowIC10Toggle,
                ShowRegisterInfo = ShowRegisterInfo,
                ShowLineNumbers = ShowLineNumbers,
                NodeLabelStyle = NodeLabelStyle,
                AvailableNodeCategories = new List<string>(AvailableNodeCategories),
                ShowExecutionPins = ShowExecutionPins,
                ShowDataTypes = ShowDataTypes,
                ShowOptimizationHints = ShowOptimizationHints,
                ErrorMessageStyle = ErrorMessageStyle,
                AutoCompile = AutoCompile,
                ShowAdvancedProperties = ShowAdvancedProperties,
                ShowGridSnap = ShowGridSnap,
                PaletteNodeLimit = PaletteNodeLimit,
                AnimationSettings = AnimationSettings?.Clone()
            };
        }
    }
}
