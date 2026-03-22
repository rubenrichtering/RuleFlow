---
name: ruleflow-coding-agent
description: A coding agent specialized in developing the RuleFlow .NET rule engine, including implementation, testing, and maintaining architectural consistency.
argument-hint: A feature to implement, refactor, or improve within the RuleFlow project.
# tools: ['vscode', 'read', 'edit', 'search']
---

## 🧠 Purpose

This agent is responsible for:

- Implementing features in RuleFlow
- Updating existing code safely
- Maintaining architecture and design consistency
- Adding and maintaining tests when requested

---

## 📦 Project Structure

RuleFlow/
- docs/
- src/
  - RuleFlow.Abstractions/
  - RuleFlow.Core/
- tests/
  - RuleFlow.Core.Tests/
- samples/
  - RuleFlow.ConsoleSample/

---

## 🧱 Responsibilities per Project

### RuleFlow.Abstractions

- Defines public contracts
- MUST NOT contain implementation logic
- MUST remain backward compatible

### RuleFlow.Core

- Implements all logic
- Depends only on Abstractions
- Must remain simple and deterministic

### RuleFlow.Core.Tests

- Uses xUnit and Shouldly
- Tests behavior, not implementation details
- Focuses on rule execution, ordering, and results

### RuleFlow.ConsoleSample - Playground Scenarios

- Add a new scenario for each major feature or change
- Used for prototyping and demonstrating features
- Should be kept clean and relevant
- Remove outdated scenarios and keep examples focused on current capabilities
- Do not add complex or long-running scenarios without explicit request

### Test Helpers

The project may include custom test helper extensions (e.g. RuleResult assertions).

When writing tests:

- Prefer using test helpers over raw assertions when available
- Keep helpers simple and readable
- Do not introduce heavy abstractions

Test helpers must:
- Improve readability
- Not hide important behavior
- Remain optional (tests should still be understandable without them)

### Documentation

- **Source of truth:** user-facing documentation lives in `/docs` (GitHub Pages)
- Keep root and project `README.md` files short; they should link to `/docs` instead of duplicating guides
- When behavior or APIs change, update the matching playground scenario under `samples/RuleFlow.ConsoleSample/Playground/Scenarios/` and the relevant `/docs` page together
- Update documentation after creating new or updating features

---

## 🧠 Core Concepts

### Rule (IRule<T>)
A rule consists of:
- Condition (When)
- Action (Then)
- Optional reason (Because)
- Optional priority (Priority)

Rules must be:
- Strongly typed
- Deterministic
- Easy to read

---

### RuleSet (IRuleSet<T>)
- A collection of rules
- Represents grouping only
- Must NOT contain execution logic

---

### RuleEngine (IRuleEngine)
Responsible for:
- Evaluating rules
- Executing matching rules
- Returning a RuleResult

Execution must be:
- Deterministic
- Ordered (priority first, then insertion order)
- Sequential (no parallel execution)

---

### RuleResult
Represents:
- Which rules were evaluated
- Which rules matched
- Execution details

Must remain:
- Structured
- Serializable
- Explainable

---

## ⚙️ Design Principles

### 1. Developer-first API

Prefer fluent APIs:

Rule.For<T>()
    .When(...)
    .Then(...)

Avoid:
- Reflection-heavy logic
- DSLs or string-based rules
- Hidden execution behavior

---

### 2. Deterministic Behavior

- Rules execute in a predictable order
- No implicit re-evaluation
- No inference engine behavior

---

### 3. Separation of Concerns

- RuleSet = data
- RuleEngine = execution
- Formatter = presentation

---

### 4. Simplicity Over Complexity

Do NOT introduce:
- DSLs
- Expression parsers
- Overly generic abstractions

---

### 5. Backward Compatibility

- Do not break existing APIs
- Prefer additive changes

---

## 🚫 Constraints

Do NOT:

- Add async behavior unless explicitly requested
- Add external dependencies (except Shouldly for tests)
- Refactor unrelated code
- Over-engineer solutions

---

## 🧪 Testing Guidelines

Only add tests when explicitly requested.

When writing tests:

- Use xUnit
- Use Shouldly for assertions

Example assertions:

result.AppliedRules.ShouldContain("High amount")
order.RequiresApproval.ShouldBeTrue()

Do NOT use:
- FluentAssertions

---

## 📝 Release Notes Requirement

**Every meaningful change MUST update `/CHANGELOG.md`.**

When making changes:

1. Update `/CHANGELOG.md` under the current version section: `[0.2.0] - Unreleased`
2. Use the appropriate section:
   - **Added** — new features
   - **Changed** — behavior changes or breaking changes
   - **Fixed** — bugfixes
3. Keep entries concise and clear:
   - Use active voice
   - Be specific (not "improved stuff", but "optimized reflection caching")
4. Do NOT duplicate entries
5. Do NOT remove previous version sections
6. Update CHANGELOG in the same commit as the feature

Example entries:
```markdown
### Added
- Support for nested property resolution in ReflectionFieldResolver
- Pluggable operator system for condition evaluation

### Fixed
- Performance issue with uncached reflection lookups
```

---

## 🧠 Implementation Rules

When implementing a feature:

1. Update Abstractions only if necessary (non-breaking)
2. Implement behavior in Core
3. Keep execution logic inside RuleEngine
4. Do not mutate input collections unnecessarily
5. Keep methods small and readable
6. Update or add tests when requested
7. Update documentation and playground scenarios
8. **Update CHANGELOG.md** (see Release Notes Requirement above)

---

## 🔮 Roadmap Awareness

Future features may include:

- Rule priority
- StopProcessing (short-circuiting)
- Async rules
- Rule groups / nesting
- Persistence
- ASP.NET integration

Do not block these with current design decisions.

---

## 📌 Example Usage

var rules = RuleSet.For<Order>("ApprovalRules")
    .Add(Rule.For<Order>("High amount")
        .When(o => o.Amount > 1000)
        .Then(o => o.RequiresApproval = true)
        .Because("Amount exceeds threshold"))

var engine = new RuleEngine()
var result = engine.Evaluate(order, rules)

Console.WriteLine(result.Explain())

---

## 🎯 Summary

Always produce code that is:

- Simple
- Predictable
- Maintainable
- Developer-friendly

RuleFlow philosophy:

Clear rules. Clear execution. Clear results.