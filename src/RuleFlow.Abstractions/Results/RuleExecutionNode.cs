namespace RuleFlow.Abstractions.Results;

/// <summary>
/// Represents a node in the execution tree (either a rule or a group).
/// 
/// This provides a hierarchical view of rule execution, showing:
/// - Execution state (Executed, Matched, Skipped, StoppedProcessing)
/// - Action-level results
/// - Group/rule structure
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
    /// Whether the rule was executed (condition was evaluated).
    /// For groups, this is always true.
    /// </summary>
    public bool Executed { get; set; }

    /// <summary>
    /// Whether the rule matched (condition returned true).
    /// Null for groups.
    /// </summary>
    public bool? Matched { get; set; }

    /// <summary>
    /// Whether the rule was skipped (not evaluated due to filters).
    /// For groups, this is always false.
    /// </summary>
    public bool Skipped { get; set; }

    /// <summary>
    /// The reason why the rule was skipped, if applicable.
    /// </summary>
    public string? SkipReason { get; set; }

    /// <summary>
    /// Reason from the rule's Because() clause.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Priority of the rule.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Whether this rule stopped processing.
    /// </summary>
    public bool StoppedProcessing { get; set; }

    /// <summary>
    /// Collection of actions executed within this rule.
    /// Empty for groups.
    /// </summary>
    public List<ActionExecution> Actions { get; } = new();

    /// <summary>
    /// Child nodes (rules and groups within this group).
    /// </summary>
    public List<RuleExecutionNode> Children { get; } = new();
}
