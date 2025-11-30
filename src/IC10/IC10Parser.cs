namespace BasicToMips.IC10;

/// <summary>
/// Parses IC10 MIPS assembly code into a structured representation
/// </summary>
public class IC10Parser
{
    public IC10Program Parse(string source)
    {
        var program = new IC10Program();
        var lines = source.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Parse the line
            var instruction = ParseLine(line, i + 1);
            if (instruction != null)
            {
                program.Instructions.Add(instruction);
            }
        }

        return program;
    }

    private IC10Instruction? ParseLine(string line, int lineNumber)
    {
        // Handle comments
        if (line.StartsWith("#"))
        {
            return new IC10Instruction
            {
                LineNumber = lineNumber,
                Type = IC10InstructionType.Comment,
                Comment = line.Substring(1).Trim()
            };
        }

        // Strip inline comments
        string? inlineComment = null;
        var commentIndex = line.IndexOf('#');
        if (commentIndex > 0)
        {
            inlineComment = line.Substring(commentIndex + 1).Trim();
            line = line.Substring(0, commentIndex).Trim();
        }

        // Handle labels
        if (line.EndsWith(":"))
        {
            return new IC10Instruction
            {
                LineNumber = lineNumber,
                Type = IC10InstructionType.Label,
                Label = line.TrimEnd(':'),
                Comment = inlineComment
            };
        }

        // Parse instruction
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        var opcode = parts[0].ToLowerInvariant();
        var operands = parts.Skip(1).ToArray();

        var instruction = new IC10Instruction
        {
            LineNumber = lineNumber,
            Opcode = opcode,
            Operands = operands,
            Comment = inlineComment,
            Type = ClassifyInstruction(opcode)
        };

        return instruction;
    }

    private IC10InstructionType ClassifyInstruction(string opcode)
    {
        return opcode switch
        {
            "alias" => IC10InstructionType.Alias,
            "define" => IC10InstructionType.Define,
            "move" => IC10InstructionType.Move,
            "add" or "sub" or "mul" or "div" or "mod" => IC10InstructionType.Arithmetic,
            "sqrt" or "abs" or "floor" or "ceil" or "round" or "trunc" or
            "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "atan2" or
            "log" or "exp" or "max" or "min" or "rand" => IC10InstructionType.Math,
            "seq" or "sne" or "slt" or "sgt" or "sle" or "sge" or
            "seqz" or "snez" or "sgtz" or "sltz" or "sgez" or "slez" or
            "snan" or "snaz" or "sap" or "sapz" => IC10InstructionType.Compare,
            "select" => IC10InstructionType.Select,
            "j" or "jr" or "jal" => IC10InstructionType.Jump,
            "beq" or "bne" or "blt" or "bgt" or "ble" or "bge" or
            "beqz" or "bnez" or "bgtz" or "bltz" or "bgez" or "blez" or
            "bnan" or "bnaz" or "bap" or "bapz" => IC10InstructionType.Branch,
            "and" or "or" or "xor" or "nor" or "not" or
            "sll" or "srl" or "sra" => IC10InstructionType.Bitwise,
            "push" or "pop" or "peek" or "put" or "get" or "poke" or "getd" or "putd" => IC10InstructionType.Stack,
            "l" or "ls" or "lr" or "lb" or "lbn" or "lbs" or "lbns" => IC10InstructionType.DeviceRead,
            "s" or "ss" or "sb" or "sbn" or "sbs" or "sbns" => IC10InstructionType.DeviceWrite,
            "yield" => IC10InstructionType.Yield,
            "sleep" => IC10InstructionType.Sleep,
            "hcf" => IC10InstructionType.Halt,
            "sdse" or "sdns" or "bdse" or "bdns" => IC10InstructionType.DeviceCheck,
            _ => IC10InstructionType.Unknown
        };
    }
}

public class IC10Program
{
    public List<IC10Instruction> Instructions { get; } = new();

    public Dictionary<string, string> Aliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, double> Defines { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Labels { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Analyze()
    {
        for (int i = 0; i < Instructions.Count; i++)
        {
            var inst = Instructions[i];

            if (inst.Type == IC10InstructionType.Alias && inst.Operands.Length >= 2)
            {
                Aliases[inst.Operands[0]] = inst.Operands[1];
            }
            else if (inst.Type == IC10InstructionType.Define && inst.Operands.Length >= 2)
            {
                if (double.TryParse(inst.Operands[1], out var value))
                {
                    Defines[inst.Operands[0]] = value;
                }
            }
            else if (inst.Type == IC10InstructionType.Label && !string.IsNullOrEmpty(inst.Label))
            {
                Labels[inst.Label] = i;
            }
        }
    }
}

public class IC10Instruction
{
    public int LineNumber { get; set; }
    public IC10InstructionType Type { get; set; }
    public string? Opcode { get; set; }
    public string[] Operands { get; set; } = Array.Empty<string>();
    public string? Label { get; set; }
    public string? Comment { get; set; }

    public override string ToString()
    {
        if (Type == IC10InstructionType.Comment)
            return $"# {Comment}";
        if (Type == IC10InstructionType.Label)
            return $"{Label}:";
        if (Opcode != null)
            return $"{Opcode} {string.Join(" ", Operands)}";
        return "";
    }
}

public enum IC10InstructionType
{
    Unknown,
    Comment,
    Label,
    Alias,
    Define,
    Move,
    Arithmetic,
    Math,
    Compare,
    Select,
    Jump,
    Branch,
    Bitwise,
    Stack,
    DeviceRead,
    DeviceWrite,
    DeviceCheck,
    Yield,
    Sleep,
    Halt
}
