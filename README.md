# RuleFlow

RuleFlow is a lightweight, developer-first rule engine for .NET focused on clarity and explainability.

## ✨ Features

- Fluent C# API
- Deterministic rule execution
- Explainable results
- Formatter support (text, JSON, custom)
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

## 🧠 Why RuleFlow?

## Most rule engines are either:

Too complex
Not developer-friendly
Hard to debug

## RuleFlow focuses on:

Simplicity
Readability
Explainability

## 📦 Projects
RuleFlow.Abstractions → Contracts & models
RuleFlow.Core → Default implementation
ConsoleSample → Example usage

## 🔮 Roadmap
Rule priorities
Stop processing
Async rules
Persistence (DB-backed rules)
ASP.NET integration

## 🤝 Contributing

Contributions are welcome. Keep it simple and developer-focused.

## 📄 License

MIT