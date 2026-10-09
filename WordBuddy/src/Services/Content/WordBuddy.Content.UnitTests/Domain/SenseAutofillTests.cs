using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

/// <summary>WB-25: auto-filled senses and child visibility.</summary>
public class SenseAutofillTests
{
    private static Sense AutoFill(string definition = "a red fruit", IReadOnlyList<string>? examples = null) =>
        Sense.CreateAutoFill(Guid.NewGuid(), Guid.NewGuid(), "apple", definition, examples ?? ["I eat an apple.", "Apples are red."]).Value;

    [Fact]
    public void Sense_CreateAutoFill_IsSystemSharedAndHiddenFromChildren()
    {
        Sense sense = AutoFill();

        sense.Origin.Should().Be(SenseOrigin.AutoFill);
        sense.Source.Should().Be(VocabularySource.System);
        sense.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        sense.VisibleToChildren.Should().BeFalse();
        sense.OwnerUserId.Should().Be(SystemOwner.UserId);
    }

    [Fact]
    public void Sense_CreateAutoFill_FirstExampleIsExampleAndHashUnchanged()
    {
        Sense sense = AutoFill(examples: ["One.", "Two."]);

        sense.Example.Should().Be("One.");
        sense.Examples.Should().Equal("One.", "Two.");
        sense.ContentHash.Should().Be(Sense.ComputeContentHash("apple", "a red fruit", "One."));
    }

    [Fact]
    public void Sense_CreateAutoFill_ListsNeverNull()
    {
        Sense sense = Sense.CreateAutoFill(Guid.NewGuid(), Guid.NewGuid(), "apple", "a fruit", null).Value;

        sense.Examples.Should().BeEmpty();
        sense.Collocations.Should().BeEmpty();
        sense.Synonyms.Should().BeEmpty();
        sense.Antonyms.Should().BeEmpty();
        sense.TopicTags.Should().BeEmpty();
        sense.Example.Should().BeNull();
    }

    [Fact]
    public void Sense_CreateAutoFill_TooManyExamples_FailsValidation()
    {
        Result<Sense> result = Sense.CreateAutoFill(Guid.NewGuid(), Guid.NewGuid(), "apple", "a fruit", ["1", "2", "3", "4"]);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Sense_CreateAutoFill_EmptyDefinition_FailsValidation()
    {
        Result<Sense> result = Sense.CreateAutoFill(Guid.NewGuid(), Guid.NewGuid(), "apple", "  ", []);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Sense_CreateAutoFill_LongRegisterNote_FailsValidation()
    {
        Result<Sense> result = Sense.CreateAutoFill(
            Guid.NewGuid(), Guid.NewGuid(), "apple", "a fruit", [], registerNote: new string('x', Sense.RegisterNoteMaxLength + 1));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Sense_CreateSystem_OriginIsManualAndListsEmpty()
    {
        Sense sense = TestWords.System();

        sense.Origin.Should().Be(SenseOrigin.Manual);
        sense.Examples.Should().BeEmpty();
    }

    [Fact]
    public void Sense_ApproveForChildren_MakesSenseVisibleToEveryChild()
    {
        Sense sense = AutoFill();
        Guid adminId = Guid.NewGuid();

        Result result = sense.ApproveForChildren(adminId);

        result.IsSuccess.Should().BeTrue();
        sense.VisibleToChildren.Should().BeTrue();
        sense.ModeratedByUserId.Should().Be(adminId);
        sense.IsVisibleTo(Guid.NewGuid(), AgeGroup.Child).Should().BeTrue();
    }

    [Fact]
    public void Sense_ApproveForChildren_Twice_ReturnsConflict()
    {
        Sense sense = AutoFill();
        sense.ApproveForChildren(Guid.NewGuid());

        Result result = sense.ApproveForChildren(Guid.NewGuid());

        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Sense_ApproveForChildren_ManualSense_ReturnsConflict()
    {
        Result result = TestWords.System().ApproveForChildren(Guid.NewGuid());

        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Sense_IsVisibleTo_AdultSeesUnapprovedAutoFill()
    {
        AutoFill().IsVisibleTo(Guid.NewGuid(), AgeGroup.Adult, callerLink: null).Should().BeTrue();
    }

    [Fact]
    public void Sense_IsVisibleTo_ChildWithoutApproval_DoesNotSeeAutoFill()
    {
        Guid childId = Guid.NewGuid();
        Sense sense = AutoFill();
        LearnerWord link = new(Guid.NewGuid(), childId, sense, isAuthor: false);

        sense.IsVisibleTo(childId, AgeGroup.Child, link).Should().BeFalse();
        sense.IsAwaitingChildApproval(AgeGroup.Child, link).Should().BeTrue();
    }

    [Fact]
    public void Sense_IsVisibleTo_ChildWithOwnApprovedLink_SeesAutoFill()
    {
        Guid childId = Guid.NewGuid();
        Sense sense = AutoFill();
        LearnerWord link = new(Guid.NewGuid(), childId, sense, isAuthor: false);
        link.ApproveForChild(Guid.NewGuid());

        sense.IsVisibleTo(childId, AgeGroup.Child, link).Should().BeTrue();
        sense.IsAwaitingChildApproval(AgeGroup.Child, link).Should().BeFalse();
    }

    [Fact]
    public void Sense_IsVisibleTo_ChildWithAnotherChildsApprovedLink_DoesNotSeeAutoFill()
    {
        Sense sense = AutoFill();
        LearnerWord otherLink = new(Guid.NewGuid(), Guid.NewGuid(), sense, isAuthor: false);
        otherLink.ApproveForChild(Guid.NewGuid());

        sense.IsVisibleTo(Guid.NewGuid(), AgeGroup.Child, otherLink).Should().BeFalse();
    }

    [Fact]
    public void Sense_IsAwaitingChildApproval_AdultNever()
    {
        AutoFill().IsAwaitingChildApproval(AgeGroup.Adult, callerLink: null).Should().BeFalse();
    }

    [Fact]
    public void Sense_MoveToLexeme_SystemSense_Moves()
    {
        Sense sense = TestWords.System();
        Guid lexemeId = Guid.NewGuid();

        sense.MoveToLexeme(lexemeId).IsSuccess.Should().BeTrue();

        sense.LexemeId.Should().Be(lexemeId);
    }

    [Fact]
    public void Sense_MoveToLexeme_PrivateLearnerSense_ReturnsConflict()
    {
        Sense sense = TestWords.Learner(Guid.NewGuid());
        Guid original = sense.LexemeId;

        sense.MoveToLexeme(Guid.NewGuid()).Error.Type.Should().Be(ErrorType.Conflict);

        sense.LexemeId.Should().Be(original);
    }

    [Fact]
    public void Sense_AddTranslation_SecondSameLocale_ReturnsConflict()
    {
        Sense sense = AutoFill();
        sense.AddTranslation(SenseTranslation.Create(Guid.NewGuid(), sense.Id, "vi", "táo").Value);

        Result result = sense.AddTranslation(SenseTranslation.Create(Guid.NewGuid(), sense.Id, "vi", "trái táo").Value);

        result.Error.Type.Should().Be(ErrorType.Conflict);
        sense.Translations.Should().ContainSingle();
    }
}
