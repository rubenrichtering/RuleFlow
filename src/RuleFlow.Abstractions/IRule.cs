namespace RuleFlow.Abstractions;

public interface IRule<T>
{
    string Name { get; }
    string? Reason { get; }
    int Priority { get; }

    bool Evaluate(T input, IRuleContext context);

    void Execute(T input, IRuleContext context);
}