namespace BasicToMips.Lexer;

public enum TokenType
{
    // Literals
    Number,
    String,
    Identifier,

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

    // Device I/O keywords (Stationeers specific)
    Device,
    Alias,
    Define,

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

    // Delimiters
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    Comma,
    Colon,
    Semicolon,

    // Special
    Newline,
    Eof,

    // Line number
    LineNumber
}
