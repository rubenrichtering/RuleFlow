using RuleFlow.Core.Context;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates context-aware rule execution with runtime data and services.
/// </summary>
public class ContextScenario : IScenario
{
    public string Name => "Context & Data";
    public string Description => "Shows runtime context with datetime and custom data";

    public async Task Run()
    {
        var order = new Order { Amount = 750, Country = "US" };

        // Create a context with business hours during the working day
        var businessHoursContext = new RuleContext(
            now: new DateTime(2024, 1, 15, 14, 30, 0) // 2:30 PM UTC
        );

        // Add custom data to the context
        businessHoursContext.Set("discount_code", "SAVE10");
        businessHoursContext.Set("customer_tier", "silver");
        businessHoursContext.Set("max_orders_per_day", 5);

        var rules = RuleSet.For<Order>("ContextAwareRules")
            .Add(Rule.For<Order>("Business hours validation")
                .When((o, ctx) =>
                {
                    var hour = ctx.Now.Hour;
                    var isBusinessHours = hour >= 9 && hour < 17;
                    Console.WriteLine($"  📅 Time check: {ctx.Now:HH:mm:ss} - Business hours? {isBusinessHours}");
                    return isBusinessHours;
                })
                .Then(o => Console.WriteLine("  → Order placed during business hours"))
                .Because("Processing orders during business hours"))
            
            .Add(Rule.For<Order>("Apply discount code")
                .When((o, ctx) =>
                {
                    var discountCode = ctx.Get<string>("discount_code");
                    return !string.IsNullOrEmpty(discountCode);
                })
                .Then((o, ctx) =>
                {
                    var discountCode = ctx.Get<string>("discount_code");
                    var discount = discountCode == "SAVE10" ? 0.10m : 0.05m;
                    var discountAmount = o.Amount * discount;
                    Console.WriteLine($"  💰 Applied discount code '{discountCode}': ${discountAmount:F2} off");
                    o.Amount -= discountAmount;
                })
                .Because("Discount code is valid"))
            
            .Add(Rule.For<Order>("Tier-based shipping")
                .When((o, ctx) =>
                {
                    var tier = ctx.Get<string>("customer_tier");
                    return tier == "silver" || tier == "gold";
                })
                .Then((o, ctx) =>
                {
                    var tier = ctx.Get<string>("customer_tier");
                    if (tier == "gold")
                    {
                        o.FreeShipping = true;
                        Console.WriteLine("  🚚 Gold member: Free shipping applied");
                    }
                    else
                    {
                        o.StandardShipping = true;
                        Console.WriteLine("  🚚 Silver member: Standard shipping applied");
                    }
                })
                .Because("Customer tier determines shipping benefit"))
            
            .Add(Rule.For<Order>("Check daily order limit")
                .WhenAsync(async (o, ctx) =>
                {
                    var maxOrders = ctx.Get<int>("max_orders_per_day");
                    Console.WriteLine($"  ⏳ Checking daily order limit (max: {maxOrders})...");
                    await Task.Delay(200); // Simulate async check
                    // Simulate that we haven't exceeded the limit
                    Console.WriteLine($"  ✓ Within daily limit");
                    return true;
                })
                .ThenAsync(async (o, ctx) =>
                {
                    Console.WriteLine("  ✓ Order approved within daily limits");
                    await Task.Delay(100); // Simulate async processing
                })
                .Because("Daily order limit not exceeded"))
            
            .Add(Rule.For<Order>("Large order flag")
                .When((o, ctx) => o.Amount > 500)
                .Then((o, ctx) =>
                {
                    Console.WriteLine("  🚩 Large order flagged for review");
                    o.RequiresApproval = true;
                })
                .Because("Order amount exceeds standard threshold"));

        var engine = new RuleEngine();

        Console.WriteLine($"Input: Order Amount = ${order.Amount}, Country = {order.Country}");
        Console.WriteLine($"Context Time: {businessHoursContext.Now:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Context Items: discount_code=SAVE10, customer_tier=silver, max_orders_per_day=5");
        Console.WriteLine();
        Console.WriteLine("Executing context-aware rules:");
        Console.WriteLine();

        var result = await engine.EvaluateAsync(order, rules, businessHoursContext);

        Console.WriteLine();
        Console.WriteLine("Results:");
        Console.WriteLine(result.Explain());
        Console.WriteLine();
        Console.WriteLine("Final Order State:");
        Console.WriteLine($"  Amount: ${order.Amount:F2}");
        Console.WriteLine($"  FreeShipping: {order.FreeShipping}");
        Console.WriteLine($"  StandardShipping: {order.StandardShipping}");
        Console.WriteLine($"  RequiresApproval: {order.RequiresApproval}");
    }
}
