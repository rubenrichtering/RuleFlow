using System.Text;
using RuleFlow.Abstractions.Formatting;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Formats RuleResult as a tree structure using the execution tree with detailed state information.
/// 
/// Shows:
/// - Rule execution states (Executed, Matched, Skipped, StoppedProcessing)
/// - Skip reasons
/// - Action-level execution details
/// - Hierarchical structure for groups and nested rules
/// </summary>
public class TextTreeFormatter : IRuleResultFormatter
{
    public string Format(RuleResult result)
    {
        var sb = new StringBuilder();

        if (result.Root != null)
        {
            // Format as tree
            AppendNode(sb, result.Root, "");
        }
        else
        {
            // Fallback to flat format if no tree is available
            foreach (var exec in result.Executions)
            {
                var status = exec.Matched ? "✔" : "✖";
                var skipIndicator = exec.Skipped ? $" [{exec.SkipReason}]" : "";
                sb.AppendLine($"{status} {exec.RuleName}{skipIndicator}" +
                              (exec.Reason != null ? $" ({exec.Reason})" : ""));
            }
        }

        return sb.ToString();
    }

    private void AppendNode(StringBuilder sb, RuleExecutionNode node, string indent)
    {
        // Skip root node formatting, just process its children
        if (node.Type == "Group" && indent == "")
        {
            // Process children of root
            foreach (var child in node.Children)
            {
                AppendNode(sb, child, indent);
            }
        }
        else
        {
            // Format the node with state information
            var marker = GetMarker(node);
            var stateInfo = GetStateInfo(node);
            var reasonInfo = !string.IsNullOrEmpty(node.Reason) ? $" ({node.Reason})" : "";

            sb.Append(indent);
            sb.Append(marker);
            sb.Append(" ");
            sb.Append(node.Name);
            sb.Append(stateInfo);
            sb.Append(reasonInfo);
            sb.AppendLine();

            // Add action details if this is a rule with actions
            if (node.Type == "Rule" && node.Actions.Count > 0)
            {
                var actionIndent = indent + "  ";
                foreach (var action in node.Actions)
                {
                    AppendActionExecution(sb, action, actionIndent);
                }
            }

            // Add children with increased indent
            var childIndent = indent + "  ";
            foreach (var child in node.Children)
            {
                AppendNode(sb, child, childIndent);
            }
        }
    }

    /// <summary>
    /// Appends action execution details to the output.
    /// </summary>
    private void AppendActionExecution(StringBuilder sb, ActionExecution action, string indent)
    {
        var marker = action.Executed ? "→" : "⊘";
        var skipInfo = action.Skipped ? $" [{action.SkipReason}]" : "";
        
        sb.Append(indent);
        sb.Append(marker);
        sb.Append(" ");
        sb.Append(action.Description);
        sb.Append(skipInfo);
        sb.AppendLine();
    }

    /// <summary>
    /// Gets a visual marker for the node type and execution state.
    /// </summary>
    private string GetMarker(RuleExecutionNode node)
    {
        return node.Type switch
        {
            "Rule" => node.Skipped ? "⊘" : node.Matched == true ? "✔" : "✖",
            "Group" => "📁",
            _ => "○"
        };
    }

    /// <summary>
    /// Gets state information string showing execution state details.
    /// </summary>
    private string GetStateInfo(RuleExecutionNode node)
    {
        var parts = new List<string>();

        if (node.Type == "Rule")
        {
            if (node.Skipped)
            {
                parts.Add($"SKIPPED [{node.SkipReason}]");
            }
            else if (!node.Executed)
            {
                parts.Add("NOT EXECUTED");
            }
            else if (node.Matched.HasValue)
            {
                parts.Add(node.Matched.Value ? "MATCHED" : "NOT MATCHED");
            }

            if (node.StoppedProcessing)
            {
                parts.Add("STOP");
            }
        }

        return parts.Count > 0 ? $" [{string.Join(" | ", parts)}]" : "";
    }
}
