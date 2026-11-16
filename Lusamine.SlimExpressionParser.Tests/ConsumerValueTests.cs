using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class ConsumerValueTests
{
    public static TheoryData<object> Numbers =>
    [
        (sbyte)-5, (byte)5, (short)-5, (ushort)5, 5, 5u, 5L, 5ul, 5f, 5.0, 5m,
    ];

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Numeric_identifier_values_become_doubles(object value)
    {
        var context = new ExpressionContext().Identifier("value", () => value);

        var result = Evaluator.Evaluate("value", context);

        Assert.IsType<double>(result);
        Assert.Equal(Convert.ToDouble(value), result);
    }

    [Theory]
    [MemberData(nameof(Numbers))]
    public void Numeric_function_results_become_doubles(object value)
    {
        var context = new ExpressionContext().Function("value", args => value);

        Assert.IsType<double>(Evaluator.Evaluate("value()", context));
    }

    [Fact]
    public void Converted_numbers_work_with_numeric_operators()
    {
        var context = new ExpressionContext()
            .Identifier("count", () => 3)
            .Identifier("price", () => 1.5m);

        Assert.Equal(4.5, Evaluator.Evaluate("count * price", context));
    }

    [Fact]
    public void Booleans_stay_booleans()
    {
        var context = new ExpressionContext()
            .Identifier("muted", () => true)
            .Function("muted", args => false);

        Assert.Equal(true, Evaluator.Evaluate("muted", context));
        Assert.Equal(false, Evaluator.Evaluate("muted()", context));
        Assert.Throws<ExpressionException>(() => Evaluator.Evaluate("muted + 1", context));
    }

    [Fact]
    public void Null_becomes_an_empty_string()
    {
        var context = new ExpressionContext()
            .Identifier("nothing", () => null)
            .Function("nothing", args => null);

        Assert.Equal("", Evaluator.Evaluate("nothing", context));
        Assert.Equal("", Evaluator.Evaluate("nothing()", context));
        Assert.Equal(true, Evaluator.Evaluate("nothing == ''", context));
    }

    [Fact]
    public void Other_objects_are_returned_unchanged()
    {
        var target = new Uri("https://example.com/");
        var context = new ExpressionContext()
            .Identifier("target", () => target)
            .Function("echo", args => args[0]);

        Assert.Same(target, Evaluator.Evaluate("target", context));
        Assert.Same(target, Evaluator.Evaluate("echo(target)", context));
    }

    [Fact]
    public void Identifiers_are_evaluated_only_when_referenced()
    {
        var calls = 0;
        var context = new ExpressionContext()
            .Identifier("unused", () => { calls++; return 1; })
            .Identifier("used", () => 2);

        Evaluator.Evaluate("used + 1", context);

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Identifiers_are_evaluated_once_per_reference()
    {
        var calls = 0;
        var context = new ExpressionContext().Identifier("roll", () => ++calls);

        // Each reference gets a fresh value, so x + x can combine two different values.
        Assert.Equal(1.0 + 2.0 + 3.0, Evaluator.Evaluate("roll + roll + roll", context));
        Assert.Equal(3, calls);
    }
}
