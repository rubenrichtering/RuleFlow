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

            sb.AppendLine($"{status} {exec.RuleName}" +
                          (exec.Reason != null ? $" ({exec.Reason})" : ""));
        }

        return sb.ToString();
    }
}