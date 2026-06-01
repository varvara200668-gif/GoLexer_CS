using GoLexer.SourceCodeDriver;
using GoLexer.Scanner;
using ErrorClass = GoLexer.Error.Error;
using ScannerClass = GoLexer.Scanner.Scanner;
using StatisticsClass = GoLexer.Statistics.Statistics;

namespace GoLexer;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Лексический анализатор языка Go");

        ErrorClass error = new();
        StatisticsClass statistics = new();
        List<string> analyzedFiles = new();

        if (args.Length == 0)
        {
            Console.WriteLine("В качестве аргументов должны быть указаны один или несколько файлов *.go");
            Console.WriteLine("Пример: dotnet run -- examples/correct.go examples/incorrect.go");
            return;
        }

        foreach (string path in args)
        {
            Driver driver = new(error);
            if (!driver.ResetText(path))
            {
                continue;
            }

            analyzedFiles.Add(path);
            ScannerClass scanner = new(driver, error);

            Token token;
            do
            {
                int errorsBefore = error.GetErrors().Count;

                token = scanner.NextLex();

                int errorsAfter = error.GetErrors().Count;
                bool tokenHasError = errorsAfter > errorsBefore;

                if (!tokenHasError && token.Type != Lex.EndOfText && token.Type != Lex.Unknown)
                {
                    statistics.AddToken(token);
                }
            }
            while (token.Type != Lex.EndOfText);
        }

        Console.WriteLine();
        Console.WriteLine("Files analyzed:");
        if (analyzedFiles.Count == 0)
        {
            Console.WriteLine("No files analyzed.");
        }
        else
        {
            foreach (string file in analyzedFiles)
            {
                Console.WriteLine($"- {file}");
            }
        }

        statistics.PrintStatistics();
        statistics.PrintIdentifiers();
        error.PrintErrors();
    }
}
