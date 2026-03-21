using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates rule priority execution.
/// </summary>
public class PriorityScenario : IScenario
{
    public string Name => "Rule Priority";
    public string Description => "Shows how rules execute in priority order";

    public async Task Run()
    {
        var order = new Order { Amount = 2500 };

        var rules = RuleSet.For<Order>("PriorityRules")
            .Add(Rule.For<Order>("Low priority rule")
                .WithPriority(1)
                .When(o => true)
                .Then(o => Console.WriteLine("  → Executing: Low priority rule"))
                .Because("Low priority rule (1)"))
            .Add(Rule.For<Order>("Highest priority rule")
                .WithPriority(100)
                .When(o => true)
                .Then(o => Console.WriteLine("  → Executing: Highest priority rule"))
                .Because("Highest priority rule (100)"))
            .Add(Rule.For<Order>("Medium priority rule")
                .WithPriority(50)
                .When(o => true)
                .Then(o => Console.WriteLine("  → Executing: Medium priority rule"))
                .Because("Medium priority rule (50)"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"Input: Order Amount = ${order.Amount}");
        Console.WriteLine();
        Console.WriteLine("Execution Order (by priority):");
        foreach (var exec in result.Executions)
        {
            Console.WriteLine($"  [{exec.Priority:D3}] {exec.RuleName}");
        }
        Console.WriteLine();
        Console.WriteLine("Tree Visualization:");
        Console.WriteLine(result.Explain());
    }
}
