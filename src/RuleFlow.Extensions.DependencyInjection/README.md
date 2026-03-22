# RuleFlow.Extensions.DependencyInjection

ASP.NET Core–style **dependency injection** for RuleFlow: register the engine and context with one call.

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRuleFlow();

// Optional:
// builder.Services.AddRuleFlow(options => { options.EnableExplainability = true; });
```

Resolves **`IRuleEngine`** (singleton) and **`IRuleContext`** (scoped) in your services.

## Documentation

Details, options table, and consumption patterns:  
[ASP.NET Core integration](https://rubenrichtering.github.io/RuleFlow/advanced/aspnet-integration.html) in the docs site.

General setup: [Getting started](https://rubenrichtering.github.io/RuleFlow/getting-started.html).
