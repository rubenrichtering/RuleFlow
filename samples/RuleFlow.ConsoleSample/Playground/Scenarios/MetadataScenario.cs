using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates rule metadata functionality.
/// </summary>
public class MetadataScenario : IScenario
{
    public string Name => "Rule Metadata";
    public string Description => "Shows how to attach and use metadata on rules";

    public async Task Run()
    {
        var order = new Order { Amount = 1500 };

        var rules = RuleSet.For<Order>("MetadataRules")
            .Add(Rule.For<Order>("High amount")
                .When(o => o.Amount > 1000)
                .Then(o => o.RequiresApproval = true)
                .Because("Amount exceeds threshold")
                .WithMetadata("Category", "Finance")
                .WithMetadata("Severity", "High")
                .WithMetadata("AuditRequired", true))
            .Add(Rule.For<Order>("Standard order")
                .When(o => o.Amount >= 100 && o.Amount <= 1000)
                .Then(o => o.IsValid = true)
                .Because("Order is within standard range")
                .WithMetadata("Category", "Standard")
                .WithMetadata("Severity", "Low"))
            .Add(Rule.For<Order>("Bulk discount eligible")
                .When(o => o.Amount >= 500)
                .Then(o => o.PremiumShipping = true)
                .Because("Order qualifies for premium shipping")
                .WithMetadata("Category", "Discount")
                .WithMetadata("Department", "Sales"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"Input: Order Amount = ${order.Amount}");
        Console.WriteLine();
        Console.WriteLine("Rules with Metadata:");
        Console.WriteLine(result.Explain());
        Console.WriteLine();
        Console.WriteLine("Applied Rules Details:");
        foreach (var exec in result.Executions.Where(e => e.Matched))
        {
            Console.WriteLine($"  Rule: {exec.RuleName}");
            if (exec.Metadata.Count > 0)
            {
                Console.WriteLine("    Metadata:");
                foreach (var kvp in exec.Metadata)
                {
                    Console.WriteLine($"      {kvp.Key}: {kvp.Value}");
                }
            }
        }
        Console.WriteLine();
        Console.WriteLine($"Final State: RequiresApproval={order.RequiresApproval}, PremiumShipping={order.PremiumShipping}");
    }
}
