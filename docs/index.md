---
title: RuleFlow
---

RuleFlow is a lightweight, developer-first rule engine for .NET focused on clarity and **explainability**: you can see which rules ran, in what order, and why.

## Why RuleFlow

- Fluent C# API for rules and rule sets
- Deterministic execution with priorities, stop-processing, and optional groups
- **Explainability**: tree and JSON views of execution (see [Explainability](concepts/explainability))
- Async conditions and actions, runtime context, and optional JSON-backed rule definitions

## Documentation

| Topic | Description |
| --- | --- |
| [Getting started](getting-started) | Install, minimal example, run the playground |
| [Rules](concepts/rules) | When, Then, ThenIf, Because, priority |
| [Rule sets](concepts/rulesets) | `RuleSet`, groups, ordering |
| [Explainability](concepts/explainability) | `Explain()`, formatters, JSON |
| [Execution options](advanced/execution-options) | Stop on first match, filters, groups, explain toggle |
| [Persistence](advanced/persistence) | Load rule definitions from JSON |

## Versioning and NuGet

Versions follow **SemVer** tags: `vMAJOR.MINOR.PATCH` (for example `v0.1.0`). Pushing a tag triggers the publish workflow; set the `NUGET_API_KEY` secret in the repository.

## GitHub Pages

Enable **Pages** in the repository settings: source **Deploy from a branch**, branch **main**, folder **/docs**. This site is plain Markdown in `docs/` with the default Jekyll integration.

## Source and playground

Examples in this documentation mirror the **RuleFlow.ConsoleSample** playground scenarios under `samples/RuleFlow.ConsoleSample/Playground/Scenarios/`.
