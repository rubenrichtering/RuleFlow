using RuleFlow.ConsoleSample.Playground;
using RuleFlow.ConsoleSample.Playground.Scenarios;
using RuleFlow.Core.Engine;
using RuleFlow.Core.Rules;

// Create the Playground with all scenarios
var playgroundRunner = new ScenarioRunner()
    .AddScenario(new BasicRulesScenario())
    .AddScenario(new PriorityScenario())
    .AddScenario(new StopProcessingScenario())
    .AddScenario(new GroupScenario())
    .AddScenario(new ExplainabilityScenario())
    .AddScenario(new ExplainabilityRefactorScenario())
    .AddScenario(new AsyncScenario())
    .AddScenario(new ContextScenario())
    .AddScenario(new MetadataScenario())
    .AddScenario(new ConditionalChainsScenario())
    .AddScenario(new ExecutionOptionsScenario())
    .AddScenario(new PersistenceScenario())
    .AddScenario(new DynamicConditionsScenario())
    .AddScenario(new DependencyInjectionScenario())
    .AddScenario(new DebugFormattingScenario());

// Run the interactive playground
await playgroundRunner.RunInteractive();
