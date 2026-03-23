using System.Collections;
using System.Globalization;
using System.Text.Json;
using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// Converts literals (including <see cref="JsonElement"/>) to the target field type.
/// </summary>
public sealed class DefaultValueConverter : IValueConverter
{
    public object? Convert(object? input, Type targetType)
    {
        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType));

        if (input == null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (input is JsonElement je)
            return ConvertJsonElement(je, underlying);

        if (underlying.IsInstanceOfType(input))
            return input;

        if (underlying.IsEnum)
            return Enum.Parse(underlying, input.ToString()!, ignoreCase: true);

        try
        {
            if (underlying == typeof(Guid) && input is string gs)
                return Guid.Parse(gs);

            if (underlying == typeof(string))
                return input.ToString();

            return System.Convert.ChangeType(input, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot convert value '{input}' to type {underlying.Name}.", ex);
        }
    }

    private static object? ConvertJsonElement(JsonElement je, Type targetType)
    {
        return je.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String when targetType == typeof(DateTime) =>
                DateTime.Parse(je.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            JsonValueKind.String when targetType == typeof(DateTimeOffset) =>
                DateTimeOffset.Parse(je.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            JsonValueKind.String => Convert(je.GetString()!, targetType),
            JsonValueKind.Number when targetType == typeof(decimal) => je.GetDecimal(),
            JsonValueKind.Number when targetType == typeof(double) => je.GetDouble(),
            JsonValueKind.Number when targetType == typeof(float) => je.GetSingle(),
            JsonValueKind.Number when targetType == typeof(long) => je.GetInt64(),
            JsonValueKind.Number when targetType == typeof(int) => je.GetInt32(),
            JsonValueKind.True or JsonValueKind.False when targetType == typeof(bool) => je.GetBoolean(),
            JsonValueKind.Array => ConvertJsonArray(je, targetType),
            _ => je.Deserialize(targetType, JsonSerializerOptions.Web)
        };
    }

    private static object ConvertJsonArray(JsonElement je, Type targetType)
    {
        if (targetType == typeof(object[]))
        {
            return je.EnumerateArray().Select(e => (object)e).ToArray();
        }

        if (targetType.IsArray && targetType.GetElementType() is { } elemType)
        {
            var list = new List<object?>();
            foreach (var item in je.EnumerateArray())
            {
                list.Add(ConvertJsonElement(item, elemType));
            }

            var arr = Array.CreateInstance(elemType, list.Count);
            for (var i = 0; i < list.Count; i++)
                arr.SetValue(list[i], i);
            return arr;
        }

        return je.Deserialize(targetType, JsonSerializerOptions.Web)
               ?? throw new InvalidOperationException("Failed to deserialize JSON array.");
    }

    private static object? Convert(string? s, Type targetType)
    {
        if (s == null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string))
            return s;

        if (underlying == typeof(Guid))
            return Guid.Parse(s);

        if (underlying == typeof(DateTime))
            return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (underlying == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (underlying.IsEnum)
            return Enum.Parse(underlying, s, ignoreCase: true);

        return System.Convert.ChangeType(s, underlying, CultureInfo.InvariantCulture);
    }
}
