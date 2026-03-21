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
        var order = new Order { Amount = 500 };

        var rules = RuleSet.For<Order>("BasicRules")
            .Add(Rule.For<Order>("Standard order")
                .When(o => o.Amount > 0)
                .Then(o => o.IsValid = true)
                .Because("Order amount is valid"))
            .Add(Rule.For<Order>("Free shipping")
                .When(o => o.Amount < 100)
                .Then(o => o.FreeShipping = true)
                .Because("Order qualifies for free shipping"))
            .Add(Rule.For<Order>("Premium shipping")
                .When(o => o.Amount >= 100 && o.Amount < 500)
                .Then(o => o.PremiumShipping = true)
                .Because("Order qualifies for premium shipping"));

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
        Console.WriteLine($"Final State: IsValid={order.IsValid}, FreeShipping={order.FreeShipping}, PremiumShipping={order.PremiumShipping}");
    }
}
