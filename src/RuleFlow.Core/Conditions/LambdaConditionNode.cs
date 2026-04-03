using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions;

/// <summary>
/// An internal condition node that wraps a lambda predicate.
/// Used exclusively by the fluent API (.When / .WhenGroup) to mix
/// lambda conditions with structured nodes (e.g. AiConditionNode) in the same tree.
/// Not serializable; never leaves Core.
/// </summary>
internal sealed class LambdaConditionNode<T> : ConditionNode
{
    private readonly Func<T, bool> _condition;

    internal LambdaConditionNode(Func<T, bool> condition)
    {
        _condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    internal bool Evaluate(T input) => _condition(input);
}
