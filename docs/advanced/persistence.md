---
title: Persistence (JSON definitions)
---

RuleFlow supports loading **rule set definitions** from JSON and mapping them to runtime rules via condition/action **registries**. Playground: **`PersistenceScenario.cs`**.

## `RuleDefinition` and `RuleSetDefinition`

Persisted rules are **data only** (no expression parsing in JSON):

- **`RuleDefinition`** — name, optional `ConditionKey`, optional structured **`Condition`** (`ConditionNode`), `ActionKeys`, optional reason, priority, stop-processing flag, metadata
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

Keep business logic in **code** when using **registry keys**; JSON only references keys. Alternatively, store a **structured condition** in JSON (see [Dynamic conditions](dynamic-conditions)).

## Mapping to executable rules

Use **`RuleDefinitionMapper<T>`** to build `Rule<T>` / `RuleSet<T>` from definitions:

```csharp
var definition = JsonSerializer.Deserialize<RuleSetDefinition>(json);
var mapper = new RuleDefinitionMapper<Order>(registry);
var executableRuleSet = mapper.MapRuleSet(definition);

var engine = new RuleEngine();
var result = engine.Evaluate(order, executableRuleSet);
```

### Dynamic conditions

When **`RuleDefinition.Condition`** is set, the mapper does **not** use `ConditionKey`. It validates the tree and wires the rule’s `When` to **`IConditionEvaluator<T>`** (typically `ConditionEvaluator<T>` with `ReflectionFieldResolver<T>`, `DefaultOperatorRegistry`, and `DefaultValueConverter`).

- **`ConditionLeaf`** — one comparison: `field`, `operator`, and either a literal **`value`** or **`compareToField`** (field-to-field).
- **`ConditionGroup`** — **`operator`**: `AND` or `OR`, plus nested **`conditions`** (leaves or groups).

Field names support **dotted paths** for nested properties, e.g. `Customer.Name`, `Customer.Address.City`. See [Dynamic conditions](dynamic-conditions).

**Registry-only** mapping (unchanged):

```csharp
var mapper = new RuleDefinitionMapper<Order>(registry);
```

**With structured conditions** (supply an evaluator):

```csharp
var fieldResolver = new ReflectionFieldResolver<Order>();
var evaluator = new ConditionEvaluator<Order>(
    fieldResolver,
    new DefaultOperatorRegistry(),
    new DefaultValueConverter());

var mapper = new RuleDefinitionMapper<Order>(registry, evaluator);
```

Example leaf comparing two fields (e.g. amount vs budget):

```json
{
  "field": "Amount",
  "operator": "greater_than",
  "compareToField": "MaxOrderValue"
}
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

- `PersistenceScenario.cs` (registry keys) and/or `DynamicConditionsScenario.cs` (structured `Condition`)
- This page and [Dynamic conditions](dynamic-conditions)
