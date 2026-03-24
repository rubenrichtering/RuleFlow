# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - Unreleased

### Added
- **Lightweight Observability Layer** for runtime insights without impacting performance
  - New `IRuleObserver<T>` interface for pluggable observability callbacks
  - Minimal context DTOs: `RuleEvaluationContext<T>`, `RuleMatchContext<T>`, `RuleExecutionContext<T>`
  - `RuleExecutionMetrics` and `RuleExecutionSummary` for aggregated metrics collection
  - Built-in `InMemoryRuleObserver<T>` for zero-boilerplate observability
  - Extension of `RuleExecutionOptions<T>` with `EnableObservability`, `EnableDetailedTiming`, and `Observer` properties
  - Observer callbacks invoked at: rule evaluation start, on match, after action execution, and completion
  - Full support for observability in async rules, nested groups, and stop-processing scenarios
  - 13 comprehensive observability tests covering disabled/enabled modes, callback ordering, exception safety, and metric accuracy
- Regression tests for custom `IRule<T>` implementations to ensure actions execute in both explainability modes
- Regression tests for nested `RuleSetDefinition` mapping to ensure deep groups are preserved
- New IncludeGroups tests for duplicate nested group names (full-path filtering + legacy leaf-name compatibility)
- Rule registry lifecycle tests covering startup registration, freeze-after-first-lookup behavior, and duplicate registration checks

### Improved
- IncludeGroups now supports deterministic full hierarchical paths (e.g., `Parent/Child`) with legacy leaf-name compatibility
- Dynamic condition array conversion now avoids per-item converter allocations
- Rule engine internals refactored into smaller helper methods for readability and maintainability
- Observability designed for zero overhead when disabled: no observer calls, no Stopwatch allocations, no context objects

### Fixed
- Fixed action execution for non-concrete `IRule<T>` implementations (custom rules now execute correctly)
- Fixed persisted ruleset mapping to preserve nested group hierarchies instead of flattening descendants
- Fixed nested execution `GroupName` tracking to record full hierarchical paths for nested groups

### Changed
- `RuleRegistry<T>` now follows a startup-mutable/runtime-read-only lifecycle: registration is blocked after first lookup
- `RuleResult` now includes optional `Metrics` property populated only when observability is enabled (backward compatible)

## [0.2.0] - 2026-03-23

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
