---
title: Persistence (JSON definitions)
---

RuleFlow supports loading **rule set definitions** from JSON and mapping them to runtime rules via condition/action **registries**. This is demonstrated in **`PersistenceScenario.cs`** (“Persistence (v1)” in the playground).

## Flow

1. Deserialize a `RuleSetDefinition` (and related types) from JSON.
2. Register **condition** and **action** delegates keyed by string identifiers.
3. Use **`RuleDefinitionMapper`** (or your own mapper) to build a `RuleSet<T>` and run it with `RuleEngine`.

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

Keys and handlers are **your** contracts: the engine executes whatever conditions and actions you register for those keys.

## Keeping docs honest

When the JSON schema or mapper behavior changes, update:

- `PersistenceScenario.cs`
- `src/RuleFlow.Core/Persistence/` and abstraction docs as needed
- This page
