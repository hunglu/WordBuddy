using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class SenseTests
{
    [Fact]
    public void Sense_AttachImage_SetsImageAssetId()
    {
        Sense sense = TestWords.System();
        Guid imageId = Guid.NewGuid();

        sense.AttachImage(imageId);

        sense.ImageAssetId.Should().Be(imageId);
    }

    [Fact]
    public void Sense_ComputeContentHash_IgnoresCaseAndSurroundingSpaces()
    {
        string a = Sense.ComputeContentHash("Apple", "A fruit", "I ate an apple.");
        string b = Sense.ComputeContentHash("  APPLE ", "a fruit ", " i ate an APPLE.");

        a.Should().Be(b);
    }

    [Fact]
    public void Sense_ComputeContentHash_DiffersWhenDefinitionDiffers()
    {
        string river = Sense.ComputeContentHash("bank", "side of a river", null);
        string money = Sense.ComputeContentHash("bank", "a place for money", null);

        river.Should().NotBe(money);
    }

    [Fact]
    public void Sense_ComputeContentHash_TreatsNullExampleAsEmpty()
    {
        Sense.ComputeContentHash("apple", "a fruit", null)
            .Should().Be(Sense.ComputeContentHash("apple", "a fruit", "  "));
    }

    [Fact]
    public void Sense_ComputeContentHash_ReturnsUpperCaseHexSha256()
    {
        string hash = Sense.ComputeContentHash("apple", "a fruit", null);

        hash.Should().HaveLength(64).And.MatchRegex("^[0-9A-F]{64}$");
    }

    /// <summary>Pins today's C# behaviour for <c>ß</c>: <see cref="string.ToUpperInvariant"/> leaves it
    /// unchanged (no "SS" expansion), so "straße" and "STRASSE" hash differently. SQL Server's
    /// <c>UPPER</c> may treat such letters differently — an accepted limitation (see
    /// <see cref="Sense.ComputeContentHash"/>).</summary>
    [Fact]
    public void Sense_ComputeContentHash_SharpS_IsNotExpandedToSs()
    {
        Sense.NormalizeWord(" straße ").Should().Be("STRAßE");

        Sense.ComputeContentHash("straße", "a street", null)
            .Should().Be(Sense.ComputeContentHash("STRAßE", "A STREET", null))
            .And.NotBe(Sense.ComputeContentHash("STRASSE", "a street", null));
    }

    [Fact]
    public void Sense_CreateLearner_SetsLearnerDefaults()
    {
        Guid ownerId = Guid.NewGuid();

        Result<Sense> result = Sense.CreateLearner(Guid.NewGuid(), Guid.NewGuid(), ownerId, AgeGroup.Child, " apple ", "a fruit", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Source.Should().Be(VocabularySource.Learner);
        result.Value.OwnerUserId.Should().Be(ownerId);
        result.Value.OwnerAgeGroup.Should().Be(AgeGroup.Child);
        result.Value.ShareStatus.Should().Be(VocabularyShareStatus.Private);
        result.Value.VisibleToChildren.Should().BeFalse();
        result.Value.ContentHash.Should().Be(Sense.ComputeContentHash("apple", "a fruit", null));
    }

    /// <summary>Pins the hash for a known input. The value was taken from the code before the
    /// Lexeme/Sense split; a change here would break dedupe against stored hashes.</summary>
    [Fact]
    public void Sense_ComputeContentHash_IsUnchangedForKnownInput()
    {
        Sense.ComputeContentHash("Apple", "A fruit", "I ate an apple.")
            .Should().Be("FA94935270D0B3098AD4F6549EAAEC6093ED08A451ED9BD94A1F46C9C491F340");
    }

    [Fact]
    public void Sense_CreateSystem_SetsLexemeId()
    {
        Guid lexemeId = Guid.NewGuid();

        Sense word = Sense.CreateSystem(Guid.NewGuid(), lexemeId, "Dog", "An animal.", "The dog barked.");

        word.LexemeId.Should().Be(lexemeId);
    }

    [Fact]
    public void Sense_CreateLearner_SetsLexemeId()
    {
        Guid lexemeId = Guid.NewGuid();

        Result<Sense> result = Sense.CreateLearner(Guid.NewGuid(), lexemeId, Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null);

        result.IsSuccess.Should().BeTrue();
        result.Value.LexemeId.Should().Be(lexemeId);
    }

    [Fact]
    public void Sense_CreateLearner_RejectsSystemOwner()
    {
        Result<Sense> result = Sense.CreateLearner(Guid.NewGuid(), Guid.NewGuid(), SystemOwner.UserId, AgeGroup.Adult, "apple", "a fruit", null);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Sense_CreateLearner_RejectsEmptyOwner()
    {
        Result<Sense> result = Sense.CreateLearner(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, AgeGroup.Adult, "apple", "a fruit", null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Sense_CreateSystem_IsOwnedBySystemAndPrivate()
    {
        Guid id = Guid.NewGuid();

        Sense word = Sense.CreateSystem(id, Guid.NewGuid(), "Dog", "An animal.", "The dog barked.");

        word.Id.Should().Be(id);
        word.Source.Should().Be(VocabularySource.System);
        word.OwnerUserId.Should().Be(SystemOwner.UserId);
        word.OwnerAgeGroup.Should().BeNull();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }

    [Fact]
    public void Sense_RequestShare_FromPrivate_MovesToPendingReview()
    {
        Sense word = TestWords.Learner(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void Sense_RequestShare_FromRejected_MovesToPendingReview()
    {
        Sense word = TestWords.Pending(Guid.NewGuid());
        word.Reject(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
    }

    [Fact]
    public void Sense_RequestShare_FromPendingReview_ReturnsConflict()
    {
        Sense word = TestWords.Pending(Guid.NewGuid());

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidShareRequest");
    }

    [Fact]
    public void Sense_RequestShare_FromShared_ReturnsConflict()
    {
        Sense word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Sense_RequestShare_SystemWord_ReturnsConflict()
    {
        Sense word = TestWords.System();

        Result result = word.RequestShare();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidShareRequest");
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }

    [Fact]
    public void Sense_Approve_FromPendingReview_SetsSharedAndModerationFields()
    {
        Sense word = TestWords.Pending(Guid.NewGuid());
        Guid moderatorId = Guid.NewGuid();

        Result result = word.Approve(visibleToChildren: true, moderatorId);

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        word.VisibleToChildren.Should().BeTrue();
        word.ModeratedByUserId.Should().Be(moderatorId);
        word.ModeratedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Sense_Approve_FromPrivate_ReturnsConflict()
    {
        Sense word = TestWords.Learner(Guid.NewGuid());

        Result result = word.Approve(visibleToChildren: true, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidApproval");
    }

    [Fact]
    public void Sense_Reject_FromPendingReview_SetsRejectedAndNotChildVisible()
    {
        Sense word = TestWords.Pending(Guid.NewGuid());

        Result result = word.Reject(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Rejected);
        word.VisibleToChildren.Should().BeFalse();
    }

    [Fact]
    public void Sense_Reject_FromShared_ReturnsConflict()
    {
        Sense word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);

        Result result = word.Reject(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidRejection");
    }

    [Fact]
    public void Sense_IsVisibleTo_ChildAndNonChildVisibleSharedWord_ReturnsFalse()
    {
        Sense word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);

        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Child).Should().BeFalse();
        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Adult).Should().BeTrue();
    }

    [Fact]
    public void Sense_IsVisibleTo_OtherLearnersPrivateWord_ReturnsFalse()
    {
        Sense word = TestWords.Learner(Guid.NewGuid());

        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Adult).Should().BeFalse();
    }

    [Fact]
    public void Sense_TransferToSystem_SetsSystemOwnerAndKeepsShared()
    {
        Sense word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        DateTime? moderatedAt = word.ModeratedAtUtc;
        Guid? moderatedBy = word.ModeratedByUserId;

        Result result = word.TransferToSystem();

        result.IsSuccess.Should().BeTrue();
        word.Source.Should().Be(VocabularySource.System);
        word.OwnerUserId.Should().Be(SystemOwner.UserId);
        word.OwnerAgeGroup.Should().BeNull();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        word.VisibleToChildren.Should().BeTrue();
        word.ModeratedAtUtc.Should().Be(moderatedAt);
        word.ModeratedByUserId.Should().Be(moderatedBy);
    }

    [Theory]
    [InlineData("Private")]
    [InlineData("PendingReview")]
    [InlineData("Rejected")]
    [InlineData("System")]
    public void Sense_TransferToSystem_ReturnsConflictWhenNotShared(string state)
    {
        Sense word = state switch
        {
            "Private" => TestWords.Learner(Guid.NewGuid()),
            "PendingReview" => TestWords.Pending(Guid.NewGuid()),
            "Rejected" => Rejected(),
            _ => TestWords.System(),
        };
        Guid ownerBefore = word.OwnerUserId;

        Result result = word.TransferToSystem();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        word.OwnerUserId.Should().Be(ownerBefore);
    }

    [Fact]
    public void Sense_TransferToSystem_ReturnsConflictWhenAlreadyTransferred()
    {
        Sense word = TestWords.Transferred(visibleToChildren: true);

        word.TransferToSystem().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Sense_CancelShareRequest_SetsPrivate()
    {
        Sense word = TestWords.Pending(Guid.NewGuid());

        Result result = word.CancelShareRequest();

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
    }

    [Theory]
    [InlineData(VocabularyShareStatus.Private)]
    [InlineData(VocabularyShareStatus.Shared)]
    [InlineData(VocabularyShareStatus.Rejected)]
    public void Sense_CancelShareRequest_ReturnsConflictWhenNotPendingReview(VocabularyShareStatus status)
    {
        Sense word = status switch
        {
            VocabularyShareStatus.Private => TestWords.Learner(Guid.NewGuid()),
            VocabularyShareStatus.Shared => TestWords.Shared(Guid.NewGuid(), visibleToChildren: true),
            _ => Rejected(),
        };

        Result result = word.CancelShareRequest();

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        word.ShareStatus.Should().Be(status);
    }

    [Fact]
    public void Sense_IsVisibleTo_HidesTransferredNonChildSafeWordFromChild()
    {
        Sense word = TestWords.Transferred(visibleToChildren: false);

        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Child).Should().BeFalse();
        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Adult).Should().BeTrue();
    }

    [Fact]
    public void Sense_IsVisibleTo_ShowsSystemLessonWordToChild()
    {
        Sense word = TestWords.System();

        word.IsVisibleTo(Guid.NewGuid(), AgeGroup.Child).Should().BeTrue();
    }

    private static Sense Rejected()
    {
        Sense word = TestWords.Pending(Guid.NewGuid());
        word.Reject(Guid.NewGuid());
        return word;
    }
}
