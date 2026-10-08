using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

/// <summary>WB-24: effective cap = supporter cap → learner own cap → backlog rule.</summary>
public class SupporterCapTests
{
    private readonly NewWordCapPolicy _policy = new(new NewWordCapOptions());
    private readonly Guid _supporter = Guid.NewGuid();

    [Fact]
    public void NewWordCapPolicy_GetCap_SupporterCapWinsOverOwnCap()
    {
        _policy.GetCap(backlog: 0, supporterCap: 2, newWordsPerDay: 7).Should().Be(2);
    }

    [Fact]
    public void NewWordCapPolicy_GetCap_OwnCapWhenNoSupporterCap()
    {
        _policy.GetCap(backlog: 0, supporterCap: null, newWordsPerDay: 7).Should().Be(7);
    }

    [Fact]
    public void NewWordCapPolicy_GetCap_BacklogRuleWhenNoCaps()
    {
        _policy.GetCap(backlog: 0, supporterCap: null, newWordsPerDay: null).Should().Be(new NewWordCapOptions().LowBacklogCap);
    }

    [Fact]
    public void VocabularyLearnerSettings_SetSupporterCap_RecordsSetterAndNullClears()
    {
        VocabularyLearnerSettings settings = VocabularyLearnerSettings.Create(Guid.NewGuid(), newWordsPerDay: 5).Value;

        settings.SetSupporterCap(3, _supporter).IsSuccess.Should().BeTrue();
        settings.SupporterCapSetBy.Should().Be(_supporter);

        settings.SetSupporterCap(null, _supporter).IsSuccess.Should().BeTrue();
        settings.SupporterNewWordCap.Should().BeNull();
        settings.SupporterCapSetBy.Should().BeNull();
        settings.NewWordsPerDay.Should().Be(5);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public void VocabularyLearnerSettings_SetSupporterCap_OutOfRangeRejected(int cap)
    {
        VocabularyLearnerSettings settings = VocabularyLearnerSettings.Create(Guid.NewGuid(), null).Value;

        settings.SetSupporterCap(cap, _supporter).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void VocabularyLearnerSettings_ClearSupporterCapSetBy_RevokedSupporterFallsBackToOwnCap()
    {
        VocabularyLearnerSettings settings = VocabularyLearnerSettings.Create(Guid.NewGuid(), newWordsPerDay: 6).Value;
        settings.SetSupporterCap(2, _supporter);

        settings.ClearSupporterCapSetBy(Guid.NewGuid()).Should().BeFalse();
        settings.ClearSupporterCapSetBy(_supporter).Should().BeTrue();

        _policy.GetCap(0, settings.SupporterNewWordCap, settings.NewWordsPerDay).Should().Be(6);
    }
}
