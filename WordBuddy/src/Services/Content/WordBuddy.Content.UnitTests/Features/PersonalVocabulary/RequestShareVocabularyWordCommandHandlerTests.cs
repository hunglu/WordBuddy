using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class RequestShareVocabularyWordCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly RequestShareVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<RequestShareVocabularyWordCommandHandler>> _logger = new();

    private RequestShareVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task RequestShareVocabularyWordCommandHandler_HandleAsync_Author_MovesToPendingReviewAndPersists()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Learner(ownerId);
        _repository
            .Setup(r => r.GetLinkAsync(ownerId, word.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserVocabularyWord(Guid.NewGuid(), ownerId, word, isAuthor: true)));
        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
        _repository.Setup(r => r.UpdateAsync(word, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new RequestShareVocabularyWordCommand(word.Id, ownerId));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
        _repository.Verify(r => r.UpdateAsync(word, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestShareVocabularyWordCommandHandler_HandleAsync_NonAuthor_ReturnsConflictWithoutUpdating()
    {
        Guid adopterId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        _repository
            .Setup(r => r.GetLinkAsync(adopterId, word.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserVocabularyWord(Guid.NewGuid(), adopterId, word, isAuthor: false)));

        Result result = await CreateHandler().HandleAsync(new RequestShareVocabularyWordCommand(word.Id, adopterId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("PersonalVocabularyWord.InvalidShareRequest");
        _repository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestShareVocabularyWordCommandHandler_HandleAsync_NoLink_ReturnsNotFoundWithoutUpdating()
    {
        Guid requesterId = Guid.NewGuid();
        Guid wordId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetLinkAsync(requesterId, wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserVocabularyWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result result = await CreateHandler().HandleAsync(new RequestShareVocabularyWordCommand(wordId, requesterId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestShareVocabularyWordCommandHandler_HandleAsync_ReturnsConflictForTransferredWord()
    {
        Guid formerOwnerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Transferred(visibleToChildren: true);
        _repository
            .Setup(r => r.GetLinkAsync(formerOwnerId, word.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserVocabularyWord(Guid.NewGuid(), formerOwnerId, word, isAuthor: true)));
        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));

        Result result = await CreateHandler().HandleAsync(new RequestShareVocabularyWordCommand(word.Id, formerOwnerId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<VocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
