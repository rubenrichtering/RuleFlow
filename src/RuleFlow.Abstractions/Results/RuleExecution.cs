namespace RuleFlow.Abstractions.Results;

public class RuleExecution
{
    public string RuleName { get; set; } = default!;
    public bool Matched { get; set; }
    public string? Reason { get; set; }
    public int Priority { get; set; }
    public bool StoppedProcessing { get; set; }

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object?> Metadata { get; set; } = new();
}