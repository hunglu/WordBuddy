using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.Features.Lessons.Commands.AttachLessonSense;
using WordBuddy.Content.Application.Features.Lessons.Commands.DeleteLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.DetachLessonSense;
using WordBuddy.Content.Application.Features.Lessons.Commands.PublishLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.ReorderLessonSenses;
using WordBuddy.Content.Application.Features.Lessons.Commands.UnpublishLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.UpdateLesson;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Lessons;

/// <summary>Admin lesson authoring handlers (WB-47): update, publish, unpublish, delete, attach,
/// detach, reorder. Repositories are mocked; the lessons and senses are real domain objects.</summary>
public class LessonCommandHandlerTests
{
    private readonly Mock<ILessonRepository> _lessons = new();
    private readonly Mock<IVocabularyWordRepository> _senses = new();

    public LessonCommandHandlerTests()
    {
        _lessons.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private Lesson SetupLesson(LessonType type = LessonType.Vocabulary, bool isPublished = false)
    {
        Lesson lesson = new(Guid.NewGuid(), "Animals", "Common animals.", type, Level.Beginner, AgeGroup.Child, isPublished);
        _lessons.Setup(r => r.GetByIdForUpdateAsync(lesson.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(lesson));
        return lesson;
    }

    private Guid SetupMissingLesson()
    {
        Guid id = Guid.NewGuid();
        _lessons.Setup(r => r.GetByIdForUpdateAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Lesson>(Error.NotFound("Lesson.NotFound", "missing")));
        return id;
    }

    private void SetupSense(Sense sense) =>
        _senses.Setup(r => r.GetByIdAsync(sense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(sense));

    // ---- UpdateLesson

    private UpdateLessonCommandHandler UpdateHandler() =>
        new(_lessons.Object, new UpdateLessonCommandValidator(), NullLogger<UpdateLessonCommandHandler>.Instance);

    [Fact]
    public async Task UpdateLessonCommandHandler_HandleAsync_ValidCommand_UpdatesAndSaves()
    {
        Lesson lesson = SetupLesson();

        Result result = await UpdateHandler().HandleAsync(new UpdateLessonCommand(lesson.Id, "Pets", "Pets at home.", Level.Advanced, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        lesson.Title.Should().Be("Pets");
        lesson.Level.Should().Be(Level.Advanced);
        lesson.TargetAgeGroup.Should().Be(AgeGroup.Adult);
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLessonCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = SetupMissingLesson();

        Result result = await UpdateHandler().HandleAsync(new UpdateLessonCommand(id, "Pets", "Pets at home.", Level.Beginner, AgeGroup.Child));

        result.Error.Type.Should().Be(ErrorType.NotFound);
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "text")]
    [InlineData("title", "")]
    public async Task UpdateLessonCommandHandler_HandleAsync_EmptyFields_ReturnsValidationError(string title, string description)
    {
        Lesson lesson = SetupLesson();

        Result result = await UpdateHandler().HandleAsync(new UpdateLessonCommand(lesson.Id, title, description, Level.Beginner, AgeGroup.Child));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLessonCommandHandler_HandleAsync_TitleTooLong_ReturnsValidationError()
    {
        Lesson lesson = SetupLesson();

        Result result = await UpdateHandler().HandleAsync(
            new UpdateLessonCommand(lesson.Id, new string('a', 201), "text", Level.Beginner, AgeGroup.Child));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    // ---- Publish / Unpublish

    [Fact]
    public async Task PublishLessonCommandHandler_HandleAsync_Draft_PublishesAndSaves()
    {
        Lesson lesson = SetupLesson(isPublished: false);

        Result result = await new PublishLessonCommandHandler(_lessons.Object, NullLogger<PublishLessonCommandHandler>.Instance)
            .HandleAsync(new PublishLessonCommand(lesson.Id));

        result.IsSuccess.Should().BeTrue();
        lesson.IsPublished.Should().BeTrue();
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishLessonCommandHandler_HandleAsync_AlreadyPublished_Succeeds()
    {
        Lesson lesson = SetupLesson(isPublished: true);

        Result result = await new PublishLessonCommandHandler(_lessons.Object, NullLogger<PublishLessonCommandHandler>.Instance)
            .HandleAsync(new PublishLessonCommand(lesson.Id));

        result.IsSuccess.Should().BeTrue();
        lesson.IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task PublishLessonCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = SetupMissingLesson();

        Result result = await new PublishLessonCommandHandler(_lessons.Object, NullLogger<PublishLessonCommandHandler>.Instance)
            .HandleAsync(new PublishLessonCommand(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UnpublishLessonCommandHandler_HandleAsync_Published_HidesAndSaves()
    {
        Lesson lesson = SetupLesson(isPublished: true);

        Result result = await new UnpublishLessonCommandHandler(_lessons.Object, NullLogger<UnpublishLessonCommandHandler>.Instance)
            .HandleAsync(new UnpublishLessonCommand(lesson.Id));

        result.IsSuccess.Should().BeTrue();
        lesson.IsPublished.Should().BeFalse();
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnpublishLessonCommandHandler_HandleAsync_AlreadyDraft_Succeeds()
    {
        Lesson lesson = SetupLesson(isPublished: false);

        Result result = await new UnpublishLessonCommandHandler(_lessons.Object, NullLogger<UnpublishLessonCommandHandler>.Instance)
            .HandleAsync(new UnpublishLessonCommand(lesson.Id));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UnpublishLessonCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = SetupMissingLesson();

        Result result = await new UnpublishLessonCommandHandler(_lessons.Object, NullLogger<UnpublishLessonCommandHandler>.Instance)
            .HandleAsync(new UnpublishLessonCommand(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    // ---- Delete

    [Fact]
    public async Task DeleteLessonCommandHandler_HandleAsync_ExistingLesson_RemovesAndSaves()
    {
        Lesson lesson = SetupLesson();

        Result result = await new DeleteLessonCommandHandler(_lessons.Object, NullLogger<DeleteLessonCommandHandler>.Instance)
            .HandleAsync(new DeleteLessonCommand(lesson.Id));

        result.IsSuccess.Should().BeTrue();
        _lessons.Verify(r => r.Remove(lesson), Times.Once);
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLessonCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFoundAndRemovesNothing()
    {
        Guid id = SetupMissingLesson();

        Result result = await new DeleteLessonCommandHandler(_lessons.Object, NullLogger<DeleteLessonCommandHandler>.Instance)
            .HandleAsync(new DeleteLessonCommand(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
        _lessons.Verify(r => r.Remove(It.IsAny<Lesson>()), Times.Never);
    }

    // ---- Attach

    private AttachLessonSenseCommandHandler AttachHandler() =>
        new(_lessons.Object, _senses.Object, new AttachLessonSenseCommandValidator(), NullLogger<AttachLessonSenseCommandHandler>.Instance);

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_SystemSense_Attaches()
    {
        Lesson lesson = SetupLesson();
        Sense sense = TestWords.System("Dog");
        SetupSense(sense);

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(lesson.Id, sense.Id));

        result.IsSuccess.Should().BeTrue();
        lesson.VocabularyWords.Should().ContainSingle(l => l.SenseId == sense.Id);
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_SharedSense_Attaches(bool visibleToChildren)
    {
        Lesson lesson = SetupLesson();
        Sense sense = TestWords.Shared(Guid.NewGuid(), visibleToChildren);
        SetupSense(sense);

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(lesson.Id, sense.Id));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_PrivateSense_IsRejected()
    {
        await AssertNotEligibleAsync(TestWords.Learner(Guid.NewGuid()));
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_PendingSense_IsRejected()
    {
        await AssertNotEligibleAsync(TestWords.Pending(Guid.NewGuid()));
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_RejectedSense_IsRejected()
    {
        Sense sense = TestWords.Pending(Guid.NewGuid());
        sense.Reject(Guid.NewGuid());

        await AssertNotEligibleAsync(sense);
    }

    private async Task AssertNotEligibleAsync(Sense sense)
    {
        Lesson lesson = SetupLesson();
        SetupSense(sense);

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(lesson.Id, sense.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lesson.SenseNotEligible");
        result.Error.Type.Should().Be(ErrorType.Validation);
        lesson.VocabularyWords.Should().BeEmpty();
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_Duplicate_ReturnsConflict()
    {
        Lesson lesson = SetupLesson();
        Sense sense = TestWords.System("Dog");
        SetupSense(sense);
        lesson.AttachSense(sense);

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(lesson.Id, sense.Id));

        result.Error.Code.Should().Be("Lesson.SenseAlreadyAttached");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_GrammarLesson_ReturnsValidationError()
    {
        Lesson lesson = SetupLesson(LessonType.Grammar);
        Sense sense = TestWords.System("Dog");
        SetupSense(sense);

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(lesson.Id, sense.Id));

        result.Error.Code.Should().Be("Lesson.NotVocabulary");
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_UnknownSense_ReturnsSenseNotFound()
    {
        Lesson lesson = SetupLesson();
        Guid senseId = Guid.NewGuid();
        _senses.Setup(r => r.GetByIdAsync(senseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Sense>(Error.NotFound("PersonalVocabularyWord.NotFound", "missing")));

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(lesson.Id, senseId));

        result.Error.Code.Should().Be("Sense.NotFound");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task AttachLessonSenseCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = SetupMissingLesson();

        Result result = await AttachHandler().HandleAsync(new AttachLessonSenseCommand(id, Guid.NewGuid()));

        result.Error.Code.Should().Be("Lesson.NotFound");
    }

    // ---- Detach

    private DetachLessonSenseCommandHandler DetachHandler() =>
        new(_lessons.Object, NullLogger<DetachLessonSenseCommandHandler>.Instance);

    [Fact]
    public async Task DetachLessonSenseCommandHandler_HandleAsync_Attached_RemovesAndRenumbers()
    {
        Lesson lesson = SetupLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        Sense bird = TestWords.System("Bird");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);
        lesson.AttachSense(bird);

        Result result = await DetachHandler().HandleAsync(new DetachLessonSenseCommand(lesson.Id, dog.Id));

        result.IsSuccess.Should().BeTrue();
        lesson.VocabularyWords.OrderBy(l => l.SortOrder).Select(l => (l.SenseId, l.SortOrder))
            .Should().Equal((cat.Id, 0), (bird.Id, 1));
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DetachLessonSenseCommandHandler_HandleAsync_NotAttached_ReturnsNotFound()
    {
        Lesson lesson = SetupLesson();

        Result result = await DetachHandler().HandleAsync(new DetachLessonSenseCommand(lesson.Id, Guid.NewGuid()));

        result.Error.Code.Should().Be("Lesson.SenseNotAttached");
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DetachLessonSenseCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = SetupMissingLesson();

        Result result = await DetachHandler().HandleAsync(new DetachLessonSenseCommand(id, Guid.NewGuid()));

        result.Error.Code.Should().Be("Lesson.NotFound");
    }

    // ---- Reorder

    private ReorderLessonSensesCommandHandler ReorderHandler() =>
        new(_lessons.Object, new ReorderLessonSensesCommandValidator(), NullLogger<ReorderLessonSensesCommandHandler>.Instance);

    [Fact]
    public async Task ReorderLessonSensesCommandHandler_HandleAsync_ExactSet_Reorders()
    {
        Lesson lesson = SetupLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);

        Result result = await ReorderHandler().HandleAsync(new ReorderLessonSensesCommand(lesson.Id, [cat.Id, dog.Id]));

        result.IsSuccess.Should().BeTrue();
        lesson.VocabularyWords.OrderBy(l => l.SortOrder).Select(l => l.SenseId).Should().Equal(cat.Id, dog.Id);
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReorderLessonSensesCommandHandler_HandleAsync_NotExactlyAttachedSet_ReturnsInvalidOrder()
    {
        Lesson lesson = SetupLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);

        Result result = await ReorderHandler().HandleAsync(new ReorderLessonSensesCommand(lesson.Id, [dog.Id]));

        result.Error.Code.Should().Be("Lesson.InvalidOrder");
        _lessons.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReorderLessonSensesCommandHandler_HandleAsync_EmptyIdInList_ReturnsValidationError()
    {
        Lesson lesson = SetupLesson();

        Result result = await ReorderHandler().HandleAsync(new ReorderLessonSensesCommand(lesson.Id, [Guid.Empty]));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _lessons.Verify(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReorderLessonSensesCommandHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = SetupMissingLesson();

        Result result = await ReorderHandler().HandleAsync(new ReorderLessonSensesCommand(id, []));

        result.Error.Code.Should().Be("Lesson.NotFound");
    }
}
