using RuleFlow.Abstractions;

namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Evaluates a <see cref="ConditionNode"/> tree against an input instance.
/// </summary>
public interface IConditionEvaluator<T>
{
    bool Evaluate(T input, ConditionNode node, IRuleContext context);

    /// <summary>
    /// Evaluates a <see cref="ConditionNode"/> tree asynchronously.
    /// The default implementation delegates to the synchronous <see cref="Evaluate"/> path.
    /// Override to support async condition types such as <see cref="AiConditionNode"/>.
    /// </summary>
    Task<bool> EvaluateAsync(T input, ConditionNode node, IRuleContext context, CancellationToken ct = default)
        => Task.FromResult(Evaluate(input, node, context));
}
