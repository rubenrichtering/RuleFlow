---
title: Dynamic conditions
---

RuleFlow supports **user-defined, structured conditions** that are stored as data (JSON, database, UI) and evaluated safely at runtime—**no** C# expression parsing, **no** dynamic compilation, **no** external scripting libraries.

Playground: **`DynamicConditionsScenario.cs`**.

## Overview

A **`ConditionNode`** is either:

- A **`ConditionLeaf`** — a single comparison (field vs literal or field vs field).
- A **`ConditionGroup`** — logical **`AND`** or **`OR`** over child nodes (nested groups allowed).

Types live in **`RuleFlow.Abstractions`** and serialize with **`System.Text.Json`**. Polymorphic JSON uses a **`kind`** discriminator: **`leaf`** or **`group`** (see examples below).

Execution uses **`IConditionEvaluator<T>`** (`ConditionEvaluator<T>` in **RuleFlow.Core**), which combines:

- **`IFieldResolver<T>`** — **`ReflectionFieldResolver<T>`** resolves dotted paths with caching.
- **`IOperatorRegistry`** — **`DefaultOperatorRegistry`** (`equals`, `greater_than`, `less_than`, `between`, `in`).
- **`IValueConverter`** — **`DefaultValueConverter`** coerces literals to the field’s type.

Persisted rules use **`RuleDefinition.Condition`**; **`RuleDefinitionMapper<T>`** maps that tree to `.When((input, ctx) => evaluator.Evaluate(input, node, ctx))`. See [Persistence](persistence).

## `ConditionLeaf`

| Member | Purpose |
| --- | --- |
| **`field`** | Property path on `T` (see nested paths below). |
| **`operator`** | Operator name (e.g. `equals`, `greater_than`). |
| **`value`** | Literal right-hand side (omit when using `compareToField`). |
| **`compareToField`** | Other property path for field-to-field comparison (do not set a literal `value` at the same time). |

## `ConditionGroup`

| Member | Purpose |
| --- | --- |
| **`operator`** | `AND` or `OR` (case-insensitive). |
| **`conditions`** | Child `ConditionNode` list (non-empty). |

## Supported operators

| Name | Meaning |
| --- | --- |
| `equals` | Equality (with basic numeric/string normalization). |
| `greater_than` | Left &gt; right. |
| `less_than` | Left &lt; right. |
| `between` | Left within inclusive `[min, max]`; **`value`** is a two-element array. |
| `in` | Left is in **`value`** (array of allowed values). |

## Nested property paths

**`ReflectionFieldResolver<T>`** treats **`field`** and **`compareToField`** as **dot-separated** paths. It walks the object graph with **simple reflection** and caches **`PropertyInfo`** per **(declaring type, segment)**.

Examples:

- `Customer.Name`
- `Customer.Address.City`

**Null handling:** if any intermediate object is **`null`**, the resolved value is **`null`** (no exception). **Unknown** property names on a type throw **`FieldResolutionException`** with the full path.

## Example (JSON)

Logical AND over a nested field and a top-level field. Note **`kind`** on each node (matches `System.Text.Json` polymorphism in code):

```json
{
  "kind": "group",
  "operator": "AND",
  "conditions": [
    {
      "kind": "leaf",
      "field": "Customer.Name",
      "operator": "equals",
      "value": "John"
    },
    {
      "kind": "leaf",
      "field": "Amount",
      "operator": "greater_than",
      "value": 100
    }
  ]
}
```

Field-to-field comparison:

```json
{
  "kind": "leaf",
  "field": "Amount",
  "operator": "greater_than",
  "compareToField": "MaxOrderValue"
}
```

## Validation

**`ConditionValidator.Validate`** ensures leaves and groups are well-formed before execution (required fields, AND/OR only, no conflicting `value` + `compareToField`).
