using System.Collections.Generic;
using System.Linq;

namespace BasicToMips.UI.VisualScripting.Canvas;

/// <summary>
/// Represents an action that can be undone and redone.
/// </summary>
public interface IUndoableAction
{
    /// <summary>
    /// Gets a description of the action for display purposes.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the action.
    /// </summary>
    void Execute();

    /// <summary>
    /// Undoes the action, restoring the previous state.
    /// </summary>
    void Undo();
}

/// <summary>
/// Manages undo/redo functionality with a stack-based approach.
/// Supports keyboard shortcuts (Ctrl+Z for undo, Ctrl+Y for redo).
/// </summary>
public class UndoRedoManager
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private int _maxHistorySize = 100;

    /// <summary>
    /// Gets or sets the maximum number of actions to keep in history.
    /// </summary>
    public int MaxHistorySize
    {
        get => _maxHistorySize;
        set
        {
            if (value > 0)
            {
                _maxHistorySize = value;
                TrimHistory();
            }
        }
    }

    /// <summary>
    /// Gets whether there are actions available to undo.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether there are actions available to redo.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Gets the description of the next action to undo, or null if none available.
    /// </summary>
    public string? NextUndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Gets the description of the next action to redo, or null if none available.
    /// </summary>
    public string? NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Event raised when the undo/redo state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Executes an action and adds it to the undo history.
    /// This clears the redo stack.
    /// </summary>
    public void ExecuteAction(IUndoableAction action)
    {
        action.Execute();
        _undoStack.Push(action);
        _redoStack.Clear();

        TrimHistory();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adds an already-executed action to the undo history.
    /// Use this when the action was performed outside the undo/redo system.
    /// </summary>
    public void AddAction(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();

        TrimHistory();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Undoes the most recent action.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo)
            return;

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Redoes the most recently undone action.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo)
            return;

        var action = _redoStack.Pop();
        action.Execute();
        _undoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Clears all undo/redo history.
    /// </summary>
    public void Clear()
    {
        var hadHistory = _undoStack.Count > 0 || _redoStack.Count > 0;

        _undoStack.Clear();
        _redoStack.Clear();

        if (hadHistory)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Trims the history to the maximum size.
    /// </summary>
    private void TrimHistory()
    {
        while (_undoStack.Count > _maxHistorySize)
        {
            // Remove oldest items from the bottom of the stack
            var items = _undoStack.ToList();
            items.RemoveAt(items.Count - 1);
            _undoStack.Clear();
            foreach (var item in items.AsEnumerable().Reverse())
            {
                _undoStack.Push(item);
            }
        }
    }
}

// Common undoable actions for visual scripting

/// <summary>
/// Action for adding a node to the canvas.
/// </summary>
public class AddNodeAction : IUndoableAction
{
    private readonly object _node;
    private readonly Action<object> _addAction;
    private readonly Action<object> _removeAction;

    public string Description { get; }

    public AddNodeAction(object node, Action<object> addAction, Action<object> removeAction, string? description = null)
    {
        _node = node;
        _addAction = addAction;
        _removeAction = removeAction;
        Description = description ?? "Add Node";
    }

    public void Execute()
    {
        _addAction(_node);
    }

    public void Undo()
    {
        _removeAction(_node);
    }
}

/// <summary>
/// Action for removing a node from the canvas.
/// </summary>
public class RemoveNodeAction : IUndoableAction
{
    private readonly object _node;
    private readonly Action<object> _addAction;
    private readonly Action<object> _removeAction;

    public string Description { get; }

    public RemoveNodeAction(object node, Action<object> addAction, Action<object> removeAction, string? description = null)
    {
        _node = node;
        _addAction = addAction;
        _removeAction = removeAction;
        Description = description ?? "Remove Node";
    }

    public void Execute()
    {
        _removeAction(_node);
    }

    public void Undo()
    {
        _addAction(_node);
    }
}

/// <summary>
/// Action for moving a node on the canvas.
/// </summary>
public class MoveNodeAction : IUndoableAction
{
    private readonly object _node;
    private readonly System.Windows.Point _oldPosition;
    private readonly System.Windows.Point _newPosition;
    private readonly Action<object, System.Windows.Point> _setPositionAction;

    public string Description { get; }

    public MoveNodeAction(object node, System.Windows.Point oldPosition, System.Windows.Point newPosition,
        Action<object, System.Windows.Point> setPositionAction, string? description = null)
    {
        _node = node;
        _oldPosition = oldPosition;
        _newPosition = newPosition;
        _setPositionAction = setPositionAction;
        Description = description ?? "Move Node";
    }

    public void Execute()
    {
        _setPositionAction(_node, _newPosition);
    }

    public void Undo()
    {
        _setPositionAction(_node, _oldPosition);
    }
}

/// <summary>
/// Combines multiple actions into a single undoable action.
/// </summary>
public class CompositeAction : IUndoableAction
{
    private readonly List<IUndoableAction> _actions;

    public string Description { get; }

    public CompositeAction(IEnumerable<IUndoableAction> actions, string description = "Multiple Actions")
    {
        _actions = actions.ToList();
        Description = description;
    }

    public void Execute()
    {
        foreach (var action in _actions)
        {
            action.Execute();
        }
    }

    public void Undo()
    {
        // Undo in reverse order
        for (int i = _actions.Count - 1; i >= 0; i--)
        {
            _actions[i].Undo();
        }
    }
}
