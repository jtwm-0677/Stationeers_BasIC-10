namespace BasicToMips.Simulator;

public class IC10Simulator
{
    public const int NumericRegisterCount = 16; // r0-r15 (valid user registers)
    public const int TotalRegisterCount = 18;   // r0-r15 + sp (index 16) + ra (index 17) for internal storage
    public const int DeviceCount = 6;           // d0-d5
    public const int StackSize = 512;

    public double[] Registers { get; } = new double[TotalRegisterCount];
    public SimDevice[] Devices { get; } = new SimDevice[DeviceCount];
    public double[] Stack { get; } = new double[StackSize];
    public int StackPointer { get; private set; } = 0;

    public int ProgramCounter { get; private set; } = 0;
    public int InstructionCount { get; private set; } = 0;
    public bool IsRunning { get; private set; } = false;
    public bool IsPaused { get; private set; } = false;
    public bool IsHalted { get; private set; } = false;
    public bool IsYielding { get; private set; } = false;
    public string? ErrorMessage { get; private set; }

    public HashSet<int> Breakpoints { get; } = new();
    public List<string> OutputLog { get; } = new();

    private string[] _lines = Array.Empty<string>();
    private Dictionary<string, int> _labels = new();
    private Dictionary<string, int> _defines = new();

    public event EventHandler? StateChanged;
    public event EventHandler<string>? OutputProduced;

    public IC10Simulator()
    {
        Reset();
    }

    public void Reset()
    {
        Array.Clear(Registers);
        Array.Clear(Stack);
        StackPointer = 0;
        ProgramCounter = 0;
        InstructionCount = 0;
        IsRunning = false;
        IsPaused = false;
        IsHalted = false;
        IsYielding = false;
        ErrorMessage = null;
        OutputLog.Clear();

        for (int i = 0; i < DeviceCount; i++)
        {
            Devices[i] = new SimDevice { Name = $"d{i}" };
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void LoadProgram(string code)
    {
        Reset();
        _lines = code.Split('\n', StringSplitOptions.None);
        _labels.Clear();
        _defines.Clear();

        // First pass: collect labels and defines
        for (int i = 0; i < _lines.Length; i++)
        {
            var line = _lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            // Check for label
            if (line.EndsWith(":"))
            {
                var labelName = line.TrimEnd(':').Trim();
                _labels[labelName] = i;
            }
            // Check for define
            else if (line.StartsWith("define ", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Substring(7).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[1], out int value))
                {
                    _defines[parts[0]] = value;
                }
            }
            // Check for alias
            else if (line.StartsWith("alias ", StringComparison.OrdinalIgnoreCase))
            {
                // Handle alias - maps a name to a device register
                var parts = line.Substring(6).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    // For simplicity, we'll store the device mapping
                    if (parts[1].StartsWith("d") && int.TryParse(parts[1].Substring(1), out int devIndex))
                    {
                        if (devIndex >= 0 && devIndex < DeviceCount)
                        {
                            Devices[devIndex].Alias = parts[0];
                        }
                    }
                }
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Step()
    {
        if (IsHalted || ProgramCounter >= _lines.Length)
        {
            IsHalted = true;
            return false;
        }

        IsYielding = false;
        var line = _lines[ProgramCounter].Trim();

        // Skip empty lines, comments, labels, and directives
        if (string.IsNullOrEmpty(line) ||
            line.StartsWith("#") ||
            line.EndsWith(":") ||
            line.StartsWith("define ", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("alias ", StringComparison.OrdinalIgnoreCase))
        {
            ProgramCounter++;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        // Remove inline comment
        var commentIndex = line.IndexOf('#');
        if (commentIndex > 0)
        {
            line = line.Substring(0, commentIndex).Trim();
        }

        try
        {
            ExecuteInstruction(line);
            InstructionCount++;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Line {ProgramCounter + 1}: {ex.Message}";
            IsHalted = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
            return false;
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        return !IsHalted;
    }

    public void Run()
    {
        IsRunning = true;
        IsPaused = false;
        IsYielding = false;

        while (IsRunning && !IsHalted && !IsPaused && !IsYielding)
        {
            if (Breakpoints.Contains(ProgramCounter))
            {
                IsPaused = true;
                break;
            }

            if (!Step()) break;

            // Prevent infinite loops (max 10000 instructions per run)
            if (InstructionCount > 10000)
            {
                ErrorMessage = "Execution limit exceeded (10000 instructions)";
                IsPaused = true;
                break;
            }
        }
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Stop()
    {
        IsRunning = false;
        IsHalted = true;
    }

    private void ExecuteInstruction(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var opcode = parts[0].ToLowerInvariant();

        switch (opcode)
        {
            // Basic operations
            case "move": Move(parts); break;
            case "add": BinaryOp(parts, (a, b) => a + b); break;
            case "sub": BinaryOp(parts, (a, b) => a - b); break;
            case "mul": BinaryOp(parts, (a, b) => a * b); break;
            case "div": BinaryOp(parts, (a, b) => b != 0 ? a / b : double.NaN); break;
            case "mod": BinaryOp(parts, (a, b) => b != 0 ? a % b : double.NaN); break;

            // Math functions
            case "sqrt": UnaryOp(parts, Math.Sqrt); break;
            case "abs": UnaryOp(parts, Math.Abs); break;
            case "floor": UnaryOp(parts, Math.Floor); break;
            case "ceil": UnaryOp(parts, Math.Ceiling); break;
            case "round": UnaryOp(parts, Math.Round); break;
            case "trunc": UnaryOp(parts, Math.Truncate); break;
            case "sin": UnaryOp(parts, Math.Sin); break;
            case "cos": UnaryOp(parts, Math.Cos); break;
            case "tan": UnaryOp(parts, Math.Tan); break;
            case "asin": UnaryOp(parts, Math.Asin); break;
            case "acos": UnaryOp(parts, Math.Acos); break;
            case "atan": UnaryOp(parts, Math.Atan); break;
            case "atan2": BinaryOp(parts, Math.Atan2); break;
            case "log": UnaryOp(parts, Math.Log); break;
            case "exp": UnaryOp(parts, Math.Exp); break;
            case "max": BinaryOp(parts, Math.Max); break;
            case "min": BinaryOp(parts, Math.Min); break;
            case "rand": Rand(parts); break;

            // Bitwise operations
            case "and": BinaryOp(parts, (a, b) => (int)a & (int)b); break;
            case "or": BinaryOp(parts, (a, b) => (int)a | (int)b); break;
            case "xor": BinaryOp(parts, (a, b) => (int)a ^ (int)b); break;
            case "nor": BinaryOp(parts, (a, b) => ~((int)a | (int)b)); break;
            case "not": UnaryOp(parts, a => ~(int)a); break;
            case "sll": BinaryOp(parts, (a, b) => (int)a << (int)b); break;
            case "srl": BinaryOp(parts, (a, b) => (int)((uint)a >> (int)b)); break;
            case "sra": BinaryOp(parts, (a, b) => (int)a >> (int)b); break;

            // Comparison
            case "seq": BinaryOp(parts, (a, b) => a == b ? 1 : 0); break;
            case "sne": BinaryOp(parts, (a, b) => a != b ? 1 : 0); break;
            case "slt": BinaryOp(parts, (a, b) => a < b ? 1 : 0); break;
            case "sgt": BinaryOp(parts, (a, b) => a > b ? 1 : 0); break;
            case "sle": BinaryOp(parts, (a, b) => a <= b ? 1 : 0); break;
            case "sge": BinaryOp(parts, (a, b) => a >= b ? 1 : 0); break;
            case "seqz": UnaryOp(parts, a => a == 0 ? 1 : 0); break;
            case "snez": UnaryOp(parts, a => a != 0 ? 1 : 0); break;
            case "sgtz": UnaryOp(parts, a => a > 0 ? 1 : 0); break;
            case "sltz": UnaryOp(parts, a => a < 0 ? 1 : 0); break;
            case "sgez": UnaryOp(parts, a => a >= 0 ? 1 : 0); break;
            case "slez": UnaryOp(parts, a => a <= 0 ? 1 : 0); break;
            case "snan": UnaryOp(parts, a => double.IsNaN(a) ? 1 : 0); break;
            case "snaz": UnaryOp(parts, a => double.IsNaN(a) || a == 0 ? 1 : 0); break;
            case "sap": TernaryOp(parts, (a, b, c) => Math.Abs(a - b) <= Math.Max(c * Math.Max(Math.Abs(a), Math.Abs(b)), double.Epsilon * 8) ? 1 : 0); break;

            // Select
            case "select": Select(parts); break;

            // Branching
            case "j": Jump(parts); break;
            case "jr": JumpRelative(parts); break;
            case "jal": JumpAndLink(parts); break;
            case "beq": BranchIf(parts, (a, b) => a == b); break;
            case "bne": BranchIf(parts, (a, b) => a != b); break;
            case "blt": BranchIf(parts, (a, b) => a < b); break;
            case "bgt": BranchIf(parts, (a, b) => a > b); break;
            case "ble": BranchIf(parts, (a, b) => a <= b); break;
            case "bge": BranchIf(parts, (a, b) => a >= b); break;
            case "beqz": BranchIfZero(parts, a => a == 0); break;
            case "bnez": BranchIfZero(parts, a => a != 0); break;
            case "bgtz": BranchIfZero(parts, a => a > 0); break;
            case "bltz": BranchIfZero(parts, a => a < 0); break;
            case "bgez": BranchIfZero(parts, a => a >= 0); break;
            case "blez": BranchIfZero(parts, a => a <= 0); break;
            case "bnan": BranchIfZero(parts, a => double.IsNaN(a)); break;
            case "bnaz": BranchIfZero(parts, a => double.IsNaN(a) || a == 0); break;
            case "bap": BranchApprox(parts); break;

            // Stack
            case "push": Push(parts); break;
            case "pop": Pop(parts); break;
            case "peek": Peek(parts); break;

            // Device I/O (simulated)
            case "l": LoadDevice(parts); break;
            case "s": StoreDevice(parts); break;
            case "ls": LoadSlot(parts); break;
            case "ss": StoreSlot(parts); break;

            // Flow control
            case "yield": IsYielding = true; ProgramCounter++; break;
            case "sleep": ProgramCounter++; break; // Simulated as instant
            case "hcf": IsHalted = true; break;

            default:
                // Unknown instruction - just advance
                ProgramCounter++;
                break;
        }
    }

    private double GetValue(string operand)
    {
        // Check for register (r0-r15 only)
        if (operand.StartsWith("r"))
        {
            if (int.TryParse(operand.Substring(1), out int regNum) && regNum >= 0 && regNum < NumericRegisterCount)
            {
                return Registers[regNum];
            }
        }
        else if (operand == "sp")
        {
            return StackPointer;
        }
        else if (operand == "ra")
        {
            return Registers[17]; // Return address stored at internal index 17
        }

        // Check for define
        if (_defines.TryGetValue(operand, out int defValue))
        {
            return defValue;
        }

        // Try to parse as number
        if (double.TryParse(operand, out double value))
        {
            return value;
        }

        throw new InvalidOperationException($"Invalid operand: {operand}");
    }

    private int GetRegisterIndex(string operand)
    {
        if (operand.StartsWith("r"))
        {
            // IC10 only has r0-r15 (16 registers)
            if (int.TryParse(operand.Substring(1), out int regNum) && regNum >= 0 && regNum < NumericRegisterCount)
            {
                return regNum;
            }
        }
        else if (operand == "sp")
        {
            return 16; // Internal index for stack pointer
        }
        else if (operand == "ra")
        {
            return 17; // Internal index for return address
        }

        throw new InvalidOperationException($"Invalid register: {operand}");
    }

    private int GetLabel(string label)
    {
        if (_labels.TryGetValue(label, out int line))
        {
            return line;
        }

        // Try as number
        if (int.TryParse(label, out int lineNum))
        {
            return lineNum;
        }

        throw new InvalidOperationException($"Unknown label: {label}");
    }

    private void Move(string[] parts)
    {
        var dest = GetRegisterIndex(parts[1]);
        var value = GetValue(parts[2]);
        Registers[dest] = value;
        ProgramCounter++;
    }

    private void BinaryOp(string[] parts, Func<double, double, double> op)
    {
        var dest = GetRegisterIndex(parts[1]);
        var a = GetValue(parts[2]);
        var b = GetValue(parts[3]);
        Registers[dest] = op(a, b);
        ProgramCounter++;
    }

    private void UnaryOp(string[] parts, Func<double, double> op)
    {
        var dest = GetRegisterIndex(parts[1]);
        var a = GetValue(parts[2]);
        Registers[dest] = op(a);
        ProgramCounter++;
    }

    private void TernaryOp(string[] parts, Func<double, double, double, double> op)
    {
        var dest = GetRegisterIndex(parts[1]);
        var a = GetValue(parts[2]);
        var b = GetValue(parts[3]);
        var c = GetValue(parts[4]);
        Registers[dest] = op(a, b, c);
        ProgramCounter++;
    }

    private void Rand(string[] parts)
    {
        var dest = GetRegisterIndex(parts[1]);
        Registers[dest] = Random.Shared.NextDouble();
        ProgramCounter++;
    }

    private void Select(string[] parts)
    {
        var dest = GetRegisterIndex(parts[1]);
        var cond = GetValue(parts[2]);
        var trueVal = GetValue(parts[3]);
        var falseVal = GetValue(parts[4]);
        Registers[dest] = cond != 0 ? trueVal : falseVal;
        ProgramCounter++;
    }

    private void Jump(string[] parts)
    {
        ProgramCounter = GetLabel(parts[1]);
    }

    private void JumpRelative(string[] parts)
    {
        var offset = (int)GetValue(parts[1]);
        ProgramCounter += offset;
    }

    private void JumpAndLink(string[] parts)
    {
        Registers[17] = ProgramCounter + 1; // ra
        ProgramCounter = GetLabel(parts[1]);
    }

    private void BranchIf(string[] parts, Func<double, double, bool> condition)
    {
        var a = GetValue(parts[1]);
        var b = GetValue(parts[2]);
        if (condition(a, b))
        {
            ProgramCounter = GetLabel(parts[3]);
        }
        else
        {
            ProgramCounter++;
        }
    }

    private void BranchIfZero(string[] parts, Func<double, bool> condition)
    {
        var a = GetValue(parts[1]);
        if (condition(a))
        {
            ProgramCounter = GetLabel(parts[2]);
        }
        else
        {
            ProgramCounter++;
        }
    }

    private void BranchApprox(string[] parts)
    {
        var a = GetValue(parts[1]);
        var b = GetValue(parts[2]);
        var eps = GetValue(parts[3]);
        if (Math.Abs(a - b) <= Math.Max(eps * Math.Max(Math.Abs(a), Math.Abs(b)), double.Epsilon * 8))
        {
            ProgramCounter = GetLabel(parts[4]);
        }
        else
        {
            ProgramCounter++;
        }
    }

    private void Push(string[] parts)
    {
        if (StackPointer >= StackSize)
        {
            throw new InvalidOperationException("Stack overflow");
        }
        Stack[StackPointer++] = GetValue(parts[1]);
        ProgramCounter++;
    }

    private void Pop(string[] parts)
    {
        if (StackPointer <= 0)
        {
            throw new InvalidOperationException("Stack underflow");
        }
        var dest = GetRegisterIndex(parts[1]);
        Registers[dest] = Stack[--StackPointer];
        ProgramCounter++;
    }

    private void Peek(string[] parts)
    {
        var dest = GetRegisterIndex(parts[1]);
        if (StackPointer <= 0)
        {
            Registers[dest] = 0;
        }
        else
        {
            Registers[dest] = Stack[StackPointer - 1];
        }
        ProgramCounter++;
    }

    private void LoadDevice(string[] parts)
    {
        var dest = GetRegisterIndex(parts[1]);
        var deviceIndex = GetDeviceIndex(parts[2]);
        var property = parts[3];

        if (deviceIndex >= 0 && deviceIndex < DeviceCount)
        {
            Registers[dest] = Devices[deviceIndex].GetProperty(property);
        }
        else
        {
            Registers[dest] = 0;
        }
        ProgramCounter++;
    }

    private void StoreDevice(string[] parts)
    {
        var deviceIndex = GetDeviceIndex(parts[1]);
        var property = parts[2];
        var value = GetValue(parts[3]);

        if (deviceIndex >= 0 && deviceIndex < DeviceCount)
        {
            Devices[deviceIndex].SetProperty(property, value);
        }
        ProgramCounter++;
    }

    private void LoadSlot(string[] parts)
    {
        var dest = GetRegisterIndex(parts[1]);
        var deviceIndex = GetDeviceIndex(parts[2]);
        var slotIndex = (int)GetValue(parts[3]);
        var property = parts[4];

        if (deviceIndex >= 0 && deviceIndex < DeviceCount)
        {
            Registers[dest] = Devices[deviceIndex].GetSlotProperty(slotIndex, property);
        }
        else
        {
            Registers[dest] = 0;
        }
        ProgramCounter++;
    }

    private void StoreSlot(string[] parts)
    {
        var deviceIndex = GetDeviceIndex(parts[1]);
        var slotIndex = (int)GetValue(parts[2]);
        var property = parts[3];
        var value = GetValue(parts[4]);

        if (deviceIndex >= 0 && deviceIndex < DeviceCount)
        {
            Devices[deviceIndex].SetSlotProperty(slotIndex, property, value);
        }
        ProgramCounter++;
    }

    private int GetDeviceIndex(string operand)
    {
        // Check for d0-d5
        if (operand.StartsWith("d") && int.TryParse(operand.Substring(1), out int index))
        {
            return index;
        }

        // Check for alias
        for (int i = 0; i < DeviceCount; i++)
        {
            if (Devices[i].Alias != null &&
                Devices[i].Alias.Equals(operand, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}

public class SimDevice
{
    public string Name { get; set; } = "";
    public string? Alias { get; set; }
    public Dictionary<string, double> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<int, Dictionary<string, double>> Slots { get; } = new();

    public SimDevice()
    {
        // Initialize default properties
        Properties["On"] = 0;
        Properties["Setting"] = 0;
        Properties["Mode"] = 0;
        Properties["Open"] = 0;
        Properties["Lock"] = 0;
        Properties["Error"] = 0;
        Properties["Power"] = 1;
        Properties["Temperature"] = 293.15; // 20C in Kelvin
        Properties["Pressure"] = 101.325;   // 1 atm
        Properties["Charge"] = 1;
    }

    public double GetProperty(string name)
    {
        return Properties.TryGetValue(name, out double value) ? value : 0;
    }

    public void SetProperty(string name, double value)
    {
        Properties[name] = value;
    }

    public double GetSlotProperty(int slot, string name)
    {
        if (Slots.TryGetValue(slot, out var slotProps) &&
            slotProps.TryGetValue(name, out double value))
        {
            return value;
        }
        return 0;
    }

    public void SetSlotProperty(int slot, string name, double value)
    {
        if (!Slots.ContainsKey(slot))
        {
            Slots[slot] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
        Slots[slot][name] = value;
    }
}
