using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates conditional chains in rules.
/// Shows multiple action steps, conditional execution, and async support.
/// </summary>
public class ConditionalChainsScenario : IScenario
{
    public string Name => "Conditional Chains";
    public string Description => "Demonstrates multiple action steps with conditional execution";

    public async Task Run()
    {
        var order = new Order
        {
            Amount = 1500,
            Country = "US",
            Customer = new Customer { Name = "John Premium", IsPremium = true }
        };

        var rules = RuleSet.For<Order>("OrderProcessing")
            .Add(Rule.For<Order>("High amount processing")
                .When(o => o.Amount > 1000)
                .Then(o =>
                {
                    o.RequiresApproval = true;
                    Console.WriteLine("  → Step 1: Set RequiresApproval = true");
                })
                .ThenIf(o => o.Customer?.IsPremium == true, o =>
                {
                    Console.WriteLine("  → Step 2: (Conditional) Premium customer detected - no extra tax");
                })
                .ThenIf(o => o.Customer?.IsPremium == false, o =>
                {
                    Console.WriteLine("  → Step 2: (Conditional - skipped) Standard customer processing");
                })
                .ThenAsync(async o =>
                {
                    await Task.Delay(100);
                    Console.WriteLine("  → Step 3: (Async) Checking fraud database...");
                })
                .Then(o =>
                {
                    o.LogProcessed = true;
                    Console.WriteLine("  → Step 4: Log to database");
                })
                .Because("Amount exceeds approval threshold"));

        Console.WriteLine($"Input: Order Amount = ${order.Amount}, Customer = {order.Customer?.Name}, Premium = {order.Customer?.IsPremium}");
        Console.WriteLine();
        Console.WriteLine("Execution flow:");

        var engine = new RuleEngine();
        var result = await engine.EvaluateAsync(order, rules);

        Console.WriteLine();
        Console.WriteLine("Applied Rules:");
        foreach (var appliedRule in result.AppliedRules)
        {
            Console.WriteLine($"  ✔ {appliedRule}");
        }

        Console.WriteLine();
        Console.WriteLine("Final State:");
        Console.WriteLine($"  RequiresApproval = {order.RequiresApproval}");
        Console.WriteLine($"  LogProcessed = {order.LogProcessed}");
        Console.WriteLine($"  Customer = {order.Customer?.Name} (Premium: {order.Customer?.IsPremium})");

        Console.WriteLine();
        Console.WriteLine("Execution Tree:");
        Console.WriteLine(result.Explain());
    }
}
