using RuleFlow.Abstractions.Observability;

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

    /// <summary>
    /// If true, enables lightweight observability callbacks and metrics collection.
    /// Default is false for zero-overhead performance when not needed.
    /// When enabled without a custom observer, a built-in InMemoryRuleObserver is used.
    /// </summary>
    public bool EnableObservability { get; set; } = false;

    /// <summary>
    /// If true, captures detailed timing (duration) for rules and overall execution.
    /// Only has effect when EnableObservability is also true.
    /// Default is false to avoid Stopwatch overhead.
    /// </summary>
    public bool EnableDetailedTiming { get; set; } = false;

    /// <summary>
    /// Optional custom observer to receive observability callbacks.
    /// If not provided and EnableObservability is true, a built-in observer is used.
    /// </summary>
    public IRuleObserver<T>? Observer { get; set; }
}

