namespace RuleFlow.ConsoleSample.Playground;

/// <summary>
/// Represents a demonstration scenario in the RuleFlow Playground.
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Name of the scenario (displayed in menu).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what this scenario demonstrates.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the scenario.
    /// </summary>
    Task Run();
}
