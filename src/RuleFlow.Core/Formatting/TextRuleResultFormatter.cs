using System.Text;
using RuleFlow.Abstractions.Formatting;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

public class TextRuleResultFormatter : IRuleResultFormatter
{
    public string Format(RuleResult result)
    {
        var sb = new StringBuilder();

        foreach (var exec in result.Executions)
        {
            var status = exec.Matched ? "✔" : "✖";

            var output = $"{status} {exec.RuleName}";

            if (exec.Reason != null)
            {
                output += $" ({exec.Reason})";
            }

            // Add metadata if available
            if (exec.Metadata.Count > 0)
            {
                var metadataStr = string.Join(", ", exec.Metadata.Select(m => $"{m.Key}={m.Value}"));
                output += $" [{metadataStr}]";
            }

            sb.AppendLine(output);
        }

        return sb.ToString();
    }
}