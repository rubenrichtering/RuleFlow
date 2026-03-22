using Microsoft.Extensions.DependencyInjection;
using RuleFlow.Abstractions;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;
using RuleFlow.Extensions.DependencyInjection;

namespace RuleFlow.ConsoleSample.Playground.Scenarios;

/// <summary>
/// Demonstrates ASP.NET Core Dependency Injection integration with RuleFlow.
/// </summary>
public class DependencyInjectionScenario : IScenario
{
    public string Name => "Dependency Injection";
    public string Description => "Demonstrates RuleFlow integration with ASP.NET Core DI";

    public async Task Run()
    {
        // Create a service collection and register RuleFlow
        var services = new ServiceCollection();
        services.AddRuleFlow();

        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();

        // Resolve the IRuleEngine from DI
        var ruleEngine = serviceProvider.GetRequiredService<IRuleEngine>();

        // Create a sample order
        var order = new Order { Amount = 1500 };

        // Define approval rules
        var rules = RuleSet.For<Order>("ApprovalRules")
            .Add(Rule.For<Order>("High amount")
                .When(o => o.Amount >= 1000)
                .Then(o => o.RequiresApproval = true)
                .Because("Amount exceeds $1000 threshold"))
            .Add(Rule.For<Order>("Premium customer")
                .When(o => o.Customer?.IsPremium ?? false)
                .Then(o => o.RequiresApproval = false)
                .Because("Premium customers are pre-approved"))
            .Add(Rule.For<Order>("Validate amount")
                .When(o => o.Amount > 0)
                .Then(o => o.IsValid = true)
                .Because("Order amount is valid"));

        // Execute rules using the injected engine
        var result = ruleEngine.Evaluate(order, rules);

        Console.WriteLine("📦 Dependency Injection Integration");
        Console.WriteLine();
        Console.WriteLine("✅ IRuleEngine resolved from DI container");
        Console.WriteLine($"   Engine type: {ruleEngine.GetType().Name}");
        Console.WriteLine();

        Console.WriteLine("Input: Order");
        Console.WriteLine($"  Amount: ${order.Amount}");
        Console.WriteLine($"  IsPremium: {(order.Customer?.IsPremium ?? false)}");
        Console.WriteLine();

        Console.WriteLine("Rules Applied:");
        foreach (var appliedRule in result.AppliedRules)
        {
            Console.WriteLine($"  ✔ {appliedRule}");
        }
        Console.WriteLine();

        Console.WriteLine("Final State:");
        Console.WriteLine($"  IsValid: {order.IsValid}");
        Console.WriteLine($"  RequiresApproval: {order.RequiresApproval}");
        Console.WriteLine();

        Console.WriteLine("Execution Tree:");
        Console.WriteLine(result.Explain());

        await Task.CompletedTask;
    }
}
