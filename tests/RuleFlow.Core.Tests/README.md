# RuleFlow.Core.Tests

This project contains the unit tests for the **RuleFlow.Core** library.

It ensures that all current features of the RuleFlow rule engine work correctly and behave as expected.

---

## Test Framework

- **xUnit** – provides a simple and standard test framework for .NET.
- **Shouldly** – used for clear, readable assertions.

---

## Project Structure


RuleFlow.Core.Tests/
├─ Engine/ # Tests for RuleEngine behavior
├─ Rules/ # Tests for Rule and RuleSet
├─ Explainability/ # Tests for RuleResult and Explain methods
├─ Helpers/ # Test helper extensions (RuleResultAssertions)
└─ RuleFlow.Core.Tests.csproj


---

## Test Helpers

The `Helpers/RuleResultAssertions.cs` file provides extension methods to improve test readability and reduce repetition.

Instead of:
```csharp
result.AppliedRules.ShouldContain("High amount");
result.AppliedRules.ShouldNotContain("Low amount");
```

Use:
```csharp
result.ShouldHaveMatched("High amount");
result.ShouldNotHaveMatched("Low amount");
result.ShouldHaveExecuted("Rule Name");
result.ShouldHaveMatchedRules(2);
```

**Available helpers:**
- `ShouldHaveMatched(ruleName)` – Assert a rule matched
- `ShouldNotHaveMatched(ruleName)` – Assert a rule did not match
- `ShouldHaveExecuted(ruleName)` – Assert a rule was executed
- `ShouldNotHaveExecuted(ruleName)` – Assert a rule was not executed
- `ShouldHaveMatchedRules(count)` – Assert count of matched rules
- `ShouldHaveExecutedRules(count)` – Assert count of executed rules

---

## Guidelines

- Tests focus on observable behavior, not internal implementation.
- Use test helpers from `RuleFlow.Core.Tests.Helpers` for `RuleResult` assertions.
- Use `Shouldly` for assertions:

```csharp
result.ShouldHaveMatched("High amount");
order.RequiresApproval.ShouldBeTrue();
```

Keep tests small, readable, and deterministic.
Organize tests by feature and folder (Engine, Rules, Explainability).

## Notes
- Do not add tests for features that are not yet implemented.
- Tests should be maintainable and provide confidence for future changes.
- CI/CD pipelines should run these tests automatically.