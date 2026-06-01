using GoLexer.Scanner;

namespace GoLexer.Statistics;

public class Statistics
{
    private int _totalTokens;
    private readonly Dictionary<Lex, int> _tokenCounts = new();
    private readonly SortedDictionary<string, SortedSet<string>> _identifiers = new(StringComparer.Ordinal);

    public void AddToken(Token token)
    {
        if (token.Type == Lex.EndOfText || token.Type == Lex.Unknown)
        {
            return;
        }

        _totalTokens++;

        if (!_tokenCounts.ContainsKey(token.Type))
        {
            _tokenCounts[token.Type] = 0;
        }

        _tokenCounts[token.Type]++;

        if (token.Type == Lex.Identifier)
        {
            string location = $"{token.FileName}:{token.Line}";
            if (!_identifiers.ContainsKey(token.Text))
            {
                _identifiers[token.Text] = new SortedSet<string>(StringComparer.Ordinal);
            }

            _identifiers[token.Text].Add(location);
        }
    }

    public void PrintStatistics()
    {
        Console.WriteLine();
        Console.WriteLine($"Total tokens: {_totalTokens}");
        Console.WriteLine();
        Console.WriteLine("Token statistics:");
        Console.WriteLine($"{ "Type",-24}{ "Count",-10}{ "Frequency"}");

        foreach (Lex type in GetOrderedTypes())
        {
            if (!_tokenCounts.TryGetValue(type, out int count))
            {
                continue;
            }

            double frequency = _totalTokens == 0 ? 0.0 : (double)count / _totalTokens * 100.0;
            Console.WriteLine($"{GetStringNameOfLex(type),-24}{count,-10}{frequency:0.00}%");
        }
    }

    public void PrintIdentifiers()
    {
        Console.WriteLine();
        Console.WriteLine("Identifiers:");

        if (_identifiers.Count == 0)
        {
            Console.WriteLine("No identifiers found.");
            return;
        }

        Console.WriteLine($"{ "Identifier",-24}{ "Locations"}");
        foreach (KeyValuePair<string, SortedSet<string>> item in _identifiers)
        {
            Console.WriteLine($"{item.Key,-24}{string.Join(", ", item.Value)}");
        }
    }

    private IEnumerable<Lex> GetOrderedTypes()
    {
        return new[]
        {
            Lex.Keyword,
            Lex.Identifier,
            Lex.IntegerLiteral,
            Lex.FloatLiteral,
            Lex.ImaginaryLiteral,
            Lex.StringLiteral,
            Lex.RuneLiteral,
            Lex.Operator,
            Lex.Delimiter
        };
    }

    private string GetStringNameOfLex(Lex lex)
    {
        return lex switch
        {
            Lex.Keyword => "KEYWORD",
            Lex.Identifier => "IDENTIFIER",
            Lex.IntegerLiteral => "INTEGER_LITERAL",
            Lex.FloatLiteral => "FLOAT_LITERAL",
            Lex.ImaginaryLiteral => "IMAGINARY_LITERAL",
            Lex.StringLiteral => "STRING_LITERAL",
            Lex.RuneLiteral => "RUNE_LITERAL",
            Lex.Operator => "OPERATOR",
            Lex.Delimiter => "DELIMITER",
            Lex.EndOfText => "END_OF_TEXT",
            Lex.Unknown => "UNKNOWN",
            _ => lex.ToString().ToUpperInvariant()
        };
    }
}
