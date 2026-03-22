using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Persistence;

namespace RuleFlow.Core.Persistence;

/// <summary>
/// Default implementation of <see cref="IRuleRegistry{T}"/>.
/// 
/// Manages registration and retrieval of condition and action logic by string keys.
/// </summary>
public class RuleRegistry<T> : IRuleRegistry<T>
{
    private readonly Dictionary<string, Func<T, IRuleContext, bool>> _conditions = new();
    private readonly Dictionary<string, Action<T, IRuleContext>> _actions = new();

    /// <inheritdoc />
    public void RegisterCondition(string key, Func<T, IRuleContext, bool> condition)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Condition key cannot be null or empty.", nameof(key));

        if (condition == null)
            throw new ArgumentNullException(nameof(condition));

        if (_conditions.ContainsKey(key))
            throw new ArgumentException($"Condition key '{key}' is already registered.", nameof(key));

        _conditions[key] = condition;
    }

    /// <inheritdoc />
    public void RegisterAction(string key, Action<T, IRuleContext> action)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Action key cannot be null or empty.", nameof(key));

        if (action == null)
            throw new ArgumentNullException(nameof(action));

        if (_actions.ContainsKey(key))
            throw new ArgumentException($"Action key '{key}' is already registered.", nameof(key));

        _actions[key] = action;
    }

    /// <inheritdoc />
    public Func<T, IRuleContext, bool> GetCondition(string key)
    {
        if (_conditions.TryGetValue(key, out var condition))
            return condition;

        throw new KeyNotFoundException($"Condition key '{key}' not found in registry.");
    }

    /// <inheritdoc />
    public Action<T, IRuleContext> GetAction(string key)
    {
        if (_actions.TryGetValue(key, out var action))
            return action;

        throw new KeyNotFoundException($"Action key '{key}' not found in registry.");
    }
}
