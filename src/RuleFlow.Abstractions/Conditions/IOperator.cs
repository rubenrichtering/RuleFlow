namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// A named comparison operator (e.g. equals, greater_than).
/// </summary>
public interface IOperator
{
    string Name { get; }

    bool Evaluate(object? left, object? right);
}
