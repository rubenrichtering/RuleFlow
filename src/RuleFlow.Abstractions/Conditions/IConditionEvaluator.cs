using RuleFlow.Abstractions;

namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Evaluates a <see cref="ConditionNode"/> tree against an input instance.
/// </summary>
public interface IConditionEvaluator<T>
{
    bool Evaluate(T input, ConditionNode node, IRuleContext context);
}
