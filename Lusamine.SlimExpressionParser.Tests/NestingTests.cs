using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class NestingTests
{
    private static readonly ExpressionContext Context = new ExpressionContext().Function("f", args => args[0]);

    private static string Parentheses(int depth) => new string('(', depth) + "1" + new string(')', depth);

    private static string Calls(int depth) => string.Concat(Enumerable.Repeat("f(", depth)) + "1" + new string(')', depth);

    [Fact]
    public void Allows_64_nested_parentheses()
    {
        Assert.Equal(1.0, Evaluator.Evaluate(Parentheses(64), Context));
    }

    [Fact]
    public void Rejects_65_nested_parentheses_at_the_65th()
    {
        var exception = Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(Parentheses(65), Context));

        Assert.Equal(64, exception.Position);
        Assert.Equal("The expression is nested more than 64 levels deep", exception.Message);
    }

    [Fact]
    public void Allows_a_chain_of_64_minus_signs()
    {
        Assert.Equal(1.0, Evaluator.Evaluate(new string('-', 64) + "1", Context));
    }

    [Fact]
    public void Rejects_a_chain_of_65_minus_signs()
    {
        var exception = Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(new string('-', 65) + "1", Context));

        Assert.Equal(64, exception.Position);
    }

    [Fact]
    public void Allows_a_chain_of_64_exclamation_marks()
    {
        Assert.Equal(true, Evaluator.Evaluate(new string('!', 64) + "true", Context));
    }

    [Fact]
    public void Rejects_a_chain_of_65_exclamation_marks()
    {
        var exception = Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(new string('!', 65) + "true", Context));

        Assert.Equal(64, exception.Position);
    }

    [Fact]
    public void Counts_nested_function_calls()
    {
        Assert.Equal(1.0, Evaluator.Evaluate(Calls(64), Context));
        Assert.Equal(128, Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(Calls(65), Context)).Position);
    }

    [Fact]
    public void Counts_parentheses_calls_and_unary_operators_together()
    {
        var withinLimit = string.Concat(Enumerable.Repeat("-(f(", 21)) + "1" + string.Concat(Enumerable.Repeat("))", 21));

        Assert.Equal(1.0, Evaluator.Evaluate("-" + withinLimit, Context));
        Assert.Throws<ExpressionException>(() => Evaluator.Evaluate("--" + withinLimit, Context));
    }

    [Theory]
    [InlineData('(')]
    [InlineData('-')]
    [InlineData('!')]
    public void Very_deep_input_throws_instead_of_overflowing_the_stack(char repeated)
    {
        var exception = Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(new string(repeated, 1_000_000), Context));

        Assert.Equal(64, exception.Position);
    }

    [Fact]
    public void Long_flat_expressions_are_not_limited()
    {
        var expression = string.Join(" + ", Enumerable.Repeat("1", 100_000));

        Assert.Equal(100_000.0, Evaluator.Evaluate(expression, Context));
    }
}
