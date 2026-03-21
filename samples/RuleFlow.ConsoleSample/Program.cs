using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

var order = new Order
{
    Amount = 1500
};

// Demonstrate rule priority
var rules = RuleSet.For<Order>("ApprovalRules")
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
var result = engine.Evaluate(order, rules);

Console.WriteLine("\n--- Execution Results ---");
foreach (var execution in result.Executions)
{
    Console.WriteLine($"Rule: {execution.RuleName}");
    Console.WriteLine($"  Priority: {execution.Priority}");
    Console.WriteLine($"  Matched: {execution.Matched}");
    Console.WriteLine($"  Reason: {execution.Reason}");
    Console.WriteLine();
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