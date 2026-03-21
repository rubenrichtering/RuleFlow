using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates asynchronous rule execution.
/// </summary>
public class AsyncScenario : IScenario
{
    public string Name => "Async Rules";
    public string Description => "Shows asynchronous condition and action execution";

    public async Task Run()
    {
        var order = new Order { Amount = 1500, Country = "US" };

        var rules = RuleSet.For<Order>("AsyncRules")
            .Add(Rule.For<Order>("Credit check")
                .WhenAsync(async o =>
                {
                    Console.WriteLine("  ⏳ Checking credit score...");
                    await Task.Delay(300); // Simulate async database call
                    var approved = o.Amount < 10000;
                    Console.WriteLine($"  ✓ Credit check: {(approved ? "APPROVED" : "DECLINED")}");
                    return approved;
                })
                .Then(o => Console.WriteLine("  → Credit approved"))
                .Because("Credit score meets requirements"))
            .Add(Rule.For<Order>("Inventory check")
                .WhenAsync(async o =>
                {
                    Console.WriteLine("  ⏳ Checking inventory...");
                    await Task.Delay(200); // Simulate async API call
                    var inStock = true;
                    Console.WriteLine($"  ✓ Inventory check: {(inStock ? "IN STOCK" : "OUT OF STOCK")}");
                    return inStock;
                })
                .Then(o => Console.WriteLine("  → Inventory reserved"))
                .Because("Product is available"))
            .Add(Rule.For<Order>("High amount processing")
                .When(o => o.Amount > 1000)
                .ThenAsync(async o =>
                {
                    Console.WriteLine("  ⏳ Processing high-value order...");
                    await Task.Delay(500); // Simulate async processing
                    o.RequiresApproval = true;
                    Console.WriteLine("  ✓ High-value order flagged");
                })
                .Because("Amount exceeds standard limit"));

        var engine = new RuleEngine();
        
        Console.WriteLine($"Input: Order Amount = ${order.Amount}, Country = {order.Country}");
        Console.WriteLine();
        Console.WriteLine("Executing async rules (simulated network calls):");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await engine.EvaluateAsync(order, rules);
        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("Results:");
        Console.WriteLine(result.Explain());
        Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Final State: RequiresApproval={order.RequiresApproval}");
    }
}
