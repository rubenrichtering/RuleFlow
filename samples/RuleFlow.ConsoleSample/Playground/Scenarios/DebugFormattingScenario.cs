using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates the console debug string formatter (ToDebugString()).
/// Shows how to produce human-readable debug output for rule execution results.
/// </summary>
public class DebugFormattingScenario : IScenario
{
    public string Name => "Debug Formatting";
    public string Description => "Demonstrates the ToDebugString() debug formatter with human-first output";

    public async Task Run()
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("EXAMPLE 1: Simple Rule Set with Debug Output");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        await RunSimpleExample();

        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("EXAMPLE 2: Complex Rules with Groups and Observability");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine();

        await RunComplexExample();
    }

    private async Task RunSimpleExample()
    {
        // Create a simple order
        var order = new Order
        {
            Amount = 2500,
            Country = "US",
            Customer = new Customer { Name = "Alice", IsPremium = true }
        };

        // Define simple rules
        var rules = RuleSet.For<Order>("OrderApproval")
            .Add(Rule.For<Order>("High Amount Check")
                .When(o => o.Amount > 1000)
                .Then(o => o.RequiresApproval = true)
                .Because("Order amount exceeds approval threshold"))
            .Add(Rule.For<Order>("Premium Shipping")
                .When(o => o.Customer?.IsPremium == true)
                .Then(o => o.PremiumShipping = true)
                .Because("Customer is premium member"));

        // Execute and get result
        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        // Display input
        Console.WriteLine($"Order Details:");
        Console.WriteLine($"  Amount: ${order.Amount}");
        Console.WriteLine($"  Customer: {order.Customer?.Name} (Premium: {order.Customer?.IsPremium})");
        Console.WriteLine();

        // Display debug output (simple, human-readable format)
        Console.WriteLine("Debug Output:");
        Console.WriteLine(result.ToDebugString());

        // Display state changes
        Console.WriteLine("State After Execution:");
        Console.WriteLine($"  RequiresApproval: {order.RequiresApproval}");
        Console.WriteLine($"  PremiumShipping: {order.PremiumShipping}");

        await Task.CompletedTask;
    }

    private async Task RunComplexExample()
    {
        // Create order for complex scenario
        var order = new Order
        {
            Amount = 500,
            Country = "CA",
            Customer = new Customer { Name = "Bob", IsPremium = false }
        };

        // Create main ruleset with nested groups
        var rules = RuleSet.For<Order>("CompleteOrderProcessing")
            .AddGroup("ValidationRules", g => g
                .Add(Rule.For<Order>("Basic Validation")
                    .When(o => !string.IsNullOrEmpty(o.Country))
                    .Then(o => o.IsValid = true)
                    .Because("Required fields present")))
            .AddGroup("ShippingRules", g => g
                .Add(Rule.For<Order>("Domestic Shipping")
                    .When(o => o.Country == "US" || o.Country == "CA")
                    .Then(o => o.StandardShipping = true)
                    .Because("Order is domestic"))
                .Add(Rule.For<Order>("Free Shipping Threshold")
                    .When(o => o.Amount > 100)
                    .Then(o => o.FreeShipping = true)
                    .Because("Order amount qualifies for free shipping")));

        // Execute with observability enabled
        var engine = new RuleEngine();
        var options = new RuleFlow.Abstractions.Execution.RuleExecutionOptions<Order>
        {
            EnableObservability = true
        };
        var result = engine.Evaluate(order, rules, options);

        // Display input
        Console.WriteLine($"Order Details:");
        Console.WriteLine($"  Amount: ${order.Amount}");
        Console.WriteLine($"  Country: {order.Country}");
        Console.WriteLine($"  Customer: {order.Customer?.Name}");
        Console.WriteLine();

        // Display debug output with observability metrics
        Console.WriteLine("Debug Output:");
        Console.WriteLine(result.ToDebugString());

        // Display final state
        Console.WriteLine("Final State:");
        Console.WriteLine($"  IsValid: {order.IsValid}");
        Console.WriteLine($"  StandardShipping: {order.StandardShipping}");
        Console.WriteLine($"  FreeShipping: {order.FreeShipping}");

        await Task.CompletedTask;
    }
}
