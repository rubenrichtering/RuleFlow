using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace RuleFlow.Core.Conditions;

internal static class OperandComparison
{
    public static bool AreEqual(object? left, object? right)
    {
        if (left == null || right == null)
            return Equals(left, right);

        if (left.GetType() == right.GetType())
            return Equals(left, right);

        if (IsNumeric(left) && IsNumeric(right))
            return ToDecimal(left) == ToDecimal(right);

        return Equals(left.ToString(), right.ToString());
    }

    public static int Compare(object? left, object? right)
    {
        if (left == null && right == null) return 0;
        if (left == null) return -1;
        if (right == null) return 1;

        if (IsNumeric(left) && IsNumeric(right))
            return decimal.Compare(ToDecimal(left), ToDecimal(right));

        if (left.GetType() == right.GetType() && left is IComparable lc)
            return lc.CompareTo(right);

        return string.CompareOrdinal(
            Convert.ToString(left, CultureInfo.InvariantCulture),
            Convert.ToString(right, CultureInfo.InvariantCulture));
    }

    public static bool IsNumeric(object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong
            or float or double or decimal
            or nint or nuint;
    }

    public static decimal ToDecimal(object value)
    {
        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    public static IEnumerable<object?> EnumerateCollection(object? right)
    {
        if (right == null)
            yield break;

        if (right is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in je.EnumerateArray())
                    yield return NormalizeJsonScalar(el);
            }
            else
                yield return NormalizeJsonScalar(je);
            yield break;
        }

        if (right is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
                yield return item;
            yield break;
        }

        yield return right;
    }

    private static object? NormalizeJsonScalar(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => el.ToString()
        };
    }

    public static (object? Min, object? Max) UnpackRange(object? right)
    {
        if (right == null)
            throw new InvalidOperationException("between operator requires a range (min and max).");

        if (right is JsonElement je && je.ValueKind == JsonValueKind.Array)
        {
            var items = je.EnumerateArray().ToArray();
            if (items.Length != 2)
                throw new InvalidOperationException("between operator requires exactly two bounds.");
            return (NormalizeJsonScalar(items[0]), NormalizeJsonScalar(items[1]));
        }

        if (right is object[] arr)
        {
            if (arr.Length != 2)
                throw new InvalidOperationException("between operator requires exactly two bounds.");
            return (arr[0], arr[1]);
        }

        if (right is IList list && list.Count == 2)
            return (list[0], list[1]);

        throw new InvalidOperationException(
            "between operator expects an array of two values [min, max].");
    }
}
