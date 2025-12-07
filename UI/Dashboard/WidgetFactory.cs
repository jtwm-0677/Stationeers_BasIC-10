using System.Collections.Generic;

namespace BasicToMips.UI.Dashboard;

/// <summary>
/// Factory for creating widgets by type name
/// </summary>
public class WidgetFactory
{
    private readonly Dictionary<string, WidgetRegistration> _registrations = new();

    public WidgetFactory()
    {
        // Register built-in widgets
        RegisterWidget<Widgets.TaskChecklistWidget>("TaskChecklist", "Task Checklist");
        RegisterWidget<Widgets.ConsoleOutputWidget>("ConsoleOutput", "Console Output");
        RegisterWidget<Widgets.LineCounterWidget>("LineCounter", "Line Counter");
        RegisterWidget<Widgets.RegisterViewWidget>("RegisterView", "Register View");
        RegisterWidget<Widgets.VariableWatchWidget>("VariableWatch", "Variable Watch");
        RegisterWidget<Widgets.DeviceMonitorWidget>("DeviceMonitor", "Device Monitor");
        RegisterWidget<Widgets.SimulationControlWidget>("SimulationControl", "Simulation Control");
    }

    /// <summary>
    /// Register a widget type
    /// </summary>
    public void RegisterWidget<T>(string typeName, string displayName) where T : WidgetBase, new()
    {
        _registrations[typeName] = new WidgetRegistration
        {
            TypeName = typeName,
            DisplayName = displayName,
            Factory = () => new T()
        };
    }

    /// <summary>
    /// Create a widget by type name
    /// </summary>
    public WidgetBase? CreateWidget(string typeName)
    {
        if (_registrations.TryGetValue(typeName, out var registration))
        {
            return registration.Factory();
        }
        return null;
    }

    /// <summary>
    /// Get all available widget types
    /// </summary>
    public List<WidgetInfo> GetAvailableWidgets()
    {
        return _registrations.Values.Select(r => new WidgetInfo { TypeName = r.TypeName, DisplayName = r.DisplayName }).ToList();
    }

    private class WidgetRegistration
    {
        public string TypeName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public Func<WidgetBase> Factory { get; set; } = () => throw new NotImplementedException();
    }
}

/// <summary>
/// Public widget info
/// </summary>
public class WidgetInfo
{
    public string TypeName { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
