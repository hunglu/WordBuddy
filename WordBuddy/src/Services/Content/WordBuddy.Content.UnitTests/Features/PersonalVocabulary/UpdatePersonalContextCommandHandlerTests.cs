using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalContext;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class UpdatePersonalContextCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly UpdatePersonalContextCommandValidator _validator = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Sense _sense = TestWords.System();

    public UpdatePersonalContextCommandHandlerTests()
    {
        _repository
            .Setup(r => r.SaveLinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private UpdatePersonalContextCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, Mock.Of<ILogger<UpdatePersonalContextCommandHandler>>());

    private LearnerWord SetupLink(string? note = null)
    {
        LearnerWord link = new(Guid.NewGuid(), _userId, _sense, isAuthor: false);
        link.SetPersonalContext(note);
        _repository
            .Setup(r => r.GetLinkAsync(_userId, _sense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(link));
        return link;
    }

    [Theory]
    [InlineData(AgeGroup.Adult)]
    [InlineData(AgeGroup.Child)]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_Text_SetsNoteAndSaves(AgeGroup _)
    {
        // The handler has no age branch: both groups use the same path through the caller's own link.
        LearnerWord link = SetupLink();

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalContextCommand(_sense.Id, _userId, " near the lake "));

        result.IsSuccess.Should().BeTrue();
        link.PersonalContext.Should().Be("near the lake");
        _repository.Verify(r => r.SaveLinkAsync(link, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_BlankText_ClearsNote(string? blank)
    {
        LearnerWord link = SetupLink("old");

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalContextCommand(_sense.Id, _userId, blank));

        result.IsSuccess.Should().BeTrue();
        link.PersonalContext.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_NotLinked_ReturnsNotFound()
    {
        _repository
            .Setup(r => r.GetLinkAsync(_userId, _sense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "no")));

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalContextCommand(_sense.Id, _userId, "x"));

        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.SaveLinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_PaddedTextWithin500AfterTrim_Succeeds()
    {
        LearnerWord link = SetupLink();

        Result result = await CreateHandler().HandleAsync(
            new UpdatePersonalContextCommand(_sense.Id, _userId, new string(' ', 10) + new string('x', 495)));

        result.IsSuccess.Should().BeTrue();
        link.PersonalContext.Should().HaveLength(495);
    }

    [Fact]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_TooLong_ReturnsValidationFailure()
    {
        LearnerWord link = SetupLink("old");

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalContextCommand(_sense.Id, _userId, new string('x', 501)));

        result.Error.Type.Should().Be(ErrorType.Validation);
        link.PersonalContext.Should().Be("old");
        _repository.Verify(r => r.SaveLinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_EmptyIds_ReturnsValidationFailure()
    {
        Result result = await CreateHandler().HandleAsync(new UpdatePersonalContextCommand(Guid.Empty, Guid.Empty, "x"));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdatePersonalContextCommandHandler_HandleAsync_Note_NeverLogged()
    {
        SetupLink();
        CapturingLogger<UpdatePersonalContextCommandHandler> logger = new();
        UpdatePersonalContextCommandHandler handler = new(_repository.Object, _validator, logger);

        await handler.HandleAsync(new UpdatePersonalContextCommand(_sense.Id, _userId, "SECRET-NOTE-123"));
        await handler.HandleAsync(new UpdatePersonalContextCommand(_sense.Id, _userId, new string('S', 501)));

        logger.Entries.Should().NotBeEmpty();
        logger.Entries.Should().NotContain(e => e.Contains("SECRET-NOTE-123") || e.Contains(new string('S', 501)));
    }
}
