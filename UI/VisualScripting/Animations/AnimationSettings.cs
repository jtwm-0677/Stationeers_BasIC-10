using System;

namespace BasicToMips.UI.VisualScripting.Animations
{
    /// <summary>
    /// User-configurable animation preferences
    /// </summary>
    public class AnimationSettings
    {
        /// <summary>
        /// Master toggle for all animations
        /// </summary>
        public bool EnableAnimations { get; set; } = true;

        /// <summary>
        /// Animation speed multiplier (0.5x - 2.0x)
        /// </summary>
        public double AnimationSpeed { get; set; } = 1.0;

        /// <summary>
        /// Particle density level
        /// </summary>
        public ParticleDensity ParticleCount { get; set; } = ParticleDensity.Medium;

        /// <summary>
        /// Enable glow effects on wires and nodes
        /// </summary>
        public bool EnableGlowEffects { get; set; } = true;

        /// <summary>
        /// Show floating value change popups
        /// </summary>
        public bool EnableValuePopups { get; set; } = true;

        /// <summary>
        /// Enable execution highlighting during simulation
        /// </summary>
        public bool EnableExecutionHighlight { get; set; } = true;

        /// <summary>
        /// Enable node hover animations
        /// </summary>
        public bool EnableNodeHoverEffects { get; set; } = true;

        /// <summary>
        /// Enable smooth canvas zoom/pan animations
        /// </summary>
        public bool EnableCanvasAnimations { get; set; } = true;

        /// <summary>
        /// Enable error shake animations
        /// </summary>
        public bool EnableErrorAnimations { get; set; } = true;

        /// <summary>
        /// Performance mode - reduces visual effects when canvas has many elements
        /// </summary>
        public bool PerformanceMode { get; set; } = false;

        /// <summary>
        /// Maximum number of nodes before auto-enabling performance mode
        /// </summary>
        public int PerformanceModeThreshold { get; set; } = 50;

        /// <summary>
        /// Get particle count based on density setting
        /// </summary>
        public int GetParticleCount(double wireLength)
        {
            if (!EnableAnimations || PerformanceMode)
                return 0;

            double baseCount = ParticleCount switch
            {
                ParticleDensity.Low => 2,
                ParticleDensity.Medium => 4,
                ParticleDensity.High => 6,
                _ => 4
            };

            // Scale by wire length (longer wires get more particles)
            double scaleFactor = Math.Min(1.0, wireLength / 200.0);
            return Math.Max(1, (int)(baseCount * scaleFactor));
        }

        /// <summary>
        /// Get effective animation speed
        /// </summary>
        public double GetEffectiveSpeed()
        {
            if (!EnableAnimations)
                return 0;

            return Math.Max(0.5, Math.Min(2.0, AnimationSpeed));
        }

        /// <summary>
        /// Clone these settings
        /// </summary>
        public AnimationSettings Clone()
        {
            return new AnimationSettings
            {
                EnableAnimations = EnableAnimations,
                AnimationSpeed = AnimationSpeed,
                ParticleCount = ParticleCount,
                EnableGlowEffects = EnableGlowEffects,
                EnableValuePopups = EnableValuePopups,
                EnableExecutionHighlight = EnableExecutionHighlight,
                EnableNodeHoverEffects = EnableNodeHoverEffects,
                EnableCanvasAnimations = EnableCanvasAnimations,
                EnableErrorAnimations = EnableErrorAnimations,
                PerformanceMode = PerformanceMode,
                PerformanceModeThreshold = PerformanceModeThreshold
            };
        }
    }

    /// <summary>
    /// Particle density options
    /// </summary>
    public enum ParticleDensity
    {
        Low,
        Medium,
        High
    }
}
