namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Resolves operators by name (case-insensitive).
/// </summary>
public interface IOperatorRegistry
{
    IOperator Get(string name);
}
