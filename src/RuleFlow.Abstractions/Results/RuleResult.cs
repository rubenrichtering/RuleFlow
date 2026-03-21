using System.Text;
using RuleFlow.Abstractions.Formatting;

namespace RuleFlow.Abstractions.Results;

public class RuleResult
{
    public List<RuleExecution> Executions { get; } = new();

    /// <summary>
    /// Root node of the execution tree.
    /// </summary>
    public RuleExecutionNode? Root { get; set; }

    public IEnumerable<string> AppliedRules =>
        Executions.Where(e => e.Matched).Select(e => e.RuleName);

    public override string ToString()
    {
        var sb = new StringBuilder();

        foreach (var exec in Executions)
        {
            var status = exec.Matched ? "✔" : "✖";

            sb.AppendLine($"{status} {exec.RuleName}" +
                          (exec.Reason != null ? $" ({exec.Reason})" : ""));
        }

        return sb.ToString();
    }
    
    public string Explain(IRuleResultFormatter? formatter = null)
    {
        formatter ??= new DefaultTextFormatter();
        return formatter.Format(this);
    }
}