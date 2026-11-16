using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class PrecedenceTests
{
    private static readonly ExpressionContext Context = new();

    [Theory]
    [InlineData("true || false && false", true)]  // && binds tighter than ||
    [InlineData("false && true || true", true)]
    [InlineData("1 == 1 && 2 == 3", false)]       // == binds tighter than &&
    [InlineData("1 == 1 || 2 == 3", true)]
    [InlineData("1 < 2 == true", true)]           // < binds tighter than ==
    [InlineData("2 >= 3 != true", true)]
    [InlineData("1 + 2 < 4", true)]               // + binds tighter than <
    [InlineData("10 - 3 >= 7", true)]
    [InlineData("1 + 2 * 3", 7.0)]                // * binds tighter than +
    [InlineData("10 - 6 / 2", 7.0)]
    [InlineData("1 + 7 % 4", 4.0)]
    [InlineData("-2 * 3 + 1", -5.0)]              // unary binds tighter than binary
    [InlineData("-2 + 3", 1.0)]
    [InlineData("!true || true", true)]
    [InlineData("!false && false", false)]
    public void Operators_follow_precedence_levels(string expression, object expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate(expression, Context));
    }

    [Theory]
    [InlineData("10 - 4 - 3", 3.0)]
    [InlineData("64 / 4 / 2", 8.0)]
    [InlineData("17 % 5 * 2", 4.0)]
    [InlineData("2 * 7 % 4", 2.0)]
    [InlineData("1 - 2 + 3", 2.0)]
    [InlineData("1 == 1 == true", true)]
    [InlineData("'a' == 'a' != false", true)]
    public void Binary_operators_are_left_associative(string expression, object expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate(expression, Context));
    }

    [Fact]
    public void Comparisons_do_not_chain_because_the_left_comparison_yields_a_boolean()
    {
        var exception = Assert.Throws<ExpressionException>(() => Evaluator.Evaluate("1 < 2 < 3", Context));
        Assert.Equal(6, exception.Position);
    }

    [Theory]
    [InlineData("(1 + 2) * 3", 9.0)]
    [InlineData("10 - (4 - 3)", 9.0)]
    [InlineData("-(2 + 3)", -5.0)]
    [InlineData("!(true || true)", false)]
    [InlineData("(true || false) && false", false)]
    [InlineData("((((7))))", 7.0)]
    [InlineData("  2*3 +\t4 *\n5  ", 26.0)]
    public void Parentheses_and_whitespace_group_as_written(string expression, object expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate(expression, Context));
    }

    [Theory]
    [InlineData("--2", 2.0)]
    [InlineData("- - -2", -2.0)]
    [InlineData("2 - -3", 5.0)]
    [InlineData("2--3", 5.0)]
    [InlineData("!!true", true)]
    [InlineData("!!!true", false)]
    public void Unary_operators_can_be_repeated(string expression, object expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate(expression, Context));
    }
}
