using System.Globalization;

namespace Lusamine.SlimExpressionParser;

/// <summary>Converts values returned by consumers and formats values as text.</summary>
internal static class Values
{
    /// <summary>Turns consumer numbers into <see cref="double"/> and <c>null</c> into an empty string.</summary>
    public static object Normalize(object? value) => value switch
    {
        null => "",
        sbyte or byte or short or ushort or int or uint or long or ulong or float or decimal
            => Convert.ToDouble(value, CultureInfo.InvariantCulture),
        _ => value,
    };

    public static string ToText(object value) => value switch
    {
        string text => text,
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        bool boolean => boolean ? "true" : "false",
        _ => value.ToString() ?? "",
    };

    /// <summary>Names the kind of a value for error messages.</summary>
    public static string Describe(object value) => value switch
    {
        double => "a number",
        string => "a string",
        bool => "a boolean",
        _ => value.GetType().Name,
    };
}
