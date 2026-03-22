using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates basic rule functionality.
/// </summary>
public class BasicRulesScenario : IScenario
{
    public string Name => "Basic Rules";
    public string Description => "Demonstrates fundamental rule execution";

    public async Task Run()
    {
        var order = new Order { Amount = 1500 };

        var rules = RuleSet.For<Order>("ApprovalRules")
            .Add(Rule.For<Order>("High amount")
                .When(o => o.Amount > 1000)
                .Then(o => o.RequiresApproval = true)
                .Because("Amount exceeds threshold"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"Input: Order Amount = ${order.Amount}");
        Console.WriteLine();
        Console.WriteLine("Rules Applied:");
        foreach (var appliedRule in result.AppliedRules)
        {
            Console.WriteLine($"  ✔ {appliedRule}");
        }
        Console.WriteLine();
        Console.WriteLine("Execution Tree:");
        Console.WriteLine(result.Explain());
        Console.WriteLine($"Final State: RequiresApproval={order.RequiresApproval}");
        await Task.CompletedTask;
    }
}
