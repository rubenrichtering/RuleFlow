using RuleFlow.Abstractions.Results;

namespace RuleFlow.Abstractions.Formatting;

public interface IRuleResultFormatter
{
    string Format(RuleResult result);
}