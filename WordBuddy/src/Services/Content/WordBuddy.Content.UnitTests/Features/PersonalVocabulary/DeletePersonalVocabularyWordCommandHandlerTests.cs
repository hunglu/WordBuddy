using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

/// <summary>Whether a word is actually orphaned (other links, shared status) is decided by
/// <see cref="IVocabularyWordRepository.DeleteIfOrphanedAsync"/> — covered against a real DB by the
/// integration tests. Here: the handler unlinks, and asks for the orphan delete only for authors.</summary>
public class DeletePersonalVocabularyWordCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly DeletePersonalVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<DeletePersonalVocabularyWordCommandHandler>> _logger = new();

    public DeletePersonalVocabularyWordCommandHandlerTests()
    {
        _repository.Setup(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private DeletePersonalVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    private UserVocabularyWord SetupLink(Guid userId, VocabularyWord word, bool isAuthor)
    {
        UserVocabularyWord link = new(Guid.NewGuid(), userId, word, isAuthor);
        _repository.Setup(r => r.GetLinkAsync(userId, word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(link));
        return link;
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_AuthorOrphan_UnlinksAndDeletesWord()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Learner(ownerId);
        UserVocabularyWord link = SetupLink(ownerId, word, isAuthor: true);
        _repository.Setup(r => r.DeleteIfOrphanedAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.UnlinkAsync(link, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteIfOrphanedAsync(word.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_AuthorWithOtherLinks_OnlyUnlinks()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Learner(ownerId);
        SetupLink(ownerId, word, isAuthor: true);
        // The repository finds other links and keeps the word.
        _repository.Setup(r => r.DeleteIfOrphanedAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(false));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_NonAuthor_OnlyUnlinksAndNeverAsksToDelete()
    {
        Guid adopterId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupLink(adopterId, word, isAuthor: false);

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, adopterId));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteIfOrphanedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_AuthorOfSharedWord_KeepsWord()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(ownerId, visibleToChildren: true);
        SetupLink(ownerId, word, isAuthor: true);
        // Shared words are never orphan-deleted.
        _repository.Setup(r => r.DeleteIfOrphanedAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(false));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_NoLink_ReturnsNotFoundWithoutUnlinking()
    {
        Guid requesterId = Guid.NewGuid();
        Guid wordId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetLinkAsync(requesterId, wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserVocabularyWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(wordId, requesterId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.DeleteIfOrphanedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
