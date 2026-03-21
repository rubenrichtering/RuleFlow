namespace RuleFlow.ConsoleSample.Playground;

/// <summary>
/// Manages and runs scenarios in the RuleFlow Playground.
/// </summary>
public class ScenarioRunner
{
    private readonly List<IScenario> _scenarios = new();

    public ScenarioRunner AddScenario(IScenario scenario)
    {
        _scenarios.Add(scenario);
        return this;
    }

    public async Task RunInteractive()
    {
        Console.Clear();
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     RuleFlow Playground v2.0           ║");
        Console.WriteLine("║          Feature Showcase              ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Available Scenarios:");
            for (int i = 0; i < _scenarios.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {_scenarios[i].Name}");
                Console.WriteLine($"     {_scenarios[i].Description}");
            }

            Console.WriteLine($"  {_scenarios.Count + 1}. Run All");
            Console.WriteLine($"  0. Exit");
            Console.WriteLine();

            Console.Write("Select scenario (0-{0}): ", _scenarios.Count + 1);
            var input = Console.ReadLine();

            if (!int.TryParse(input, out int choice))
            {
                Console.WriteLine("Invalid input. Please try again.\n");
                continue;
            }

            if (choice == 0)
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            if (choice == _scenarios.Count + 1)
            {
                await RunAll();
            }
            else if (choice > 0 && choice <= _scenarios.Count)
            {
                await RunScenario(_scenarios[choice - 1]);
            }
            else
            {
                Console.WriteLine("Invalid choice. Please try again.\n");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
        }
    }

    public async Task RunAll()
    {
        Console.WriteLine("\n╔════════════════════════════════════════╗");
        Console.WriteLine("║         Running All Scenarios          ║");
        Console.WriteLine("╚════════════════════════════════════════╝\n");

        foreach (var scenario in _scenarios)
        {
            await RunScenario(scenario);
            Console.WriteLine("\n" + new string('─', 50) + "\n");
        }
    }

    private async Task RunScenario(IScenario scenario)
    {
        Console.WriteLine($"\n╔════════════════════════════════════════╗");
        Console.WriteLine($"║ {scenario.Name,-38} ║");
        Console.WriteLine($"╚════════════════════════════════════════╝\n");

        try
        {
            await scenario.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
        }
    }
}
