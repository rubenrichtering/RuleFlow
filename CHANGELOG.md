# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - Unreleased

### Added
- Dynamic Condition System with `ConditionNode`, `ConditionLeaf`, and `ConditionGroup`
- Pluggable operator system for condition evaluation (`OperatorRegistry`, `IOperator`)
- Field resolver abstraction for dynamic property resolution (`IFieldResolver`, `ReflectionFieldResolver`)
- Support for nested property paths using dot notation (e.g., `Customer.Address.City`)
- Cached reflection for improved performance in dynamic condition evaluation
- Comprehensive test coverage expansion: 49 new tests across scenario, edge case, and explainability validation
  - 14 real-world scenario tests (Order Approval Flow, nested groups, dynamic conditions, etc.)
  - 23 edge case hardening tests (null handling, exceptions, type mismatches, determinism)
  - 11 explainability verification tests (rule tracking, execution order, stop processing)
- Automated validation of edge cases (null properties, empty rulesets, exceptions in conditions/actions)
- Determinism verification tests ensuring consistent execution across multiple evaluations
- Stop processing validation in complex scenarios

### Improved
- Error handling clarity and consistency across the engine
- Test helper extensions for more readable and maintainable test code
- Explainability output now more thoroughly tested and validated
- Production-readiness verification of core features

## [0.1.0] - 2026-03-22

### Added
- Core rule engine (`IRuleEngine`, `RuleEngine`)
- Fluent API for defining rules (`Rule.For<T>()`, `.When()`, `.Then()`)
- Rule sets and nested groups (`IRuleSet<T>`, `RuleSet.For<T>()`)
- Rule priority and execution ordering
- Stop processing (`StopProcessing` execution option)
- Async rule support (async conditions and actions)
- Conditional chains (`ThenIf()` for secondary actions)
- Explainability system with execution records (`RuleExecution`, `ActionExecution`)
- Multiple output formatters (tree view, JSON, plain text)
- Execution options for filtering and control
- Rule persistence via definitions (`RuleDefinition`)
- ASP.NET Core integration (`AddRuleFlow()` service extension)
- Repository examples and documentation
