namespace BasicToMips.Lexer;

public enum TokenType
{
    // Literals
    Number,
    String,
    Identifier,
    True,
    False,

    // Keywords
    Let,
    Print,
    Input,
    If,
    Then,
    Else,
    ElseIf,
    EndIf,
    For,
    To,
    Step,
    Next,
    While,
    Wend,
    Do,
    Loop,
    Until,
    Goto,
    Gosub,
    Return,
    End,
    Rem,
    Dim,
    As,
    Integer,
    Single,
    And,
    Or,
    Not,
    Mod,
    Def,
    Fn,
    Sub,
    EndSub,
    Function,
    EndFunction,
    Call,
    Exit,
    Sleep,
    Yield,
    Break,
    Continue,
    Select,
    Case,
    Default,
    EndSelect,
    Const,
    Var,
    Push,
    Pop,
    Peek,
    Include,
    On,
    Data,
    Read,
    Restore,

    // Bitwise operators (keywords)
    BitAnd,
    BitOr,
    BitXor,
    BitNot,
    Shl,
    Shr,

    // Device I/O keywords (Stationeers specific)
    Device,
    Alias,
    Define,
    Hash,

    // Operators
    Plus,
    Minus,
    Multiply,
    Divide,
    Power,
    Equal,
    NotEqual,
    LessThan,
    GreaterThan,
    LessEqual,
    GreaterEqual,
    Ampersand,      // & for bitwise AND
    Pipe,           // | for bitwise OR
    Caret,          // ^ for XOR (or power depending on context)
    Tilde,          // ~ for bitwise NOT
    ShiftLeft,      // <<
    ShiftRight,     // >>
    Increment,      // ++
    Decrement,      // --
    PlusEqual,      // +=
    MinusEqual,     // -=
    MultiplyEqual,  // *=
    DivideEqual,    // /=

    // Delimiters
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    Comma,
    Colon,
    Semicolon,
    Dot,

    // Special
    Newline,
    Eof,

    // Line number
    LineNumber,

    // Comments (when preserved)
    Comment,
    MetaComment  // ##Meta: tags for compiler directives
}
