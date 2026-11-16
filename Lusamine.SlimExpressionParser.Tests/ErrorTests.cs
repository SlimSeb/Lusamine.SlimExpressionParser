using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class ErrorTests
{
    private static readonly ExpressionContext Context = new ExpressionContext()
        .Identifier("x", () => 1)
        .Function("f", args => args.Count);

    private static ExpressionException Fails(string expression) =>
        Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(expression, Context));

    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 3)]
    public void Empty_expressions_throw(string expression, int position)
    {
        var exception = Fails(expression);

        Assert.Equal(position, exception.Position);
        Assert.Equal("The expression is empty", exception.Message);
    }

    [Theory]
    [InlineData("y", 0, "Unknown identifier 'y'")]
    [InlineData("1 + unknown * 2", 4, "Unknown identifier 'unknown'")]
    [InlineData("f(1, g(2))", 5, "Unknown function 'g'")]
    [InlineData("x()", 0, "Unknown function 'x'")]
    [InlineData("f + 1", 0, "Unknown identifier 'f'")]
    public void Unknown_names_throw_at_the_name(string expression, int position, string message)
    {
        var exception = Fails(expression);

        Assert.Equal(position, exception.Position);
        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("1 +", 3, "Unexpected end of expression")]
    [InlineData("(1 + 2", 6, "Expected ')'")]
    [InlineData("f(1 2)", 4, "Expected ')'")]
    [InlineData("f(1,)", 4, "Unexpected ')'")]
    [InlineData("f(,1)", 2, "Unexpected ','")]
    [InlineData("f(1", 3, "Expected ')'")]
    [InlineData("()", 1, "Unexpected ')'")]
    [InlineData("1 + * 2", 4, "Unexpected '*'")]
    [InlineData("#", 0, "Unexpected '#'")]
    [InlineData("1 & 2", 2, "Unexpected '&'")]
    [InlineData("1 = 1", 2, "Unexpected '='")]
    [InlineData(".5", 0, "Unexpected '.'")]
    [InlineData("1.", 1, "Unexpected '.'")]
    [InlineData("1e5", 1, "Unexpected 'e'")]
    [InlineData("'abc", 0, "Unterminated string")]
    [InlineData("x + \"abc", 4, "Unterminated string")]
    [InlineData(@"'a\nb'", 2, "A backslash can only escape the quote character or a backslash")]
    [InlineData(@"'a\""'", 2, "A backslash can only escape the quote character or a backslash")]
    public void Syntax_errors_throw_at_the_offending_character(string expression, int position, string message)
    {
        var exception = Fails(expression);

        Assert.Equal(position, exception.Position);
        Assert.Equal(message, exception.Message);
    }

    [Theory]
    [InlineData("1 2", 2)]
    [InlineData("x y", 2)]
    [InlineData("f() 1", 4)]
    [InlineData("(1) (2)", 4)]
    [InlineData("'a' 'b'", 4)]
    [InlineData("1 + 2)", 5)]
    public void Trailing_tokens_throw(string expression, int position)
    {
        Assert.Equal(position, Fails(expression).Position);
    }

    [Fact]
    public void Number_literals_too_large_for_a_double_throw()
    {
        var exception = Fails("1 + 1" + new string('0', 400));

        Assert.Equal(4, exception.Position);
        Assert.Equal("The number is too large", exception.Message);
    }

    [Fact]
    public void Function_exceptions_propagate_unchanged()
    {
        var thrown = new InvalidOperationException("boom");
        var context = new ExpressionContext().Function("fail", args => throw thrown);

        Assert.Same(thrown, Assert.Throws<InvalidOperationException>(() => Evaluator.Evaluate("1 + fail()", context)));
    }

    [Fact]
    public void Identifier_exceptions_propagate_unchanged()
    {
        var thrown = new KeyNotFoundException("missing");
        var context = new ExpressionContext().Identifier("fail", () => throw thrown);

        Assert.Same(thrown, Assert.Throws<KeyNotFoundException>(() => Evaluator.Evaluate("fail", context)));
    }
}
