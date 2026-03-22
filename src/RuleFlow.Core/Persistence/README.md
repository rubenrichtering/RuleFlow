# RuleFlow.Core Persistence

## Overview

The persistence layer contains the implementations for loading and executing persisted rule definitions.

## Components

### RuleRegistry<T>

Default implementation of `IRuleRegistry<T>`:

```csharp
var registry = new RuleRegistry<Order>();

registry.RegisterCondition("HighAmount", (order, context) => order.Amount > 500);
registry.RegisterAction("RequireApproval", (order, context) => order.RequiresApproval = true);

var condition = registry.GetCondition("HighAmount");
var action = registry.GetAction("RequireApproval");
```

**Behavior:**
- Throws `ArgumentException` if a key is already registered
- Throws `KeyNotFoundException` if a key is not found
- Validates that keys are not null or empty

### RuleDefinitionMapper<T>

Maps persisted rule definitions to executable `Rule<T>` and `RuleSet<T>` instances:

```csharp
var mapper = new RuleDefinitionMapper<Order>(registry);

// Map a single rule definition
var executableRule = mapper.MapRule(ruleDefinition);

// Map a rule set definition (with support for nested groups)
var executableRuleSet = mapper.MapRuleSet(ruleSetDefinition);
```

**Mapping Process:**
1. Resolves the condition key from the registry
2. Resolves all action keys from the registry
3. Builds the rule using the fluent API (`.When()`, `.Then()`, etc.)
4. Applies metadata (reason, priority, stop processing, custom metadata)

## Usage Example

### Step 1: Define rules as JSON

```json
{
  "name": "ApprovalRules",
  "rules": [
    {
      "name": "High amount",
      "conditionKey": "HighAmount",
      "actionKeys": ["RequireApproval"],
      "reason": "Amount exceeds threshold",
      "priority": 10,
      "stopProcessing": false
    }
  ]
}
```

### Step 2: Register conditions and actions

```csharp
var registry = new RuleRegistry<Order>();

registry.RegisterCondition("HighAmount", (order, _) => order.Amount > 500);
registry.RegisterAction("RequireApproval", (order, _) => order.RequiresApproval = true);
```

### Step 3: Load definition and map to executable rules

```csharp
var definition = JsonSerializer.Deserialize<RuleSetDefinition>(json);
var mapper = new RuleDefinitionMapper<Order>(registry);
var executableRuleSet = mapper.MapRuleSet(definition);
```

### Step 4: Execute

```csharp
var engine = new RuleEngine();
var order = new Order { Amount = 750 };
var result = engine.Evaluate(order, executableRuleSet);

Console.WriteLine($"Rules applied: {string.Join(", ", result.AppliedRules)}");
Console.WriteLine($"Requires approval: {order.RequiresApproval}");
```

## Supported Features

✅ String-based condition/action mapping  
✅ Priority and stop processing  
✅ Custom metadata  
✅ Nested rule groups  
✅ JSON serialization  

## Future Enhancements

- Rule versioning
- Rule versioning history
- UI editor
- Dynamic expression parsing (not recommended - keep logic in code)
- Persistence to database
- Rule templates

## Best Practices

1. **Keep logic in code**: Register conditions and actions in C#, not in JSON strings.
2. **Use meaningful keys**: `"HighAmount"` is better than `"check1"`.
3. **Document your registry**: Comment what each key does.
4. **Validate definitions**: Check that all keys exist before mapping.

## See Also

- [RuleFlow.Abstractions Persistence](../RuleFlow.Abstractions/Persistence/README.md)
- [Persistence Scenario](../../../samples/RuleFlow.ConsoleSample/Playground/Scenarios/PersistenceScenario.cs)
