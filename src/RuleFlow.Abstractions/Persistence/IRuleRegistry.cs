namespace RuleFlow.Abstractions.Persistence;

/// <summary>
/// Registry for resolving condition and action logic from string keys.
/// 
/// Used by the persistence layer to map RuleDefinition to executable Rule<T>.
/// </summary>
public interface IRuleRegistry<T>
{
    /// <summary>
    /// Registers a condition logic with a string key.
    /// </summary>
    /// <param name="key">Unique identifier for this condition.</param>
    /// <param name="condition">The condition logic.</param>
    /// <exception cref="ArgumentException">If key is null or empty, or already registered.</exception>
    void RegisterCondition(string key, Func<T, IRuleContext, bool> condition);

    /// <summary>
    /// Registers an action logic with a string key.
    /// </summary>
    /// <param name="key">Unique identifier for this action.</param>
    /// <param name="action">The action logic.</param>
    /// <exception cref="ArgumentException">If key is null or empty, or already registered.</exception>
    void RegisterAction(string key, Action<T, IRuleContext> action);

    /// <summary>
    /// Retrieves a registered condition by key.
    /// </summary>
    /// <param name="key">The condition key.</param>
    /// <returns>The condition logic.</returns>
    /// <exception cref="KeyNotFoundException">If key not found.</exception>
    Func<T, IRuleContext, bool> GetCondition(string key);

    /// <summary>
    /// Retrieves a registered action by key.
    /// </summary>
    /// <param name="key">The action key.</param>
    /// <returns>The action logic.</returns>
    /// <exception cref="KeyNotFoundException">If key not found.</exception>
    Action<T, IRuleContext> GetAction(string key);
}
