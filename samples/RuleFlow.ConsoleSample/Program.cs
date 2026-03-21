using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

var order = new Order
{
    Amount = 1500
};

// ===== SYNC EXAMPLE =====
Console.WriteLine("===== Synchronous Rules =====\n");

// Demonstrate rule priority
var syncRules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("Low priority rule")
        .WithPriority(1)
        .When(o => true)
        .Then(o => Console.WriteLine("Executing: Low priority rule (Priority 1)"))
        .Because("Low priority rule executed"))
    .Add(Rule.For<Order>("High priority rule")
        .WithPriority(10)
        .When(o => true)
        .Then(o => Console.WriteLine("Executing: High priority rule (Priority 10)"))
        .Because("High priority rule executed"))
    .Add(Rule.For<Order>("Medium priority rule")
        .WithPriority(5)
        .When(o => true)
        .Then(o => Console.WriteLine("Executing: Medium priority rule (Priority 5)"))
        .Because("Medium priority rule executed"))
    .Add(Rule.For<Order>("No priority rule")
        .When(o => true)
        .Then(o => Console.WriteLine("Executing: No priority rule (Default 0)"))
        .Because("No priority rule executed"));

var engine = new RuleEngine();
var result = engine.Evaluate(order, syncRules);

Console.WriteLine("\n--- Synchronous Execution Results ---");
foreach (var execution in result.Executions)
{
    Console.WriteLine($"Rule: {execution.RuleName}");
    Console.WriteLine($"  Priority: {execution.Priority}");
    Console.WriteLine($"  Matched: {execution.Matched}");
    Console.WriteLine($"  Reason: {execution.Reason}");
    Console.WriteLine();
}

// ===== ASYNC EXAMPLE =====
Console.WriteLine("\n\n===== Asynchronous Rules =====\n");

await DemoAsyncRules(order);

Console.WriteLine("\nSample completed.");

// Async demonstration function
async Task DemoAsyncRules(Order order)
{
    var asyncRules = RuleSet.For<Order>("AsyncApprovalRules")
        .Add(Rule.For<Order>("Check credit score")
            .WhenAsync(async o =>
            {
                Console.WriteLine("  [Async] Checking credit score...");
                await Task.Delay(500); // Simulate async database call
                return true;
            })
            .Then(o => Console.WriteLine("  [Action] Credit score approved"))
            .Because("Customer credit score is acceptable"))
        .Add(Rule.For<Order>("Verify inventory")
            .WhenAsync(async o =>
            {
                Console.WriteLine("  [Async] Verifying inventory...");
                await Task.Delay(300); // Simulate async API call
                return o.Amount < 5000;
            })
            .Then(o => Console.WriteLine("  [Action] Inventory verified"))
            .Because("Product is in stock"))
        .Add(Rule.For<Order>("High amount check")
            .When(o => o.Amount > 1000)
            .ThenAsync(async o =>
            {
                Console.WriteLine("  [Action] Processing high-amount order...");
                await Task.Delay(200); // Simulate async operation
                o.RequiresApproval = true;
                Console.WriteLine("  [Action] Approval flag set");
            })
            .Because("Amount exceeds standard limit"));

    var asyncResult = await engine.EvaluateAsync(order, asyncRules);

    Console.WriteLine("\n--- Asynchronous Execution Results ---");
    foreach (var execution in asyncResult.Executions)
    {
        Console.WriteLine($"Rule: {execution.RuleName}");
        Console.WriteLine($"  Matched: {execution.Matched}");
        Console.WriteLine($"  Reason: {execution.Reason}");
        Console.WriteLine();
    }

    Console.WriteLine($"Order requires approval: {order.RequiresApproval}");
}

public class Order
{
    public decimal Amount { get; set; }
    public bool RequiresApproval { get; set; }

    public Customer Customer { get; set; } = new();
}

public class Customer
{
    public bool IsPreferred { get; set; }
}