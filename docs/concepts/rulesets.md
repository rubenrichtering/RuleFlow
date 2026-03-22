---
title: Rule sets
---

A **rule set** is a named collection of rules and optional **nested groups**. It implements `IRuleSet<T>` and is built with `RuleSet.For<T>("Name")`. Playground: **Rule Groups** (`GroupScenario.cs`).

## Adding rules

```csharp
var rules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("High amount")
        .When(o => o.Amount > 1000)
        .Then(o => o.RequiresApproval = true)
        .Because("Amount exceeds threshold"));
```

## Groups

Use `.AddGroup("GroupName", g => { … })` to nest another `RuleSet<T>` inside the parent. Groups help organization, scoped execution, and explainability (the tree shows group nodes).

Example:

```csharp
var mainRuleSet = RuleSet.For<Order>("OrderProcessing")
    .AddGroup("Validation", g => g
        .Add(Rule.For<Order>("Amount check")
            .When(o => o.Amount > 0)
            .Then(o => o.IsValid = true)
            .Because("Order amount is positive")))
    .AddGroup("Shipping Eligibility", g => g
        .Add(Rule.For<Order>("Standard shipping")
            .When(o => o.Amount > 100)
            .Then(o => o.StandardShipping = true)
            .Because("Amount qualifies for shipping")));
```

## Ordering and priority

Rules are ordered by **priority** (highest first) with stable ordering for equal priorities. Groups are visited according to the engine’s unified pipeline.

## Execution options

You can limit which **groups** run via `RuleExecutionOptions<T>.IncludeGroups` — see [Execution options](../advanced/execution-options) and `ExecutionOptionsScenario.cs`.
