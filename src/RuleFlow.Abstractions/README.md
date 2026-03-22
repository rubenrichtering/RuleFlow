# RuleFlow.Abstractions

Public **contracts** and **models** for RuleFlow: no implementation logic here.

## Main types

| Area | Examples |
| --- | --- |
| Engine | `IRuleEngine`, `RuleExecutionOptions<T>` |
| Rules | `IRule<T>`, `IRuleSet<T>` |
| Results | `RuleResult`, `RuleExecution`, `ActionExecution`, `RuleExecutionNode` |
| Context | `IRuleContext` |
| Persistence | `RuleDefinition`, `RuleSetDefinition`, `IRuleRegistry<T>` |
| Formatting | Formatter interfaces used by explainability output |

## Documentation

Full documentation: [https://rubenrichtering.github.io/RuleFlow/](https://rubenrichtering.github.io/RuleFlow/)

Concepts: [Rules](https://rubenrichtering.github.io/RuleFlow/concepts/rules.html), [Explainability](https://rubenrichtering.github.io/RuleFlow/concepts/explainability.html).
