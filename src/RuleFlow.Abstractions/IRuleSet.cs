namespace RuleFlow.Abstractions;

public interface IRuleSet<T>
{
    string Name { get; }

    IReadOnlyList<IRule<T>> Rules { get; }
}