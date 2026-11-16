namespace Lusamine.SlimExpressionParser;

/// <summary>Evaluates a single expression.</summary>
public static class Evaluator
{
    /// <summary>Parses and evaluates <paramref name="expression"/> with the identifiers and functions in <paramref name="context"/>.</summary>
    /// <returns>A <see cref="double"/>, <see cref="string"/>, <see cref="bool"/> or another object returned by the context.</returns>
    /// <exception cref="ExpressionException">The expression is invalid or uses an operator with the wrong kinds of values.</exception>
    public static object Evaluate(string expression, ExpressionContext context)
    {
        if (expression is null) throw new ArgumentNullException(nameof(expression));
        if (context is null) throw new ArgumentNullException(nameof(context));
        return new Parser(expression, context).Evaluate();
    }
}
