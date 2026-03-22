using Microsoft.Extensions.DependencyInjection;
using RuleFlow.Abstractions;
using RuleFlow.Core.Context;
using RuleFlow.Core.Engine;

namespace RuleFlow.Extensions.DependencyInjection;

/// <summary>
/// Service collection extensions for RuleFlow Dependency Injection integration.
/// </summary>
public static class RuleFlowServiceCollectionExtensions
{
    /// <summary>
    /// Registers RuleFlow services with the Dependency Injection container.
    /// </summary>
    /// <param name="services">The service collection to add RuleFlow to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This registers:
    /// - IRuleEngine as singleton (RuleEngine)
    /// - IRuleContext as scoped (DefaultRuleContext)
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddRuleFlow();
    /// </code>
    /// </example>
    public static IServiceCollection AddRuleFlow(this IServiceCollection services)
    {
        return AddRuleFlow(services, configure: null);
    }

    /// <summary>
    /// Registers RuleFlow services with the Dependency Injection container with optional configuration.
    /// </summary>
    /// <param name="services">The service collection to add RuleFlow to.</param>
    /// <param name="configure">Optional action to configure RuleFlow options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This registers:
    /// - IRuleEngine as singleton (RuleEngine)
    /// - IRuleContext as scoped (DefaultRuleContext)
    /// - RuleFlow options for configuration
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddRuleFlow(options =>
    /// {
    ///     options.EnableExplainability = true;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddRuleFlow(
        this IServiceCollection services,
        Action<RuleFlowOptions>? configure)
    {
        // Register the rule engine as a singleton
        services.AddSingleton<IRuleEngine, RuleEngine>();

        // Register the default rule context as scoped
        services.AddScoped<IRuleContext, DefaultRuleContext>();

        // Register options
        if (configure != null)
        {
            services.Configure<RuleFlowOptions>(configure);
        }
        else
        {
            services.Configure<RuleFlowOptions>(_ => { });
        }

        return services;
    }
}
