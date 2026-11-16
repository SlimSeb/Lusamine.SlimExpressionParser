namespace Lusamine.SlimExpressionParser;

/// <summary>The identifiers and functions an expression can use. There are no built-ins.</summary>
public sealed class ExpressionContext
{
    internal Dictionary<string, Func<object?>> Identifiers { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, Func<Arguments, object?>> Functions { get; } = new(StringComparer.Ordinal);

    /// <summary>Registers an identifier, replacing any earlier one with the same name.</summary>
    /// <param name="name">The case-sensitive name. <c>true</c> and <c>false</c> are reserved.</param>
    /// <param name="value">Called each time an expression references the identifier.</param>
    /// <returns>This context, for chaining.</returns>
    public ExpressionContext Identifier(string name, Func<object?> value)
    {
        Identifiers[Validate(name)] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <summary>Registers a function, replacing any earlier one with the same name.</summary>
    /// <param name="name">The case-sensitive name. <c>true</c> and <c>false</c> are reserved.</param>
    /// <param name="function">Called with the evaluated arguments each time an expression calls the function.</param>
    /// <returns>This context, for chaining.</returns>
    public ExpressionContext Function(string name, Func<Arguments, object?> function)
    {
        Functions[Validate(name)] = function ?? throw new ArgumentNullException(nameof(function));
        return this;
    }

    private static string Validate(string name) => name switch
    {
        null => throw new ArgumentNullException(nameof(name)),
        "true" or "false" => throw new ArgumentException($"'{name}' is a boolean literal and cannot be registered.", nameof(name)),
        _ => name,
    };
}
