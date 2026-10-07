using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

/// <summary>D-3: one backlog rule for every user (child and adult); the per-user setting overrides it.</summary>
public class NewWordCapPolicyTests
{
    private readonly NewWordCapPolicy _policy = new(new NewWordCapOptions());

    [Theory]
    [InlineData(0, 10)]
    [InlineData(20, 10)]
    [InlineData(21, 8)]
    [InlineData(40, 8)]
    [InlineData(41, 6)]
    [InlineData(60, 6)]
    [InlineData(61, 5)]
    [InlineData(10_000, 5)]
    public void NewWordCapPolicy_GetCap_BacklogSteps(int backlog, int expected)
    {
        _policy.GetCap(backlog, newWordsPerDay: null).Should().Be(expected);
    }

    [Fact]
    public void NewWordCapPolicy_GetCap_DefaultRuleStaysWithinFiveToTen()
    {
        for (int backlog = 0; backlog <= 500; backlog++)
        {
            _policy.GetCap(backlog, newWordsPerDay: null).Should().BeInRange(5, 10);
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 20)]
    [InlineData(5, 50)]
    public void NewWordCapPolicy_GetCap_UserSettingOverridesBacklogRule(int backlog, int setting)
    {
        _policy.GetCap(backlog, setting).Should().Be(setting);
    }

    [Fact]
    public void NewWordCapPolicy_GetCap_UsesConfiguredTable()
    {
        NewWordCapPolicy policy = new(new NewWordCapOptions { LowBacklogMax = 5, LowBacklogCap = 3 });

        policy.GetCap(5, null).Should().Be(3);
        policy.GetCap(6, null).Should().Be(8);
    }
}
