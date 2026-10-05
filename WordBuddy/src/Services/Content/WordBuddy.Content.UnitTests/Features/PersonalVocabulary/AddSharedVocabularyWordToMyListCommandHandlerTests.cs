using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class AddSharedVocabularyWordToMyListCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly AddSharedVocabularyWordToMyListCommandValidator _validator = new();
    private readonly Mock<ILogger<AddSharedVocabularyWordToMyListCommandHandler>> _logger = new();

    public AddSharedVocabularyWordToMyListCommandHandlerTests()
    {
        _repository
            .Setup(r => r.GetLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));
        _repository
            .Setup(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private AddSharedVocabularyWordToMyListCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    private void SetupSource(Sense source) =>
        _repository.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(source));

    private void VerifyNoLink() =>
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_SharedWord_CreatesNonAuthorLinkAndReturnsSharedId()
    {
        Sense source = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupSource(source);
        Guid requesterId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, requesterId, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(source.Id);
        _repository.Verify(
            r => r.LinkAsync(
                It.Is<LearnerWord>(l => l.UserId == requesterId && l.SenseId == source.Id && !l.IsAuthor),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_AlreadyLinked_IsIdempotent()
    {
        Sense source = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupSource(source);
        Guid requesterId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetLinkAsync(requesterId, source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LearnerWord(Guid.NewGuid(), requesterId, source, isAuthor: false)));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, requesterId, AgeGroup.Adult));

        result.Value.Should().Be(source.Id);
        VerifyNoLink();
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_ChildAndNonChildVisibleWord_ReturnsNotFound()
    {
        Sense source = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);
        SetupSource(source);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, Guid.NewGuid(), AgeGroup.Child));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AddSharedVocabularyWordToMyList.NotShared");
        VerifyNoLink();
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_ChildAndChildVisibleWord_Links()
    {
        Sense source = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupSource(source);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, Guid.NewGuid(), AgeGroup.Child));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_WordNotShared_ReturnsNotFoundWithoutLinking()
    {
        Sense source = TestWords.Learner(Guid.NewGuid());
        SetupSource(source);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, Guid.NewGuid(), AgeGroup.Adult));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        VerifyNoLink();
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_SystemWord_ReturnsNotFound()
    {
        Sense source = TestWords.System();
        SetupSource(source);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, Guid.NewGuid(), AgeGroup.Adult));

        result.Error.Type.Should().Be(ErrorType.NotFound);
        VerifyNoLink();
    }

    [Fact]
    public async Task AddSharedVocabularyWordToMyListCommandHandler_HandleAsync_SourceWordNotFound_ReturnsFailure()
    {
        Guid sourceId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Sense>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(sourceId, Guid.NewGuid(), AgeGroup.Adult));

        result.IsFailure.Should().BeTrue();
        VerifyNoLink();
    }
}
