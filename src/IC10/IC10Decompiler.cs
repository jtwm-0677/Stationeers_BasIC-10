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
    private readonly Dictionary<int, string> _lineToLabel = new();
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
    /// Tries to find a device type name for a hash.
    /// Checks local context first, then the living hash dictionary.
    /// Returns [DEVICE_UNKNOWN:hash] if not found to prompt user to fill in.
    /// </summary>
    private string LookupDeviceHash(string hashStr)
    {
        if (int.TryParse(hashStr, out int hash))
        {
            // Check local context first
            if (_hashToDevice.TryGetValue(hash, out var deviceName))
            {
                return $"HASH(\"{deviceName}\")";
            }

            // Fall back to living hash dictionary
            var dictLookup = Data.HashDictionary.LookupHash(hash);
            if (dictLookup != null)
            {
                return $"HASH(\"{dictLookup}\")";
            }

            // Unknown hash - label it clearly for user to fill in
            return $"HASH(\"[DEVICE_UNKNOWN:{hash}]\")";
        }
        return hashStr; // Return as-is if not a numeric hash
    }

    /// <summary>
    /// Tries to find a user-defined device name for a hash.
    /// Checks local context first, then the living hash dictionary.
    /// Returns [NAME_UNKNOWN:hash] if not found to prompt user to fill in.
    /// </summary>
    private string LookupUserNameHash(string hashStr)
    {
        if (int.TryParse(hashStr, out int hash))
        {
            // Check local context first
            if (_hashToUserName.TryGetValue(hash, out var userName))
            {
                return $"HASH(\"{userName}\")";
            }

            // Fall back to living hash dictionary
            var dictLookup = Data.HashDictionary.LookupHash(hash);
            if (dictLookup != null)
            {
                return $"HASH(\"{dictLookup}\")";
            }

            // Unknown hash - label it clearly for user to fill in
            return $"HASH(\"[NAME_UNKNOWN:{hash}]\")";
        }
        return hashStr; // Return as-is if not a numeric hash
    }

    public string Decompile()
    {
        _output.Clear();
        _output.AppendLine("# Decompiled from IC10 MIPS assembly");
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

        // First pass: collect all numeric jump targets and generate labels for them
        CollectNumericJumpTargets();

        // Second pass: process instructions with label insertion
        for (int i = 0; i < _program.Instructions.Count; i++)
        {
            var inst = _program.Instructions[i];

            // Insert generated label if this instruction address is a jump target
            // (only executable instructions have valid addresses)
            if (inst.InstructionAddress >= 0 && _lineToLabel.TryGetValue(inst.InstructionAddress, out var label))
            {
                _output.AppendLine($"{label}:");
            }

            DecompileInstruction(inst, i);
        }

        return _output.ToString();
    }

    /// <summary>
    /// First pass: find all numeric jump targets and create labels for them.
    /// Jump targets in MIPS are instruction addresses (0-based, executable instructions only).
    /// </summary>
    private void CollectNumericJumpTargets()
    {
        foreach (var inst in _program.Instructions)
        {
            string? target = null;

            // Check jump instructions
            if (inst.Type == IC10InstructionType.Jump && inst.Operands.Length >= 1)
            {
                target = inst.Operands[0];
            }
            // Check branch instructions
            else if (inst.Type == IC10InstructionType.Branch)
            {
                if (inst.Opcode?.EndsWith("z") == true && inst.Operands.Length >= 2)
                {
                    target = inst.Operands[1];
                }
                else if (inst.Operands.Length >= 3)
                {
                    target = inst.Operands[2];
                }
            }

            // If target is a number (instruction address), create a label for it
            if (target != null && int.TryParse(target, out int address))
            {
                if (!_lineToLabel.ContainsKey(address))
                {
                    _lineToLabel[address] = $"addr_{address}";
                }
            }
        }
    }

    /// <summary>
    /// Converts a jump target to a label name. If the target is a number (instruction address), returns the generated label.
    /// </summary>
    private string TranslateJumpTarget(string target)
    {
        if (int.TryParse(target, out int address))
        {
            if (_lineToLabel.TryGetValue(address, out var label))
            {
                return label;
            }
            // If we don't have a label (shouldn't happen), create one
            var newLabel = $"addr_{address}";
            _lineToLabel[address] = newLabel;
            return newLabel;
        }
        return target; // Already a label name
    }

    private void DecompileInstruction(IC10Instruction inst, int index)
    {
        switch (inst.Type)
        {
            case IC10InstructionType.Comment:
                _output.AppendLine($"# {inst.Comment}");
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
                _output.AppendLine($"    # IC10: {inst}");
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
                    _output.AppendLine($"    GOTO {TranslateJumpTarget(target)}");
                }
                break;
            case "jal":
                _output.AppendLine($"    GOSUB {TranslateJumpTarget(target)}");
                break;
            case "jr":
                _output.AppendLine($"    # JUMP RELATIVE {target}");
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
            var target = TranslateJumpTarget(inst.Operands[1]);

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
            var target = TranslateJumpTarget(inst.Operands[2]);

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
                    _output.AppendLine($"    # Stack store: {device}[{index}] = {value}");
                }
                break;
            case "get":
                // get dest device index - load value from stack index on device
                if (inst.Operands.Length >= 3)
                {
                    var getDest = GetVarName(inst.Operands[0]);
                    var getDevice = TranslateDevice(inst.Operands[1]);
                    var getIndex = TranslateOperand(inst.Operands[2]);
                    _output.AppendLine($"    # Stack load: {getDest} = {getDevice}[{getIndex}]");
                }
                break;
            case "poke":
                if (inst.Operands.Length >= 2)
                {
                    var addr = TranslateOperand(inst.Operands[0]);
                    var pokeValue = TranslateOperand(inst.Operands[1]);
                    _output.AppendLine($"    POKE {addr}, {pokeValue}");
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
                        "2" => "Minimum",
                        "3" => "Maximum",
                        _ => lbsMode
                    };
                    _output.AppendLine($"    {dest} = BATCHSLOT({lbsHash}, {lbsSlot}, \"{lbsProp}\", {lbsModeStr})");
                }
                break;

            case "lbns":
                // lbns dest deviceHash nameHash slotIndex property mode
                if (inst.Operands.Length >= 6)
                {
                    var lbnsDevHash = LookupDeviceHash(inst.Operands[1]);
                    var lbnsNameHash = LookupUserNameHash(inst.Operands[2]);
                    var lbnsSlot = TranslateOperand(inst.Operands[3]);
                    var lbnsProp = inst.Operands[4];
                    var lbnsMode = inst.Operands[5];
                    var lbnsModeStr = lbnsMode switch
                    {
                        "0" => "Average",
                        "1" => "Sum",
                        "2" => "Minimum",
                        "3" => "Maximum",
                        _ => lbnsMode
                    };
                    _output.AppendLine($"    {dest} = BATCHSLOT({lbnsDevHash}, {lbnsNameHash}, {lbnsSlot}, \"{lbnsProp}\", {lbnsModeStr})");
                }
                break;

            default:
                _output.AppendLine($"    # IC10: {inst}");
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
                    _output.AppendLine($"    BATCHSLOT_WRITE({sbsHash}, {sbsSlot}, \"{sbsProp}\", {sbsValue})");
                }
                break;

            case "sbns":
                // sbns deviceHash nameHash slotIndex property value
                if (inst.Operands.Length >= 5)
                {
                    var sbnsDevHash = LookupDeviceHash(inst.Operands[0]);
                    var sbnsNameHash = LookupUserNameHash(inst.Operands[1]);
                    var sbnsSlot = TranslateOperand(inst.Operands[2]);
                    var sbnsProp = inst.Operands[3];
                    var sbnsValue = TranslateOperand(inst.Operands[4]);
                    _output.AppendLine($"    BATCHSLOT_WRITE({sbnsDevHash}, {sbnsNameHash}, {sbnsSlot}, \"{sbnsProp}\", {sbnsValue})");
                }
                break;

            default:
                _output.AppendLine($"    # IC10: {inst}");
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
            // First check if this is an alias name directly (e.g., "RoomSensor" used as destination)
            if (_program.Aliases.ContainsKey(register))
            {
                varName = register;
            }
            // Check if this is a register that has an alias pointing to it
            else if (IsActualRegister(register))
            {
                // Look for an alias that maps to this register
                var alias = _program.Aliases.FirstOrDefault(a =>
                    a.Value.Equals(register, StringComparison.OrdinalIgnoreCase));
                if (alias.Key != null)
                {
                    varName = alias.Key;
                }
                else
                {
                    varName = $"v{register.Substring(1)}";
                }
            }
            else
            {
                varName = $"var{_varCounter++}";
            }
            _registerVars[register] = varName;
        }
        return varName;
    }

    /// <summary>
    /// Checks if the operand is an actual IC10 register (r0-r17, ra, sp)
    /// rather than an alias name that happens to start with 'r'.
    /// </summary>
    private static bool IsActualRegister(string operand)
    {
        if (string.IsNullOrEmpty(operand)) return false;

        // Check for ra (return address) and sp (stack pointer)
        if (operand.Equals("ra", StringComparison.OrdinalIgnoreCase) ||
            operand.Equals("sp", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for r0-r17 pattern
        if (operand.StartsWith("r", StringComparison.OrdinalIgnoreCase) && operand.Length >= 2)
        {
            var numPart = operand.Substring(1);
            if (int.TryParse(numPart, out int regNum))
            {
                return regNum >= 0 && regNum <= 17;
            }
        }

        return false;
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
