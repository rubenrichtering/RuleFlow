namespace RuleFlow.Abstractions.Execution;

/// <summary>
/// Options to control how the RuleEngine evaluates and executes rules.
/// </summary>
public class RuleExecutionOptions<T>
{
    /// <summary>
    /// If true, stops execution after the first rule matches.
    /// Rule.StopProcessing (rule-level) takes precedence over this setting.
    /// </summary>
    public bool StopOnFirstMatch { get; set; } = false;

    /// <summary>
    /// Optional filter to determine which rules should be evaluated.
    /// Rules that return false from this filter will be skipped.
    /// </summary>
    public Func<IRule<T>, bool>? MetadataFilter { get; set; }

    /// <summary>
    /// If specified, only rules in these groups will be evaluated.
    /// Root RuleSet always executes.
    /// </summary>
    public IReadOnlyCollection<string>? IncludeGroups { get; set; }

    /// <summary>
    /// If false, the execution tree (Root node and children) will not be built.
    /// Results will still be available but the tree structure will be minimal.
    /// Default is true to maintain full explainability.
    /// </summary>
    public bool EnableExplainability { get; set; } = true;
}

