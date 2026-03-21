using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates hierarchical rule groups.
/// </summary>
public class GroupScenario : IScenario
{
    public string Name => "Rule Groups";
    public string Description => "Shows hierarchical organization of rules";

    public async Task Run()
    {
        var order = new Order { Amount = 1500, Country = "US" };

        var mainRuleSet = RuleSet.For<Order>("OrderProcessing")
            .AddGroup("Validation", g => g
                .Add(Rule.For<Order>("Amount check")
                    .When(o => o.Amount > 0)
                    .Then(o => o.IsValid = true)
                    .Because("Order amount is positive")))
            .AddGroup("Shipping Eligibility", g => g
                .Add(Rule.For<Order>("Standard shipping")
                    .When(o => o.Amount > 100)
                    .Then(o => o.StandardShipping = true)
                    .Because("Amount qualifies for shipping"))
                .Add(Rule.For<Order>("Domestic delivery")
                    .When(o => o.Country == "US")
                    .Then(o => Console.WriteLine("  → Applying domestic shipping rate"))
                    .Because("Shipping within US")))
            .AddGroup("Approval Process", g => g
                .Add(Rule.For<Order>("Moderate amount")
                    .WithPriority(10)
                    .When(o => o.Amount >= 1000 && o.Amount < 5000)
                    .Then(o =>
                    {
                        o.RequiresApproval = true;
                        Console.WriteLine("  → Flagged for manager review");
                    })
                    .Because("Amount requires manager approval")));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, mainRuleSet);

        Console.WriteLine($"Input: Order Amount = ${order.Amount}, Country = {order.Country}");
        Console.WriteLine();
        Console.WriteLine("Grouped Rules Applied:");
        var groupedByGroup = result.Executions.GroupBy(e => e.GroupName ?? "Root").OrderBy(g => g.Key);
        foreach (var group in groupedByGroup)
        {
            Console.WriteLine($"  {(group.Key == "Root" ? "Main Rules" : group.Key)}:");
            foreach (var exec in group)
            {
                var status = exec.Matched ? "✔" : "✖";
                Console.WriteLine($"    {status} {exec.RuleName}");
            }
        }
        Console.WriteLine();
        Console.WriteLine("Hierarchical Tree:");
        Console.WriteLine(result.Explain());
        Console.WriteLine($"Final State: IsValid={order.IsValid}, RequiresApproval={order.RequiresApproval}");
    }
}
