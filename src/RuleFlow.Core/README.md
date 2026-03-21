# RuleFlow.Core

RuleFlow.Core contains the default implementation of the RuleFlow rule engine.

## Features

- ✅ Fluent rule definition API
- ✅ Deterministic rule execution
- ✅ Explainable results (text tree, JSON, custom formatters)
- ✅ Async rule support (conditions and actions)
- ✅ Runtime context with custom data
- ✅ Rule priorities and stop-processing
- ✅ Hierarchical rule groups

## Core Components

### Rule<T>

Defines a single rule with:
- **Condition** (When/WhenAsync) - evaluates the input
- **Action** (Then/ThenAsync) - executes if condition matches
- **Reason** (Because) - explains why the rule exists
- **Priority** - execution order (higher first)
- **StopProcessing** - stops remaining rules if matched

### RuleContext

Runtime context providing:
- **Now** - current DateTime (defaults to UTC now)
- **Items** - key-value store for custom data
- **Get<T>(key)** - type-safe item access
- **Set<T>(key, value)** - add/update items

### RuleSet<T>

Container for rules and groups:
- **Rules** - ordered collection of rules
- **Groups** - nested RuleSets
- **Name** - identifies the rule set

### RuleEngine

Executes rules:
- **Evaluate<T>()** - synchronous evaluation
- **EvaluateAsync<T>()** - asynchronous evaluation
- Both return **RuleResult** with execution details

### RuleResult

Contains execution information:
- **Executions** - list of rule executions (flat)
- **Root** - tree-based execution model
- **Explain()** - formatted output (text, JSON)

## Example: Context-Aware Rules

```csharp
// Create context with runtime data
var context = new RuleContext(
    now: DateTime.Now,
    items: new Dictionary<string, object?>
    {
        { "user_role", "admin" },
        { "discount_code", "SAVE10" }
    }
);

var rules = RuleSet.For<Order>("ContextAware")
    .Add(Rule.For<Order>("Time-based rule")
        .When((o, ctx) => ctx.Now.Hour >= 9 && ctx.Now.Hour < 17)
        .Then(o => o.IsValid = true)
        .Because("Processing during business hours"))
    .Add(Rule.For<Order>("Role-based discount")
        .When((o, ctx) => ctx.Get<string>("user_role") == "admin")
        .Then((o, ctx) => 
        {
            var code = ctx.Get<string>("discount_code");
            Console.WriteLine($"Admin using {code}");
        })
        .Because("Admins get special treatment"))
    .Add(Rule.For<Order>("Async credit check")
        .WhenAsync(async (o, ctx) =>
        {
            var role = ctx.Get<string>("user_role");
            return await ValidateCredit(o, role);
        })
        .Then(o => o.RequiresApproval = false)
        .Because("Credit validated asynchronously"));

var engine = new RuleEngine();
var result = await engine.EvaluateAsync(order, rules, context);

// Display results
Console.WriteLine(result.Explain());
```

## Example: Rule Priorities & Groups

```csharp
var rules = RuleSet.For<Order>("ShippingRules")
    .Add(Rule.For<Order>("Premium shipping")
        .When(o => o.Amount > 5000)
        .Then(o => o.PreferredShipping = true)
        .WithPriority(100))
    .Add(Rule.For<Order>("Standard shipping")
        .When(o => o.Amount > 100)
        .Then(o => o.StandardShipping = true)
        .WithPriority(50))
    .AddGroup(RuleSet.For<Order>("Express")
        .Add(Rule.For<Order>("Next day")
            .When(o => o.Amount > 10000)
            .Then(o => o.ExpressShipping = true)
            .StopIfMatched()));

var engine = new RuleEngine();
var result = engine.Evaluate(order, rules);
```

## Execution Order

1. Rules sorted by **Priority** (descending)
2. Rules with same priority: **insertion order**
3. Within ruleset: **groups follow**
4. **StopProcessing** halts remaining execution

## Backward Compatibility

All existing APIs maintained:

```csharp
// Old style still works
Rule.For<Order>("Simple")
    .When(o => o.Amount > 100)
    .Then(o => o.RequiresApproval = true)

// New style with context
Rule.For<Order>("Advanced")
    .When((o, ctx) => ctx.Now.Hour >= 9)
    .Then((o, ctx) => ctx.Set("processed", true))

// Async still works
Rule.For<Order>("Async")
    .WhenAsync(async o => await CheckAsync(o))
    .ThenAsync(async o => await ProcessAsync(o))
```

## Design Philosophy

- **Developer-first** - Easy to read and write
- **Deterministic** - Predictable execution order
- **Explainable** - Results tell you what happened
- **Extensible** - Custom formatters, conditions, actions
- **Performance-focused** - No unnecessary allocations
- **Simple** - No magic or hidden behavior

## Future Roadmap

- Dynamic rule compilation
- Rule persistence (database)
- ASP.NET Core middleware
- Expression-based rules
- Rule versioning