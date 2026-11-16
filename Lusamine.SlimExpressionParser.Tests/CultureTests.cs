using System.Globalization;
using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class CultureTests
{
    private static readonly ExpressionContext Context = new ExpressionContext()
        .Identifier("half", () => 2.5)
        .Function("number", args => args.Number(0))
        .Function("text", args => args.Text(0));

    [Fact]
    public void Number_literals_use_the_invariant_culture()
    {
        Assert.Equal(2.5, InFrench(() => Evaluator.Evaluate("1.5 + 1", Context)));
    }

    [Fact]
    public void Arguments_number_parses_with_the_invariant_culture()
    {
        Assert.Equal(2.5, InFrench(() => Evaluator.Evaluate("number('2.5')", Context)));
        Assert.Throws<ExpressionException>(() => InFrench(() => Evaluator.Evaluate("number('2,5')", Context)));
    }

    [Fact]
    public void Arguments_text_formats_with_the_invariant_culture()
    {
        Assert.Equal("2.5", InFrench(() => Evaluator.Evaluate("text(half)", Context)));
        Assert.Equal("1000.5", InFrench(() => Evaluator.Evaluate("text(1000.5)", Context)));
    }

    [Fact]
    public void Concatenation_formats_with_the_invariant_culture()
    {
        Assert.Equal("x2.5", InFrench(() => Evaluator.Evaluate("'x' + half", Context)));
    }

    [Fact]
    public void The_original_culture_is_restored()
    {
        var original = CultureInfo.CurrentCulture;

        Assert.Throws<ExpressionException>(() => InFrench(() => Evaluator.Evaluate("number('2,5')", Context)));

        Assert.Same(original, CultureInfo.CurrentCulture);
    }

    private static object InFrench(Func<object> action)
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            Assert.Equal("2,5", 2.5.ToString(CultureInfo.CurrentCulture));
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }
}
