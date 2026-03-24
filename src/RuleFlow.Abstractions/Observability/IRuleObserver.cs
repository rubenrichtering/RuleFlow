namespace RuleFlow.Abstractions.Observability;

/// <summary>
/// Optional lightweight observer for rule execution events.
/// Implementations must be synchronous and safe to invoke repeatedly.
/// Exceptions in observer implementations must be handled gracefully by the engine.
/// </summary>
/// <typeparam name="T">The type of object being evaluated.</typeparam>
public interface IRuleObserver<T>
{
    /// <summary>
    /// Called before a rule's condition is evaluated.
    /// </summary>
    void OnRuleEvaluating(RuleEvaluationContext<T> context);

    /// <summary>
    /// Called when a rule's condition evaluated to true.
    /// </summary>
    void OnRuleMatched(RuleMatchContext<T> context);

    /// <summary>
    /// Called after a matched rule's actions have been executed.
    /// </summary>
    void OnRuleExecuted(RuleExecutionContext<T> context);

    /// <summary>
    /// Called once at the end of the entire rule evaluation and execution cycle.
    /// </summary>
    void OnExecutionCompleted(RuleExecutionSummary summary);
}
