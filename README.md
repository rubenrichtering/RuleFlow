# RuleFlow

RuleFlow is a lightweight, developer-first rule engine for .NET focused on clarity and explainability.

## ✨ Features

- Fluent C# API
- Deterministic rule execution
- Explainable results (tree-based, JSON, custom formatters)
- Async rule support (conditions and actions)
- Runtime context with custom data
- Rule priorities and stop-processing
- Hierarchical rule groups
- Interface-driven design

## 🚀 Quick Start

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

## 🎯 Key Concepts

### Rules
A rule has a condition (When) and action (Then):

```csharp
Rule.For<T>(name)
    .When(input => condition)
    .Then(input => action)
    .Because("reason")
    .WithPriority(10)
    .StopIfMatched()
```

### Async Support
Both conditions and actions can be async:

```csharp
Rule.For<Order>("Credit check")
    .WhenAsync(async o => await ValidateCredit(o))
    .ThenAsync(async o => await ProcessOrder(o))
```

### Runtime Context
Pass runtime data and services via context:

```csharp
var context = new RuleContext(
    now: DateTime.Now,
    items: new Dictionary<string, object?> 
    { 
        { "user_role", "admin" },
        { "api_limit", 100 }
    }
);

Rule.For<Order>("Time-based")
    .When((o, ctx) => ctx.Now.Hour >= 9)
    .Then(o => o.IsValid = true)

// Access context items
context.Get<string>("user_role")
context.Set("processed", true)
```

### Rule Groups
Organize rules hierarchically:

```csharp
var rules = RuleSet.For<Order>("Shipping")
    .Add(Rule.For<Order>("..."))
    .AddGroup(RuleSet.For<Order>("Express")
        .Add(Rule.For<Order>("...")))
```

## 🧠 Why RuleFlow?

Most rule engines are either:
- Too complex
- Not developer-friendly
- Hard to debug

RuleFlow focuses on:
- Simplicity
- Readability
- Explainability

## 📦 Projects

- **RuleFlow.Abstractions** → Public contracts & models
- **RuleFlow.Core** → Default implementation
- **RuleFlow.Core.Tests** → 99+ unit tests
- **ConsoleSample** → Interactive playground with scenarios

## 🔮 Roadmap

- ✅ Basic rule execution
- ✅ Rule priorities
- ✅ Stop processing
- ✅ Async rules
- ✅ Runtime context with data
- ✅ Explainability (tree & JSON)
- ✅ Rule groups
- 🔲 Rule persistence (database)
- 🔲 Dynamic rule compilation
- 🔲 ASP.NET Core integration

## 🤝 Contributing

Contributions are welcome. Keep it simple and developer-focused.

## 📄 License

MIT