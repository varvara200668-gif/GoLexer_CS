using GoLexer.Error;

namespace GoLexer.SourceCodeDriver;

public class Driver
{
    private string _text = string.Empty;
    private int _position;
    private int _nextLine = 1;
    private int _nextColumn = 1;
    private readonly GoLexer.Error.Error _error;

    public char Ch { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public int Line { get; private set; } = 1;
    public int Column { get; private set; }

    public Driver(GoLexer.Error.Error error)
    {
        _error = error;
    }

    public bool ResetText(string path)
    {
        FileName = path;
        _text = string.Empty;
        _position = 0;
        _nextLine = 1;
        _nextColumn = 1;
        Line = 1;
        Column = 0;
        Ch = '\0';

        try
        {
            _text = File.ReadAllText(path);
            _text = _text.Replace("\r\n", "\n").Replace('\r', '\n');
            NextCh();
            return true;
        }
        catch (Exception ex)
        {
            _error.AddError(path, 0, 0, "ошибка открытия файла", ex.Message);
            Ch = '\0';
            return false;
        }
    }

    public void NextCh()
    {
        if (_position >= _text.Length)
        {
            Ch = '\0';
            Line = _nextLine;
            Column = _nextColumn;
            return;
        }

        Ch = _text[_position];
        _position++;

        Line = _nextLine;
        Column = _nextColumn;

        if (Ch == '\n')
        {
            _nextLine++;
            _nextColumn = 1;
        }
        else
        {
            _nextColumn++;
        }
    }

    public char PeekCh(int offset = 0)
    {
        int index = _position + offset;
        if (index >= _text.Length)
        {
            return '\0';
        }
        return _text[index];
    }

    public bool IsEnd()
    {
        return Ch == '\0';
    }
}
