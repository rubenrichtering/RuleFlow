namespace RuleFlow.Abstractions;

public interface IRuleContext
{
    /// <summary>
    /// Gets the current DateTime (typically UTC now).
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets a read-only dictionary of custom data items that can be passed to rules.
    /// </summary>
    IReadOnlyDictionary<string, object?> Items { get; }

    /// <summary>
    /// Attempts to get a value from the context by key, with type safety.
    /// </summary>
    T? Get<T>(string key);
}