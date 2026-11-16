using System.Collections;
using System.Globalization;

namespace Lusamine.SlimExpressionParser;

/// <summary>The evaluated arguments of a function call, with helpers that convert them.</summary>
public sealed class Arguments : IReadOnlyList<object>
{
    private readonly List<object> _values;
    private readonly int _position;

    internal Arguments(List<object> values, int position)
    {
        _values = values;
        _position = position;
    }

    /// <summary>The number of arguments.</summary>
    public int Count => _values.Count;

    /// <summary>Gets an argument's value unchanged.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public object this[int index] => _values[index];

    /// <summary>Returns a number as is, or parses a string with the invariant culture.</summary>
    /// <exception cref="ExpressionException">The argument is missing, isn't a number or a string, or the string isn't a finite number.</exception>
    public double Number(int index) => Get(index) switch
    {
        double number => number,
        string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            && !double.IsNaN(parsed) && !double.IsInfinity(parsed) => parsed,
        _ => throw Invalid(index, "a number or numeric text"),
    };

    /// <summary>Converts the argument to text: strings as is, numbers with the invariant culture, booleans as <c>true</c> or <c>false</c>, and other objects with <see cref="object.ToString"/>.</summary>
    /// <exception cref="ExpressionException">The argument is missing.</exception>
    public string Text(int index) => Values.ToText(Get(index));

    /// <summary>Returns a boolean as is, or parses the strings <c>"true"</c> and <c>"false"</c> ignoring case.</summary>
    /// <exception cref="ExpressionException">The argument is missing or isn't a boolean, <c>"true"</c> or <c>"false"</c>.</exception>
    public bool Boolean(int index) => Get(index) switch
    {
        bool boolean => boolean,
        string text when text.Equals("true", StringComparison.OrdinalIgnoreCase) => true,
        string text when text.Equals("false", StringComparison.OrdinalIgnoreCase) => false,
        _ => throw Invalid(index, "a boolean or the text true or false"),
    };

    /// <summary>Enumerates the argument values unchanged.</summary>
    public IEnumerator<object> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private object Get(int index) => index >= 0 && index < _values.Count
        ? _values[index]
        : throw new ExpressionException($"Argument {index} is missing: the function received {_values.Count}", _position);

    private ExpressionException Invalid(int index, string expected) => new($"Argument {index} must be {expected}", _position);
}
