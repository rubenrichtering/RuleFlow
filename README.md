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
- **Rule persistence** (load rule definitions from JSON)

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

### Persistence (v1)
Load rule definitions from external sources (JSON, database, etc.):

```csharp
// Define rules as JSON
var definitionJson = """
{
  "name": "ApprovalRules",
  "rules": [
    {
      "name": "High amount",
      "conditionKey": "HighAmount",
      "actionKeys": ["RequireApproval"],
      "priority": 10
    }
  ]
}
""";

// Register your conditions and actions
var registry = new RuleRegistry<Order>();
registry.RegisterCondition("HighAmount", (order, _) => order.Amount > 500);
registry.RegisterAction("RequireApproval", (order, _) => order.RequiresApproval = true);

// Map definitions to executable rules
var definition = JsonSerializer.Deserialize<RuleSetDefinition>(definitionJson);
var mapper = new RuleDefinitionMapper<Order>(registry);
var executableRuleSet = mapper.MapRuleSet(definition);

// Execute like normal
var engine = new RuleEngine();
var result = engine.Evaluate(order, executableRuleSet);
```

**Key Points:**
- Definitions are **data only** (no expression parsing)
- Conditions/actions are registered by **string key**
- Maps to **executable rules** using the fluent API
- Fully **JSON-serializable**

See [Persistence Layer Documentation](src/RuleFlow.Core/Persistence/README.md) for details.

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
- ✅ Persistence Layer (v1)
  - ✅ Load rule definitions from JSON
  - ✅ Registry pattern for conditions/actions
  - ✅ Mapping to executable rules
  - 🔲 Database storage
  - 🔲 Versioning / Audit trail
- 🔲 Dynamic rule compilation
- 🔲 Execution Options / Modes  
   - Partial execution  
   - Scenario-based evaluation
- 🔲 ASP.NET Integration  
   - services.AddRuleFlow()
- 🔲 Performance Optimization  
   - Compiled expressions  
   - Caching rules

## 🤝 Contributing

Contributions are welcome. Keep it simple and developer-focused.

## 📄 License

MIT