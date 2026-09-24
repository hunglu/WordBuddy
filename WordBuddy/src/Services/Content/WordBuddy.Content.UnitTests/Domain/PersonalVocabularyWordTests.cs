using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class PersonalVocabularyWordTests
{
    private static PersonalVocabularyWord CreateWord() =>
        new(Guid.NewGuid(), Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", "I ate an apple.");

    [Fact]
    public void RequestShare_FromPrivate_MovesToPendingReview()
    {
        PersonalVocabularyWord word = CreateWord();

        Result result = word.RequestShare();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void RequestShare_FromRejected_MovesToPendingReview()
    {
        PersonalVocabularyWord word = CreateWord();
        word.RequestShare();
        word.Reject(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void RequestShare_FromPendingReview_ReturnsFailure()
    {
        PersonalVocabularyWord word = CreateWord();
        word.RequestShare();

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void Approve_FromPendingReview_MovesToSharedAndSetsVisibility()
    {
        PersonalVocabularyWord word = CreateWord();
        word.RequestShare();
        Guid moderatorId = Guid.NewGuid();

        Result result = word.Approve(true, moderatorId);

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        word.VisibleToChildren.Should().BeTrue();
        word.ModeratedByUserId.Should().Be(moderatorId);
        word.ModeratedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Approve_FromPrivate_ReturnsFailure()
    {
        PersonalVocabularyWord word = CreateWord();

        Result result = word.Approve(true, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }

    [Fact]
    public void Approve_AlreadyShared_ReturnsFailure()
    {
        PersonalVocabularyWord word = CreateWord();
        word.RequestShare();
        word.Approve(true, Guid.NewGuid());

        Result result = word.Approve(false, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_FromPendingReview_MovesToRejected()
    {
        PersonalVocabularyWord word = CreateWord();
        word.RequestShare();
        Guid moderatorId = Guid.NewGuid();

        Result result = word.Reject(moderatorId);

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Rejected);
        word.VisibleToChildren.Should().BeFalse();
        word.ModeratedByUserId.Should().Be(moderatorId);
    }

    [Fact]
    public void Reject_FromPrivate_ReturnsFailure()
    {
        PersonalVocabularyWord word = CreateWord();

        Result result = word.Reject(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }
}
