namespace BasicToMips.UI.VisualScripting
{
    /// <summary>
    /// Experience levels for visual scripting UI
    /// Adjusts complexity and available features based on user expertise
    /// </summary>
    public enum ExperienceLevel
    {
        /// <summary>
        /// Beginner mode: Simplified UI, friendly labels, limited node set
        /// </summary>
        Beginner,

        /// <summary>
        /// Intermediate mode: Balanced UI, mixed labels, most nodes available
        /// </summary>
        Intermediate,

        /// <summary>
        /// Expert mode: Full UI, technical labels, all nodes and features
        /// </summary>
        Expert,

        /// <summary>
        /// Custom mode: User-defined configuration
        /// </summary>
        Custom
    }

    /// <summary>
    /// Node label display style
    /// </summary>
    public enum NodeLabelStyle
    {
        /// <summary>
        /// User-friendly descriptions: "Turn Light On"
        /// </summary>
        Friendly,

        /// <summary>
        /// Mix of friendly and technical: "sensor.Temperature"
        /// </summary>
        Mixed,

        /// <summary>
        /// Technical IC10-like: "s d0 On 1"
        /// </summary>
        Technical
    }

    /// <summary>
    /// Error message detail level
    /// </summary>
    public enum ErrorMessageStyle
    {
        /// <summary>
        /// Simple, user-friendly messages: "Connect the sensor first"
        /// </summary>
        Simple,

        /// <summary>
        /// Detailed explanations with context
        /// </summary>
        Detailed,

        /// <summary>
        /// Technical messages with IC10 details and line numbers
        /// </summary>
        Technical
    }
}
