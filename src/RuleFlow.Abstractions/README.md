# RuleFlow.Abstractions

RuleFlow.Abstractions contains the core contracts and shared models for RuleFlow.

## Purpose

This project defines the public API surface of RuleFlow:
- Interfaces for rules, rule sets, and the rule engine
- Result models for rule execution
- Formatting abstractions

## Key Concepts

### IRule<T>
Represents a single rule with:
- Evaluation logic
- Execution logic

### IRuleSet<T>
A collection of rules for a specific domain model.

### IRuleEngine
Responsible for evaluating rules against an input.

### RuleResult
Contains the outcome of rule evaluation, including:
- Which rules matched
- Execution details

## Design Goals

- No implementation logic
- Stable public contract
- Extensible and testable

## Used by

- RuleFlow.Core (default implementation)
- Future integrations (ASP.NET, persistence, etc.)