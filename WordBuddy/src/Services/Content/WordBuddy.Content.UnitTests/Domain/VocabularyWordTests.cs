using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class VocabularyWordTests
{
    [Fact]
    public void VocabularyWord_ComputeContentHash_IgnoresCaseAndSurroundingSpaces()
    {
        string a = VocabularyWord.ComputeContentHash("Apple", "A fruit", "I ate an apple.");
        string b = VocabularyWord.ComputeContentHash("  APPLE ", "a fruit ", " i ate an APPLE.");

        a.Should().Be(b);
    }

    [Fact]
    public void VocabularyWord_ComputeContentHash_DiffersWhenDefinitionDiffers()
    {
        string river = VocabularyWord.ComputeContentHash("bank", "side of a river", null);
        string money = VocabularyWord.ComputeContentHash("bank", "a place for money", null);

        river.Should().NotBe(money);
    }

    [Fact]
    public void VocabularyWord_ComputeContentHash_TreatsNullExampleAsEmpty()
    {
        VocabularyWord.ComputeContentHash("apple", "a fruit", null)
            .Should().Be(VocabularyWord.ComputeContentHash("apple", "a fruit", "  "));
    }

    [Fact]
    public void VocabularyWord_ComputeContentHash_ReturnsUpperCaseHexSha256()
    {
        string hash = VocabularyWord.ComputeContentHash("apple", "a fruit", null);

        hash.Should().HaveLength(64).And.MatchRegex("^[0-9A-F]{64}$");
    }

    [Fact]
    public void VocabularyWord_CreateLearner_SetsLearnerDefaults()
    {
        Guid ownerId = Guid.NewGuid();

        Result<VocabularyWord> result = VocabularyWord.CreateLearner(Guid.NewGuid(), ownerId, AgeGroup.Child, " apple ", "a fruit", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Should().Be(VocabularySource.Learner);
        result.Value.OwnerUserId.Should().Be(ownerId);
        result.Value.OwnerAgeGroup.Should().Be(AgeGroup.Child);
        result.Value.ShareStatus.Should().Be(VocabularyShareStatus.Private);
        result.Value.VisibleToChildren.Should().BeFalse();
        result.Value.NormalizedWord.Should().Be("APPLE");
        result.Value.ContentHash.Should().Be(VocabularyWord.ComputeContentHash("apple", "a fruit", null));
    }

    [Fact]
    public void VocabularyWord_CreateLearner_RejectsSystemOwner()
    {
        Result<VocabularyWord> result = VocabularyWord.CreateLearner(Guid.NewGuid(), SystemOwner.UserId, AgeGroup.Adult, "apple", "a fruit", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void VocabularyWord_CreateLearner_RejectsEmptyOwner()
    {
        Result<VocabularyWord> result = VocabularyWord.CreateLearner(Guid.NewGuid(), Guid.Empty, AgeGroup.Adult, "apple", "a fruit", null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void VocabularyWord_CreateSystem_IsOwnedBySystemAndPrivate()
    {
        Guid id = Guid.NewGuid();

        VocabularyWord word = VocabularyWord.CreateSystem(id, "Dog", "An animal.", "The dog barked.");

        word.Id.Should().Be(id);
        word.Source.Should().Be(VocabularySource.System);
        word.OwnerUserId.Should().Be(SystemOwner.UserId);
        word.OwnerAgeGroup.Should().BeNull();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }

    [Fact]
    public void VocabularyWord_RequestShare_FromPrivate_MovesToPendingReview()
    {
        VocabularyWord word = TestWords.Learner(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void VocabularyWord_RequestShare_FromRejected_MovesToPendingReview()
    {
        VocabularyWord word = TestWords.Pending(Guid.NewGuid());
        word.Reject(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void VocabularyWord_RequestShare_FromPendingReview_ReturnsConflict()
    {
        VocabularyWord word = TestWords.Pending(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidShareRequest");
    }

    [Fact]
    public void VocabularyWord_RequestShare_FromShared_ReturnsConflict()
    {
        VocabularyWord word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void VocabularyWord_RequestShare_SystemWord_ReturnsConflict()
    {
        VocabularyWord word = TestWords.System();

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidShareRequest");
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }

    [Fact]
    public void VocabularyWord_Approve_FromPendingReview_SetsSharedAndModerationFields()
    {
        VocabularyWord word = TestWords.Pending(Guid.NewGuid());
        Guid moderatorId = Guid.NewGuid();

        Result result = word.Approve(visibleToChildren: true, moderatorId);

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        word.VisibleToChildren.Should().BeTrue();
        word.ModeratedByUserId.Should().Be(moderatorId);
        word.ModeratedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void VocabularyWord_Approve_FromPrivate_ReturnsConflict()
    {
        VocabularyWord word = TestWords.Learner(Guid.NewGuid());

        Result result = word.Approve(visibleToChildren: true, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidApproval");
    }

    [Fact]
    public void VocabularyWord_Reject_FromPendingReview_SetsRejectedAndNotChildVisible()
    {
        VocabularyWord word = TestWords.Pending(Guid.NewGuid());

        Result result = word.Reject(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Rejected);
        word.VisibleToChildren.Should().BeFalse();
    }

    [Fact]
    public void VocabularyWord_Reject_FromShared_ReturnsConflict()
    {
        VocabularyWord word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);

        Result result = word.Reject(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidRejection");
    }

    [Fact]
    public void VocabularyWord_IsVisibleTo_ChildAndNonChildVisibleSharedWord_ReturnsFalse()
    {
        VocabularyWord word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);

        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Child).Should().BeFalse();
        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Adult).Should().BeTrue();
    }

    [Fact]
    public void VocabularyWord_IsVisibleTo_OtherLearnersPrivateWord_ReturnsFalse()
    {
        VocabularyWord word = TestWords.Learner(Guid.NewGuid());

        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Adult).Should().BeFalse();
    }
}
