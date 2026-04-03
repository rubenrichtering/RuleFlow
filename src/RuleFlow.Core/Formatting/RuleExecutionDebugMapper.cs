using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Observability;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Pure transformation layer: maps <see cref="RuleResult"/> to <see cref="RuleExecutionDebugView"/>.
/// Contains no business logic – only structural mapping.
/// </summary>
internal static class RuleExecutionDebugMapper
{
    /// <summary>
    /// Maps a <see cref="RuleResult"/> to a <see cref="RuleExecutionDebugView"/>.
    /// Uses the execution tree when explainability is enabled, or falls back to flat executions.
    /// </summary>
    public static RuleExecutionDebugView Map(RuleResult result)
    {
        var view = new RuleExecutionDebugView
        {
            RuleSetName = result.Root?.Name ?? "(unnamed)",
            Metrics = MapMetrics(result.Metrics)
        };

        if (result.Root != null)
        {
            PopulateFromChildren(view.Groups, view.Rules, result.Root.Children, view.RuleSetName);
        }
        else
        {
            PopulateFromFlatExecutions(view.Groups, view.Rules, result.Executions);
        }

        return view;
    }

    // ── Tree-based path ──────────────────────────────────────────────────────

    private static void PopulateFromChildren(
        List<DebugGroup> groups,
        List<DebugRule> rules,
        List<RuleExecutionNode> children,
        string parentPath)
    {
        foreach (var child in children)
        {
            if (child.Type == "Group")
            {
                var fullPath = $"{parentPath}/{child.Name}";
                var group = new DebugGroup
                {
                    Name = child.Name,
                    FullPath = fullPath
                };
                PopulateFromChildren(group.Groups, group.Rules, child.Children, fullPath);
                groups.Add(group);
            }
            else
            {
                rules.Add(MapRuleFromNode(child));
            }
        }
    }

    private static DebugRule MapRuleFromNode(RuleExecutionNode node) => new()
    {
        Name = node.Name,
        Matched = node.Matched ?? false,
        Executed = node.Executed,
        Skipped = node.Skipped,
        SkipReason = node.SkipReason,
        Reason = node.Reason,
        StoppedProcessing = node.StoppedProcessing,
        ActionsExecuted = node.Actions.Count(a => a.Executed),
        Actions = node.Actions.Select(MapAction).ToList(),
        Condition = node.ConditionTree
    };

    // ── Flat-execution fallback (explainability disabled) ────────────────────

    private static void PopulateFromFlatExecutions(
        List<DebugGroup> groups,
        List<DebugRule> rules,
        List<RuleExecution> executions)
    {
        foreach (var grouping in executions.GroupBy(e => e.GroupName))
        {
            if (grouping.Key is null)
            {
                foreach (var exec in grouping)
                    rules.Add(MapRuleFromExecution(exec));
            }
            else
            {
                var debugGroup = new DebugGroup
                {
                    Name = grouping.Key,
                    FullPath = grouping.Key
                };
                foreach (var exec in grouping)
                    debugGroup.Rules.Add(MapRuleFromExecution(exec));
                groups.Add(debugGroup);
            }
        }
    }

    private static DebugRule MapRuleFromExecution(RuleExecution exec) => new()
    {
        Name = exec.RuleName,
        Matched = exec.Matched,
        Executed = exec.Executed,
        Skipped = exec.Skipped,
        SkipReason = exec.SkipReason,
        Reason = exec.Reason,
        StoppedProcessing = exec.StoppedProcessing,
        ActionsExecuted = exec.Actions.Count(a => a.Executed),
        Actions = exec.Actions.Select(MapAction).ToList()
    };

    // ── Shared helpers ───────────────────────────────────────────────────────

    private static DebugAction MapAction(ActionExecution action) => new()
    {
        Description = action.Description,
        Executed = action.Executed,
        Skipped = action.Skipped,
        SkipReason = action.SkipReason
    };

    private static DebugMetrics? MapMetrics(RuleExecutionMetrics? metrics)
    {
        if (metrics is null) return null;

        return new DebugMetrics
        {
            RulesEvaluated = metrics.TotalRulesEvaluated,
            RulesMatched = metrics.RulesMatched,
            ActionsExecuted = metrics.ActionsExecuted,
            TotalExecutionTimeMs = metrics.TotalElapsedMilliseconds ?? 0
        };
    }
}
