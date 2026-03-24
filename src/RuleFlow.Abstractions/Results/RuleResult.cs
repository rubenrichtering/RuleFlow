using System.Text;
using RuleFlow.Abstractions.Formatting;
using RuleFlow.Abstractions.Observability;

namespace RuleFlow.Abstractions.Results;

public class RuleResult
{
    public List<RuleExecution> Executions { get; } = new();

    /// <summary>
    /// Root node of the execution tree.
    /// </summary>
    public RuleExecutionNode? Root { get; set; }

    /// <summary>
    /// Metrics collected during execution (only populated when observability is enabled).
    /// </summary>
    public RuleExecutionMetrics? Metrics { get; set; }

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