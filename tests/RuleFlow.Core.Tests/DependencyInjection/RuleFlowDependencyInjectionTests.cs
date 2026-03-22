using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RuleFlow.Abstractions;
using RuleFlow.Core.Rules;
using RuleFlow.Extensions.DependencyInjection;
using Shouldly;

namespace RuleFlow.Core.Tests.DependencyInjection;

public class RuleFlowDependencyInjectionTests
{
    [Fact]
    public void AddRuleFlow_ShouldRegisterRuleEngine()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRuleFlow();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var ruleEngine = serviceProvider.GetService<IRuleEngine>();
        ruleEngine.ShouldNotBeNull();
    }

    [Fact]
    public void AddRuleFlow_ShouldRegisterRuleEngineAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRuleFlow();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var engine1 = serviceProvider.GetRequiredService<IRuleEngine>();
        var engine2 = serviceProvider.GetRequiredService<IRuleEngine>();

        // Assert
        ReferenceEquals(engine1, engine2).ShouldBeTrue();
    }

    [Fact]
    public void AddRuleFlow_ShouldRegisterRuleContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRuleFlow();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var context = serviceProvider.GetService<IRuleContext>();
        context.ShouldNotBeNull();
    }

    [Fact]
    public void AddRuleFlow_ShouldRegisterRuleContextAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRuleFlow();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using (var scope1 = serviceProvider.CreateScope())
        {
            var context1 = scope1.ServiceProvider.GetRequiredService<IRuleContext>();
            var context2 = scope1.ServiceProvider.GetRequiredService<IRuleContext>();
            
            // Assert - same scope should return same instance
            ReferenceEquals(context1, context2).ShouldBeTrue();
        }

        using (var scope2 = serviceProvider.CreateScope())
        {
            var context3 = scope2.ServiceProvider.GetRequiredService<IRuleContext>();
            
            // Assert - different scopes should return different instances
            var scope1Context = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IRuleContext>();
            ReferenceEquals(scope1Context, context3).ShouldBeFalse();
        }
    }

    [Fact]
    public void AddRuleFlow_WithConfiguration_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRuleFlow(options =>
        {
            options.EnableExplainability = true;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var optionsMonitor = serviceProvider.GetRequiredService<IOptions<RuleFlowOptions>>();
        optionsMonitor.Value.EnableExplainability.ShouldBeTrue();
    }

    [Fact]
    public void AddRuleFlow_ShouldAllowRuleExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRuleFlow();
        var serviceProvider = services.BuildServiceProvider();
        var engine = serviceProvider.GetRequiredService<IRuleEngine>();

        // Create test data
        var input = new TestData { Value = 100 };
        var rules = RuleSet.For<TestData>("TestRules")
            .Add(Rule.For<TestData>("Test rule")
                .When(x => x.Value > 50)
                .Then(x => x.IsApplied = true)
                .Because("Value exceeds threshold"));

        // Act
        var result = engine.Evaluate(input, rules);

        // Assert
        input.IsApplied.ShouldBeTrue();
        result.AppliedRules.ShouldContain("Test rule");
    }

    private class TestData
    {
        public int Value { get; set; }
        public bool IsApplied { get; set; }
    }
}
