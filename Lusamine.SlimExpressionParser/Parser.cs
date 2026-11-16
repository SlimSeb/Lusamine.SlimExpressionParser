using System.Globalization;
using System.Text;

namespace Lusamine.SlimExpressionParser;

/// <summary>A recursive-descent parser that evaluates the expression while it parses it.</summary>
internal sealed class Parser(string text, ExpressionContext context)
{
    private const int MaxDepth = 64;

    // Binary operators from lowest to highest precedence. Longer symbols come first, so "<=" wins over "<".
    private static readonly string[][] Levels =
    [
        ["||"],
        ["&&"],
        ["==", "!="],
        ["<=", ">=", "<", ">"],
        ["+", "-"],
        ["*", "/", "%"],
    ];

    private readonly string _text = text;
    private readonly ExpressionContext _context = context;
    private int _position;
    private int _depth;

    public object Evaluate()
    {
        SkipWhitespace();
        if (_position == _text.Length) throw new ExpressionException("The expression is empty", _position);
        var value = ParseBinary(0);
        SkipWhitespace();
        if (_position < _text.Length) throw new ExpressionException($"Unexpected '{_text[_position]}'", _position);
        return value;
    }

    private object ParseBinary(int level)
    {
        if (level == Levels.Length) return ParseUnary();
        var left = ParseBinary(level + 1);
        while (Match(Levels[level]) is { } symbol)
        {
            int symbolPosition = _position - symbol.Length;
            var right = ParseBinary(level + 1);
            left = Operators.Binary(symbol, left, right, symbolPosition);
        }
        return left;
    }

    private object ParseUnary()
    {
        SkipWhitespace();
        int start = _position;
        if (Peek() is not ('-' or '!')) return ParsePrimary();
        char symbol = _text[_position++];
        Enter(start);
        var operand = ParseUnary();
        _depth--;
        return Operators.Unary(symbol, operand, start);
    }

    private object ParsePrimary()
    {
        int start = _position;
        if (_position == _text.Length) throw new ExpressionException("Unexpected end of expression", start);
        char current = _text[_position];
        if (current == '(')
        {
            _position++;
            Enter(start);
            var value = ParseBinary(0);
            Expect(')');
            _depth--;
            return value;
        }
        if (IsDigit(current)) return ParseNumber(start);
        if (current is '\'' or '"') return ParseString(start);
        if (char.IsLetter(current) || current == '_') return ParseName(start);
        throw new ExpressionException($"Unexpected '{current}'", start);
    }

    private double ParseNumber(int start)
    {
        while (IsDigit(Peek())) _position++;
        if (Peek() == '.' && _position + 1 < _text.Length && IsDigit(_text[_position + 1]))
        {
            _position++;
            while (IsDigit(Peek())) _position++;
        }
        if (!double.TryParse(Slice(start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
            || double.IsInfinity(number))
        {
            throw new ExpressionException("The number is too large", start);
        }
        return number;
    }

    private string ParseString(int start)
    {
        char quote = _text[_position++];
        var builder = new StringBuilder();
        while (_position < _text.Length)
        {
            char current = _text[_position++];
            if (current == quote) return builder.ToString();
            if (current == '\\')
            {
                if (Peek() != quote && Peek() != '\\')
                {
                    throw new ExpressionException("A backslash can only escape the quote character or a backslash", _position - 1);
                }
                current = _text[_position++];
            }
            builder.Append(current);
        }
        throw new ExpressionException("Unterminated string", start);
    }

    private object ParseName(int start)
    {
        while (char.IsLetterOrDigit(Peek()) || Peek() == '_') _position++;
        var name = Slice(start);
        if (name is "true") return true;
        if (name is "false") return false;
        SkipWhitespace();
        if (Peek() != '(')
        {
            var identifier = Find(_context.Identifiers, name) ?? throw new ExpressionException($"Unknown identifier '{name}'", start);
            return Values.Normalize(identifier());
        }

        var function = Find(_context.Functions, name) ?? throw new ExpressionException($"Unknown function '{name}'", start);
        _position++;
        Enter(start);
        var values = new List<object>();
        if (!TryConsume(')'))
        {
            do
            {
                values.Add(ParseBinary(0));
            }
            while (TryConsume(','));
            Expect(')');
        }
        _depth--;
        return Values.Normalize(function(new Arguments(values, start)));
    }

    private void Enter(int start)
    {
        if (++_depth > MaxDepth)
        {
            throw new ExpressionException($"The expression is nested more than {MaxDepth} levels deep", start);
        }
    }

    private string? Match(string[] symbols)
    {
        SkipWhitespace();
        foreach (var symbol in symbols)
        {
            if (string.CompareOrdinal(_text, _position, symbol, 0, symbol.Length) != 0) continue;
            _position += symbol.Length;
            return symbol;
        }
        return null;
    }

    private bool TryConsume(char expected)
    {
        SkipWhitespace();
        if (Peek() != expected) return false;
        _position++;
        return true;
    }

    private void Expect(char expected)
    {
        if (!TryConsume(expected)) throw new ExpressionException($"Expected '{expected}'", _position);
    }

    private char Peek() => _position < _text.Length ? _text[_position] : '\0';

    private void SkipWhitespace()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position])) _position++;
    }

    private static bool IsDigit(char value) => value is >= '0' and <= '9';

#if NET10_0_OR_GREATER
    private ReadOnlySpan<char> Slice(int start) => _text.AsSpan(start, _position - start);

    private static TValue? Find<TValue>(Dictionary<string, TValue> map, ReadOnlySpan<char> name) where TValue : class
        => map.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out var value) ? value : null;
#else
    private string Slice(int start) => _text.Substring(start, _position - start);

    private static TValue? Find<TValue>(Dictionary<string, TValue> map, string name) where TValue : class
        => map.TryGetValue(name, out var value) ? value : null;
#endif
}
