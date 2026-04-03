namespace RuleFlow.Abstractions.Debug;

/// <summary>
/// Debug representation of a rule group within the execution hierarchy.
/// </summary>
public class DebugGroup
{
    /// <summary>Name of the group.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full hierarchical path from the rule set root, e.g. <c>"OrderApproval/Validation/HighValue"</c>.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>Nested child groups within this group.</summary>
    public List<DebugGroup> Groups { get; set; } = [];

    /// <summary>Rules directly contained in this group.</summary>
    public List<DebugRule> Rules { get; set; } = [];
}
