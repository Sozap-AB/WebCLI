using System.Collections.Generic;
using System.Linq;

internal static class Extensions
{
    public static IEnumerable<T> AppendRange<T>(this IEnumerable<T> source, IEnumerable<T> items)
    {
        var result = source;

        foreach (var item in items)
        {
            result = result.Append(item);
        }

        return result;
    }
}