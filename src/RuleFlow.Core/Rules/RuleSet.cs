using RuleFlow.Abstractions;

namespace RuleFlow.Core.Rules;

public class RuleSet<T> : IRuleSet<T>
{
    public string Name { get; }

    private readonly List<IRule<T>> _rules = new();

    private RuleSet(string name)
    {
        Name = name;
    }

    public static RuleSet<T> For(string name)
    {
        return new RuleSet<T>(name);
    }

    public RuleSet<T> Add(IRule<T> rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        _rules.Add(rule);
        return this;
    }

    public IReadOnlyList<IRule<T>> Rules => _rules;
}