using System.Text.Json;
using RuleFlow.Abstractions.Formatting;
using RuleFlow.Abstractions.Results;

namespace RuleFlow.Core.Formatting;

public class JsonRuleResultFormatter : IRuleResultFormatter
{
    public string Format(RuleResult result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}