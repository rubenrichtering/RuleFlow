# RuleFlow.Abstractions Persistence

## Overview

The persistence layer provides contracts for loading and executing rule definitions from external sources.

## Key Components

### RuleDefinition

A DTO that represents a single rule in persisted form:

```csharp
public class RuleDefinition
{
    public string Name { get; set; }
    public string? Reason { get; set; }
    public int Priority { get; set; }
    public bool StopProcessing { get; set; }
    public string ConditionKey { get; set; }
    public List<string> ActionKeys { get; set; }
    public Dictionary<string, object?> Metadata { get; set; }
}
```

### RuleSetDefinition

A DTO that represents a collection of rules (with optional nested groups):

```csharp
public class RuleSetDefinition
{
    public string Name { get; set; }
    public List<RuleDefinition> Rules { get; set; }
    public List<RuleSetDefinition> Groups { get; set; }
}
```

### IRuleRegistry<T>

An interface for managing condition and action logic by string keys:

```csharp
public interface IRuleRegistry<T>
{
    void RegisterCondition(string key, Func<T, IRuleContext, bool> condition);
    void RegisterAction(string key, Action<T, IRuleContext> action);

    Func<T, IRuleContext, bool> GetCondition(string key);
    Action<T, IRuleContext> GetAction(string key);
}
```

## Design Principles

1. **Separation of Concerns**: Definitions are data; execution is separate.
2. **Explicit Mapping**: No automatic expression parsing or reflection.
3. **Simple Registry**: String keys map to compiled functions.
4. **JSON-Serializable**: All DTOs work with `System.Text.Json`.

## See Also

- [RuleFlow.Core Persistence](../../src/RuleFlow.Core/Persistence/README.md)
- [Persistence Scenario](../../../samples/RuleFlow.ConsoleSample/Playground/Scenarios/PersistenceScenario.cs)
