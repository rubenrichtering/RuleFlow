namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Logical grouping of child conditions (AND / OR).
/// </summary>
public class ConditionGroup : ConditionNode
{
    public string Operator { get; set; } = "AND";

    public List<ConditionNode> Conditions { get; set; } = new();
}
