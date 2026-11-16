using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class ArgumentsTests
{
    private static readonly object Target = new();

    private static readonly ExpressionContext Context = new ExpressionContext()
        .Identifier("args", () => "6")
        .Identifier("target", () => Target)
        .Function("number", args => args.Number(0))
        .Function("text", args => args.Text(0))
        .Function("boolean", args => args.Boolean(0))
        .Function("dice", args => args.Number(0) * args.Number(1))
        .Function("second", args => args.Number(1));

    private static object Evaluate(string expression) => Evaluator.Evaluate(expression, Context);

    private static ExpressionException Fails(string expression) =>
        Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(expression, Context));

    [Theory]
    [InlineData("number(2.5)", 2.5)]
    [InlineData("number('6')", 6.0)]
    [InlineData("number(' -1.5e3 ')", -1500.0)]
    [InlineData("number('.5')", 0.5)]
    [InlineData("dice(1, args)", 6.0)]
    public void Number_accepts_doubles_and_numeric_strings(string expression, double expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Fact]
    public void Number_accepts_computed_non_finite_values()
    {
        Assert.Equal(double.PositiveInfinity, Evaluate("number(1 / 0)"));
        Assert.Equal(double.NaN, Evaluate("number(0 / 0)"));
    }

    [Theory]
    [InlineData("number('abc')")]
    [InlineData("number('')")]
    [InlineData("number('1,5')")]
    [InlineData("number('0x10')")]
    [InlineData("number('NaN')")]
    [InlineData("number('Infinity')")]
    [InlineData("number('-Infinity')")]
    [InlineData("number('1e999')")]
    [InlineData("number(true)")]
    [InlineData("number(target)")]
    public void Number_rejects_other_values_at_the_function_name(string expression)
    {
        var exception = Fails(expression);

        Assert.Equal(0, exception.Position);
        Assert.Equal("Argument 0 must be a number or numeric text", exception.Message);
    }

    [Theory]
    [InlineData("boolean(true)", true)]
    [InlineData("boolean(false)", false)]
    [InlineData("boolean('true')", true)]
    [InlineData("boolean('FALSE')", false)]
    [InlineData("boolean('True')", true)]
    public void Boolean_accepts_booleans_and_true_or_false_text(string expression, bool expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Theory]
    [InlineData("boolean('yes')")]
    [InlineData("boolean(' true')")]
    [InlineData("boolean('1')")]
    [InlineData("boolean(1)")]
    [InlineData("boolean(target)")]
    public void Boolean_rejects_other_values_at_the_function_name(string expression)
    {
        var exception = Fails("1 + " + expression);

        Assert.Equal(4, exception.Position);
        Assert.Equal("Argument 0 must be a boolean or the text true or false", exception.Message);
    }

    [Theory]
    [InlineData("text('abc')", "abc")]
    [InlineData("text(1.5)", "1.5")]
    [InlineData("text(42)", "42")]
    [InlineData("text(-0.25)", "-0.25")]
    [InlineData("text(0.1 + 0.2)", "0.30000000000000004")]
    [InlineData("text(1000000 * 1000000 * 1000000 * 1000)", "1E+21")]
    [InlineData("text(1 / 0)", "Infinity")]
    [InlineData("text(true)", "true")]
    [InlineData("text(false)", "false")]
    [InlineData("text(target)", "System.Object")]
    public void Text_formats_values(string expression, string expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Theory]
    [InlineData("second(1)", 0, "Argument 1 is missing: the function received 1")]
    [InlineData("2 * second()", 4, "Argument 1 is missing: the function received 0")]
    [InlineData("'x' + text()", 6, "Argument 0 is missing: the function received 0")]
    [InlineData("(boolean())", 1, "Argument 0 is missing: the function received 0")]
    public void Missing_arguments_throw_at_the_function_name(string expression, int position, string message)
    {
        var exception = Fails(expression);

        Assert.Equal(position, exception.Position);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Negative_indexes_throw_expression_exceptions()
    {
        var context = new ExpressionContext().Function("f", args => args.Text(-1));

        Assert.Equal(0, Assert.Throws<ExpressionException>(() => Evaluator.Evaluate("f(1)", context)).Position);
    }

    [Fact]
    public void Count_indexer_and_enumeration_expose_the_values_unchanged()
    {
        Arguments? captured = null;
        var context = new ExpressionContext()
            .Identifier("target", () => Target)
            .Function("capture", args => captured = args);

        Evaluator.Evaluate("capture(1 + 1, 'two', true, target)", context);

        Assert.NotNull(captured);
        Assert.Equal(4, captured.Count);
        Assert.Equal(2.0, captured[0]);
        Assert.Equal("two", captured[1]);
        Assert.Equal(true, captured[2]);
        Assert.Same(Target, captured[3]);
        Assert.Equal(new object[] { 2.0, "two", true, Target }, captured.ToList());
    }

    [Fact]
    public void Indexer_throws_argument_out_of_range_for_invalid_indexes()
    {
        var context = new ExpressionContext()
            .Function("past", args => args[args.Count])
            .Function("negative", args => args[-1]);

        Assert.Throws<ArgumentOutOfRangeException>(() => Evaluator.Evaluate("past(1)", context));
        Assert.Throws<ArgumentOutOfRangeException>(() => Evaluator.Evaluate("negative(1)", context));
    }

    [Fact]
    public void Argument_errors_propagate_unchanged_through_outer_calls()
    {
        ExpressionException? thrown = null;
        var context = new ExpressionContext()
            .Function("outer", args => args[0])
            .Function("inner", args =>
            {
                try
                {
                    return args.Number(0);
                }
                catch (ExpressionException exception)
                {
                    thrown = exception;
                    throw;
                }
            });

        var exception = Assert.Throws<ExpressionException>(() => Evaluator.Evaluate("outer(inner('x'))", context));

        Assert.Same(thrown, exception);
        Assert.Equal(6, exception.Position);
    }
}
