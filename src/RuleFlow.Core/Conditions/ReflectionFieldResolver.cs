using System.Collections.Concurrent;
using System.Reflection;
using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// Resolves public instance properties by dotted path (e.g. <c>Customer.Name</c>, <c>Customer.Address.City</c>),
/// with per-(declaring type, segment) <see cref="PropertyInfo"/> caching.
/// Unknown segments throw <see cref="FieldResolutionException"/>; a null intermediate instance yields null.
/// </summary>
public sealed class ReflectionFieldResolver<T> : IFieldResolver<T>
{
    private static readonly ConcurrentDictionary<(Type DeclaringType, string SegmentKey), PropertyInfo?> PropertyCache = new();

    public object? GetValue(T input, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field name must not be empty.", nameof(field));

        if (input is null)
            return null;

        var parts = SplitPath(field);
        object? current = input;

        foreach (var segment in parts)
        {
            if (current == null)
                return null;

            var declaringType = current.GetType();
            var prop = GetCachedProperty(declaringType, segment, field);
            current = prop.GetValue(current);
        }

        return current;
    }

    public Type? GetFieldType(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        var parts = SplitPath(field);
        Type currentType = typeof(T);

        foreach (var segment in parts)
        {
            var prop = GetCachedProperty(currentType, segment, field);
            currentType = prop.PropertyType;
        }

        return currentType;
    }

    private static string[] SplitPath(string field)
    {
        var parts = field.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new ArgumentException("Field path must contain at least one segment.", nameof(field));

        return parts;
    }

    private static PropertyInfo GetCachedProperty(Type declaringType, string segment, string fullPath)
    {
        var key = (declaringType, segment.ToLowerInvariant());
        var prop = PropertyCache.GetOrAdd(key, static (k, seg) =>
        {
            var (type, _) = k;
            return type.GetProperty(seg, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        }, segment);

        if (prop == null)
        {
            throw new FieldResolutionException(
                fullPath,
                $"No public instance property '{segment}' on type '{declaringType.Name}' (path '{fullPath}').");
        }

        return prop;
    }
}
