using System.Text;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Abstractions.Formatting;

/// <summary>
/// Default text formatter for displaying execution results in flat list format.
/// Shows execution state (Executed, Matched, Skipped) for each rule.
/// </summary>
public class DefaultTextFormatter : IRuleResultFormatter
{
    public string Format(RuleResult result)
    {
        var sb = new StringBuilder();

        foreach (var exec in result.Executions)
        {
            // Determine status marker based on execution state
            var status = exec.Skipped ? "⊘" : (exec.Matched ? "✔" : "✖");

            var output = $"{status} {exec.RuleName}";

            // Add execution state details
            var stateDetails = GetStateDetails(exec);
            if (!string.IsNullOrEmpty(stateDetails))
            {
                output += $" [{stateDetails}]";
            }

            // Add reason
            if (exec.Reason != null)
            {
                output += $" ({exec.Reason})";
            }

            // Add skip reason if skipped
            if (exec.Skipped && !string.IsNullOrEmpty(exec.SkipReason))
            {
                output += $" — Skipped: {exec.SkipReason}";
            }

            // Add stop indicator
            if (exec.StoppedProcessing)
            {
                output += " → STOPPED";
            }

            // Add metadata if available
            if (exec.Metadata.Count > 0)
            {
                var metadataStr = string.Join(", ", exec.Metadata.Select(m => $"{m.Key}={m.Value}"));
                output += $" {{{metadataStr}}}";
            }

            // Add action details if any
            if (exec.Actions.Count > 0)
            {
                var actionDetails = string.Join(", ", exec.Actions.Select(a => a.Executed ? $"✓ {a.Description}" : $"✗ {a.Description}"));
                output += $" → [{actionDetails}]";
            }

            sb.AppendLine(output);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets a summary of the execution state.
    /// </summary>
    private string GetStateDetails(RuleExecution exec)
    {
        var details = new List<string>();

        if (exec.Skipped)
        {
            details.Add($"SKIPPED");
        }
        else if (!exec.Executed)
        {
            details.Add("NOT EXECUTED");
        }
        else
        {
            details.Add(exec.Matched ? "EXECUTED & MATCHED" : "EXECUTED");
        }

        if (exec.StoppedProcessing)
        {
            details.Add("STOPPED");
        }

        return string.Join(" | ", details);
    }
}