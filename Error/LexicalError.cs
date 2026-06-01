namespace GoLexer.Error;

public class LexicalError
{
    public string FileName { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Fragment { get; set; } = string.Empty;
}
