namespace BasicToMips.CodeGen;

/// <summary>
/// Smart register allocator that:
/// - Tracks which registers hold which variables
/// - Reuses registers when values are no longer needed
/// - Spills to stack (db) when registers run out
/// - Returns raw values (literals/defines) when possible instead of allocating
/// </summary>
public class RegisterAllocator
{
    private const int MaxRegisters = 16; // r0-r15
    private const int PreferredMax = 12; // Prefer r0-r11, reserve r12-r15 for temps

    // Maps variable name to register number
    private readonly Dictionary<string, int> _variableRegisters = new();

    // Maps register number to what it currently holds (null = free)
    private readonly string?[] _registerContents = new string?[MaxRegisters];

    // Stack slots used for spilled variables
    private readonly Dictionary<string, int> _spilledVariables = new();
    private int _nextStackSlot = 0;

    // Defined constants (can use directly without register)
    private readonly HashSet<string> _defines = new();

    // Track temporary allocations that can be freed after use
    private readonly HashSet<int> _tempRegisters = new();

    public void AddDefine(string name)
    {
        _defines.Add(name);
    }

    public bool IsDefine(string name) => _defines.Contains(name);

    /// <summary>
    /// Get or allocate a register for a variable.
    /// Returns the register name (e.g., "r0").
    /// </summary>
    public string GetVariableRegister(string variableName, List<string> emitBuffer)
    {
        // Already in a register?
        if (_variableRegisters.TryGetValue(variableName, out int regNum))
        {
            return $"r{regNum}";
        }

        // Was spilled to stack? Reload it
        if (_spilledVariables.TryGetValue(variableName, out int stackSlot))
        {
            int reg = AllocateRegister(variableName, emitBuffer);
            emitBuffer.Add($"get r{reg} db {stackSlot}");
            _spilledVariables.Remove(variableName);
            return $"r{reg}";
        }

        // New variable - allocate register
        int newReg = AllocateRegister(variableName, emitBuffer);
        return $"r{newReg}";
    }

    /// <summary>
    /// Allocate a temporary register for intermediate calculations.
    /// Caller must call FreeTemp() when done.
    /// </summary>
    public string AllocateTemp(List<string> emitBuffer)
    {
        // First try to find a completely free register
        for (int i = PreferredMax; i < MaxRegisters; i++)
        {
            if (_registerContents[i] == null && !_tempRegisters.Contains(i))
            {
                _tempRegisters.Add(i);
                return $"r{i}";
            }
        }

        // Try lower registers
        for (int i = 0; i < PreferredMax; i++)
        {
            if (_registerContents[i] == null && !_tempRegisters.Contains(i))
            {
                _tempRegisters.Add(i);
                return $"r{i}";
            }
        }

        // All registers in use - spill the least recently used variable
        // For simplicity, spill the first variable we find
        for (int i = 0; i < PreferredMax; i++)
        {
            if (_registerContents[i] != null && !_tempRegisters.Contains(i))
            {
                SpillRegister(i, emitBuffer);
                _tempRegisters.Add(i);
                return $"r{i}";
            }
        }

        // Fallback to r15
        _tempRegisters.Add(15);
        return "r15";
    }

    /// <summary>
    /// Free a temporary register for reuse.
    /// </summary>
    public void FreeTemp(string reg)
    {
        if (reg.StartsWith("r") && int.TryParse(reg[1..], out int num))
        {
            _tempRegisters.Remove(num);
        }
    }

    /// <summary>
    /// Check if a value can be used directly as an operand (literal or define).
    /// </summary>
    public bool CanUseDirectly(string value)
    {
        // Numeric literals
        if (double.TryParse(value, out _)) return true;
        // Defined constants
        if (_defines.Contains(value)) return true;
        return false;
    }

    private int AllocateRegister(string variableName, List<string> emitBuffer)
    {
        // Find a free register
        for (int i = 0; i < PreferredMax; i++)
        {
            if (_registerContents[i] == null && !_tempRegisters.Contains(i))
            {
                _registerContents[i] = variableName;
                _variableRegisters[variableName] = i;
                return i;
            }
        }

        // No free registers - spill one
        for (int i = 0; i < PreferredMax; i++)
        {
            if (!_tempRegisters.Contains(i))
            {
                SpillRegister(i, emitBuffer);
                _registerContents[i] = variableName;
                _variableRegisters[variableName] = i;
                return i;
            }
        }

        throw new InvalidOperationException("No registers available for allocation");
    }

    private void SpillRegister(int regNum, List<string> emitBuffer)
    {
        var variableName = _registerContents[regNum];
        if (variableName == null) return;

        int stackSlot = _nextStackSlot++;
        emitBuffer.Add($"put db {stackSlot} r{regNum}");
        _spilledVariables[variableName] = stackSlot;
        _variableRegisters.Remove(variableName);
        _registerContents[regNum] = null;
    }

    /// <summary>
    /// Reset allocator state (for new compilation).
    /// </summary>
    public void Reset()
    {
        _variableRegisters.Clear();
        Array.Fill(_registerContents, null);
        _spilledVariables.Clear();
        _nextStackSlot = 0;
        _defines.Clear();
        _tempRegisters.Clear();
    }
}
