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
└─ RuleFlow.Core.Tests.csproj


---

## Guidelines

- Tests focus on observable behavior, not internal implementation.
- Use `Shouldly` for assertions:

```csharp
result.AppliedRules.ShouldContain("High amount");
order.RequiresApproval.ShouldBeTrue();
```

Keep tests small, readable, and deterministic.
Organize tests by feature and folder (Engine, Rules, Explainability).

## Notes
- Do not add tests for features that are not yet implemented.
- Tests should be maintainable and provide confidence for future changes.
- CI/CD pipelines should run these tests automatically.