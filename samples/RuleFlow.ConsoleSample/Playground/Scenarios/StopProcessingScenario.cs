using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates StopProcessing behavior.
/// </summary>
public class StopProcessingScenario : IScenario
{
    public string Name => "Stop Processing";
    public string Description => "Shows how StopProcessing halts rule execution";

    public async Task Run()
    {
        var order = new Order { Amount = 5000 };

        var rules = RuleSet.For<Order>("StopProcessingRules")
            .Add(Rule.For<Order>("Normal check")
                .When(o => o.Amount > 0)
                .Then(o => o.IsValid = true)
                .Because("Amount is valid"))
            .Add(Rule.For<Order>("Very high amount")
                .WithPriority(10)
                .When(o => o.Amount > 2000)
                .Then(o =>
                {
                    o.RequiresApproval = true;
                    Console.WriteLine("  → Flagged for high amount approval (STOP)");
                })
                .StopIfMatched()
                .Because("Amount exceeds limit - escalate immediately"))
            .Add(Rule.For<Order>("Additional check")
                .When(o => o.Amount > 1000)
                .Then(o => Console.WriteLine("  → This should NOT execute"))
                .Because("This runs after STOP"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"Input: Order Amount = ${order.Amount}");
        Console.WriteLine();
        Console.WriteLine("Execution:");
        foreach (var exec in result.Executions)
        {
            var status = exec.Matched ? "✔" : "✖";
            var stop = exec.StoppedProcessing ? " [STOPPED PROCESSING HERE]" : "";
            var skipped = exec.Matched == false && !exec.StoppedProcessing ? " [SKIPPED AFTER STOP]" : "";
            Console.WriteLine($"  {status} {exec.RuleName}{stop}{skipped}");
        }
        Console.WriteLine();
        Console.WriteLine("Execution Tree:");
        Console.WriteLine(result.Explain());
        Console.WriteLine($"Final State: RequiresApproval={order.RequiresApproval}");
    }
}
