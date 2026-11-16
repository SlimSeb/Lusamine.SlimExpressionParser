// Quick manual testing for the evaluator.
//   dotnet run --project Lusamine.SlimExpressionParser.Sample                     interactive prompt
//   dotnet run --project Lusamine.SlimExpressionParser.Sample -- "cos(0) + 1"     evaluate the arguments and exit

using System.Globalization;
using Lusamine.SlimExpressionParser;

var random = new Random();

var context = new ExpressionContext()
    .Identifier("pi", () => Math.PI)
    .Identifier("args", () => "6")
    .Identifier("name", () => "Lusamine")
    .Identifier("muted", () => false)
    .Identifier("nothing", () => null)
    .Identifier("roll", () => random.Next(1, 7))
    .Function("sin", args => Math.Sin(args.Number(0)))
    .Function("cos", args => Math.Cos(args.Number(0)))
    .Function("sqrt", args => Math.Sqrt(args.Number(0)))
    .Function("round", args => Math.Round(args.Number(0), args.Count > 1 ? (int)args.Number(1) : 0))
    .Function("dice", args => Enumerable.Range(0, (int)args.Number(0)).Sum(_ => random.Next(1, (int)args.Number(1) + 1)))
    .Function("count", args => args.Count)
    .Function("len", args => args.Text(0).Length)
    .Function("upper", args => args.Text(0).ToUpperInvariant())
    .Function("repeat", args => string.Concat(Enumerable.Repeat(args.Text(0), (int)args.Number(1))))
    .Function("if", args => args.Boolean(0) ? args[1] : args[2]);

if (args.Length > 0)
{
    foreach (var expression in args)
    {
        Console.WriteLine($"> {expression}");
        Run(expression);
    }
    return;
}

Console.WriteLine("Identifiers: pi, args (\"6\"), name, muted, nothing (null), roll (1-6, new value per reference)");
Console.WriteLine("Functions:   sin, cos, sqrt, round, dice(count, sides), count, len, upper, repeat, if");
Console.WriteLine("Enter an expression, or press Ctrl+D (Ctrl+Z on Windows) to quit.");
while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line is null) return;
    if (line.Trim().Length > 0) Run(line);
}

void Run(string expression)
{
    try
    {
        Console.WriteLine($"= {Describe(Evaluator.Evaluate(expression, context))}");
    }
    catch (ExpressionException exception)
    {
        // The prompt "> " is two characters wide, so the caret lines up with the input.
        Console.WriteLine(new string(' ', exception.Position + 2) + "^");
        Console.WriteLine($"error at {exception.Position}: {exception.Message}");
    }
    catch (Exception exception)
    {
        Console.WriteLine($"{exception.GetType().Name} from a delegate: {exception.Message}");
    }
}

static string Describe(object value) => value switch
{
    double number => $"{number.ToString("R", CultureInfo.InvariantCulture)}  (number)",
    string text => $"\"{text}\"  (string)",
    bool boolean => boolean ? "true  (boolean)" : "false  (boolean)",
    _ => $"{value}  ({value.GetType().Name})",
};
