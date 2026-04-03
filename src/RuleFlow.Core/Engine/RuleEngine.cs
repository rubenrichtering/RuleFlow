using System.Diagnostics;
using System.Linq;
using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Observability;
using RuleFlow.Abstractions.Results;
using RuleFlow.Core.Conditions;
using RuleFlow.Core.Context;
using RuleFlow.Core.Observability;
using RuleFlow.Core.Rules;

namespace RuleFlow.Core.Engine;

/// <summary>
/// RuleEngine implements a single, unified async-first execution pipeline.
/// All execution paths (sync and async) delegate to a single internal async method.
/// </summary>
public class RuleEngine : IRuleEngine
{
    /// <summary>
    /// Evaluates rules against input synchronously.
    /// Delegates to async pipeline.
    /// </summary>
    public RuleResult Evaluate<T>(T input, IRuleSet<T> ruleSet, IRuleContext? context = null)
    {
        return Evaluate(input, ruleSet, context, options: null);
    }

    /// <summary>
    /// Evaluates rules against input with execution options synchronously.
    /// Delegates to async pipeline.
    /// </summary>
    public RuleResult Evaluate<T>(T input, IRuleSet<T> ruleSet, RuleExecutionOptions<T> options)
    {
        return Evaluate(input, ruleSet, context: null, options);
    }

    /// <summary>
    /// Evaluates rules against input with context and options synchronously.
    /// Delegates to async pipeline.
    /// </summary>
    public RuleResult Evaluate<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context,
        RuleExecutionOptions<T>? options
    )
    {
        return EvaluateAsync(input, ruleSet, context, options).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Evaluates rules against input asynchronously.
    /// Delegates to unified internal pipeline.
    /// </summary>
    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context = null
    )
    {
        return await EvaluateAsync(input, ruleSet, context, options: null);
    }

    /// <summary>
    /// Evaluates rules against input with execution options asynchronously.
    /// Delegates to unified internal pipeline.
    /// </summary>
    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        RuleExecutionOptions<T> options
    )
    {
        return await EvaluateAsync(input, ruleSet, context: null, options);
    }

    /// <summary>
    /// Evaluates rules against input with context and options asynchronously.
    /// Main entry point that delegates to unified internal pipeline.
    /// </summary>
    public async Task<RuleResult> EvaluateAsync<T>(
        T input,
        IRuleSet<T> ruleSet,
        IRuleContext? context,
        RuleExecutionOptions<T>? options
    )
    {
        if (default(T) is null && input is null)
            throw new ArgumentNullException(nameof(input));

        if (ruleSet == null)
            throw new ArgumentNullException(nameof(ruleSet));

        context ??= new RuleContext();
        options ??= new RuleExecutionOptions<T>();

        var result = new RuleResult();
        var executionState = new ExecutionState();

        // Initialize observability runtime
        var observabilityEnabled = options.EnableObservability;
        var observabilityRuntime = new ObservabilityRuntime<T>(
            options,
            observabilityEnabled
        );

        RuleExecutionNode? root = null;
        if (options.EnableExplainability)
        {
            root = new RuleExecutionNode
            {
                Name = ruleSet.Name,
                Type = "Group",
                Executed = true,
                Priority = 0,
            };
        }

        // Single unified execution pipeline
        var evalContext = new EvaluationContext<T>(
            input,
            context,
            result,
            root,
            options,
            executionState,
            groupPath: null,
            observabilityRuntime
        );

        // Start observability timing if enabled
        observabilityRuntime.StartTiming();

        await EvaluateInternalAsync(ruleSet, evalContext);

        if (options.EnableExplainability)
        {
            result.Root = root;
        }

        // Complete observability and populate metrics
        if (observabilityEnabled)
        {
            observabilityRuntime.CompleteTiming();
            var summary = observabilityRuntime.GetSummary(result.Executions);
            result.Metrics = summary.Metrics;

            // Call final completion callback
            SafeInvokeObserver(() => 
                observabilityRuntime.Observer.OnExecutionCompleted(summary)
            );
        }

        return result;
    }


    /// <summary>
    /// SINGLE UNIFIED EXECUTION METHOD - The source of truth for all rule evaluation and execution.
    ///
    /// Responsibilities:
    /// - Evaluate conditions (respecting async priority)
    /// - Record execution results with clear state tracking (Executed/Skipped/Matched)
    /// - Track action-level execution
    /// - Execute actions (respecting async priority)
    /// - Handle StopProcessing and StopOnFirstMatch
    /// - Process groups recursively (using the same pipeline)
    /// - Apply metadata filters and inclusion filters
    /// - Build explainability tree
    ///
    /// All execution paths (sync and async) flow through this method.
    /// </summary>
    private async Task EvaluateInternalAsync<T>(IRuleSet<T> ruleSet, EvaluationContext<T> eval)
    {
        // Check if this group should be included
        if (
            eval.GroupPath != null
            && !ShouldEvaluateGroup(ruleSet, eval.GroupPath, eval.Options.IncludeGroups)
        )
        {
            return; // Skip this group
        }

        var orderedRules = GetRulesSortedByPriorityDescending(ruleSet);

        if (await EvaluateRulesInCurrentRuleSetAsync(orderedRules, eval))
        {
            return;
        }

        await EvaluateGroupsInInsertionOrderAsync(ruleSet, eval);
    }

    private async Task<bool> EvaluateRulesInCurrentRuleSetAsync<T>(
        IReadOnlyList<IRule<T>> orderedRules,
        EvaluationContext<T> eval
    )
    {
        // === EVALUATE RULES IN CURRENT RULESET ===
        for (var i = 0; i < orderedRules.Count; i++)
        {
            var rule = orderedRules[i];
            if (eval.Options.MetadataFilter != null && !eval.Options.MetadataFilter(rule))
            {
                RecordSkippedRule(
                    eval.Result,
                    eval.ParentNode,
                    eval.Options.EnableExplainability,
                    rule,
                    eval.GroupPath
                );

                continue;
            }

            // Observability hook: before rule evaluation
            if (eval.ObservabilityRuntime.Enabled)
            {
                SafeInvokeObserver(() =>
                    eval.ObservabilityRuntime.OnRuleEvaluating(rule.Name, eval.Input, eval.GroupPath)
                );
            }

            // Evaluate the rule: prefer the options-aware path for Rule<T> (supports AI features,
            // timeout, failure strategy, logging, caching, metrics). Fall back to the interface
            // contract for custom IRule<T> implementations.
            bool matched;
            RuleFlow.Abstractions.Debug.DebugConditionNode? conditionTree = null;

            if (rule is Rule<T> coreRule)
            {
                var needsDebugTree = eval.Options.EnableExplainability
                    && (coreRule.HasStructuredCondition || coreRule.HasFluentCondition);

                if (needsDebugTree)
                {
                    matched = await coreRule.EvaluateWithDebugAndOptionsAsync(
                        eval.Input, eval.RuleContext, eval.Options,
                        eval.ObservabilityRuntime.AiMetrics,
                        tree => conditionTree = tree);
                }
                else
                {
                    matched = await coreRule.EvaluateWithOptionsAsync(
                        eval.Input, eval.RuleContext, eval.Options,
                        eval.ObservabilityRuntime.AiMetrics);
                }
            }
            else
            {
                matched = await rule.EvaluateAsync(eval.Input, eval.RuleContext);
            }

            var execution = CreateExecutionRecord(rule, matched, eval.GroupPath);

            foreach (var kvp in rule.Metadata)
            {
                execution.Metadata[kvp.Key] = kvp.Value;
            }

            eval.Result.Executions.Add(execution);

            RuleExecutionNode? ruleNode = null;
            if (eval.Options.EnableExplainability && eval.ParentNode != null)
            {
                ruleNode = CreateRuleNode(rule, matched);
                ruleNode.ConditionTree = conditionTree;
                eval.ParentNode.Children.Add(ruleNode);
            }

            if (matched)
            {
                // Observability hook: rule matched
                if (eval.ObservabilityRuntime.Enabled)
                {
                    SafeInvokeObserver(() =>
                        eval.ObservabilityRuntime.OnRuleMatched(rule.Name, eval.Input, eval.GroupPath, rule.Reason)
                    );
                }

                var actionCount = await ExecuteMatchedRuleAsync(
                    rule,
                    eval.Input,
                    eval.RuleContext,
                    execution,
                    ruleNode,
                    eval.Options.EnableExplainability
                );

                // Observability hook: after rule execution
                if (eval.ObservabilityRuntime.Enabled)
                {
                    SafeInvokeObserver(() =>
                        eval.ObservabilityRuntime.OnRuleExecuted(rule.Name, eval.Input, eval.GroupPath, actionCount, true)
                    );
                }
            }

            if (
                matched
                && ShouldStopAfterMatchedRule(
                    rule,
                    execution,
                    eval.ParentNode,
                    eval.Options,
                    eval.ExecutionState
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private static RuleExecution CreateExecutionRecord<T>(
        IRule<T> rule,
        bool matched,
        string? groupPath
    )
    {
        return new RuleExecution
        {
            RuleName = rule.Name,
            Executed = true,
            Skipped = false,
            Matched = matched,
            Reason = rule.Reason,
            Priority = rule.Priority,
            GroupName = groupPath,
        };
    }

    private static RuleExecutionNode CreateRuleNode<T>(IRule<T> rule, bool matched)
    {
        return new RuleExecutionNode
        {
            Name = rule.Name,
            Type = "Rule",
            Executed = true,
            Skipped = false,
            Matched = matched,
            Reason = rule.Reason,
            Priority = rule.Priority,
        };
    }

    private static void RecordSkippedRule<T>(
        RuleResult result,
        RuleExecutionNode? parentNode,
        bool explainabilityEnabled,
        IRule<T> rule,
        string? groupPath
    )
    {
        var skippedExecution = new RuleExecution
        {
            RuleName = rule.Name,
            Executed = false,
            Skipped = true,
            SkipReason = "MetadataFilter",
            Matched = false,
            Reason = rule.Reason,
            Priority = rule.Priority,
            GroupName = groupPath,
        };

        foreach (var kvp in rule.Metadata)
        {
            skippedExecution.Metadata[kvp.Key] = kvp.Value;
        }

        result.Executions.Add(skippedExecution);

        if (explainabilityEnabled && parentNode != null)
        {
            var skippedRuleNode = new RuleExecutionNode
            {
                Name = rule.Name,
                Type = "Rule",
                Executed = false,
                Skipped = true,
                SkipReason = "MetadataFilter",
                Matched = false,
                Reason = rule.Reason,
                Priority = rule.Priority,
            };

            parentNode.Children.Add(skippedRuleNode);
        }
    }

    private async Task<int> ExecuteMatchedRuleAsync<T>(
        IRule<T> rule,
        T input,
        IRuleContext context,
        RuleExecution execution,
        RuleExecutionNode? ruleNode,
        bool explainabilityEnabled
    )
    {
        if (explainabilityEnabled && ruleNode != null)
        {
            var actionExecutions = await ExecuteActionsWithTrackingAsync(rule, input, context);
            execution.Actions.AddRange(actionExecutions);
            ruleNode.Actions.AddRange(actionExecutions);
            return actionExecutions.Count;
        }

        // Explainability disabled: run actions without per-step ActionExecution allocations.
        // Non-Rule<T> IRule implementations still execute via tracking helper.
        if (rule is Rule<T>)
        {
            await rule.ExecuteAsync(input, context);
            return 0; // No tracking when explainability disabled for Rule<T>
        }

        var trackedExecutions = await ExecuteActionsWithTrackingAsync(rule, input, context);
        execution.Actions.AddRange(trackedExecutions);
        return trackedExecutions.Count;
    }

    private static bool ShouldStopAfterMatchedRule<T>(
        IRule<T> rule,
        RuleExecution execution,
        RuleExecutionNode? parentNode,
        RuleExecutionOptions<T> options,
        ExecutionState executionState
    )
    {
        if (rule.StopProcessing)
        {
            execution.StoppedProcessing = true;
            if (options.EnableExplainability && parentNode != null && parentNode.Children.Count > 0)
            {
                parentNode.Children[parentNode.Children.Count - 1].StoppedProcessing = true;
            }

            return true;
        }

        if (options.StopOnFirstMatch)
        {
            executionState.StoppedByFirstMatchOption = true;
            return true;
        }

        return false;
    }

    private async Task<bool> EvaluateGroupsInInsertionOrderAsync<T>(
        IRuleSet<T> ruleSet,
        EvaluationContext<T> eval
    )
    {
        foreach (var group in ruleSet.Groups)
        {
            if (await ProcessGroupAsync(group, eval))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> ProcessGroupAsync<T>(IRuleSet<T> group, EvaluationContext<T> eval)
    {
        var groupNode = CreateGroupNode(eval, group.Name);
        var groupParentNode = groupNode ?? eval.ParentNode;
        var previousExecutionCount = eval.Result.Executions.Count;
        var childGroupPath = BuildGroupPath(eval.GroupPath, group.Name);

        // Observability: track group traversal
        if (eval.ObservabilityRuntime.Enabled)
        {
            eval.ObservabilityRuntime.IncrementGroupsTraversed();
        }

        var childEval = eval.WithGroup(groupParentNode, childGroupPath);
        await EvaluateInternalAsync(group, childEval);

        if (DidAnyExecutionStopProcessing(eval.Result, previousExecutionCount))
        {
            if (eval.Options.EnableExplainability && groupNode != null)
            {
                MarkRemainingAsSkipped(groupNode);
            }

            return true;
        }

        return eval.ExecutionState.StoppedByFirstMatchOption;
    }

    private static RuleExecutionNode? CreateGroupNode<T>(
        EvaluationContext<T> eval,
        string groupName
    )
    {
        if (!eval.Options.EnableExplainability)
            return null;

        var groupNode = new RuleExecutionNode
        {
            Name = groupName,
            Type = "Group",
            Executed = true,
            Priority = 0,
        };

        eval.ParentNode!.Children.Add(groupNode);
        return groupNode;
    }

    private static bool DidAnyExecutionStopProcessing(RuleResult result, int startIndex)
    {
        for (var i = startIndex; i < result.Executions.Count; i++)
        {
            if (result.Executions[i].StoppedProcessing)
                return true;
        }

        return false;
    }

    private static IReadOnlyList<IRule<T>> GetRulesSortedByPriorityDescending<T>(
        IRuleSet<T> ruleSet
    )
    {
        if (ruleSet is RuleSet<T> concrete)
        {
            return concrete.GetRulesSortedByPriorityDescending();
        }

        return ruleSet.Rules.OrderByDescending(r => r.Priority).ToList();
    }

    private static string BuildGroupPath(string? parentPath, string groupName)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
            return groupName;

        return $"{parentPath}/{groupName}";
    }

    private static bool ShouldEvaluateGroup<T>(
        IRuleSet<T> ruleSet,
        string groupPath,
        IReadOnlyCollection<string>? includeGroups
    )
    {
        if (includeGroups == null)
            return true;

        if (IsDirectIncludedGroup(groupPath, includeGroups))
            return true;

        // Keep traversing ancestor groups if any descendant is included.
        foreach (var child in ruleSet.Groups)
        {
            var childPath = BuildGroupPath(groupPath, child.Name);
            if (IsDirectIncludedGroup(childPath, includeGroups))
                return true;

            if (ShouldEvaluateGroup(child, childPath, includeGroups))
                return true;
        }

        return false;
    }

    private static bool IsDirectIncludedGroup(
        string groupPath,
        IReadOnlyCollection<string> includeGroups
    )
    {
        foreach (var include in includeGroups)
        {
            if (string.IsNullOrWhiteSpace(include))
                continue;

            // Preferred matching: full, hierarchical path (e.g. Parent/Child).
            if (string.Equals(include, groupPath, StringComparison.OrdinalIgnoreCase))
                return true;

            // Backward compatibility: leaf-name matching (e.g. Child).
            if (
                string.Equals(
                    include,
                    ExtractLeafGroupName(groupPath),
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return true;
        }

        return false;
    }

    private static string ExtractLeafGroupName(string groupPath)
    {
        var idx = groupPath.LastIndexOf('/');
        if (idx < 0 || idx == groupPath.Length - 1)
            return groupPath;

        return groupPath[(idx + 1)..];
    }

    /// <summary>
    /// Executes all action steps in a rule and tracks which ones executed vs. were skipped.
    /// Returns a list of ActionExecution records for explainability.
    /// </summary>
    private async Task<List<ActionExecution>> ExecuteActionsWithTrackingAsync<T>(
        IRule<T> rule,
        T input,
        IRuleContext context
    )
    {
        var actionExecutions = new List<ActionExecution>();

        // Get the action steps from the rule
        var rule_impl = rule as Rule<T>;
        if (rule_impl == null)
        {
            // For custom IRule<T> implementations, execute via interface contract.
            await rule.ExecuteAsync(input, context);
            return actionExecutions;
        }

        var actionSteps = rule_impl.GetActionSteps();

        foreach (var step in actionSteps)
        {
            // Check if this is a conditional step
            if (step.PredicateAsync == null)
            {
                // Unconditional action - always execute
                await step.ExecuteAsync(input, context);

                actionExecutions.Add(
                    new ActionExecution
                    {
                        Description = step.Label,
                        Executed = true,
                        Skipped = false,
                    }
                );
            }
            else
            {
                // Conditional action - check predicate first
                bool predicatePassed = await step.PredicateAsync(input, context);

                if (predicatePassed)
                {
                    await step.ExecuteAsync(input, context);

                    actionExecutions.Add(
                        new ActionExecution
                        {
                            Description = step.Label,
                            Executed = true,
                            Skipped = false,
                        }
                    );
                }
                else
                {
                    actionExecutions.Add(
                        new ActionExecution
                        {
                            Description = step.Label,
                            Executed = false,
                            Skipped = true,
                            SkipReason = "PredicateNotMet",
                        }
                    );
                }
            }
        }

        return actionExecutions;
    }

    /// <summary>
    /// Marks remaining unevaluated rules in a node as skipped in the explainability tree.
    /// </summary>
    private void MarkRemainingAsSkipped(RuleExecutionNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.Type == "Rule" && !child.Executed)
            {
                child.Skipped = true;
                child.SkipReason = "StoppedByPreviousRule";
            }
            else if (child.Type == "Group")
            {
                MarkRemainingAsSkipped(child);
            }
        }
    }

    /// <summary>
    /// Safely invokes an observer callback, catching and suppressing any exceptions
    /// to ensure observability failures never break rule execution.
    /// </summary>
    private static void SafeInvokeObserver(Action observerAction)
    {
        try
        {
            observerAction.Invoke();
        }
        catch
        {
            // Silently catch observer exceptions to prevent observability from breaking execution
        }
    }
}

/// <summary>
/// Internal state tracking during rule execution.
/// </summary>
internal class ExecutionState
{
    /// <summary>
    /// Set to true when StopOnFirstMatch option causes early termination.
    /// </summary>
    public bool StoppedByFirstMatchOption { get; set; }
}

internal sealed class EvaluationContext<T>
{
    public EvaluationContext(
        T input,
        IRuleContext ruleContext,
        RuleResult result,
        RuleExecutionNode? parentNode,
        RuleExecutionOptions<T> options,
        ExecutionState executionState,
        string? groupPath,
        ObservabilityRuntime<T> observabilityRuntime
    )
    {
        Input = input;
        RuleContext = ruleContext;
        Result = result;
        ParentNode = parentNode;
        Options = options;
        ExecutionState = executionState;
        GroupPath = groupPath;
        ObservabilityRuntime = observabilityRuntime;
    }

    public T Input { get; }
    public IRuleContext RuleContext { get; }
    public RuleResult Result { get; }
    public RuleExecutionNode? ParentNode { get; }
    public RuleExecutionOptions<T> Options { get; }
    public ExecutionState ExecutionState { get; }
    public string? GroupPath { get; }
    public ObservabilityRuntime<T> ObservabilityRuntime { get; }

    public EvaluationContext<T> WithGroup(RuleExecutionNode? parentNode, string groupPath)
    {
        return new EvaluationContext<T>(
            Input,
            RuleContext,
            Result,
            parentNode,
            Options,
            ExecutionState,
            groupPath,
            ObservabilityRuntime
        );
    }
}

/// <summary>
/// Per-evaluation observability runtime state.
/// Lightweight container for observer, timing, and metric tracking.
/// Only allocates when observability is enabled.
/// </summary>
internal sealed class ObservabilityRuntime<T>
{
    private readonly bool _timingEnabled;
    private Stopwatch? _stopwatch;
    private DateTime? _startTime;
    private int _groupsTraversed;

    public IRuleObserver<T> Observer { get; }
    public bool Enabled { get; }

    /// <summary>
    /// Tracks AI-specific metrics for this execution cycle.
    /// Only allocated when observability is enabled — null otherwise.
    /// </summary>
    public AiMetricsTracker? AiMetrics { get; }

    public ObservabilityRuntime(RuleExecutionOptions<T> options, bool enabled)
    {
        Enabled = enabled;
        Observer = options.Observer ?? new InMemoryRuleObserver<T>();
        _timingEnabled = options.EnableDetailedTiming && enabled;
        _groupsTraversed = 0;

        // Allocate AI tracker only when observability is active
        AiMetrics = enabled ? new AiMetricsTracker() : null;
    }

    public void StartTiming()
    {
        if (_timingEnabled)
        {
            _stopwatch = Stopwatch.StartNew();
            _startTime = DateTime.UtcNow;
        }
    }

    public void CompleteTiming()
    {
        _stopwatch?.Stop();
    }

    public void IncrementGroupsTraversed()
    {
        _groupsTraversed++;
    }

    public void OnRuleEvaluating<TInput>(string ruleName, TInput input, string? groupPath)
    {
        if (!Enabled) return;

        SafeInvokeObserver(() =>
            Observer.OnRuleEvaluating(new RuleEvaluationContext<T>
            {
                RuleName = ruleName,
                Input = (T)(object)input!,
                GroupPath = groupPath,
                StartTime = _timingEnabled ? _startTime : null
            })
        );
    }

    public void OnRuleMatched<TInput>(string ruleName, TInput input, string? groupPath, string? reason)
    {
        if (!Enabled) return;

        SafeInvokeObserver(() =>
            Observer.OnRuleMatched(new RuleMatchContext<T>
            {
                RuleName = ruleName,
                Input = (T)(object)input!,
                GroupPath = groupPath,
                Reason = reason,
                DurationFromEvaluation = _timingEnabled ? TimeSpan.FromMilliseconds(_stopwatch?.ElapsedMilliseconds ?? 0) : null
            })
        );
    }

    public void OnRuleExecuted<TInput>(string ruleName, TInput input, string? groupPath, int actionsCount, bool executed)
    {
        if (!Enabled) return;

        SafeInvokeObserver(() =>
            Observer.OnRuleExecuted(new RuleExecutionContext<T>
            {
                RuleName = ruleName,
                Input = (T)(object)input!,
                GroupPath = groupPath,
                ActionsExecutedCount = actionsCount,
                Executed = executed,
                TotalDuration = _timingEnabled ? _stopwatch?.Elapsed : null
            })
        );
    }

    public RuleExecutionSummary GetSummary(List<RuleExecution> executions)
    {
        var inMemoryObserver = Observer as InMemoryRuleObserver<T>;
        
        // Populate metrics from execution records
        if (inMemoryObserver != null)
        {
            inMemoryObserver.SetTotalRulesEvaluated(executions.Count);
            if (_timingEnabled && _stopwatch != null)
            {
                inMemoryObserver.SetTotalElapsedMilliseconds(_stopwatch.ElapsedMilliseconds);
                inMemoryObserver.SetTimeBoundaries(_startTime, DateTime.UtcNow);
            }
        }

        var matchedCount = executions.Count(e => e.Matched);
        var actionsCount = executions.SelectMany(e => e.Actions).Count(a => a.Executed);
        var stoppedEarly = executions.Any(e => e.StoppedProcessing);

        var summary = new RuleExecutionSummary
        {
            TotalRulesEvaluated = executions.Count,
            RulesMatched = matchedCount,
            ActionsExecuted = actionsCount,
            GroupsTraversed = _groupsTraversed,
            ExecutionStopped = stoppedEarly,
            TotalExecutionTime = _timingEnabled ? _stopwatch?.Elapsed : null,
            Metrics = new RuleExecutionMetrics
            {
                TotalRulesEvaluated = executions.Count,
                RulesMatched = matchedCount,
                ActionsExecuted = actionsCount,
                GroupsTraversed = _groupsTraversed,
                TotalElapsedMilliseconds = _timingEnabled ? _stopwatch?.ElapsedMilliseconds : null,
                StartedAt = _startTime,
                CompletedAt = DateTime.UtcNow,
                AiEvaluations = AiMetrics?.AiEvaluations ?? 0,
                AiFailures = AiMetrics?.AiFailures ?? 0,
                AiSkipped = AiMetrics?.AiSkipped ?? 0,
                AiTotalDuration = AiMetrics?.AiTotalDuration ?? TimeSpan.Zero,
            }
        };

        return summary;
    }

    private static void SafeInvokeObserver(Action observerAction)
    {
        try
        {
            observerAction.Invoke();
        }
        catch
        {
            // Silently catch observer exceptions
        }
    }
}
