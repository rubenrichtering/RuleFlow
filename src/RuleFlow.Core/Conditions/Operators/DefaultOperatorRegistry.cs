using System.Collections.ObjectModel;
using RuleFlow.Abstractions.Conditions;

namespace RuleFlow.Core.Conditions.Operators;

/// <summary>
/// Default registry with built-in operators: equals, greater_than, less_than, between, in.
/// </summary>
public sealed class DefaultOperatorRegistry : IOperatorRegistry
{
    private readonly IReadOnlyDictionary<string, IOperator> _byName;

    public DefaultOperatorRegistry()
    {
        IOperator[] ops =
        [
            new EqualsOperator(),
            new GreaterThanOperator(),
            new LessThanOperator(),
            new BetweenOperator(),
            new InOperator()
        ];

        _byName = new ReadOnlyDictionary<string, IOperator>(
            ops.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase));
    }

    public IOperator Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Operator name must not be empty.", nameof(name));

        if (_byName.TryGetValue(name, out var op))
            return op;

        throw new KeyNotFoundException($"Unknown operator '{name}'.");
    }
}
