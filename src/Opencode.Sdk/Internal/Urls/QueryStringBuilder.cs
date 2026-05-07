using System.Globalization;

namespace Opencode.Internal.Urls;

internal static class QueryStringBuilder
{
    public static string Build(IReadOnlyDictionary<string, object?>? query)
    {
        if (query is null || query.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>(query.Count);

        foreach (var (key, value) in query)
        {
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(StringifyValue(value))}");
        }

        return string.Join("&", parts);
    }

    private static string StringifyValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            bool boolean => boolean ? "true" : "false",
            sbyte number => number.ToString(CultureInfo.InvariantCulture),
            byte number => number.ToString(CultureInfo.InvariantCulture),
            short number => number.ToString(CultureInfo.InvariantCulture),
            ushort number => number.ToString(CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            uint number => number.ToString(CultureInfo.InvariantCulture),
            long number => number.ToString(CultureInfo.InvariantCulture),
            ulong number => number.ToString(CultureInfo.InvariantCulture),
            float number => number.ToString(CultureInfo.InvariantCulture),
            double number => number.ToString(CultureInfo.InvariantCulture),
            decimal number => number.ToString(CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"Cannot stringify type {value.GetType().FullName}.", nameof(value)),
        };
    }
}