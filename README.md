# Lusamine.SlimExpressionParser

A small, dependency-free evaluator for single short expressions such as `cos(123) + sin(456)`, `args` or `count() >= 2 && !muted`.
It targets `net10.0` and `netstandard2.0`.

There are no built-in identifiers or functions: you register every one of them. The library evaluates one expression at a time. If your expressions sit inside surrounding text, such as `{...}` templates, extract them yourself first.

## Usage

```csharp
using Lusamine.SlimExpressionParser;

var context = new ExpressionContext()
    .Identifier("args", () => target)
    .Identifier("pi", () => Math.PI)
    .Function("cos", args => Math.Cos(args.Number(0)))
    .Function("repeat", args => string.Concat(Enumerable.Repeat(args.Text(0), (int)args.Number(1))))
    .Function("if", args => args.Boolean(0) ? args[1] : args[2]);

var number = Evaluator.Evaluate("cos(0) + 1", context);                 // 2.0
var text = Evaluator.Evaluate("args", context);                         // the value of target
var choice = Evaluator.Evaluate("if(pi > 3, 'big', 'small')", context); // "big"
```

Registering a name again replaces the previous registration. Identifiers and functions have separate namespaces.
An identifier's delegate runs only when the expression references it, once per reference, so `x + x` can combine two different values.
Function arguments are evaluated left to right before the call.

## Values

Results are `double`, `string`, `bool` or any other object your delegates return.
Numeric values from delegates (`int`, `long`, `decimal`, `float`, ...) become `double`, `bool` stays `bool`, and `null` becomes `""`.

`Arguments` is an `IReadOnlyList<object>` of the evaluated values, plus three helpers:

| Method | Accepts | Otherwise |
|---|---|---|
| `Number(i)` | a number, or text that parses to a finite number with `CultureInfo.InvariantCulture` | throws `ExpressionException` |
| `Text(i)` | anything: numbers use the invariant culture, booleans become `true`/`false` | |
| `Boolean(i)` | a boolean, or the text `true`/`false` in any case | throws `ExpressionException` |

The helpers also throw `ExpressionException` when the argument is missing. The indexer throws `ArgumentOutOfRangeException` instead.

## Syntax

- Numbers: `123`, `1.5`. The decimal separator is always `.`. There is no exponent notation.
- Strings: `'text'` or `"text"`. A backslash escapes the quote character or another backslash, and nothing else.
- Booleans: `true` and `false`, lowercase only. These names can't be registered.
- Identifiers: a letter or `_`, then letters, digits or `_`. Names are case-sensitive.
- Calls: `name()`, `name(a, b)`. Arguments can be any expression.
- Parentheses group expressions. Whitespace between tokens is ignored.

Operators, from lowest to highest precedence. All binary operators are left-associative.

| Operators | Operands | Result |
|---|---|---|
| `\|\|` | two booleans | boolean |
| `&&` | two booleans | boolean |
| `==` `!=` | two numbers, two strings or two booleans | boolean |
| `<` `<=` `>` `>=` | two numbers | boolean |
| `+` `-` | two numbers, or `+` with at least one string | number or string |
| `*` `/` `%` | two numbers | number |
| unary `-` `!` | a number for `-`, a boolean for `!` | number or boolean |

- `&&` and `||` always evaluate both operands. They don't short-circuit, so `false && f()` still calls `f`.
- `+` concatenates when either operand is a string and converts the other operand to text like `Arguments.Text`.
- Operators never convert values: `true + 1`, `'2' * 2` and `1 == '1'` are errors. Equality with other objects is an error too.
- Strings compare ordinally. Numbers follow IEEE rules: `0 / 0 == 0 / 0` is `false` and `1 / 0` is infinity.

## Errors

Invalid expressions throw `ExpressionException`. Its `Position` is the zero-based index of the problem:

- syntax errors, empty expressions and trailing tokens;
- unknown identifiers and functions, at the name;
- operators used with the wrong kinds of values, at the operator;
- `Arguments` conversion errors and missing arguments, at the function name;
- nesting deeper than 64 levels, counting parentheses, function calls and each unary `-` or `!`. This keeps untrusted input such as `((((...` from overflowing the stack.

Exceptions thrown by your delegates propagate unchanged.

## Sample

`Lusamine.SlimExpressionParser.Sample` is a console app for trying expressions against a small sample context:

```sh
dotnet run --project Lusamine.SlimExpressionParser.Sample                   # interactive prompt
dotnet run --project Lusamine.SlimExpressionParser.Sample -- "cos(0) + 1"   # evaluate and exit
```

## Tests

The tests run against either library build:

```sh
dotnet test                                          # net10.0 build
dotnet test -p:LibraryTargetFramework=netstandard2.0 # netstandard2.0 build
```

`LibraryBuildTests` fails if the loaded assembly isn't the selected build.
