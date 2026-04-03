using System.Text.Json.Serialization;

namespace RuleFlow.Abstractions.Conditions;

/// <summary>
/// Root of the structured condition tree (JSON-serializable).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ConditionLeaf), "leaf")]
[JsonDerivedType(typeof(ConditionGroup), "group")]
[JsonDerivedType(typeof(AiConditionNode), "ai")]
public abstract class ConditionNode
{
}
