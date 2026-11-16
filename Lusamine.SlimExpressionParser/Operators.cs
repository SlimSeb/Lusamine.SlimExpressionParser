namespace Lusamine.SlimExpressionParser;

/// <summary>Applies operators to evaluated values. Operators never coerce their operands.</summary>
internal static class Operators
{
    public static object Binary(string symbol, object left, object right, int position)
    {
        object? result = (symbol, left, right) switch
        {
            ("||", bool first, bool second) => first || second,
            ("&&", bool first, bool second) => first && second,
            ("==" or "!=", _, _) when AreEqual(left, right) is bool equal => equal == (symbol == "=="),
            ("<", double first, double second) => first < second,
            ("<=", double first, double second) => first <= second,
            (">", double first, double second) => first > second,
            (">=", double first, double second) => first >= second,
            ("+", string, _) or ("+", _, string) => Values.ToText(left) + Values.ToText(right),
            ("+", double first, double second) => first + second,
            ("-", double first, double second) => first - second,
            ("*", double first, double second) => first * second,
            ("/", double first, double second) => first / second,
            ("%", double first, double second) => first % second,
            _ => null,
        };
        return result ?? throw new ExpressionException(
            $"Operator '{symbol}' cannot be applied to {Values.Describe(left)} and {Values.Describe(right)}", position);
    }

    public static object Unary(char symbol, object operand, int position)
    {
        object? result = (symbol, operand) switch
        {
            ('-', double number) => -number,
            ('!', bool boolean) => !boolean,
            _ => null,
        };
        return result ?? throw new ExpressionException(
            $"Operator '{symbol}' cannot be applied to {Values.Describe(operand)}", position);
    }

    /// <summary>Compares two numbers (IEEE), strings (ordinal) or booleans; returns null for any other pair.</summary>
    private static bool? AreEqual(object left, object right) => (left, right) switch
    {
        (double first, double second) => first == second,
        (string first, string second) => string.Equals(first, second, StringComparison.Ordinal),
        (bool first, bool second) => first == second,
        _ => null,
    };
}
