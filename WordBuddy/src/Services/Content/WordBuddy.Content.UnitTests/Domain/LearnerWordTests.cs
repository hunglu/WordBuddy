using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class LearnerWordTests
{
    [Fact]
    public void LearnerWord_Constructor_SetsAddedByLearnerAndNoPersonalContext()
    {
        Guid senseId = Guid.NewGuid();

        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), senseId, isAuthor: true);

        link.SenseId.Should().Be(senseId);
        link.AddedBy.Should().Be(LearnerWordAddedBy.Learner);
        link.PersonalContext.Should().BeNull();
    }

    [Fact]
    public void LearnerWord_ConstructorWithSense_SetsAddedByLearnerAndNoPersonalContext()
    {
        Sense sense = TestWords.Learner(Guid.NewGuid());

        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), sense, isAuthor: false);

        link.SenseId.Should().Be(sense.Id);
        link.Sense.Should().BeSameAs(sense);
        link.AddedBy.Should().Be(LearnerWordAddedBy.Learner);
        link.PersonalContext.Should().BeNull();
    }

    [Fact]
    public void LearnerWord_ApproveForChild_SetsApproval()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: false);
        Guid supporterId = Guid.NewGuid();

        Result result = link.ApproveForChild(supporterId);

        result.IsSuccess.Should().BeTrue();
        link.ChildApprovedAtUtc.Should().NotBeNull();
        link.ChildApprovedByUserId.Should().Be(supporterId);
    }

    [Fact]
    public void LearnerWord_ApproveForChild_Twice_ReturnsConflict()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: false);
        Guid firstSupporter = Guid.NewGuid();
        link.ApproveForChild(firstSupporter);

        Result result = link.ApproveForChild(Guid.NewGuid());

        result.Error.Type.Should().Be(ErrorType.Conflict);
        link.ChildApprovedByUserId.Should().Be(firstSupporter);
    }

    [Fact]
    public void LearnerWord_RequireChildApproval_SetsFlag()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: false);

        link.RequireChildApproval();

        link.RequiresChildApproval.Should().BeTrue();
    }

    [Fact]
    public void LearnerWord_SetPersonalContext_Text_StoresTrimmedText()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: true);

        Result result = link.SetPersonalContext("  at the zoo  ");

        result.IsSuccess.Should().BeTrue();
        link.PersonalContext.Should().Be("at the zoo");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LearnerWord_SetPersonalContext_BlankText_ClearsNote(string? blank)
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: true);
        link.SetPersonalContext("old note");

        Result result = link.SetPersonalContext(blank);

        result.IsSuccess.Should().BeTrue();
        link.PersonalContext.Should().BeNull();
    }

    [Fact]
    public void LearnerWord_SetPersonalContext_MaxLength_Succeeds()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: true);
        string text = new('a', LearnerWord.PersonalContextMaxLength);

        Result result = link.SetPersonalContext(text);

        result.IsSuccess.Should().BeTrue();
        link.PersonalContext.Should().Be(text);
    }

    [Fact]
    public void LearnerWord_SetPersonalContext_TooLong_FailsWithoutTextInMessageAndKeepsOldNote()
    {
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isAuthor: true);
        link.SetPersonalContext("old note");
        string text = new('b', LearnerWord.PersonalContextMaxLength + 1);

        Result result = link.SetPersonalContext(text);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("LearnerWord.PersonalContextTooLong");
        result.Error.Description.Should().NotContain(text);
        link.PersonalContext.Should().Be("old note");
    }
}
