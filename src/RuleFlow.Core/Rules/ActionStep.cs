using RuleFlow.Abstractions;

namespace RuleFlow.Core.Rules;

/// <summary>
/// Represents a single action step in the execution chain of a rule.
/// A step can be a simple action (unconditional) or a conditional action (ThenIf).
/// </summary>
internal sealed class ActionStep<T>
{
    /// <summary>
    /// Required. The action to execute if this step matches.
    /// </summary>
    public required Func<T, IRuleContext, Task> ExecuteAsync { get; init; }

    /// <summary>
    /// Optional. If provided, the action only executes if this predicate returns true.
    /// If null, the action always executes (Then, not ThenIf).
    /// </summary>
    public Func<T, IRuleContext, Task<bool>>? PredicateAsync { get; init; }

    /// <summary>
    /// Whether this step contains an async component.
    /// </summary>
    public bool IsAsync { get; init; }

    /// <summary>
    /// Human-readable label for this step (e.g., "Then: ...", "ThenIf: ...").
    /// Used for explainability.
    /// </summary>
    public string Label { get; init; } = "";

}
