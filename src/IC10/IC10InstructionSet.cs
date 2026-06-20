namespace BasicToMips.IC10;

/// <summary>
/// Knowledge about IC10 instructions used to validate raw ASM passthrough blocks.
/// Findings are advisory only (warnings) - the set is intentionally permissive so that
/// genuinely-new game instructions not yet listed here don't block compilation. (#8)
/// </summary>
public static class IC10InstructionSet
{
    /// <summary>
    /// Known IC10 opcodes. Unknown opcodes in an ASM block produce a warning, not an error.
    /// </summary>
    public static readonly HashSet<string> KnownOpcodes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Declarations / misc
        "alias", "define", "move", "yield", "sleep", "hcf", "label",
        // Arithmetic
        "add", "sub", "mul", "div", "mod", "abs", "ceil", "floor", "round", "trunc",
        "exp", "log", "max", "min", "rand", "sqrt",
        // Trig
        "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
        // Bitwise / logic
        "and", "or", "xor", "nor", "not", "sll", "srl", "sra", "sla", "ext", "ins",
        // Comparison / set
        "select",
        "seq", "seqz", "sne", "snez", "slt", "sltz", "sle", "slez",
        "sgt", "sgtz", "sge", "sgez", "sap", "sapz", "sna", "snan", "snaz", "snanz",
        "sdse", "sdns",
        // Branch (absolute)
        "j", "jal", "jr",
        "beq", "beqz", "bne", "bnez", "blt", "bltz", "bgt", "bgtz", "ble", "blez",
        "bge", "bgez", "bap", "bapz", "bna", "bnan", "bnaz", "bnanz",
        "bdse", "bdns",
        // Branch-and-link variants
        "beqal", "bneal", "bltal", "bgtal", "bleal", "bgeal",
        "beqzal", "bnezal", "bltzal", "bgtzal", "blezal", "bgezal",
        "bapal", "bapzal", "bnaal", "bnazal", "bdseal", "bdnsal",
        // Branch (relative)
        "breq", "breqz", "brne", "brnez", "brlt", "brltz", "brgt", "brgtz",
        "brle", "brlez", "brge", "brgez", "brap", "brapz", "brna", "brnaz",
        "brdse", "brdns",
        // Device read / write
        "l", "lb", "lbn", "lbns", "lbs", "lr", "ls", "ld",
        "s", "sb", "sbn", "sbns", "sbs", "ss", "sd", "rmap",
        // Stack
        "push", "pop", "peek", "poke", "get", "put", "getd", "putd", "clr", "clrd",
    };

    /// <summary>
    /// Expected operand count for instructions whose arity is unambiguous and stable.
    /// Device I/O and branch instructions are deliberately omitted (mode/variant differences
    /// make their arity ambiguous) and are not arity-checked.
    /// </summary>
    public static readonly Dictionary<string, int> Arity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["move"] = 2,
        ["add"] = 3, ["sub"] = 3, ["mul"] = 3, ["div"] = 3, ["mod"] = 3,
        ["and"] = 3, ["or"] = 3, ["xor"] = 3, ["nor"] = 3,
        ["sll"] = 3, ["srl"] = 3, ["sra"] = 3,
        ["max"] = 3, ["min"] = 3, ["atan2"] = 3,
        ["abs"] = 2, ["ceil"] = 2, ["floor"] = 2, ["round"] = 2, ["trunc"] = 2,
        ["exp"] = 2, ["log"] = 2, ["sqrt"] = 2, ["not"] = 2,
        ["sin"] = 2, ["cos"] = 2, ["tan"] = 2, ["asin"] = 2, ["acos"] = 2, ["atan"] = 2,
        ["seq"] = 3, ["sne"] = 3, ["slt"] = 3, ["sle"] = 3, ["sgt"] = 3, ["sge"] = 3,
        ["seqz"] = 2, ["snez"] = 2, ["sltz"] = 2, ["slez"] = 2, ["sgtz"] = 2, ["sgez"] = 2,
        ["select"] = 4,
    };

    /// <summary>
    /// Instructions whose first operand is a destination register that gets written.
    /// Used to warn when an ASM block writes r0-r13 (which may hold a BASIC variable).
    /// </summary>
    public static readonly HashSet<string> WritesFirstOperand = new(StringComparer.OrdinalIgnoreCase)
    {
        "move",
        "add", "sub", "mul", "div", "mod", "abs", "ceil", "floor", "round", "trunc",
        "exp", "log", "max", "min", "rand", "sqrt",
        "sin", "cos", "tan", "asin", "acos", "atan", "atan2",
        "and", "or", "xor", "nor", "not", "sll", "srl", "sra",
        "select",
        "seq", "seqz", "sne", "snez", "slt", "sltz", "sle", "slez",
        "sgt", "sgtz", "sge", "sgez", "sap", "sapz", "snan", "snaz",
        "l", "lb", "lbn", "lbns", "lbs", "lr", "ls", "ld",
        "pop", "peek", "get", "getd",
    };

    /// <summary>
    /// True if the operand names a general-purpose register r0-r13 (the range the compiler
    /// allocates BASIC variables into; r14/r15 are scratch and safe for ASM).
    /// </summary>
    public static bool IsVariableRegister(string operand)
    {
        if (operand.Length < 2 || (operand[0] != 'r' && operand[0] != 'R')) return false;
        if (!int.TryParse(operand.AsSpan(1), out var n)) return false;
        return n >= 0 && n <= 13;
    }
}
