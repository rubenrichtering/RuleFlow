using System.Text;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Abstractions.Formatting;

public class DefaultTextFormatter : IRuleResultFormatter
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