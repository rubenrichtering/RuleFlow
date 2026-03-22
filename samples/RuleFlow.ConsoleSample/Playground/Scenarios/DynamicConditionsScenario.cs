using System.Text.Json;
using RuleFlow.Abstractions.Persistence;
using RuleFlow.Core.Conditions;
using RuleFlow.Core.Conditions.Operators;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Persistence;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates JSON-defined structured conditions: literals, field-to-field, AND/OR groups.
/// </summary>
public class DynamicConditionsScenario : IScenario
{
    public string Name => "Dynamic conditions";
    public string Description => "JSON-driven ConditionNode trees (field/value, field/field, AND/OR)";

    public async Task Run()
    {
        await Task.CompletedTask;
        Console.WriteLine("=== Dynamic Conditions Scenario ===\n");

        var ruleSetJson = """
        {
          "name": "DynamicOrderRules",
          "rules": [
            {
              "name": "Over ceiling (field vs field)",
              "condition": {
                "kind": "leaf",
                "field": "Amount",
                "operator": "greater_than",
                "compareToField": "MaxOrderValue"
              },
              "actionKeys": [ "RequireApproval" ],
              "reason": "Amount exceeds configured ceiling",
              "priority": 10,
              "stopProcessing": false,
              "metadata": {}
            },
            {
              "name": "Mid-range band (between)",
              "condition": {
                "kind": "leaf",
                "field": "Amount",
                "operator": "between",
                "value": [ 100, 250 ]
              },
              "actionKeys": [ "FlagReview" ],
              "reason": "Order in review band",
              "priority": 5,
              "stopProcessing": false,
              "metadata": {}
            },
            {
              "name": "US or CA premium lane (OR group)",
              "condition": {
                "kind": "group",
                "operator": "OR",
                "conditions": [
                  {
                    "kind": "leaf",
                    "field": "Country",
                    "operator": "in",
                    "value": [ "US", "CA" ]
                  },
                  {
                    "kind": "leaf",
                    "field": "Amount",
                    "operator": "equals",
                    "value": 0
                  }
                ]
              },
              "actionKeys": [ "ApplyDomesticHandling" ],
              "reason": "Domestic or zero-amount path",
              "priority": 3,
              "stopProcessing": false,
              "metadata": {}
            },
            {
              "name": "High value AND domestic (AND group)",
              "condition": {
                "kind": "group",
                "operator": "AND",
                "conditions": [
                  {
                    "kind": "leaf",
                    "field": "Amount",
                    "operator": "greater_than",
                    "value": 200
                  },
                  {
                    "kind": "leaf",
                    "field": "Country",
                    "operator": "equals",
                    "value": "US"
                  }
                ]
              },
              "actionKeys": [ "RequireApproval" ],
              "reason": "High-value US orders need approval",
              "priority": 15,
              "stopProcessing": false,
              "metadata": {}
            }
          ],
          "groups": []
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        Console.WriteLine("Step 1: Deserialize RuleSetDefinition with Condition trees");
        var definition = JsonSerializer.Deserialize<RuleSetDefinition>(ruleSetJson, options)
            ?? throw new InvalidOperationException("Deserialize failed");

        Console.WriteLine($"✓ Loaded {definition.Name} with {definition.Rules.Count} rules\n");

        Console.WriteLine("Step 2: Build registry, evaluator, and mapper");
        var registry = new RuleRegistry<Order>();

        registry.RegisterAction("RequireApproval", (o, _) => o.RequiresApproval = true);
        registry.RegisterAction("FlagReview", (o, _) => o.LogProcessed = true);
        registry.RegisterAction("ApplyDomesticHandling", (o, _) => o.StandardShipping = true);

        var fieldResolver = new ReflectionFieldResolver<Order>();
        var operatorRegistry = new DefaultOperatorRegistry();
        var converter = new DefaultValueConverter();
        var evaluator = new ConditionEvaluator<Order>(fieldResolver, operatorRegistry, converter);
        var mapper = new RuleDefinitionMapper<Order>(registry, evaluator);

        var ruleSet = mapper.MapRuleSet(definition);
        var engine = new RuleEngine();

        Console.WriteLine("✓ Mapped and ready to execute\n");
        Console.WriteLine("Step 3: Run cases (expected outcomes in comments)\n");

        void RunCase(string label, Order order)
        {
            order.RequiresApproval = false;
            order.LogProcessed = false;
            order.StandardShipping = false;

            engine.Evaluate(order, ruleSet);
            Console.WriteLine($"{label}");
            Console.WriteLine($"  Amount={order.Amount}, Max={order.MaxOrderValue}, Country={order.Country}");
            Console.WriteLine($"  RequiresApproval={order.RequiresApproval}, LogProcessed={order.LogProcessed}, StandardShipping={order.StandardShipping}");
            Console.WriteLine();
        }

        // Over ceiling: Amount 1500 > Max 1000 → RequireApproval from first rule
        RunCase("Case A — over ceiling vs MaxOrderValue", new Order
        {
            Amount = 1500m,
            MaxOrderValue = 1000m,
            Country = "DE"
        });

        // Mid-range: between 100–250 → FlagReview (LogProcessed)
        RunCase("Case B — between band", new Order
        {
            Amount = 200m,
            MaxOrderValue = 5000m,
            Country = "DE"
        });

        // OR: Country US → domestic handling
        RunCase("Case C — OR group (US in list)", new Order
        {
            Amount = 50m,
            MaxOrderValue = 5000m,
            Country = "US"
        });

        // AND: Amount > 200 and US → high priority RequireApproval
        RunCase("Case D — AND group (high US)", new Order
        {
            Amount = 400m,
            MaxOrderValue = 5000m,
            Country = "US"
        });

        Console.WriteLine("=== Dynamic Conditions Scenario Complete ===");
    }
}
