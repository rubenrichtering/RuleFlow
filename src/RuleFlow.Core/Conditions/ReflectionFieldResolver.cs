using System.Collections.Concurrent;
using System.Reflection;
using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// Resolves public instance properties by name (case-insensitive), with <see cref="PropertyInfo"/> caching.
/// </summary>
public sealed class ReflectionFieldResolver<T> : IFieldResolver<T>
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public object? GetValue(T input, string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Field name must not be empty.", nameof(field));

        var prop = ResolveProperty(field);
        if (prop == null)
            return null;

        return prop.GetValue(input);
    }

    public Type? GetFieldType(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return null;

        return ResolveProperty(field)?.PropertyType;
    }

    private static PropertyInfo? ResolveProperty(string field)
    {
        return Cache.GetOrAdd(field, static name =>
        {
            var t = typeof(T);
            return t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        });
    }
}
