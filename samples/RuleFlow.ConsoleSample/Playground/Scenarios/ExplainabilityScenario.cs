using RuleFlow.Core.Engine;
using RuleFlow.Core.Formatting;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates Explainability v2 output formats.
/// </summary>
public class ExplainabilityScenario : IScenario
{
    public string Name => "Explainability v2";
    public string Description => "Shows structured tree and JSON output formats";

    public async Task Run()
    {
        var order = new Order { Amount = 3500, Country = "CA" };

        var mainRuleSet = RuleSet.For<Order>("OrderEngine")
            .AddGroup("Validation", g => g
                .Add(Rule.For<Order>("Amount validation")
                    .When(o => o.Amount > 0)
                    .Then(o => o.IsValid = true)
                    .Because("Order amount is positive"))
                .Add(Rule.For<Order>("Country validation")
                    .When(o => !string.IsNullOrEmpty(o.Country))
                    .Then(o => Console.WriteLine("  → Country is valid"))
                    .Because("Country code is present")))
            .AddGroup("Approval", g => g
                .Add(Rule.For<Order>("High amount approval")
                    .WithPriority(100)
                    .When(o => o.Amount > 2000)
                    .Then(o =>
                    {
                        o.RequiresApproval = true;
                        Console.WriteLine("  → Requires approval (HIGH PRIORITY)");
                    })
                    .StopIfMatched()
                    .Because("Amount exceeds approval threshold"))
                .Add(Rule.For<Order>("Standard approval")
                    .When(o => o.Amount > 500)
                    .Then(o => Console.WriteLine("  → Requires standard check (NEVER REACHED)"))
                    .Because("This won't execute due to STOP")));

        var engine = new RuleEngine();
        var result = engine.Evaluate(order, mainRuleSet);

        Console.WriteLine($"Input: Order Amount = ${order.Amount}, Country = {order.Country}");
        Console.WriteLine();

        // Show tree format
        Console.WriteLine("═══ TREE FORMAT (v2) ═══");
        Console.WriteLine(result.Explain(new TextTreeFormatter()));
        Console.WriteLine();

        // Show flat format (backward compatibility)
        Console.WriteLine("═══ LEGACY FORMAT (Backward Compatible) ═══");
        Console.WriteLine(result.ToString());
        Console.WriteLine();

        // Show JSON format
        Console.WriteLine("═══ JSON FORMAT (Structured Output) ═══");
        var jsonFormatter = new JsonRuleResultFormatter();
        var json = jsonFormatter.Format(result);
        // Print only a portion for readability
        var lines = json.Split('\n');
        var maxLines = Math.Min(30, lines.Length);
        for (int i = 0; i < maxLines; i++)
        {
            Console.WriteLine(lines[i]);
        }
        if (lines.Length > maxLines)
        {
            Console.WriteLine($"... ({lines.Length - maxLines} more lines)");
        }
    }
}
