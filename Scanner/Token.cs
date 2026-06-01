namespace GoLexer.Scanner;

public enum Lex
{
    Keyword,
    Identifier,
    IntegerLiteral,
    FloatLiteral,
    ImaginaryLiteral,
    StringLiteral,
    RuneLiteral,
    Operator,
    Delimiter,
    EndOfText,
    Unknown
}

public class Token
{
    public Lex Type { get; set; }
    public string Text { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
