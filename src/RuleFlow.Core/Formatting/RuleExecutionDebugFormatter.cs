using System.Globalization;
using System.Text;
using RuleFlow.Abstractions.Debug;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Internal formatter for producing human-first debug output from a
/// <see cref="RuleExecutionDebugView"/> (or directly from a <see cref="RuleResult"/>).
/// Renders execution trees with hierarchical structure, condition details, and metrics.
/// AI conditions are rendered with full audit information and are visually distinct.
/// </summary>
internal class RuleExecutionDebugFormatter
{
    /// <summary>
    /// Formats a <see cref="RuleResult"/> into a debug string.
    /// Delegates to the mapper then to <see cref="Format(RuleExecutionDebugView)"/>.
    /// </summary>
    public string Format(RuleResult? result)
    {
        if (result is null) return string.Empty;
        var view = RuleExecutionDebugMapper.Map(result);
        return Format(view);
    }

    /// <summary>
    /// Formats a <see cref="RuleExecutionDebugView"/> into a human-readable debug string.
    /// </summary>
    public string Format(RuleExecutionDebugView view)
    {
        var sb = new StringBuilder();

        // 1. Header
        AppendLine(sb, 0, $"RuleSet: {view.RuleSetName}");
        sb.AppendLine();

        // 2. Execution tree (groups and rules)
        foreach (var group in view.Groups)
            RenderGroup(sb, group, 0);

        foreach (var rule in view.Rules)
            RenderRule(sb, rule, 0);

        sb.AppendLine();

        // 3. Metrics summary (only when observability was enabled)
        if (view.Metrics is not null)
            RenderMetrics(sb, view.Metrics);

        return sb.ToString();
    }

    // ── Rendering helpers ────────────────────────────────────────────────────

    private void RenderGroup(StringBuilder sb, DebugGroup group, int indentLevel)
    {
        AppendLine(sb, indentLevel, $"📁 {group.Name}");

        foreach (var child in group.Groups)
            RenderGroup(sb, child, indentLevel + 1);

        foreach (var rule in group.Rules)
            RenderRule(sb, rule, indentLevel + 1);
    }

    private void RenderRule(StringBuilder sb, DebugRule rule, int indentLevel)
    {
        // Determine if the top-level condition (if any) is an AI node.
        if (rule.Condition is DebugAiConditionLeaf aiCondition)
        {
            RenderAiConditionRule(sb, rule, aiCondition, indentLevel);
            return;
        }

        var marker = rule.Skipped ? "❌" : rule.Matched ? "✅" : "❌";
        AppendLine(sb, indentLevel, $"{marker} {rule.Name}");

        if (rule.Skipped && !string.IsNullOrEmpty(rule.SkipReason))
        {
            AppendLine(sb, indentLevel + 1, $"Skipped: {rule.SkipReason}");
            return; // No further details for skipped rules
        }

        if (!string.IsNullOrEmpty(rule.Reason))
            AppendLine(sb, indentLevel + 1, $"Reason: {rule.Reason}");

        foreach (var action in rule.Actions)
        {
            var actionMarker = action.Executed ? "→" : "⊘";
            var skipInfo = action.Skipped && !string.IsNullOrEmpty(action.SkipReason)
                ? $" [{action.SkipReason}]"
                : string.Empty;
            AppendLine(sb, indentLevel + 1, $"{actionMarker} {action.Description}{skipInfo}");
        }

        if (rule.StoppedProcessing)
            AppendLine(sb, indentLevel + 1, "🛑 Processing stopped");

        if (rule.Condition is not null)
            RenderConditionNode(sb, rule.Condition, indentLevel + 1);
    }

    private void RenderAiConditionRule(
        StringBuilder sb, DebugRule rule, DebugAiConditionLeaf ai, int indentLevel)
    {
        string prefix;
        if (!ai.AiEvaluated)
            prefix = "[AI ⏭ SKIPPED]";
        else if (ai.AiFailed)
            prefix = "[AI ⚠ FAILED]";
        else
            prefix = ai.Result ? "[AI ✅]" : "[AI ❌]";

        AppendLine(sb, indentLevel, $"{prefix} {rule.Name}");

        if (!string.IsNullOrWhiteSpace(ai.AiPrompt))
            AppendLine(sb, indentLevel + 1, $"Prompt: {ai.AiPrompt}");

        if (ai.AiFailed)
        {
            AppendLine(sb, indentLevel + 1, "⚠ AI evaluation failed — fallback applied");
        }
        else if (ai.AiEvaluated)
        {
            if (!string.IsNullOrWhiteSpace(ai.AiReason))
                AppendLine(sb, indentLevel + 1, $"Reason: {ai.AiReason}");

            if (ai.AiConfidence.HasValue)
                AppendLine(sb, indentLevel + 1, $"Confidence: {ai.AiConfidence.Value.ToString("0.##", CultureInfo.InvariantCulture)}");

            AppendLine(sb, indentLevel + 1, "⚠ AI-generated — verify manually");
        }

        if (!string.IsNullOrEmpty(rule.Reason))
            AppendLine(sb, indentLevel + 1, $"Reason: {rule.Reason}");

        foreach (var action in rule.Actions)
        {
            var actionMarker = action.Executed ? "→" : "⊘";
            var skipInfo = action.Skipped && !string.IsNullOrEmpty(action.SkipReason)
                ? $" [{action.SkipReason}]"
                : string.Empty;
            AppendLine(sb, indentLevel + 1, $"{actionMarker} {action.Description}{skipInfo}");
        }

        if (rule.StoppedProcessing)
            AppendLine(sb, indentLevel + 1, "🛑 Processing stopped");
    }

    private void RenderConditionNode(StringBuilder sb, DebugConditionNode node, int indentLevel)
    {
        if (node is DebugAiConditionLeaf ai)
        {
            RenderInlineAiCondition(sb, ai, indentLevel);
        }
        else if (node is DebugConditionLeaf leaf)
        {
            var compareInfo = leaf.Expected?.ToString() ?? "null";
            AppendLine(sb, indentLevel, $"{leaf.Field} {leaf.Operator} {compareInfo}");
        }
        else if (node is DebugConditionGroup group)
        {
            AppendLine(sb, indentLevel, $"{group.Operator}:");
            foreach (var child in group.Children)
                RenderConditionNode(sb, child, indentLevel + 1);
        }
    }

    private void RenderInlineAiCondition(StringBuilder sb, DebugAiConditionLeaf ai, int indentLevel)
    {
        string prefix;
        if (!ai.AiEvaluated)
            prefix = "[AI ⏭ SKIPPED]";
        else if (ai.AiFailed)
            prefix = "[AI ⚠ FAILED]";
        else
            prefix = ai.Result ? "[AI ✅]" : "[AI ❌]";

        AppendLine(sb, indentLevel, prefix);

        if (!string.IsNullOrWhiteSpace(ai.AiPrompt))
            AppendLine(sb, indentLevel + 1, $"Prompt: {ai.AiPrompt}");

        if (ai.AiFailed)
        {
            AppendLine(sb, indentLevel + 1, "⚠ AI evaluation failed — fallback applied");
        }
        else if (ai.AiEvaluated)
        {
            if (!string.IsNullOrWhiteSpace(ai.AiReason))
                AppendLine(sb, indentLevel + 1, $"Reason: {ai.AiReason}");

            if (ai.AiConfidence.HasValue)
                AppendLine(sb, indentLevel + 1, $"Confidence: {ai.AiConfidence.Value.ToString("0.##", CultureInfo.InvariantCulture)}");

            AppendLine(sb, indentLevel + 1, "⚠ AI-generated — verify manually");
        }
    }

    private void RenderMetrics(StringBuilder sb, DebugMetrics metrics)
    {
        sb.AppendLine("─────────────────────────────");
        AppendLine(sb, 0, "Execution Summary:");
        AppendLine(sb, 1, $"Rules evaluated: {metrics.RulesEvaluated}");
        AppendLine(sb, 1, $"Rules matched: {metrics.RulesMatched}");
        AppendLine(sb, 1, $"Actions executed: {metrics.ActionsExecuted}");

        if (metrics.TotalExecutionTimeMs > 0)
            AppendLine(sb, 1, $"Elapsed: {metrics.TotalExecutionTimeMs}ms");
    }

    private static void AppendLine(StringBuilder sb, int indentLevel, string text)
    {
        var indent = new string(' ', indentLevel * 2);
        sb.AppendLine($"{indent}{text}");
    }
}
