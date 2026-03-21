using RuleFlow.Abstractions;

namespace RuleFlow.Core.Rules;

public class RuleSet<T> : IRuleSet<T>
{
    public string Name { get; }

    private readonly List<IRule<T>> _rules = new();
    private readonly List<IRuleSet<T>> _groups = new();

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

    public RuleSet<T> AddGroup(string name, Func<RuleSet<T>, RuleSet<T>> configure)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var group = new RuleSet<T>(name);
        configure(group);

        _groups.Add(group);

        return this;
    }

    public IReadOnlyList<IRule<T>> Rules => _rules;

    public IReadOnlyList<IRuleSet<T>> Groups => _groups;
}