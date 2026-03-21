using System.Text;
using RuleFlow.Abstractions.Formatting;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

/// <summary>
/// Formats RuleResult as a tree structure using the execution tree (Explainability v2).
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
                sb.AppendLine($"{status} {exec.RuleName}" +
                              (exec.Reason != null ? $" ({exec.Reason})" : ""));
            }
        }

        return sb.ToString();
    }

    private void AppendNode(StringBuilder sb, RuleExecutionNode node, string indent)
    {
        // Skip root node name, just process its children
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
            // Format the node
            var marker = GetMarker(node);
            var stopIndicator = node.StoppedProcessing ? " (STOP)" : "";
            var skipIndicator = !node.Executed && node.Type == "Rule" ? " (SKIPPED)" : "";

            sb.Append(indent);
            sb.Append(marker);
            sb.Append(" ");
            sb.Append(node.Name);

            if (!string.IsNullOrEmpty(node.Reason))
            {
                sb.Append($" ({node.Reason})");
            }

            sb.Append(stopIndicator);
            sb.Append(skipIndicator);
            sb.AppendLine();

            // Add children with increased indent
            var childIndent = indent + "  ";
            foreach (var child in node.Children)
            {
                AppendNode(sb, child, childIndent);
            }
        }
    }

    private string GetMarker(RuleExecutionNode node)
    {
        return node.Type switch
        {
            "Rule" => node.Matched == true ? "✔" : "✖",
            "Group" => "📁",
            _ => "○"
        };
    }
}
