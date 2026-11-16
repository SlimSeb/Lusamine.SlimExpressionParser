namespace Lusamine.SlimExpressionParser;

/// <summary>Thrown when an expression is invalid or cannot be evaluated.</summary>
public sealed class ExpressionException : Exception
{
    internal ExpressionException(string message, int position) : base(message) => Position = position;

    /// <summary>The zero-based index in the expression where the error was found.</summary>
    public int Position { get; }
}
