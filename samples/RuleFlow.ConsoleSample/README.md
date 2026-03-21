# RuleFlow.ConsoleSample

This project demonstrates how to use RuleFlow in a simple console application.

## What it shows

- Creating rules
- Executing rules
- Reading results
- Using explainability

## Example

```csharp
var order = new Order { Amount = 1500 };

var rules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("High amount")
        .When(o => o.Amount > 1000)
        .Then(o => o.RequiresApproval = true)
        .Because("Amount exceeds threshold"));

var engine = new RuleEngine();
var result = engine.Evaluate(order, rules);

Console.WriteLine(result.Explain());
```

## Output
✔ High amount (Amount exceeds threshold)

## Purpose

This project is intended as:

A quick start
A reference implementation
A playground for experimenting