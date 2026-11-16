using Xunit;

namespace Lusamine.SlimExpressionParser.Tests;

public class FunctionTests
{
    private static readonly ExpressionContext Context = new ExpressionContext()
        .Identifier("pi", () => Math.PI)
        .Function("cos", args => Math.Cos(args.Number(0)))
        .Function("sin", args => Math.Sin(args.Number(0)))
        .Function("max", args => Math.Max(args.Number(0), args.Number(1)))
        .Function("count", args => args.Count)
        .Function("join", args => string.Join("|", args.Select(value => value.ToString())))
        .Function("repeat", args => string.Concat(Enumerable.Repeat(args.Text(0), (int)args.Number(1))))
        .Function("if", args => args.Boolean(0) ? args[1] : args[2]);

    [Fact]
    public void Evaluates_cos_plus_sin_exactly_like_Math()
    {
        Assert.Equal(Math.Cos(123) + Math.Sin(456), Evaluator.Evaluate("cos(123)+sin(456)", Context));
    }

    [Fact]
    public void Evaluates_nested_calls()
    {
        Assert.Equal(4.0, Evaluator.Evaluate("max(1, max(max(2, 4), 3))", Context));
        Assert.Equal(Math.Cos(Math.Sin(Math.PI)), Evaluator.Evaluate("cos(sin(pi))", Context));
    }

    [Fact]
    public void Calls_functions_with_zero_arguments()
    {
        Assert.Equal(0.0, Evaluator.Evaluate("count()", Context));
        Assert.Equal(0.0, Evaluator.Evaluate("count( )", Context));
    }

    [Fact]
    public void Accepts_any_expression_as_an_argument()
    {
        Assert.Equal(3.0, Evaluator.Evaluate("count(1 + 2, 'a' + 'b', !true)", Context));
        Assert.Equal("small", Evaluator.Evaluate("if(pi > 4, 'big', 'small')", Context));
    }

    [Fact]
    public void Allows_whitespace_between_the_name_and_the_parenthesis()
    {
        Assert.Equal(2.0, Evaluator.Evaluate("max (1, 2)", Context));
    }

    [Fact]
    public void Passes_string_arguments_with_escaped_quotes_and_backslashes()
    {
        var result = Evaluator.Evaluate(@"join('it\'s', ""say \""hi\"""", 'a\\b', ""'"", '""')", Context);

        Assert.Equal(@"it's|say ""hi""|a\b|'|""", result);
    }

    [Fact]
    public void Evaluates_arguments_left_to_right_before_the_call()
    {
        var log = new List<string>();
        var context = new ExpressionContext()
            .Identifier("a", () => { log.Add("a"); return 1; })
            .Identifier("b", () => { log.Add("b"); return 2; })
            .Function("f", args => { log.Add("f"); return args.Count; });

        Evaluator.Evaluate("f(a, b, a)", context);

        Assert.Equal(new[] { "a", "b", "a", "f" }, log);
    }

    [Fact]
    public void Runs_the_documented_usage_example()
    {
        var target = new object();
        var context = new ExpressionContext()
            .Identifier("args", () => target)
            .Identifier("pi", () => Math.PI)
            .Function("cos", args => Math.Cos(args.Number(0)))
            .Function("repeat", args => string.Concat(Enumerable.Repeat(args.Text(0), (int)args.Number(1))))
            .Function("if", args => args.Boolean(0) ? args[1] : args[2]);

        Assert.Equal(2.0, Evaluator.Evaluate("cos(0) + 1", context));
        Assert.Same(target, Evaluator.Evaluate("args", context));
        Assert.Equal("big", Evaluator.Evaluate("if(pi > 3, 'big', 'small')", context));
        Assert.Equal("abab", Evaluator.Evaluate("repeat('ab', 2)", context));
    }

    [Fact]
    public void Identifiers_and_functions_have_separate_namespaces()
    {
        var context = new ExpressionContext()
            .Identifier("value", () => 1)
            .Function("value", args => 2);

        Assert.Equal(3.0, Evaluator.Evaluate("value + value()", context));
    }

    [Fact]
    public void The_last_registration_of_a_name_wins()
    {
        var context = new ExpressionContext()
            .Identifier("x", () => 1)
            .Identifier("x", () => 2)
            .Function("f", args => "first")
            .Function("f", args => "second");

        Assert.Equal(2.0, Evaluator.Evaluate("x", context));
        Assert.Equal("second", Evaluator.Evaluate("f()", context));
    }

    [Fact]
    public void Names_are_case_sensitive()
    {
        var context = new ExpressionContext()
            .Identifier("name", () => "lower")
            .Identifier("Name", () => "upper");

        Assert.Equal("lower", Evaluator.Evaluate("name", context));
        Assert.Equal("upper", Evaluator.Evaluate("Name", context));
        Assert.Throws<ExpressionException>(() => Evaluator.Evaluate("NAME", context));
    }

    [Fact]
    public void Names_can_contain_underscores_and_digits()
    {
        var context = new ExpressionContext()
            .Identifier("_x1", () => 1)
            .Function("f_2", args => 2);

        Assert.Equal(3.0, Evaluator.Evaluate("_x1 + f_2()", context));
    }
}
