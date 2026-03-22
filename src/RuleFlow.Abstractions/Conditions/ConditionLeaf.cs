namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// A single comparison: field vs literal value or vs another field.
/// </summary>
public class ConditionLeaf : ConditionNode
{
    public string Field { get; set; } = default!;

    public string Operator { get; set; } = default!;

    public object? Value { get; set; }

    public string? CompareToField { get; set; }
}
