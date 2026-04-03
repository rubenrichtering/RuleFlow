using RuleFlow.Abstractions.Conditions;
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

    /// <summary>
    /// If true, AI-backed <see cref="AiConditionNode"/> conditions are evaluated via the
    /// registered <see cref="IAiConditionEvaluator{T}"/>.
    /// Default is <see langword="false"/> — AI conditions are skipped and resolve to
    /// <see langword="false"/> when disabled, with zero overhead.
    /// </summary>
    public bool EnableAiConditions { get; set; } = false;

    /// <summary>
    /// Maximum time allowed for a single AI condition evaluation.
    /// When the timeout elapses, the evaluation is cancelled and the
    /// <see cref="AiFailureStrategy"/> is applied.
    /// Default is <see langword="null"/> (no timeout).
    /// </summary>
    public TimeSpan? AiTimeout { get; set; }

    /// <summary>
    /// Determines the fallback value when an AI condition fails due to an
    /// exception, timeout, or cancellation.
    /// Default is <see cref="AiFailureStrategy.ReturnFalse"/> — the safe option.
    /// </summary>
    public AiFailureStrategy AiFailureStrategy { get; set; } = AiFailureStrategy.ReturnFalse;

    /// <summary>
    /// If true, AI condition results are cached within a single execution using
    /// the prompt and serialized input as the cache key.
    /// Prevents redundant AI calls for identical inputs within one rule evaluation.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool EnableAiCaching { get; set; } = false;

    /// <summary>
    /// Optional logger for AI condition lifecycle events (evaluating, evaluated, failure).
    /// Designed for audit logging, compliance tracking, and debugging.
    /// No-op when <see langword="null"/>. Logger exceptions are suppressed.
    /// </summary>
    public IAiExecutionLogger? AiLogger { get; set; }
}

