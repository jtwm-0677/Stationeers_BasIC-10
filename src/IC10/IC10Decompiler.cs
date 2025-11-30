using System.Text;
using BasicToMips.Data;

namespace BasicToMips.IC10;

/// <summary>
/// Decompiles IC10 MIPS assembly back to BASIC code
/// </summary>
public class IC10Decompiler
{
    private readonly IC10Program _program;
    private readonly StringBuilder _output = new();
    private readonly Dictionary<string, string> _registerVars = new();
    private readonly Dictionary<int, string> _hashToDevice = new();
    private readonly Dictionary<int, string> _hashToUserName = new();
    private int _varCounter = 0;

    public IC10Decompiler(IC10Program program)
    {
        _program = program;
        _program.Analyze();
        InitializeHashLookups();
        ExtractNameMappingsFromComments();
    }

    private void InitializeHashLookups()
    {
        // Build reverse lookup from device database
        foreach (var device in DeviceDatabase.Devices)
        {
            if (!_hashToDevice.ContainsKey(device.Hash))
            {
                _hashToDevice[device.Hash] = device.PrefabName;
            }
        }
    }

    /// <summary>
    /// Extracts device type and user-defined name mappings from compiler-generated comments.
    /// The compiler emits comments like:
    ///   # Device type: "StructureLogicSwitch"
    ///   # Device name: "MySwitch"
    /// followed by define statements with the hash values.
    /// </summary>
    private void ExtractNameMappingsFromComments()
    {
        string? pendingDeviceType = null;
        string? pendingDeviceName = null;

        for (int i = 0; i < _program.Instructions.Count; i++)
        {
            var inst = _program.Instructions[i];

            if (inst.Type == IC10InstructionType.Comment && inst.Comment != null)
            {
                // Check for device type comment: Device type: "SomeName"
                if (inst.Comment.StartsWith("Device type:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        inst.Comment, @"Device type:\s*""([^""]+)""");
                    if (match.Success)
                    {
                        pendingDeviceType = match.Groups[1].Value;
                    }
                }
                // Check for device name comment: Device name: "SomeName"
                else if (inst.Comment.StartsWith("Device name:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        inst.Comment, @"Device name:\s*""([^""]+)""");
                    if (match.Success)
                    {
                        pendingDeviceName = match.Groups[1].Value;
                    }
                }
            }
            else if (inst.Type == IC10InstructionType.Define && inst.Operands.Length >= 2)
            {
                var defineName = inst.Operands[0];
                var defineValue = inst.Operands[1];

                // If we have a pending device type and this define ends with _HASH
                if (pendingDeviceType != null && defineName.EndsWith("_HASH"))
                {
                    if (int.TryParse(defineValue, out int hash))
                    {
                        // Add to device lookup (may override database entry with more specific name)
                        _hashToDevice[hash] = pendingDeviceType;
                    }
                    pendingDeviceType = null;
                }
                // If we have a pending device name and this define ends with _NAME
                else if (pendingDeviceName != null && defineName.EndsWith("_NAME"))
                {
                    if (int.TryParse(defineValue, out int hash))
                    {
                        _hashToUserName[hash] = pendingDeviceName;
                    }
                    pendingDeviceName = null;
                }
            }
        }
    }

    /// <summary>
    /// Tries to find a device type name for a hash, returns the hash as string if not found
    /// </summary>
    private string LookupDeviceHash(string hashStr)
    {
        if (int.TryParse(hashStr, out int hash))
        {
            if (_hashToDevice.TryGetValue(hash, out var deviceName))
            {
                return $"HASH(\"{deviceName}\")";
            }
        }
        return hashStr; // Return raw hash if not found
    }

    /// <summary>
    /// Tries to find a user-defined device name for a hash, returns the hash as string if not found
    /// </summary>
    private string LookupUserNameHash(string hashStr)
    {
        if (int.TryParse(hashStr, out int hash))
        {
            if (_hashToUserName.TryGetValue(hash, out var userName))
            {
                return $"HASH(\"{userName}\")";
            }
        }
        return hashStr; // Return raw hash if not found
    }

    public string Decompile()
    {
        _output.Clear();
        _output.AppendLine("' Decompiled from IC10 MIPS assembly");
        _output.AppendLine();

        // Output aliases
        foreach (var alias in _program.Aliases)
        {
            _output.AppendLine($"ALIAS {alias.Key} {alias.Value}");
        }

        // Output defines
        foreach (var define in _program.Defines)
        {
            _output.AppendLine($"DEFINE {define.Key} {FormatNumber(define.Value)}");
        }

        if (_program.Aliases.Count > 0 || _program.Defines.Count > 0)
        {
            _output.AppendLine();
        }

        // Track which labels we've emitted
        var emittedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process instructions
        for (int i = 0; i < _program.Instructions.Count; i++)
        {
            var inst = _program.Instructions[i];
            DecompileInstruction(inst, i);
        }

        return _output.ToString();
    }

    private void DecompileInstruction(IC10Instruction inst, int index)
    {
        switch (inst.Type)
        {
            case IC10InstructionType.Comment:
                _output.AppendLine($"' {inst.Comment}");
                break;

            case IC10InstructionType.Label:
                _output.AppendLine($"{inst.Label}:");
                break;

            case IC10InstructionType.Alias:
            case IC10InstructionType.Define:
                // Already handled at the top
                break;

            case IC10InstructionType.Move:
                DecompileMove(inst);
                break;

            case IC10InstructionType.Arithmetic:
                DecompileArithmetic(inst);
                break;

            case IC10InstructionType.Math:
                DecompileMath(inst);
                break;

            case IC10InstructionType.Compare:
                DecompileCompare(inst);
                break;

            case IC10InstructionType.Select:
                DecompileSelect(inst);
                break;

            case IC10InstructionType.Jump:
                DecompileJump(inst);
                break;

            case IC10InstructionType.Branch:
                DecompileBranch(inst, index);
                break;

            case IC10InstructionType.Bitwise:
                DecompileBitwise(inst);
                break;

            case IC10InstructionType.Stack:
                DecompileStack(inst);
                break;

            case IC10InstructionType.DeviceRead:
                DecompileDeviceRead(inst);
                break;

            case IC10InstructionType.DeviceWrite:
                DecompileDeviceWrite(inst);
                break;

            case IC10InstructionType.DeviceCheck:
                DecompileDeviceCheck(inst);
                break;

            case IC10InstructionType.Yield:
                _output.AppendLine("    YIELD");
                break;

            case IC10InstructionType.Sleep:
                if (inst.Operands.Length > 0)
                    _output.AppendLine($"    SLEEP {TranslateOperand(inst.Operands[0])}");
                else
                    _output.AppendLine("    SLEEP 1");
                break;

            case IC10InstructionType.Halt:
                _output.AppendLine("    END");
                break;

            default:
                // Output as comment for unknown instructions
                _output.AppendLine($"    ' IC10: {inst}");
                break;
        }
    }

    private void DecompileMove(IC10Instruction inst)
    {
        if (inst.Operands.Length < 2) return;

        var dest = GetVarName(inst.Operands[0]);
        var src = TranslateOperand(inst.Operands[1]);

        _output.AppendLine($"    {dest} = {src}");
    }

    private void DecompileArithmetic(IC10Instruction inst)
    {
        if (inst.Operands.Length < 3) return;

        var dest = GetVarName(inst.Operands[0]);
        var op1 = TranslateOperand(inst.Operands[1]);
        var op2 = TranslateOperand(inst.Operands[2]);

        var op = inst.Opcode switch
        {
            "add" => "+",
            "sub" => "-",
            "mul" => "*",
            "div" => "/",
            "mod" => "MOD",
            _ => "?"
        };

        _output.AppendLine($"    {dest} = {op1} {op} {op2}");
    }

    private void DecompileMath(IC10Instruction inst)
    {
        if (inst.Operands.Length < 2) return;

        var dest = GetVarName(inst.Operands[0]);
        var funcName = inst.Opcode?.ToUpperInvariant() ?? "";

        if (inst.Opcode == "rand")
        {
            _output.AppendLine($"    {dest} = RND()");
        }
        else if (inst.Operands.Length >= 3 && (inst.Opcode == "max" || inst.Opcode == "min" || inst.Opcode == "atan2"))
        {
            var op1 = TranslateOperand(inst.Operands[1]);
            var op2 = TranslateOperand(inst.Operands[2]);
            _output.AppendLine($"    {dest} = {funcName}({op1}, {op2})");
        }
        else
        {
            var op1 = TranslateOperand(inst.Operands[1]);
            _output.AppendLine($"    {dest} = {funcName}({op1})");
        }
    }

    private void DecompileCompare(IC10Instruction inst)
    {
        if (inst.Operands.Length < 2) return;

        var dest = GetVarName(inst.Operands[0]);

        // Handle zero comparisons
        if (inst.Opcode?.EndsWith("z") == true && inst.Opcode != "snaz")
        {
            var op1 = TranslateOperand(inst.Operands[1]);
            var comparison = inst.Opcode switch
            {
                "seqz" => "== 0",
                "snez" => "<> 0",
                "sgtz" => "> 0",
                "sltz" => "< 0",
                "sgez" => ">= 0",
                "slez" => "<= 0",
                _ => "== 0"
            };
            _output.AppendLine($"    {dest} = ({op1} {comparison})");
        }
        else if (inst.Operands.Length >= 3)
        {
            var op1 = TranslateOperand(inst.Operands[1]);
            var op2 = TranslateOperand(inst.Operands[2]);

            var comparison = inst.Opcode switch
            {
                "seq" => "==",
                "sne" => "<>",
                "slt" => "<",
                "sgt" => ">",
                "sle" => "<=",
                "sge" => ">=",
                "snan" => "ISNAN",
                "snaz" => "ISNANORZERO",
                _ => "=="
            };

            if (inst.Opcode == "snan" || inst.Opcode == "snaz")
            {
                _output.AppendLine($"    {dest} = {comparison}({op1})");
            }
            else
            {
                _output.AppendLine($"    {dest} = ({op1} {comparison} {op2})");
            }
        }
    }

    private void DecompileSelect(IC10Instruction inst)
    {
        if (inst.Operands.Length < 4) return;

        var dest = GetVarName(inst.Operands[0]);
        var cond = TranslateOperand(inst.Operands[1]);
        var trueVal = TranslateOperand(inst.Operands[2]);
        var falseVal = TranslateOperand(inst.Operands[3]);

        _output.AppendLine($"    {dest} = IIF({cond}, {trueVal}, {falseVal})");
    }

    private void DecompileJump(IC10Instruction inst)
    {
        if (inst.Operands.Length < 1) return;

        var target = inst.Operands[0];

        switch (inst.Opcode)
        {
            case "j":
                // Check if it's a return (j ra)
                if (target.Equals("ra", StringComparison.OrdinalIgnoreCase))
                {
                    _output.AppendLine("    RETURN");
                }
                else
                {
                    _output.AppendLine($"    GOTO {target}");
                }
                break;
            case "jal":
                _output.AppendLine($"    GOSUB {target}");
                break;
            case "jr":
                _output.AppendLine($"    ' JUMP RELATIVE {target}");
                break;
        }
    }

    private void DecompileBranch(IC10Instruction inst, int index)
    {
        if (inst.Operands.Length < 2) return;

        // For zero-comparison branches
        if (inst.Opcode?.EndsWith("z") == true)
        {
            var op1 = TranslateOperand(inst.Operands[0]);
            var target = inst.Operands[1];

            var comparison = inst.Opcode switch
            {
                "beqz" => "== 0",
                "bnez" => "<> 0",
                "bgtz" => "> 0",
                "bltz" => "< 0",
                "bgez" => ">= 0",
                "blez" => "<= 0",
                "bnan" => "ISNAN",
                _ => "== 0"
            };

            if (inst.Opcode == "bnan")
            {
                _output.AppendLine($"    IF ISNAN({op1}) THEN GOTO {target}");
            }
            else
            {
                _output.AppendLine($"    IF {op1} {comparison} THEN GOTO {target}");
            }
        }
        else if (inst.Operands.Length >= 3)
        {
            var op1 = TranslateOperand(inst.Operands[0]);
            var op2 = TranslateOperand(inst.Operands[1]);
            var target = inst.Operands[2];

            var comparison = inst.Opcode switch
            {
                "beq" => "==",
                "bne" => "<>",
                "blt" => "<",
                "bgt" => ">",
                "ble" => "<=",
                "bge" => ">=",
                _ => "=="
            };

            _output.AppendLine($"    IF {op1} {comparison} {op2} THEN GOTO {target}");
        }
    }

    private void DecompileBitwise(IC10Instruction inst)
    {
        if (inst.Operands.Length < 2) return;

        var dest = GetVarName(inst.Operands[0]);

        if (inst.Opcode == "not" && inst.Operands.Length >= 2)
        {
            var op1 = TranslateOperand(inst.Operands[1]);
            _output.AppendLine($"    {dest} = BNOT({op1})");
        }
        else if (inst.Operands.Length >= 3)
        {
            var op1 = TranslateOperand(inst.Operands[1]);
            var op2 = TranslateOperand(inst.Operands[2]);

            var funcName = inst.Opcode switch
            {
                "and" => "BAND",
                "or" => "BOR",
                "xor" => "BXOR",
                "nor" => "BNOR",
                "sll" => "SHL",
                "srl" => "SHR",
                "sra" => "SHRA",
                _ => inst.Opcode?.ToUpperInvariant() ?? ""
            };

            _output.AppendLine($"    {dest} = {funcName}({op1}, {op2})");
        }
    }

    private void DecompileStack(IC10Instruction inst)
    {
        if (inst.Operands.Length < 1) return;

        var operand = TranslateOperand(inst.Operands[0]);

        switch (inst.Opcode)
        {
            case "push":
                _output.AppendLine($"    PUSH {operand}");
                break;
            case "pop":
                var dest = GetVarName(inst.Operands[0]);
                _output.AppendLine($"    POP {dest}");
                break;
            case "peek":
                var destPeek = GetVarName(inst.Operands[0]);
                _output.AppendLine($"    PEEK {destPeek}");
                break;
            case "put":
                // put device index value - store value at stack index on device
                if (inst.Operands.Length >= 3)
                {
                    var device = TranslateDevice(inst.Operands[0]);
                    var index = TranslateOperand(inst.Operands[1]);
                    var value = TranslateOperand(inst.Operands[2]);
                    _output.AppendLine($"    ' Stack store: {device}[{index}] = {value}");
                }
                break;
            case "get":
                // get dest device index - load value from stack index on device
                if (inst.Operands.Length >= 3)
                {
                    var getDest = GetVarName(inst.Operands[0]);
                    var getDevice = TranslateDevice(inst.Operands[1]);
                    var getIndex = TranslateOperand(inst.Operands[2]);
                    _output.AppendLine($"    ' Stack load: {getDest} = {getDevice}[{getIndex}]");
                }
                break;
            case "poke":
                // poke address value - store value at memory address
                if (inst.Operands.Length >= 2)
                {
                    var addr = TranslateOperand(inst.Operands[0]);
                    var pokeValue = TranslateOperand(inst.Operands[1]);
                    _output.AppendLine($"    ' Memory store: [{addr}] = {pokeValue}");
                }
                break;
        }
    }

    private void DecompileDeviceRead(IC10Instruction inst)
    {
        if (inst.Operands.Length < 3) return;

        var dest = GetVarName(inst.Operands[0]);
        var device = TranslateDevice(inst.Operands[1]);

        switch (inst.Opcode)
        {
            case "l":
                var prop = inst.Operands[2];
                _output.AppendLine($"    {dest} = {device}.{prop}");
                break;

            case "ls":
                if (inst.Operands.Length >= 4)
                {
                    var slot = TranslateOperand(inst.Operands[2]);
                    var slotProp = inst.Operands[3];
                    _output.AppendLine($"    {dest} = {device}.Slot({slot}).{slotProp}");
                }
                break;

            case "lb":
                if (inst.Operands.Length >= 4)
                {
                    var hash = LookupDeviceHash(inst.Operands[1]);
                    var batchProp = inst.Operands[2];
                    var mode = inst.Operands[3];
                    var modeStr = mode switch
                    {
                        "0" => "Average",
                        "1" => "Sum",
                        "2" => "Min",
                        "3" => "Max",
                        _ => mode
                    };
                    _output.AppendLine($"    {dest} = BATCHREAD({hash}, \"{batchProp}\", {modeStr})");
                }
                break;

            case "lbn":
                // lbn dest deviceHash nameHash property mode
                if (inst.Operands.Length >= 5)
                {
                    var deviceHash = LookupDeviceHash(inst.Operands[1]);
                    var nameHash = LookupUserNameHash(inst.Operands[2]);
                    var lbnProp = inst.Operands[3];
                    var lbnMode = inst.Operands[4];
                    var lbnModeStr = lbnMode switch
                    {
                        "0" => "Average",
                        "1" => "Sum",
                        "2" => "Min",
                        "3" => "Max",
                        _ => lbnMode
                    };
                    _output.AppendLine($"    {dest} = BATCHREAD_NAMED({deviceHash}, {nameHash}, \"{lbnProp}\", {lbnModeStr})");
                }
                break;

            case "lbs":
                // lbs dest deviceHash slotIndex property mode
                if (inst.Operands.Length >= 5)
                {
                    var lbsHash = LookupDeviceHash(inst.Operands[1]);
                    var lbsSlot = TranslateOperand(inst.Operands[2]);
                    var lbsProp = inst.Operands[3];
                    var lbsMode = inst.Operands[4];
                    var lbsModeStr = lbsMode switch
                    {
                        "0" => "Average",
                        "1" => "Sum",
                        "2" => "Min",
                        "3" => "Max",
                        _ => lbsMode
                    };
                    _output.AppendLine($"    {dest} = BATCHREAD_SLOT({lbsHash}, {lbsSlot}, \"{lbsProp}\", {lbsModeStr})");
                }
                break;

            default:
                _output.AppendLine($"    ' IC10: {inst}");
                break;
        }
    }

    private void DecompileDeviceWrite(IC10Instruction inst)
    {
        if (inst.Operands.Length < 3) return;

        var device = TranslateDevice(inst.Operands[0]);

        switch (inst.Opcode)
        {
            case "s":
                var prop = inst.Operands[1];
                var value = TranslateOperand(inst.Operands[2]);
                _output.AppendLine($"    {device}.{prop} = {value}");
                break;

            case "ss":
                if (inst.Operands.Length >= 4)
                {
                    var slot = TranslateOperand(inst.Operands[1]);
                    var slotProp = inst.Operands[2];
                    var slotValue = TranslateOperand(inst.Operands[3]);
                    _output.AppendLine($"    {device}.Slot({slot}).{slotProp} = {slotValue}");
                }
                break;

            case "sb":
                if (inst.Operands.Length >= 3)
                {
                    var hash = LookupDeviceHash(inst.Operands[0]);
                    var batchProp = inst.Operands[1];
                    var batchValue = TranslateOperand(inst.Operands[2]);
                    _output.AppendLine($"    BATCHWRITE({hash}, \"{batchProp}\", {batchValue})");
                }
                break;

            case "sbn":
                // sbn deviceHash nameHash property value
                if (inst.Operands.Length >= 4)
                {
                    var sbnDevHash = LookupDeviceHash(inst.Operands[0]);
                    var sbnNameHash = LookupUserNameHash(inst.Operands[1]);
                    var sbnProp = inst.Operands[2];
                    var sbnValue = TranslateOperand(inst.Operands[3]);
                    _output.AppendLine($"    BATCHWRITE_NAMED({sbnDevHash}, {sbnNameHash}, \"{sbnProp}\", {sbnValue})");
                }
                break;

            case "sbs":
                // sbs deviceHash slotIndex property value
                if (inst.Operands.Length >= 4)
                {
                    var sbsHash = LookupDeviceHash(inst.Operands[0]);
                    var sbsSlot = TranslateOperand(inst.Operands[1]);
                    var sbsProp = inst.Operands[2];
                    var sbsValue = TranslateOperand(inst.Operands[3]);
                    _output.AppendLine($"    BATCHWRITE_SLOT({sbsHash}, {sbsSlot}, \"{sbsProp}\", {sbsValue})");
                }
                break;

            default:
                _output.AppendLine($"    ' IC10: {inst}");
                break;
        }
    }

    private void DecompileDeviceCheck(IC10Instruction inst)
    {
        if (inst.Operands.Length < 2) return;

        var dest = GetVarName(inst.Operands[0]);
        var device = TranslateDevice(inst.Operands[1]);

        var funcName = inst.Opcode switch
        {
            "sdse" => "DEVICESET",
            "sdns" => "DEVICENOTSET",
            "bdse" => "BATCHDEVICESET",
            "bdns" => "BATCHDEVICENOTSET",
            _ => "DEVICECHECK"
        };

        _output.AppendLine($"    {dest} = {funcName}({device})");
    }

    private string GetVarName(string register)
    {
        if (!_registerVars.TryGetValue(register, out var varName))
        {
            // Generate a meaningful variable name based on the register
            if (register.StartsWith("r", StringComparison.OrdinalIgnoreCase))
            {
                varName = $"v{register.Substring(1)}";
            }
            else
            {
                varName = $"var{_varCounter++}";
            }
            _registerVars[register] = varName;
        }
        return varName;
    }

    private string TranslateOperand(string operand)
    {
        // Check if it's a register
        if (operand.StartsWith("r", StringComparison.OrdinalIgnoreCase) &&
            operand.Length > 1 && char.IsDigit(operand[1]))
        {
            return GetVarName(operand);
        }

        // Check for defines
        if (_program.Defines.ContainsKey(operand))
        {
            return operand; // Use the constant name
        }

        // It's a literal value
        return operand;
    }

    private string TranslateDevice(string device)
    {
        // Check for aliases
        foreach (var alias in _program.Aliases)
        {
            if (alias.Value.Equals(device, StringComparison.OrdinalIgnoreCase))
            {
                return alias.Key;
            }
        }
        return device;
    }

    private static string FormatNumber(double value)
    {
        if (value == Math.Floor(value) && Math.Abs(value) < 1e10)
        {
            return ((long)value).ToString();
        }
        return value.ToString("G");
    }
}
