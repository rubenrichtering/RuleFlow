---
title: Persistence (JSON definitions)
---

RuleFlow supports loading **rule set definitions** from JSON and mapping them to runtime rules via condition/action **registries**. Playground: **`PersistenceScenario.cs`**.

## `RuleDefinition` and `RuleSetDefinition`

Persisted rules are **data only** (no expression parsing in JSON):

- **`RuleDefinition`** — name, `ConditionKey`, `ActionKeys`, optional reason, priority, stop-processing flag, metadata
- **`RuleSetDefinition`** — name, list of rules, nested **groups** (each group is another `RuleSetDefinition`)

These types live in **RuleFlow.Abstractions** and serialize with `System.Text.Json`.

## Registry pattern

Implement **`IRuleRegistry<T>`** (default: **`RuleRegistry<T>`**) to map string keys to C# delegates:

```csharp
var registry = new RuleRegistry<Order>();

registry.RegisterCondition("HighAmount", (order, _) => order.Amount > 500);
registry.RegisterAction("RequireApproval", (order, _) => order.RequiresApproval = true);
```

- Keys must be registered before mapping; duplicate keys throw
- Conditions: `Func<T, IRuleContext, bool>`
- Actions: `Action<T, IRuleContext>`

Keep business logic in **code**; JSON only references keys.

## Mapping to executable rules

Use **`RuleDefinitionMapper<T>`** to build `Rule<T>` / `RuleSet<T>` from definitions:

```csharp
var definition = JsonSerializer.Deserialize<RuleSetDefinition>(json);
var mapper = new RuleDefinitionMapper<Order>(registry);
var executableRuleSet = mapper.MapRuleSet(definition);

var engine = new RuleEngine();
var result = engine.Evaluate(order, executableRuleSet);
```

## JSON shape

The sample scenario embeds JSON similar to:

```json
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
    }
  ],
  "groups": []
}
```

## Keeping docs honest

When the JSON schema or mapper behavior changes, update:

- `PersistenceScenario.cs`
- This page
