# RuleFlow.Core

RuleFlow.Core contains the default implementation of the RuleFlow engine.

## Features

- Fluent rule definition API
- Deterministic rule execution
- Explainable results
- Formatter support

## Example

```csharp
var rules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("High amount")
        .When(o => o.Amount > 1000)
        .Then(o => o.RequiresApproval = true)
        .Because("Amount exceeds threshold"));

var engine = new RuleEngine();
var result = engine.Evaluate(order, rules);

Console.WriteLine(result.Explain());
```

## Concepts
Rule<T>

Defines:

Condition (When)
Action (Then)
Reason (Because)
RuleEngine

Executes rules in order and returns a RuleResult.

Formatters

Used to convert RuleResult into:

Text
JSON
Custom formats
Design Goals
Developer-first API
No magic or hidden behavior
High performance
Easy to extend
Future
Rule priorities
Stop processing
Async rules
Persistence support