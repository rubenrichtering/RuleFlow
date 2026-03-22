using RuleFlow.Abstractions;
using RuleFlow.Abstractions.Execution;
using RuleFlow.Abstractions.Formatting;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates the improved explainability model with clear execution states.
/// 
/// Shows:
/// - Matched vs Skipped vs Stopped states
/// - Action-level execution tracking (Then / ThenIf)
/// - MetadataFilter effects
/// - StopProcessing behavior
/// - Hierarchical execution tree
/// - Multiple formatter outputs
/// </summary>
public class ExplainabilityRefactorScenario : IScenario
{
    public string Name => "Explainability Refactor";
    public string Description => "Demonstrates improved explainability with clear execution states";

    public async Task Run()
    {
        Console.WriteLine("📋 EXPLAINABILITY: Clear Execution States");
        Console.WriteLine("═════════════════════════════════════════════════════════════\n");

        await DemoExecutedVsMatched();
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        await DemoSkippedRules();
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        await DemoActionExecution();
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        await DemoStoppedProcessing();
    }

    private async Task DemoExecutedVsMatched()
    {
        Console.WriteLine("CASE 1: Distinguishing Executed vs Matched");
        Console.WriteLine("─────────────────────────────────────────\n");

        var order = new Order { Amount = 500, Country = "US", IsValid = true };

        var rules = RuleSet.For<Order>("ValidationRules")
            .Add(Rule.For<Order>("Check High Amount")
                .When(o => o.Amount > 1000)
                .Then(o => o.RequiresApproval = true)
                .Because("Amount exceeds approval threshold"))
            .Add(Rule.For<Order>("Check Valid Order")
                .When(o => o.IsValid)
                .Then(o => o.FreeShipping = true)
                .Because("Valid order"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"📦 Input: Amount=${order.Amount}, IsValid={order.IsValid}\n");
        Console.WriteLine("📊 TREE FORMAT (Improved):");
        Console.WriteLine(result.Explain(new TextTreeFormatter()));

        Console.WriteLine("\n📝 FLAT FORMAT (Improved):");
        Console.WriteLine(result.Explain(new DefaultTextFormatter()));

        Console.WriteLine("💡 Analysis:");
        Console.WriteLine("- 'Check High Amount': EXECUTED but NOT MATCHED (condition failed)");
        Console.WriteLine("- 'Check Valid Order': EXECUTED and MATCHED (condition passed)");
    }

    private async Task DemoSkippedRules()
    {
        Console.WriteLine("CASE 2: Skipped Rules with Skip Reasons");
        Console.WriteLine("─────────────────────────────────────────\n");

        var order = new Order { Amount = 300 };

        var rules = RuleSet.For<Order>("CategorizedRules")
            .Add(Rule.For<Order>("Finance Rule 1")
                .WithMetadata("Category", "Finance")
                .When(o => true)
                .Then(o => Console.WriteLine("  Processing finance rule"))
                .Because("Finance logic"))
            .Add(Rule.For<Order>("Shipping Rule 1")
                .WithMetadata("Category", "Shipping")
                .When(o => true)
                .Then(o => Console.WriteLine("  Processing shipping rule"))
                .Because("Shipping logic"))
            .Add(Rule.For<Order>("Approval Rule")
                .WithMetadata("Category", "Approval")
                .When(o => true)
                .Then(o => Console.WriteLine("  Processing approval rule"))
                .Because("Approval logic"));

        var engine = new RuleEngine();

        // Execute with metadata filter (only Finance rules)
        var options = new RuleExecutionOptions<Order>
        {
            MetadataFilter = r => r.Metadata.ContainsKey("Category") && 
                                 (string?)r.Metadata["Category"] == "Finance"
        };

        var result = engine.Evaluate(order, rules, options);

        Console.WriteLine($"📦 Input: Amount=${order.Amount}\n");
        Console.WriteLine("🔍 Filter: Only rules with Category='Finance' are included\n");

        Console.WriteLine("📊 TREE FORMAT:");
        Console.WriteLine(result.Explain(new TextTreeFormatter()));

        Console.WriteLine("\n📝 FLAT FORMAT:");
        Console.WriteLine(result.Explain(new DefaultTextFormatter()));

        Console.WriteLine("💡 Analysis:");
        Console.WriteLine("- 'Finance Rule 1': EXECUTED (passed filter)");
        Console.WriteLine("- 'Shipping Rule 1': SKIPPED [MetadataFilter]");
        Console.WriteLine("- 'Approval Rule': SKIPPED [MetadataFilter]");
    }

    private async Task DemoActionExecution()
    {
        Console.WriteLine("CASE 3: Action-Level Execution Tracking");
        Console.WriteLine("──────────────────────────────────────\n");

        var order = new Order { Amount = 2500, IsValid = true, Country = "US" };

        var rules = RuleSet.For<Order>("ShippingRules")
            .Add(Rule.For<Order>("Eligible for Standard Shipping")
                .When(o => o.Amount >= 100)
                .Then(o =>
                {
                    o.StandardShipping = true;
                    Console.WriteLine("  → StandardShipping enabled");
                })
                .ThenIf(
                    o => o.Amount >= 500,
                    o =>
                    {
                        o.PremiumShipping = true;
                        Console.WriteLine("  → PremiumShipping enabled (bonus)");
                    })
                .ThenIf(
                    o => o.Amount < 200,
                    o => Console.WriteLine("  → This won't execute (order too large)"))
                .Because("Order qualifies for shipping"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"📦 Input: Amount=${order.Amount}, IsValid={order.IsValid}\n");

        Console.WriteLine("📊 TREE FORMAT (Shows Action Details):");
        Console.WriteLine(result.Explain(new TextTreeFormatter()));

        Console.WriteLine("\n📝 FLAT FORMAT (Shows Action Details):");
        Console.WriteLine(result.Explain(new DefaultTextFormatter()));

        Console.WriteLine("💡 Analysis:");
        var execution = result.Executions.First();
        Console.WriteLine($"- Rule executed: {execution.Executed}");
        Console.WriteLine($"- Rule matched: {execution.Matched}");
        Console.WriteLine($"- Actions executed: {execution.Actions.Count}");
        foreach (var action in execution.Actions)
        {
            Console.WriteLine($"  - {action.Description}: {(action.Executed ? "✓ Executed" : "✗ Skipped")}");
        }
    }

    private async Task DemoStoppedProcessing()
    {
        Console.WriteLine("CASE 4: Stopped Processing & State Consistency");
        Console.WriteLine("─────────────────────────────────────────────\n");

        var order = new Order { Amount = 5000 };

        var rules = RuleSet.For<Order>("ApprovalRules")
            .Add(Rule.For<Order>("Critical Check")
                .WithPriority(20)
                .When(o => o.Amount >= 5000)
                .Then(o =>
                {
                    o.RequiresApproval = true;
                    Console.WriteLine("  → Flagged for critical approval");
                })
                .StopIfMatched()
                .Because("Amount exceeds critical threshold"))
            .Add(Rule.For<Order>("High Amount Check")
                .WithPriority(10)
                .When(o => o.Amount >= 1000)
                .Then(o =>
                {
                    o.RequiresApproval = true;
                    Console.WriteLine("  → Flagged for high amount (won't reach here)");
                })
                .Because("Amount exceeds high threshold"))
            .Add(Rule.For<Order>("Standard Check")
                .When(o => o.Amount >= 500)
                .Then(o =>
                {
                    o.RequiresApproval = true;
                    Console.WriteLine("  → Flagged for standard check (won't reach here)");
                })
                .Because("Amount exceeds standard threshold"));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, rules);

        Console.WriteLine($"📦 Input: Amount=${order.Amount}\n");

        Console.WriteLine("📊 TREE FORMAT (Shows STOP Impact):");
        Console.WriteLine(result.Explain(new TextTreeFormatter()));

        Console.WriteLine("\n📝 FLAT FORMAT (Shows STOP Impact):");
        Console.WriteLine(result.Explain(new DefaultTextFormatter()));

        Console.WriteLine("\n💡 State Consistency Analysis:");
        foreach (var exec in result.Executions)
        {
            var state = exec.Skipped ? "SKIPPED" : (exec.Executed ? "EXECUTED" : "NOT EXECUTED");
            var matched = exec.Executed && exec.Matched ? "MATCHED" : (exec.Executed ? "NOT MATCHED" : "N/A");
            var stopped = exec.StoppedProcessing ? " → STOPPED" : "";

            Console.WriteLine($"- {exec.RuleName}:");
            Console.WriteLine($"  State: {state}, Result: {matched}{stopped}");
            
            // Verify invariants
            if (exec.Executed && exec.Skipped)
            {
                Console.WriteLine("  ⚠️  ERROR: Cannot be both Executed and Skipped!");
            }
            if (exec.Matched && !exec.Executed)
            {
                Console.WriteLine("  ⚠️  ERROR: Cannot be Matched if not Executed!");
            }
        }

        Console.WriteLine("\n✅ All state invariants are maintained:");
        Console.WriteLine("- If Skipped: Executed=false, Matched=false");
        Console.WriteLine("- If Executed & Matched: then actions executed");
        Console.WriteLine("- If StoppedProcessing: remaining rules are skipped");
    }
}
