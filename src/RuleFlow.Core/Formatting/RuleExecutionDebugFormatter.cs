using System.Text;
using RuleFlow.Abstractions.Conditions;
using RuleFlow.Abstractions.Observability;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Internal formatter for producing human-first debug output from RuleResult.
/// Renders execution trees with hierarchical structure, condition details, and metrics.
/// </summary>
internal class RuleExecutionDebugFormatter
{
    /// <summary>
    /// Formats a RuleResult into a debug string with tree structure, conditions, and metrics.
    /// </summary>
    /// <param name="result">The result to format. Can be null.</param>
    /// <returns>Formatted debug string, or empty string if result is null.</returns>
    public string Format(RuleResult? result)
    {
        if (result == null)
            return string.Empty;

        var sb = new StringBuilder();

        // 1. Header: RuleSet name
        var ruleSetName = result.Root?.Name ?? "(unnamed)";
        AppendLine(sb, 0, $"RuleSet: {ruleSetName}");
        sb.AppendLine();

        // 2. Tree structure
        if (result.Root != null)
        {
            RenderExecutionTree(sb, result);
        }
        else
        {
            // Graceful degradation: flat list grouped by GroupName
            RenderFlatExecutions(sb, result);
        }

        sb.AppendLine();

        // 3. Execution summary (only if metrics available)
        if (result.Metrics != null)
        {
            RenderMetrics(sb, result.Metrics);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders the execution tree starting from root node.
    /// </summary>
    private void RenderExecutionTree(StringBuilder sb, RuleResult result)
    {
        if (result.Root?.Children.Count > 0)
        {
            foreach (var child in result.Root.Children)
            {
                RenderNode(sb, child, 0);
            }
        }
    }

    /// <summary>
    /// Recursively renders a single node and its children.
    /// </summary>
    private void RenderNode(StringBuilder sb, RuleExecutionNode node, int indentLevel)
    {
        // Render node header with status marker
        var marker = GetStatusMarker(node);
        AppendLine(sb, indentLevel, $"{marker} {node.Name}");

        // Render detailed rule information
        if (node.Type == "Rule")
        {
            RenderRuleDetail(sb, node, indentLevel + 1);
        }

        // Render child nodes recursively
        foreach (var child in node.Children)
        {
            RenderNode(sb, child, indentLevel + 1);
        }
    }

    /// <summary>
    /// Renders flat list of rules when no tree is available.
    /// Grouped by GroupName for better organization.
    /// </summary>
    private void RenderFlatExecutions(StringBuilder sb, RuleResult result)
    {
        var groups = result.Executions
            .GroupBy(e => e.GroupName ?? "(root)")
            .ToList();

        foreach (var group in groups)
        {
            if (group.Key != "(root)")
            {
                AppendLine(sb, 0, $"Group: {group.Key}");
            }

            foreach (var execution in group)
            {
                var marker = execution.Skipped ? "❌" : execution.Matched ? "✅" : "❌";
                var skipInfo = execution.Skipped ? $" [{execution.SkipReason}]" : "";
                var reasonInfo = !string.IsNullOrEmpty(execution.Reason) ? $" ({execution.Reason})" : "";

                AppendLine(sb, group.Key != "(root)" ? 1 : 0, $"{marker} {execution.RuleName}{reasonInfo}{skipInfo}");

                // Render actions if any
                if (execution.Actions.Count > 0)
                {
                    var actionIndent = group.Key != "(root)" ? 2 : 1;
                    foreach (var action in execution.Actions)
                    {
                        var actionMarker = action.Executed ? "→" : "⊘";
                        var actionSkipInfo = action.Skipped ? $" [{action.SkipReason}]" : "";
                        AppendLine(sb, actionIndent, $"{actionMarker} {action.Description}{actionSkipInfo}");
                    }
                }
            }

            if (group.Key != "(root)" && group != groups.Last())
            {
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Renders detailed information for a rule node (reason, skip reason, actions, conditions).
    /// </summary>
    private void RenderRuleDetail(StringBuilder sb, RuleExecutionNode node, int indentLevel)
    {
        // Skip reason (if rule was skipped)
        if (node.Skipped && !string.IsNullOrEmpty(node.SkipReason))
        {
            AppendLine(sb, indentLevel, $"Skipped: {node.SkipReason}");
            return; // Don't render other details for skipped rules
        }

        // Rule reason (from Because clause)
        if (!string.IsNullOrEmpty(node.Reason))
        {
            AppendLine(sb, indentLevel, $"Reason: {node.Reason}");
        }

        // Action summaries
        if (node.Actions.Count > 0)
        {
            foreach (var action in node.Actions)
            {
                var actionMarker = action.Executed ? "→" : "⊘";
                var actionSkipInfo = action.Skipped ? $" [{action.SkipReason}]" : "";
                AppendLine(sb, indentLevel, $"{actionMarker} {action.Description}{actionSkipInfo}");
            }
        }

        // Stop processing marker
        if (node.StoppedProcessing)
        {
            AppendLine(sb, indentLevel, "🛑 Processing stopped");
        }
    }

    /// <summary>
    /// Renders condition tree if available in rule metadata.
    /// Only renders when ConditionNode is present.
    /// </summary>
    private void RenderConditions(StringBuilder sb, ConditionNode? conditionNode, int indentLevel)
    {
        if (conditionNode == null)
            return;

        AppendLine(sb, indentLevel, "Condition:");
        RenderConditionNode(sb, conditionNode, indentLevel + 1, isLast: true);
    }

    /// <summary>
    /// Recursively renders a condition node (leaf or group).
    /// </summary>
    private void RenderConditionNode(StringBuilder sb, ConditionNode node, int indentLevel, bool isLast)
    {
        if (node is ConditionLeaf leaf)
        {
            // Render leaf: field operator value
            var compareInfo = !string.IsNullOrEmpty(leaf.CompareToField)
                ? $"{leaf.CompareToField}"
                : leaf.Value?.ToString() ?? "null";

            AppendLine(sb, indentLevel, $"{leaf.Field} {leaf.Operator} {compareInfo}");
        }
        else if (node is ConditionGroup group)
        {
            // Render group header
            AppendLine(sb, indentLevel, $"{group.Operator}:");

            // Render children
            for (int i = 0; i < group.Conditions.Count; i++)
            {
                var isLastChild = i == group.Conditions.Count - 1;
                RenderConditionNode(sb, group.Conditions[i], indentLevel + 1, isLastChild);
            }
        }
    }

    /// <summary>
    /// Renders execution metrics (rules evaluated, matched, elapsed time).
    /// </summary>
    private void RenderMetrics(StringBuilder sb, RuleExecutionMetrics metrics)
    {
        sb.AppendLine("─────────────────────────────");
        AppendLine(sb, 0, "Execution Summary:");
        AppendLine(sb, 1, $"Rules evaluated: {metrics.TotalRulesEvaluated}");
        AppendLine(sb, 1, $"Rules matched: {metrics.RulesMatched}");
        AppendLine(sb, 1, $"Actions executed: {metrics.ActionsExecuted}");
        AppendLine(sb, 1, $"Groups traversed: {metrics.GroupsTraversed}");

        if (metrics.TotalElapsedMilliseconds.HasValue)
        {
            AppendLine(sb, 1, $"Elapsed: {metrics.TotalElapsedMilliseconds}ms");
        }
    }

    /// <summary>
    /// Gets the status marker for a node based on its execution state.
    /// </summary>
    private string GetStatusMarker(RuleExecutionNode node)
    {
        if (node.Type == "Group")
            return "📁";

        if (node.Skipped)
            return "❌";

        return node.Matched == true ? "✅" : "❌";
    }

    /// <summary>
    /// Appends a line with proper indentation (2-space indent per level).
    /// </summary>
    private void AppendLine(StringBuilder sb, int indentLevel, string text)
    {
        var indent = new string(' ', indentLevel * 2);
        sb.AppendLine($"{indent}{text}");
    }
}
