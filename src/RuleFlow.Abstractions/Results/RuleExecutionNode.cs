namespace RuleFlow.Abstractions.Results;

/// <summary>
/// Represents a node in the execution tree (either a rule or a group).
/// </summary>
public class RuleExecutionNode
{
    /// <summary>
    /// Name of the rule or group.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Type of node: "Rule" or "Group".
    /// </summary>
    public string Type { get; set; } = default!;

    /// <summary>
    /// Whether the rule matched. Null for groups.
    /// </summary>
    public bool? Matched { get; set; }

    /// <summary>
    /// Reason why the rule was applied.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Priority of the rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether the rule was executed.
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// Whether this rule stopped processing.
    /// </summary>
    public bool StoppedProcessing { get; set; }

    /// <summary>
    /// Child nodes (rules and groups within this group).
    /// </summary>
    public List<RuleExecutionNode> Children { get; } = new();
}
