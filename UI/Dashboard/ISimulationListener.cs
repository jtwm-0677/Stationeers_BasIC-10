namespace BasicToMips.UI.Dashboard;

/// <summary>
/// Interface for widgets that need to respond to simulation events
/// </summary>
public interface ISimulationListener
{
    /// <summary>
    /// Called when simulation starts
    /// </summary>
    void OnSimulationStarted();

    /// <summary>
    /// Called when simulation stops
    /// </summary>
    void OnSimulationStopped();

    /// <summary>
    /// Called on each simulation step
    /// </summary>
    /// <param name="lineNumber">Current line number being executed</param>
    void OnSimulationStep(int lineNumber);

    /// <summary>
    /// Called when a value changes (register or variable)
    /// </summary>
    /// <param name="name">Name of the changed value</param>
    /// <param name="oldValue">Previous value</param>
    /// <param name="newValue">New value</param>
    void OnValueChanged(string name, double oldValue, double newValue);
}
