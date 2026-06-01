using System.Text;
using GoLexer.SourceCodeDriver;

namespace GoLexer.Scanner;

public class Scanner
{
    private readonly Driver _driver;
    private readonly GoLexer.Error.Error _error;
    private Token _currentToken = new();

    private readonly Dictionary<string, Lex> _keywords = new()
    {
        {"break", Lex.Keyword}, {"default", Lex.Keyword}, {"func", Lex.Keyword},
        {"interface", Lex.Keyword}, {"select", Lex.Keyword}, {"case", Lex.Keyword},
        {"defer", Lex.Keyword}, {"go", Lex.Keyword}, {"map", Lex.Keyword},
        {"struct", Lex.Keyword}, {"chan", Lex.Keyword}, {"else", Lex.Keyword},
        {"goto", Lex.Keyword}, {"package", Lex.Keyword}, {"switch", Lex.Keyword},
        {"const", Lex.Keyword}, {"fallthrough", Lex.Keyword}, {"if", Lex.Keyword},
        {"range", Lex.Keyword}, {"type", Lex.Keyword}, {"continue", Lex.Keyword},
        {"for", Lex.Keyword}, {"import", Lex.Keyword}, {"return", Lex.Keyword},
        {"var", Lex.Keyword}
    };

    private readonly string[] _operators =
    {
        "<<=", ">>=", "&^=", "...",
        "<<", ">>", "&^", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
        "&&", "||", "<-", "++", "--", "==", "!=", "<=", ">=", ":=",
        "+", "-", "*", "/", "%", "&", "|", "^", "<", ">", "=", "!", "."
    };

    private readonly HashSet<char> _delimiters = new()
    {
        '(', ')', '[', ']', '{', '}', ',', ';', ':'
    };

    public Scanner(Driver driver, GoLexer.Error.Error error)
    {
        _driver = driver;
        _error = error;
    }

    public Token CurrentToken
    {
        get { return _currentToken; }
    }

    public List<Token> ScanAllTokens()
    {
        List<Token> tokens = new();
        Token token;

        do
        {
            token = NextLex();
            if (token.Type != Lex.EndOfText && token.Type != Lex.Unknown)
            {
                tokens.Add(token);
            }
        }
        while (token.Type != Lex.EndOfText);

        return tokens;
    }

    public Token NextLex()
    {
        SkipWhitespace();

        int line = _driver.Line;
        int column = _driver.Column;

        if (_driver.IsEnd())
        {
            _currentToken = MakeToken(Lex.EndOfText, string.Empty, line, column);
            return _currentToken;
        }

        if (IsIdentifierStart(_driver.Ch))
        {
            _currentToken = ScanName();
        }
        else if (char.IsDigit(_driver.Ch) || (_driver.Ch == '.' && char.IsDigit(_driver.PeekCh())))
        {
            _currentToken = ScanNumber();
        }
        else if (_driver.Ch == '"')
        {
            _currentToken = ScanString();
        }
        else if (_driver.Ch == '`')
        {
            _currentToken = ScanRawString();
        }
        else if (_driver.Ch == '\'')
        {
            _currentToken = ScanRune();
        }
        else if (_driver.Ch == '/' && _driver.PeekCh() == '/')
        {
            ScanLineComment();
            return NextLex();
        }
        else if (_driver.Ch == '/' && _driver.PeekCh() == '*')
        {
            ScanBlockComment();
            return NextLex();
        }
        else
        {
            _currentToken = ScanOperatorOrDelimiter();
        }

        return _currentToken;
    }

    public Token ScanName()
    {
        int line = _driver.Line;
        int column = _driver.Column;
        StringBuilder buffer = new();

        while (IsIdentifierPart(_driver.Ch))
        {
            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        string text = buffer.ToString();
        Lex type = _keywords.ContainsKey(text) ? Lex.Keyword : Lex.Identifier;
        return MakeToken(type, text, line, column);
    }

    public Token ScanNumber()
    {
        int line = _driver.Line;
        int column = _driver.Column;
        StringBuilder buffer = new();
        bool isFloat = false;
        bool isImaginary = false;
        bool invalid = false;

        if (_driver.Ch == '.')
        {
            isFloat = true;
            buffer.Append(_driver.Ch);
            _driver.NextCh();
            ReadDecimalDigits(buffer, ref invalid);
        }
        else if (_driver.Ch == '0' && IsBasePrefix(_driver.PeekCh()))
        {
            buffer.Append(_driver.Ch);
            char prefix = _driver.PeekCh();
            _driver.NextCh();
            buffer.Append(_driver.Ch);
            _driver.NextCh();

            int numberBase = GetNumberBase(prefix);
            bool hasDigits = ReadDigits(buffer, numberBase, ref invalid);
            if (!hasDigits)
            {
                invalid = true;
            }

            if (_driver.Ch == '.' || _driver.Ch == 'e' || _driver.Ch == 'E' || _driver.Ch == 'p' || _driver.Ch == 'P')
            {
                invalid = true;
                ReadNumberTail(buffer);
            }
        }
        else
        {
            ReadDecimalDigits(buffer, ref invalid);

            if (_driver.Ch == '.')
            {
                isFloat = true;
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                ReadDecimalDigits(buffer, ref invalid);
            }

            if (_driver.Ch == 'e' || _driver.Ch == 'E')
            {
                isFloat = true;
                buffer.Append(_driver.Ch);
                _driver.NextCh();

                if (_driver.Ch == '+' || _driver.Ch == '-')
                {
                    buffer.Append(_driver.Ch);
                    _driver.NextCh();
                }

                bool hasExponentDigits = ReadDecimalDigits(buffer, ref invalid);
                if (!hasExponentDigits)
                {
                    invalid = true;
                }
            }
        }

        if (_driver.Ch == 'i')
        {
            isImaginary = true;
            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        if (IsIdentifierStart(_driver.Ch))
        {
            invalid = true;
            ReadNumberTail(buffer);
        }

        string text = buffer.ToString();
        if (HasBadUnderscoreUsage(text))
        {
            invalid = true;
        }

        if (invalid)
        {
            _error.AddError(_driver.FileName, line, column, "некорректная числовая константа", text);
        }

        Lex type = isImaginary ? Lex.ImaginaryLiteral : isFloat ? Lex.FloatLiteral : Lex.IntegerLiteral;
        return MakeToken(type, text, line, column);
    }

    public Token ScanString()
    {
        int line = _driver.Line;
        int column = _driver.Column;
        StringBuilder buffer = new();
        bool closed = false;

        buffer.Append(_driver.Ch);
        _driver.NextCh();

        while (!_driver.IsEnd())
        {
            if (_driver.Ch == '\n')
            {
                break;
            }

            if (_driver.Ch == '\\')
            {
                buffer.Append(_driver.Ch);
                _driver.NextCh();

                if (_driver.IsEnd() || _driver.Ch == '\n')
                {
                    break;
                }

                if (!IsValidEscape(_driver.Ch))
                {
                    _error.AddError(_driver.FileName, _driver.Line, _driver.Column, "некорректная escape-последовательность", "\\" + _driver.Ch);
                }

                buffer.Append(_driver.Ch);
                _driver.NextCh();
                continue;
            }

            if (_driver.Ch == '"')
            {
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                closed = true;
                break;
            }

            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        string text = buffer.ToString();
        if (!closed)
        {
            _error.AddError(_driver.FileName, line, column, "незакрытая строка", text);
        }

        return MakeToken(Lex.StringLiteral, text, line, column);
    }

    public Token ScanRawString()
    {
        int line = _driver.Line;
        int column = _driver.Column;
        StringBuilder buffer = new();
        bool closed = false;

        buffer.Append(_driver.Ch);
        _driver.NextCh();

        while (!_driver.IsEnd())
        {
            if (_driver.Ch == '`')
            {
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                closed = true;
                break;
            }

            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        string text = buffer.ToString();
        if (!closed)
        {
            _error.AddError(_driver.FileName, line, column, "незакрытая сырая строка", text);
        }

        return MakeToken(Lex.StringLiteral, text, line, column);
    }

    public Token ScanRune()
    {
        int line = _driver.Line;
        int column = _driver.Column;
        StringBuilder buffer = new();
        bool closed = false;
        bool hasContent = false;

        buffer.Append(_driver.Ch);
        _driver.NextCh();

        while (!_driver.IsEnd())
        {
            if (_driver.Ch == '\n')
            {
                break;
            }

            if (_driver.Ch == '\\')
            {
                hasContent = true;
                buffer.Append(_driver.Ch);
                _driver.NextCh();

                if (_driver.IsEnd() || _driver.Ch == '\n')
                {
                    break;
                }

                if (!IsValidEscape(_driver.Ch))
                {
                    _error.AddError(_driver.FileName, _driver.Line, _driver.Column, "некорректная escape-последовательность", "\\" + _driver.Ch);
                }

                buffer.Append(_driver.Ch);
                _driver.NextCh();
                continue;
            }

            if (_driver.Ch == '\'')
            {
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                closed = true;
                break;
            }

            hasContent = true;
            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        string text = buffer.ToString();
        if (!closed)
        {
            _error.AddError(_driver.FileName, line, column, "незакрытый rune literal", text);
        }
        else if (!hasContent)
        {
            _error.AddError(_driver.FileName, line, column, "некорректный rune literal", text);
        }

        return MakeToken(Lex.RuneLiteral, text, line, column);
    }

    public void ScanLineComment()
    {
        while (!_driver.IsEnd() && _driver.Ch != '\n')
        {
            _driver.NextCh();
        }
    }

    public void ScanBlockComment()
    {
        int line = _driver.Line;
        int column = _driver.Column;
        StringBuilder buffer = new();
        bool closed = false;

        buffer.Append(_driver.Ch);
        _driver.NextCh();
        buffer.Append(_driver.Ch);
        _driver.NextCh();

        while (!_driver.IsEnd())
        {
            if (_driver.Ch == '*' && _driver.PeekCh() == '/')
            {
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                closed = true;
                break;
            }

            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        string text = buffer.ToString();
        if (!closed)
        {
            _error.AddError(_driver.FileName, line, column, "незакрытый многострочный комментарий", text);
        }
    }

    public Token ScanOperatorOrDelimiter()
    {
        int line = _driver.Line;
        int column = _driver.Column;

        if (_delimiters.Contains(_driver.Ch))
        {
            string delimiter = _driver.Ch.ToString();
            _driver.NextCh();
            return MakeToken(Lex.Delimiter, delimiter, line, column);
        }

        foreach (string op in _operators)
        {
            if (Matches(op))
            {
                for (int i = 0; i < op.Length; i++)
                {
                    _driver.NextCh();
                }

                return MakeToken(Lex.Operator, op, line, column);
            }
        }

        string fragment = _driver.Ch.ToString();
        _error.AddError(_driver.FileName, line, column, "неизвестный символ", fragment);
        _driver.NextCh();
        return MakeToken(Lex.Unknown, fragment, line, column);
    }

    public string GetStringNameOfLex(Lex lex)
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

    private void SkipWhitespace()
    {
        while (_driver.Ch == ' ' || _driver.Ch == '\t' || _driver.Ch == '\n')
        {
            _driver.NextCh();
        }
    }

    private Token MakeToken(Lex type, string text, int line, int column)
    {
        return new Token
        {
            Type = type,
            Text = text,
            FileName = _driver.FileName,
            Line = line,
            Column = column
        };
    }

    private bool Matches(string text)
    {
        if (_driver.Ch != text[0])
        {
            return false;
        }

        for (int i = 1; i < text.Length; i++)
        {
            if (_driver.PeekCh(i - 1) != text[i])
            {
                return false;
            }
        }

        return true;
    }

    private bool IsIdentifierStart(char ch)
    {
        return ch == '_' || (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    private bool IsIdentifierPart(char ch)
    {
        return IsIdentifierStart(ch) || char.IsDigit(ch);
    }

    private bool IsBasePrefix(char ch)
    {
        return ch == 'b' || ch == 'B' || ch == 'o' || ch == 'O' || ch == 'x' || ch == 'X';
    }

    private int GetNumberBase(char prefix)
    {
        return prefix switch
        {
            'b' or 'B' => 2,
            'o' or 'O' => 8,
            'x' or 'X' => 16,
            _ => 10
        };
    }

    private bool ReadDecimalDigits(StringBuilder buffer, ref bool invalid)
    {
        bool hasDigits = false;
        bool previousWasUnderscore = false;

        while (char.IsDigit(_driver.Ch) || _driver.Ch == '_')
        {
            if (_driver.Ch == '_')
            {
                if (!hasDigits || previousWasUnderscore)
                {
                    invalid = true;
                }

                previousWasUnderscore = true;
            }
            else
            {
                hasDigits = true;
                previousWasUnderscore = false;
            }

            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        if (previousWasUnderscore)
        {
            invalid = true;
        }

        return hasDigits;
    }

    private bool ReadDigits(StringBuilder buffer, int numberBase, ref bool invalid)
    {
        bool hasDigits = false;

        while (IsDigitForNumberOrUnderscore(_driver.Ch))
        {
            if (_driver.Ch == '_')
            {
                buffer.Append(_driver.Ch);
                _driver.NextCh();
                continue;
            }

            if (!IsDigitForBase(_driver.Ch, numberBase))
            {
                invalid = true;
            }
            else
            {
                hasDigits = true;
            }

            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }

        return hasDigits;
    }

    private bool IsDigitForNumberOrUnderscore(char ch)
    {
        return char.IsDigit(ch) || ch == '_' || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
    }

    private bool IsDigitForBase(char ch, int numberBase)
    {
        if (ch >= '0' && ch <= '9')
        {
            return ch - '0' < numberBase;
        }

        if (numberBase == 16 && ch >= 'a' && ch <= 'f')
        {
            return true;
        }

        if (numberBase == 16 && ch >= 'A' && ch <= 'F')
        {
            return true;
        }

        return false;
    }

    private void ReadNumberTail(StringBuilder buffer)
    {
        while (!_driver.IsEnd() && !char.IsWhiteSpace(_driver.Ch) && !_delimiters.Contains(_driver.Ch) && !IsOperatorStart(_driver.Ch))
        {
            buffer.Append(_driver.Ch);
            _driver.NextCh();
        }
    }

    private bool IsOperatorStart(char ch)
    {
        return "+-*/%&|^<>=!.".Contains(ch);
    }

    private bool HasBadUnderscoreUsage(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        string cleaned = text.EndsWith("i", StringComparison.Ordinal) ? text[..^1] : text;
        if (cleaned.StartsWith("_", StringComparison.Ordinal) || cleaned.EndsWith("_", StringComparison.Ordinal))
        {
            return true;
        }

        return cleaned.Contains("__", StringComparison.Ordinal)
               || cleaned.Contains("._", StringComparison.Ordinal)
               || cleaned.Contains("_.", StringComparison.Ordinal)
               || cleaned.Contains("e_", StringComparison.Ordinal)
               || cleaned.Contains("E_", StringComparison.Ordinal)
               || cleaned.Contains("_e", StringComparison.Ordinal)
               || cleaned.Contains("_E", StringComparison.Ordinal)
               || cleaned.Contains("+_", StringComparison.Ordinal)
               || cleaned.Contains("-_", StringComparison.Ordinal);
    }

    private bool IsValidEscape(char ch)
    {
        return ch == 'a' || ch == 'b' || ch == 'f' || ch == 'n' || ch == 'r' || ch == 't' || ch == 'v'
               || ch == '\\' || ch == '\'' || ch == '"' || ch == '0';
    }
}
