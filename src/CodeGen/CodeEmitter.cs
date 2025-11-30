using System.Text;

namespace BasicToMips.CodeGen;

/// <summary>
/// Efficient IC10 code emitter that:
/// - Avoids redundant move instructions
/// - Uses immediate values directly when possible
/// - Combines operations where IC10 supports it
/// - Tracks current values in registers to avoid reloads
/// </summary>
public class CodeEmitter
{
    private readonly StringBuilder _output = new();
    private readonly List<string> _lines = new();
    private readonly RegisterAllocator _registers;

    // Track what value each register currently holds (for optimization)
    private readonly Dictionary<string, string> _registerValues = new();

    public CodeEmitter(RegisterAllocator registers)
    {
        _registers = registers;
    }

    public int LineCount => _lines.Count;

    /// <summary>
    /// Emit a raw instruction line.
    /// </summary>
    public void Emit(string instruction)
    {
        _lines.Add(instruction);
    }

    /// <summary>
    /// Emit a label.
    /// </summary>
    public void EmitLabel(string label)
    {
        _lines.Add($"{label}:");
    }

    /// <summary>
    /// Emit a comment.
    /// </summary>
    public void EmitComment(string comment)
    {
        _lines.Add($"# {comment}");
    }

    /// <summary>
    /// Emit a define statement.
    /// </summary>
    public void EmitDefine(string name, string value)
    {
        _lines.Add($"define {name} {value}");
        _registers.AddDefine(name);
    }

    /// <summary>
    /// Emit a move only if necessary (dest doesn't already contain value).
    /// </summary>
    public void EmitMove(string dest, string source)
    {
        if (dest == source) return;

        // Check if dest already holds this value
        if (_registerValues.TryGetValue(dest, out var currentValue) && currentValue == source)
        {
            return; // Already has the value
        }

        _lines.Add($"move {dest} {source}");
        _registerValues[dest] = source;
    }

    /// <summary>
    /// Emit a binary operation, using immediate values when possible.
    /// Returns the destination register.
    /// </summary>
    public string EmitBinaryOp(string op, string left, string right, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        // Emit any spill instructions first
        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        _lines.Add($"{op} {dest} {left} {right}");
        _registerValues.Remove(dest); // Value is now computed, not a simple copy

        return dest;
    }

    /// <summary>
    /// Emit a unary operation.
    /// </summary>
    public string EmitUnaryOp(string op, string operand, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        _lines.Add($"{op} {dest} {operand}");
        _registerValues.Remove(dest);

        return dest;
    }

    /// <summary>
    /// Emit a device read instruction.
    /// </summary>
    public string EmitDeviceRead(string device, string property, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        _lines.Add($"l {dest} {device} {property}");
        _registerValues.Remove(dest);

        return dest;
    }

    /// <summary>
    /// Emit a batch device read instruction (lb or lbn).
    /// </summary>
    public string EmitBatchRead(string hash, string? nameHash, string property, int mode, string? destReg = null)
    {
        var emitBuffer = new List<string>();
        var dest = destReg ?? _registers.AllocateTemp(emitBuffer);

        foreach (var line in emitBuffer)
        {
            _lines.Add(line);
        }

        if (nameHash != null)
        {
            _lines.Add($"lbn {dest} {hash} {nameHash} {property} {mode}");
        }
        else
        {
            _lines.Add($"lb {dest} {hash} {property} {mode}");
        }
        _registerValues.Remove(dest);

        return dest;
    }

    /// <summary>
    /// Emit a device write instruction.
    /// </summary>
    public void EmitDeviceWrite(string device, string property, string value)
    {
        _lines.Add($"s {device} {property} {value}");
    }

    /// <summary>
    /// Emit a batch device write instruction (sb or sbn).
    /// </summary>
    public void EmitBatchWrite(string hash, string? nameHash, string property, string value)
    {
        if (nameHash != null)
        {
            _lines.Add($"sbn {hash} {nameHash} {property} {value}");
        }
        else
        {
            _lines.Add($"sb {hash} {property} {value}");
        }
    }

    /// <summary>
    /// Emit a conditional branch.
    /// </summary>
    public void EmitBranch(string condition, string left, string right, string label)
    {
        _lines.Add($"{condition} {left} {right} {label}");
    }

    /// <summary>
    /// Emit a conditional branch against zero.
    /// </summary>
    public void EmitBranchZero(string condition, string operand, string label)
    {
        _lines.Add($"{condition} {operand} {label}");
    }

    /// <summary>
    /// Emit an unconditional jump.
    /// </summary>
    public void EmitJump(string label)
    {
        _lines.Add($"j {label}");
    }

    /// <summary>
    /// Clear tracked register values (e.g., after a label where control flow merges).
    /// </summary>
    public void InvalidateRegisterTracking()
    {
        _registerValues.Clear();
    }

    /// <summary>
    /// Get the final output as a string.
    /// </summary>
    public string GetOutput()
    {
        return string.Join("\n", _lines);
    }

    /// <summary>
    /// Reset for new compilation.
    /// </summary>
    public void Reset()
    {
        _lines.Clear();
        _registerValues.Clear();
    }
}
