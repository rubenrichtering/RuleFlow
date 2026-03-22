using RuleFlow.Abstractions.Execution;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates RuleExecutionOptions for controlling rule execution behavior.
/// </summary>
public class ExecutionOptionsScenario : IScenario
{
    public string Name => "Execution Options";
    public string Description => "Shows how to use RuleExecutionOptions to control rule execution";

    public async Task Run()
    {
        await DemoStopOnFirstMatch();
        Console.WriteLine("\n" + new string('=', 60) + "\n");
        await DemoMetadataFilter();
        Console.WriteLine("\n" + new string('=', 60) + "\n");
        await DemoIncludeGroups();
        Console.WriteLine("\n" + new string('=', 60) + "\n");
        await DemoExplainabilityToggle();
    }

    private async Task DemoStopOnFirstMatch()
    {
        Console.WriteLine("📌 CASE 1: StopOnFirstMatch Option");
        Console.WriteLine("═════════════════════════════════════════════════════════════");

        var order = new Order { Amount = 2500 };

        var rules = RuleSet.For<Order>("ValidationRules")
            .Add(Rule.For<Order>("Low amount")
                .When(o => o.Amount < 1000)
                .Then(o => Console.WriteLine("  → Low amount rule executed"))
                .Because("Amount < 1000"))
            .Add(Rule.For<Order>("Medium amount")
                .WithPriority(5)
                .When(o => o.Amount >= 1000 && o.Amount < 5000)
                .Then(o => Console.WriteLine("  → Medium amount rule executed"))
                .Because("Amount >= 1000 && < 5000"))
            .Add(Rule.For<Order>("High amount")
                .WithPriority(10)
                .When(o => o.Amount >= 5000)
                .Then(o => Console.WriteLine("  → High amount rule executed"))
                .Because("Amount >= 5000"));

        var engine = new RuleEngine();

        Console.WriteLine($"Input: Order Amount = ${order.Amount}\n");

        // Without StopOnFirstMatch
        Console.WriteLine("Without StopOnFirstMatch:");
        var result1 = engine.Evaluate(order, rules);
        Console.WriteLine($"Rules executed: {result1.AppliedRules.Count()}\n");

        // With StopOnFirstMatch
        Console.WriteLine("With StopOnFirstMatch = true:");
        order.Amount = 2500; // Reset
        var options = new RuleExecutionOptions<Order> { StopOnFirstMatch = true };
        var result2 = engine.Evaluate(order, rules, options);
        Console.WriteLine($"Rules executed: {result2.AppliedRules.Count()}");
        Console.WriteLine("✓ Execution stopped after first match!");
    }

    private async Task DemoMetadataFilter()
    {
        Console.WriteLine("📌 CASE 2: Metadata Filter");
        Console.WriteLine("═════════════════════════════════════════════════════════════");

        var order = new Order { Amount = 3000 };

        var rules = RuleSet.For<Order>("CategoryFilterRules")
            .Add(Rule.For<Order>("Finance Rule 1")
                .WithMetadata("Category", "Finance")
                .When(o => o.Amount > 1000)
                .Then(o => Console.WriteLine("  → Finance rule 1 executed"))
                .Because("Finance category"))
            .Add(Rule.For<Order>("Shipping Rule 1")
                .WithMetadata("Category", "Shipping")
                .When(o => o.Amount > 500)
                .Then(o => Console.WriteLine("  → Shipping rule 1 executed"))
                .Because("Shipping category"))
            .Add(Rule.For<Order>("Finance Rule 2")
                .WithMetadata("Category", "Finance")
                .When(o => o.Amount > 2000)
                .Then(o => Console.WriteLine("  → Finance rule 2 executed"))
                .Because("Finance category"))
            .Add(Rule.For<Order>("Shipping Rule 2")
                .WithMetadata("Category", "Shipping")
                .When(o => o.Amount > 2000)
                .Then(o => Console.WriteLine("  → Shipping rule 2 executed"))
                .Because("Shipping category"));

        var engine = new RuleEngine();

        Console.WriteLine($"Input: Order Amount = ${order.Amount}\n");

        // Execute all rules
        Console.WriteLine("All rules:");
        var resultAll = engine.Evaluate(order, rules);
        Console.WriteLine($"Rules matched: {resultAll.AppliedRules.Count()}\n");

        // Filter by Finance category only
        Console.WriteLine("Only Finance category:");
        var optionsFinance = new RuleExecutionOptions<Order>
        {
            MetadataFilter = rule =>
                rule.Metadata.TryGetValue("Category", out var category) &&
                category?.Equals("Finance") == true
        };
        var resultFinance = engine.Evaluate(order, rules, optionsFinance);
        Console.WriteLine($"Rules matched: {resultFinance.AppliedRules.Count()}");
        foreach (var rule in resultFinance.AppliedRules)
        {
            Console.WriteLine($"  - {rule}");
        }

        // Filter by Shipping category only
        Console.WriteLine("\nOnly Shipping category:");
        var optionsShipping = new RuleExecutionOptions<Order>
        {
            MetadataFilter = rule =>
                rule.Metadata.TryGetValue("Category", out var category) &&
                category?.Equals("Shipping") == true
        };
        var resultShipping = engine.Evaluate(order, rules, optionsShipping);
        Console.WriteLine($"Rules matched: {resultShipping.AppliedRules.Count()}");
        foreach (var rule in resultShipping.AppliedRules)
        {
            Console.WriteLine($"  - {rule}");
        }
    }

    private async Task DemoIncludeGroups()
    {
        Console.WriteLine("📌 CASE 3: Include Groups");
        Console.WriteLine("═════════════════════════════════════════════════════════════");

        var order = new Order { Amount = 3000 };

        var rules = RuleSet.For<Order>("RootRules")
            .Add(Rule.For<Order>("Root rule 1")
                .When(o => o.Amount > 0)
                .Then(o => Console.WriteLine("  → Root rule executed"))
                .Because("Root level"))
            .AddGroup("ApprovalGroup", g => g
                .Add(Rule.For<Order>("Requires approval check")
                    .When(o => o.Amount > 2000)
                    .Then(o => Console.WriteLine("  → Approval rule executed"))
                    .Because("Amount requires approval")))
            .AddGroup("ShippingGroup", g => g
                .Add(Rule.For<Order>("Shipping fee calculation")
                    .When(o => o.Amount > 1000)
                    .Then(o => Console.WriteLine("  → Shipping rule executed"))
                    .Because("Shipping fee applies")));

        var engine = new RuleEngine();

        Console.WriteLine($"Input: Order Amount = ${order.Amount}\n");

        // Execute all groups
        Console.WriteLine("All groups:");
        var resultAll = engine.Evaluate(order, rules);
        Console.WriteLine($"Rules matched: {resultAll.AppliedRules.Count()}\n");

        // Execute only ApprovalGroup
        Console.WriteLine("Only ApprovalGroup:");
        var optionsApproval = new RuleExecutionOptions<Order>
        {
            IncludeGroups = new[] { "ApprovalGroup" }
        };
        var resultApproval = engine.Evaluate(order, rules, optionsApproval);
        Console.WriteLine($"Rules matched: {resultApproval.AppliedRules.Count()}");
        foreach (var rule in resultApproval.AppliedRules)
        {
            Console.WriteLine($"  - {rule}");
        }

        // Execute only ShippingGroup
        Console.WriteLine("\nOnly ShippingGroup:");
        var optionsShipping2 = new RuleExecutionOptions<Order>
        {
            IncludeGroups = new[] { "ShippingGroup" }
        };
        var resultShipping = engine.Evaluate(order, rules, optionsShipping2);
        Console.WriteLine($"Rules matched: {resultShipping.AppliedRules.Count()}");
        foreach (var rule in resultShipping.AppliedRules)
        {
            Console.WriteLine($"  - {rule}");
        }
    }

    private async Task DemoExplainabilityToggle()
    {
        Console.WriteLine("📌 CASE 4: Explainability Toggle");
        Console.WriteLine("═════════════════════════════════════════════════════════════");

        var order = new Order { Amount = 3000 };

        var rules = RuleSet.For<Order>("ExplainabilityRules")
            .Add(Rule.For<Order>("Rule 1")
                .When(o => o.Amount > 1000)
                .Then(o => Console.WriteLine("  → Executed"))
                .Because("Amount > 1000"))
            .Add(Rule.For<Order>("Rule 2")
                .When(o => o.Amount > 2000)
                .Then(o => Console.WriteLine("  → Executed"))
                .Because("Amount > 2000"));

        var engine = new RuleEngine();

        Console.WriteLine($"Input: Order Amount = ${order.Amount}\n");

        // With explainability enabled (default)
        Console.WriteLine("With EnableExplainability = true (default):");
        var resultWithExplain = engine.Evaluate(order, rules);
        Console.WriteLine($"Root node exists: {resultWithExplain.Root != null}");
        Console.WriteLine($"Has execution tree: {resultWithExplain.Root?.Children.Count > 0}");
        Console.WriteLine(resultWithExplain.Explain());

        // With explainability disabled
        Console.WriteLine("\nWith EnableExplainability = false:");
        order.Amount = 3000; // Reset
        var optionsNoExplain = new RuleExecutionOptions<Order> { EnableExplainability = false };
        var resultNoExplain = engine.Evaluate(order, rules, optionsNoExplain);
        Console.WriteLine($"Root node exists: {resultNoExplain.Root != null}");
        Console.WriteLine($"Executions still recorded: {resultNoExplain.Executions.Count}");
        Console.WriteLine($"Applied rules: {string.Join(", ", resultNoExplain.AppliedRules)}");
        Console.WriteLine("✓ Rules executed and recorded, but tree not built (faster)");
    }
}
