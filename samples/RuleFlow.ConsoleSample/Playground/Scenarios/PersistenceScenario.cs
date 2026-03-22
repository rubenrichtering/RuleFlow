using System.Text.Json;
using RuleFlow.Abstractions.Persistence;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Persistence;
using RuleFlow.Core.Rules;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates rule persistence (loading rules from JSON definitions).
/// </summary>
public class PersistenceScenario : IScenario
{
    public string Name => "Persistence (v1)";
    public string Description => "Demonstrates loading and executing rules from persisted definitions";

    public async Task Run()
    {
        Console.WriteLine("=== Rule Persistence Scenario ===\n");

        // Step 1: Define rules as JSON
        var ruleSetJson = """
        {
          "name": "ApprovalRules",
          "rules": [
            {
              "name": "High amount",
              "conditionKey": "HighAmount",
              "actionKeys": [ "RequireApproval" ],
              "reason": "Amount exceeds threshold",
              "priority": 10,
              "stopProcessing": false,
              "metadata": {}
            },
            {
              "name": "Premium customer",
              "conditionKey": "IsPremium",
              "actionKeys": [ "SkipApproval" ],
              "reason": "Premium customers are pre-approved",
              "priority": 20,
              "stopProcessing": true,
              "metadata": {}
            },
            {
              "name": "Large order",
              "conditionKey": "IsLargeOrder",
              "actionKeys": [ "ApplyPremiumShipping", "RequireApproval" ],
              "reason": "Large orders get premium shipping",
              "priority": 5,
              "stopProcessing": false,
              "metadata": { "escalationLevel": "Manager" }
            }
          ],
          "groups": []
        }
        """;

        Console.WriteLine("Step 1: Load RuleSetDefinition from JSON");
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var definition = JsonSerializer.Deserialize<RuleSetDefinition>(ruleSetJson, options)
            ?? throw new InvalidOperationException("Failed to deserialize definition");

        Console.WriteLine($"✓ Loaded definition: {definition.Name}");
        Console.WriteLine($"  - {definition.Rules.Count} rules");
        Console.WriteLine();

        // Step 2: Create registry and register conditions/actions
        Console.WriteLine("Step 2: Register conditions and actions");
        var registry = new RuleRegistry<Order>();

        registry.RegisterCondition("HighAmount", (order, _) => order.Amount > 500);
        registry.RegisterCondition("IsPremium", (order, _) => order.Customer?.IsPremium ?? false);
        registry.RegisterCondition("IsLargeOrder", (order, _) => order.Amount > 1000);

        registry.RegisterAction("RequireApproval", (order, _) => order.RequiresApproval = true);
        registry.RegisterAction("SkipApproval", (order, _) => order.RequiresApproval = false);
        registry.RegisterAction("ApplyPremiumShipping", (order, _) => order.PremiumShipping = true);

        Console.WriteLine("✓ Registered 3 conditions and 3 actions");
        Console.WriteLine();

        // Step 3: Map definition to executable RuleSet<T>
        Console.WriteLine("Step 3: Map definition to executable rules");
        var mapper = new RuleDefinitionMapper<Order>(registry);
        var ruleSet = mapper.MapRuleSet(definition);

        Console.WriteLine($"✓ Mapped RuleSet: {ruleSet.Name}");
        Console.WriteLine($"  - {ruleSet.Rules.Count} executable rules");
        foreach (var rule in ruleSet.Rules)
        {
            Console.WriteLine($"    • {rule.Name} (Priority: {rule.Priority})");
        }
        Console.WriteLine();

        // Step 4: Execute against test orders
        Console.WriteLine("Step 4: Execute against test orders");
        var engine = new RuleEngine();

        var testOrders = new[]
        {
            new Order { Amount = 250, Customer = new Customer { Name = "John", IsPremium = false } },
            new Order { Amount = 750, Customer = new Customer { Name = "Jane", IsPremium = false } },
            new Order { Amount = 1500, Customer = new Customer { Name = "Bob", IsPremium = false } },
            new Order { Amount = 300, Customer = new Customer { Name = "Alice", IsPremium = true } }
        };

        foreach (var order in testOrders)
        {
            var customer = order.Customer?.Name ?? "Unknown";
            var isPremium = order.Customer?.IsPremium ?? false;
            Console.WriteLine($"\nOrder: Amount=${order.Amount}, Customer={customer}, Premium={isPremium}");
            
            var result = engine.Evaluate(order, ruleSet);
            
            Console.WriteLine($"  Applied Rules: {string.Join(", ", result.AppliedRules)}");
            Console.WriteLine($"  Requires Approval: {order.RequiresApproval}");
            
            if (order.PremiumShipping)
            {
                Console.WriteLine($"  ✓ Premium Shipping Applied");
            }
        }

        Console.WriteLine();
        Console.WriteLine("=== Persistence Scenario Complete ===");
    }
}
