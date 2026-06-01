namespace GoLexer.Error;

public class Error
{
    private readonly List<LexicalError> _errors = new();

    public void AddError(string fileName, int line, int column, string message, string fragment)
    {
        _errors.Add(new LexicalError
        {
            FileName = fileName,
            Line = line,
            Column = column,
            Message = message,
            Fragment = fragment
        });
    }

    public IReadOnlyList<LexicalError> GetErrors()
    {
        return _errors;
    }

    public bool HasErrors()
    {
        return _errors.Count > 0;
    }

    public void PrintErrors()
    {
        Console.WriteLine();
        Console.WriteLine("Lexical errors:");

        if (!HasErrors())
        {
            Console.WriteLine("No lexical errors found.");
            return;
        }

        foreach (LexicalError error in _errors)
        {
            string fragment = string.IsNullOrEmpty(error.Fragment) ? string.Empty : $" [{error.Fragment}]";
            Console.WriteLine($"{error.FileName}:{error.Line}:{error.Column} — {error.Message}{fragment}");
        }
    }
}
