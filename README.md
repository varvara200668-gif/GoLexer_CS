# GoLexer

GoLexer — учебный лексический анализатор языка Go, написанный на C#.

Проект сделан в стиле простого преподавательского компилятора с архитектурой `Driver / Scanner / Error / Program`, но без синтаксического анализатора. Программа не проверяет грамматику Go целиком, а выполняет только лексический анализ исходных файлов.

## Основные возможности

Программа принимает один или несколько файлов `.go` и формирует единый результат по всем входным файлам:

- считает общее количество лексем;
- считает абсолютную частоту лексем каждого типа;
- считает относительную частоту лексем каждого типа в процентах;
- собирает идентификаторы в лексикографическом порядке;
- для каждого идентификатора показывает имя файла и номера строк;
- накапливает лексические ошибки и выводит их после анализа всех файлов.

## Язык реализации

C# / .NET 8.

## Анализируемый язык

Go.

## Поддерживаемые типы лексем

- `Keyword` — ключевые слова Go;
- `Identifier` — идентификаторы;
- `IntegerLiteral` — целые числовые литералы;
- `FloatLiteral` — вещественные числовые литералы;
- `ImaginaryLiteral` — мнимые числовые литералы;
- `StringLiteral` — строковые литералы;
- `RuneLiteral` — rune literal;
- `Operator` — операторы;
- `Delimiter` — разделители;

Комментарии распознаются лексером, но не добавляются в список лексем и не учитываются в статистике.

## Поддерживаемые ошибки

- неизвестный символ;
- незакрытая строка;
- незакрытая сырая строка;
- незакрытый rune literal;
- некорректная escape-последовательность;
- незакрытый многострочный комментарий;
- некорректная числовая константа;
- ошибка открытия файла.

Ошибки не завершают программу сразу. Анализатор продолжает обработку остальных файлов, а затем выводит общий список ошибок.

## Структура проекта

```text
GoLexer/
├── Program.cs
├── GoLexer.csproj
├── Dockerfile
├── README.md
├── SourceCodeDriver/
│   └── Driver.cs
├── Scanner/
│   ├── Scanner.cs
│   └── Token.cs
├── Error/
│   ├── Error.cs
│   └── LexicalError.cs
├── Statistics/
│   └── Statistics.cs
├── specification/
│   └── go_lexical_specification.md
└── examples/
    ├── correct.go
    └── incorrect.go
```

## Сборка через .NET

В терминале Rider нужно перейти в папку проекта:

```bash
cd путь/к/GoLexer
```

Восстановить и собрать проект:

```bash
dotnet restore
dotnet build
```

## Запуск

Запуск одного файла:

```bash
dotnet run -- examples/correct.go
```

Запуск нескольких файлов:

```bash
dotnet run -- examples/correct.go examples/incorrect.go
```

После публикации или сборки исполняемый файл можно запускать так:

```bash
GoLexer file1.go file2.go file3.go
```

## Запуск через Docker

Сборка образа:

```bash
docker build -t golexer .
```

Запуск с примерами из проекта:

```bash
docker run --rm golexer examples/correct.go examples/incorrect.go
```

Если нужно передать файлы из текущей папки компьютера:

```bash
docker run --rm -v "$PWD":/data golexer /data/file1.go /data/file2.go
```

## Пример вывода

```text
Лексический анализатор языка Go

Files analyzed:
- examples/correct.go
- examples/incorrect.go

Total tokens: 95

Token statistics:
Type                    Count     Frequency
KEYWORD                 8         8.42%
IDENTIFIER              31        32.63%
INTEGER_LITERAL         5         5.26%
FLOAT_LITERAL           4         4.21%
IMAGINARY_LITERAL       1         1.05%
STRING_LITERAL          4         4.21%
RUNE_LITERAL            2         2.11%
OPERATOR                19        20.00%
DELIMITER               19        20.00%
COMMENT                 2         2.11%

Identifiers:
Identifier              Locations
Println                 examples/correct.go:19
bin                     examples/correct.go:10, examples/correct.go:19
count                   examples/correct.go:8, examples/correct.go:19
main                    examples/correct.go:1, examples/correct.go:6

Lexical errors:
examples/incorrect.go:4:13 — незакрытая строка ["broken string]
examples/incorrect.go:5:14 — некорректная числовая константа [0b102]
examples/incorrect.go:6:20 — некорректная escape-последовательность [\q]
examples/incorrect.go:8:10 — неизвестный символ [@]
examples/incorrect.go:9:5 — незакрытый многострочный комментарий [/* comment is not closed]
```
