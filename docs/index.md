---
title: RuleFlow
---

## What is RuleFlow?

RuleFlow is a **lightweight**, **developer-friendly** rule engine for .NET. You define rules with a fluent API, run them through `IRuleEngine`, and inspect outcomes—including **explainability** so you can see what ran, in what order, and why.

## Why use it

- Small surface area: rules, rule sets, execution options—no ceremony
- Deterministic execution with priorities, optional groups, and stop rules
- **Explainability** built in: flat execution records, hierarchical trees, JSON and text formatters
- **Debug DTO + JSON** — `ToDebugView()` / `ToDebugJson()` for structured, UI-friendly execution output
- Async conditions and actions, runtime context, and optional JSON-backed definitions

## Key features

| | |
| --- | --- |
| **Lightweight** | Focused API; core types are easy to learn and test |
| **Developer-friendly** | Fluent `Rule` / `RuleSet` builders; familiar C# lambdas |
| **Explainability** | `RuleResult` with `RuleExecution`, `ActionExecution`, and formatters |
| **Debug DTO + JSON** | `ToDebugView()` and `ToDebugJson()` for structured, UI-friendly execution output |
| **Observability** | Optional metrics, custom observers, zero overhead when disabled |

## Documentation

| Topic | Description |
| --- | --- |
| [Getting started](getting-started) | Install, minimal example, run the playground |
| [Rules](concepts/rules) | `When`, `Then`, `ThenIf`, `Because`, priority |
| [Rule sets](concepts/rulesets) | `RuleSet`, groups, ordering |
| [Explainability](concepts/explainability) | `RuleExecution`, `ActionExecution`, debug string, debug DTO and JSON |
| [Execution options](advanced/execution-options) | Stop on first match, filters, groups, explain toggle |
| [Observability](advanced/observability) | Metrics, custom observers, performance monitoring |
| [Persistence](advanced/persistence) | `RuleDefinition`, registry, JSON definitions |
| [Dynamic conditions](advanced/dynamic-conditions) | `ConditionNode`, operators, nested field paths |
| [ASP.NET Core integration](advanced/aspnet-integration) | `AddRuleFlow()` and DI |

## Versioning and NuGet

Versions follow **SemVer** tags: `vMAJOR.MINOR.PATCH` (for example `v0.1.0`). Pushing a tag triggers the publish workflow; set the `NUGET_API_KEY` secret in the repository.

## GitHub Pages

In the repository settings, enable **Pages**: **Deploy from a branch**, branch **main**, folder **`/docs`**. This site is Markdown in `docs/` with the default Jekyll integration.

## Releases

See the full changelog on GitHub:
https://github.com/rubenrichtering/RuleFlow/CHANGELOG.md

## Source and playground

Examples in this documentation mirror the **RuleFlow.ConsoleSample** scenarios under `samples/RuleFlow.ConsoleSample/Playground/Scenarios/`.
