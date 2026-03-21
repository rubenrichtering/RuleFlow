using RuleFlow.Abstractions;

namespace RuleFlow.Core.Context;

/// <summary>
/// Default implementation of IRuleContext with runtime data and custom items storage.
/// </summary>
public class RuleContext : IRuleContext
{
    private readonly Dictionary<string, object?> _items;

    /// <summary>
    /// Gets the current DateTime in UTC.
    /// </summary>
    public DateTime Now { get; }

    /// <summary>
    /// Gets a read-only view of the items dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Items { get; }

    /// <summary>
    /// Creates a new RuleContext with the current time.
    /// </summary>
    public RuleContext() : this(DateTime.UtcNow, new Dictionary<string, object?>())
    {
    }

    /// <summary>
    /// Creates a new RuleContext with a specific time and items.
    /// </summary>
    public RuleContext(DateTime now) : this(now, new Dictionary<string, object?>())
    {
    }

    /// <summary>
    /// Creates a new RuleContext with a specific time and pre-populated items.
    /// </summary>
    public RuleContext(DateTime now, IDictionary<string, object?> items)
    {
        Now = now;
        _items = new Dictionary<string, object?>(items ?? new Dictionary<string, object?>());
        Items = _items.AsReadOnly();
    }

    /// <summary>
    /// Gets a value from the context by key with type safety.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (_items.TryGetValue(key, out var value))
        {
            return (T?)value;
        }
        return default;
    }

    /// <summary>
    /// Sets or updates a value in the context.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        _items[key] = value;
    }

    /// <summary>
    /// Gets the default/empty context instance for backward compatibility.
    /// </summary>
    public static IRuleContext Default { get; } = new RuleContext();
}

/// <summary>
/// Backwards compatibility alias.
/// </summary>
public class DefaultRuleContext : RuleContext
{
    public static DefaultRuleContext Instance { get; } = new();
}