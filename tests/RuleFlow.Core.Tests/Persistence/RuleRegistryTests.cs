using RuleFlow.Core.Persistence;
using Shouldly;

namespace RuleFlow.Core.Tests.Persistence;

public class RuleRegistryTests
{
    private sealed class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Should_allow_registration_before_first_lookup()
    {
        var registry = new RuleRegistry<TestObject>();

        registry.RegisterCondition("IsPositive", (obj, _) => obj.Value > 0);
        registry.RegisterAction("Mark", (_, _) => { });

        var condition = registry.GetCondition("IsPositive");
        var action = registry.GetAction("Mark");

        condition(new TestObject { Value = 1 }, null!).ShouldBeTrue();
        action.ShouldNotBeNull();
    }

    [Fact]
    public void Should_become_read_only_after_first_lookup()
    {
        var registry = new RuleRegistry<TestObject>();
        registry.RegisterAction("Mark", (_, _) => { });

        _ = registry.GetAction("Mark");

        var ex = Should.Throw<InvalidOperationException>(
            () => registry.RegisterCondition("LateCondition", (_, _) => true));

        ex.Message.ShouldContain("read-only");
    }

    [Fact]
    public void Should_still_throw_for_duplicate_keys_before_freeze()
    {
        var registry = new RuleRegistry<TestObject>();

        registry.RegisterCondition("Duplicate", (_, _) => true);

        Should.Throw<ArgumentException>(
            () => registry.RegisterCondition("Duplicate", (_, _) => false));
    }
}
