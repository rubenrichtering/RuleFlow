using RuleFlow.Abstractions.Conditions;
using RuleFlow.Core.Conditions;

namespace RuleFlow.Core.Builders;

/// <summary>
/// Fluent builder for creating a <see cref="ConditionGroup"/> that mixes
/// lambda predicates and AI conditions.
/// Used by <c>Rule&lt;T&gt;.WhenGroup(...)</c>.
/// </summary>
public sealed class ConditionGroupBuilder<T>
{
    private readonly List<ConditionNode> _conditions = new();
    private string _operator = "AND";

    /// <summary>
    /// Adds a lambda predicate condition to the group.
    /// </summary>
    public ConditionGroupBuilder<T> When(Func<T, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _conditions.Add(new LambdaConditionNode<T>(condition));
        return this;
    }

    /// <summary>
    /// Adds an AI condition with the given prompt and the full input as context.
    /// </summary>
    public ConditionGroupBuilder<T> WhenAI(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));

        _conditions.Add(new AiConditionNode { Prompt = prompt });
        return this;
    }

    /// <summary>
    /// Adds an AI condition with the given prompt and a focused sub-object projection
    /// passed to the AI evaluator as context.
    /// </summary>
    public ConditionGroupBuilder<T> WhenAI(string prompt, Func<T, object> inputSelector)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
        ArgumentNullException.ThrowIfNull(inputSelector);

        _conditions.Add(new AiConditionNode
        {
            Prompt = prompt,
            InputSelector = input => inputSelector((T)input!)
        });
        return this;
    }

    /// <summary>
    /// Switches the group logical operator to OR.
    /// By default the group uses AND.
    /// </summary>
    public ConditionGroupBuilder<T> Or()
    {
        _operator = "OR";
        return this;
    }

    /// <summary>
    /// Switches the group logical operator back to AND (default).
    /// </summary>
    public ConditionGroupBuilder<T> And()
    {
        _operator = "AND";
        return this;
    }

    internal ConditionGroup Build() => new()
    {
        Operator = _operator,
        Conditions = _conditions
    };
}
