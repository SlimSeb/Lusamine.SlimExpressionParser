using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class OperatorTests
{
    private static readonly ExpressionContext Context = new ExpressionContext()
        .Identifier("nan", () => double.NaN)
        .Identifier("item", () => new Item())
        .Identifier("args", () => "6");

    private static object Evaluate(string expression) => Evaluator.Evaluate(expression, Context);

    private static ExpressionException Fails(string expression) =>
        Assert.Throws<ExpressionException>(() => Evaluator.Evaluate(expression, Context));

    [Fact]
    public void Boolean_literals_are_case_sensitive()
    {
        Assert.Equal(true, Evaluate("true"));
        Assert.Equal(false, Evaluate("false"));
        Assert.Equal("Unknown identifier 'True'", Fails("True").Message);
    }

    [Fact]
    public void True_and_false_cannot_be_registered_as_names()
    {
        var context = new ExpressionContext();

        Assert.Throws<ArgumentException>(() => context.Identifier("true", () => 1));
        Assert.Throws<ArgumentException>(() => context.Identifier("false", () => 1));
        Assert.Throws<ArgumentException>(() => context.Function("true", args => 1));
        Assert.Throws<ArgumentException>(() => context.Function("false", args => 1));
    }

    [Fact]
    public void Calling_a_boolean_literal_is_a_syntax_error()
    {
        Assert.Equal(5, Fails("true (1)").Position);
    }

    [Theory]
    [InlineData("1 < 2", true)]
    [InlineData("2 < 2", false)]
    [InlineData("2 <= 2", true)]
    [InlineData("3 <= 2", false)]
    [InlineData("3 > 2", true)]
    [InlineData("2 > 2", false)]
    [InlineData("2 >= 2", true)]
    [InlineData("1 >= 2", false)]
    [InlineData("1.5 == 1.50", true)]
    [InlineData("1 != 2", true)]
    [InlineData("'a' == 'a'", true)]
    [InlineData("'a' != 'b'", true)]
    [InlineData("true == true", true)]
    [InlineData("true != false", true)]
    public void Compares_values(string expression, bool expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Theory]
    [InlineData("true && true", true)]
    [InlineData("true && false", false)]
    [InlineData("false && true", false)]
    [InlineData("false && false", false)]
    [InlineData("true || true", true)]
    [InlineData("true || false", true)]
    [InlineData("false || true", true)]
    [InlineData("false || false", false)]
    [InlineData("!true", false)]
    [InlineData("!false", true)]
    public void Applies_boolean_operators(string expression, bool expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Theory]
    [InlineData("false && check()")]
    [InlineData("true || check()")]
    public void And_and_or_always_evaluate_both_operands(string expression)
    {
        var calls = 0;
        var context = new ExpressionContext().Function("check", args => { calls++; return true; });

        Evaluator.Evaluate(expression, context);

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("1 == '1'", 2)]
    [InlineData("'true' == true", 7)]
    [InlineData("1 != true", 2)]
    [InlineData("args == 6", 5)]
    [InlineData("item == item", 5)]
    [InlineData("item != 'x'", 5)]
    public void Equality_between_different_kinds_or_other_objects_throws(string expression, int position)
    {
        Assert.Equal(position, Fails(expression).Position);
    }

    [Theory]
    [InlineData("'a' < 'b'", 4)]
    [InlineData("true > false", 5)]
    [InlineData("'1' <= 2", 4)]
    [InlineData("item >= 1", 5)]
    public void Comparing_non_numbers_throws(string expression, int position)
    {
        Assert.Equal(position, Fails(expression).Position);
    }

    [Theory]
    [InlineData("nan == nan", false)]
    [InlineData("nan != nan", true)]
    [InlineData("nan < 1", false)]
    [InlineData("nan >= 1", false)]
    [InlineData("0 / 0 == 0 / 0", false)]
    public void Nan_follows_ieee_comparison_rules(string expression, bool expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Theory]
    [InlineData("'a' == 'A'", false)]
    [InlineData("'straße' == 'strasse'", false)]
    [InlineData("'é' == 'é'", false)]  // equal under culture-aware comparison
    [InlineData("'b' != 'B'", true)]
    public void Strings_are_compared_ordinally(string expression, bool expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Fact]
    public void Division_and_modulo_follow_ieee_rules()
    {
        Assert.Equal(double.PositiveInfinity, Evaluate("1 / 0"));
        Assert.Equal(double.NegativeInfinity, Evaluate("-1 / 0"));
        Assert.Equal(double.NaN, Evaluate("0 / 0"));
        Assert.Equal(double.NaN, Evaluate("5 % 0"));
        Assert.Equal(-1.0, Evaluate("-7 % 3"));
        Assert.Equal(0.30000000000000004, Evaluate("0.1 + 0.2"));
    }

    [Theory]
    [InlineData("'a' + 'b'", "ab")]
    [InlineData("'n=' + 1.5", "n=1.5")]
    [InlineData("2 + 'x'", "2x")]
    [InlineData("'is ' + true", "is true")]
    [InlineData("false + ''", "false")]
    [InlineData("'' + 0.1 + 0.2", "0.10.2")]
    [InlineData("'' + (0.1 + 0.2)", "0.30000000000000004")]
    [InlineData("1 + 2 + 'x'", "3x")]
    [InlineData("'' + 1 / 0", "Infinity")]
    [InlineData("'' + item", "an item")]
    public void Plus_concatenates_when_either_operand_is_a_string(string expression, string expected)
    {
        Assert.Equal(expected, Evaluate(expression));
    }

    [Theory]
    [InlineData("true + 1", 5, "Operator '+' cannot be applied to a boolean and a number")]
    [InlineData("'a' - 1", 4, "Operator '-' cannot be applied to a string and a number")]
    [InlineData("'2' * 2", 4, "Operator '*' cannot be applied to a string and a number")]
    [InlineData("4 / '2'", 2, "Operator '/' cannot be applied to a number and a string")]
    [InlineData("4 % true", 2, "Operator '%' cannot be applied to a number and a boolean")]
    [InlineData("1 && true", 2, "Operator '&&' cannot be applied to a number and a boolean")]
    [InlineData("true || 'x'", 5, "Operator '||' cannot be applied to a boolean and a string")]
    [InlineData("item + 1", 5, "Operator '+' cannot be applied to Item and a number")]
    [InlineData("-'a'", 0, "Operator '-' cannot be applied to a string")]
    [InlineData("1 + !2", 4, "Operator '!' cannot be applied to a number")]
    public void Invalid_operand_types_throw_at_the_operator(string expression, int position, string message)
    {
        var exception = Fails(expression);

        Assert.Equal(position, exception.Position);
        Assert.Equal(message, exception.Message);
    }

    private sealed class Item
    {
        public override string ToString() => "an item";
    }
}
