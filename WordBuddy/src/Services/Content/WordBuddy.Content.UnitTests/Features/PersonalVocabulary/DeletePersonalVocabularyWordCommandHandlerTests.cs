using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

/// <summary>Whether a word is actually orphaned (other links, shared status) is decided by
/// <see cref="IVocabularyWordRepository.DeleteIfOrphanedAsync"/> — covered against a real DB by the
/// integration tests. Here: the handler unlinks, asks for the orphan delete only for authors, and
/// requires confirmation before it transfers a Shared word or cancels a share request.</summary>
public class DeletePersonalVocabularyWordCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly DeletePersonalVocabularyWordCommandValidator _validator = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<ILogger<DeletePersonalVocabularyWordCommandHandler>> _logger = new();

    public DeletePersonalVocabularyWordCommandHandlerTests()
    {
        _repository.Setup(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private DeletePersonalVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _cache.Object, _logger.Object);

    private UserVocabularyWord SetupLink(Guid userId, VocabularyWord word, bool isAuthor)
    {
        UserVocabularyWord link = new(Guid.NewGuid(), userId, word, isAuthor);
        _repository.Setup(r => r.GetLinkAsync(userId, word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(link));
        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
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

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_ReturnsConflictWhenSharedAndNotConfirmed()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(ownerId, visibleToChildren: true);
        SetupLink(ownerId, word, isAuthor: true);

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("PersonalVocabularyWord.DeleteConfirmationRequired");
        word.OwnerUserId.Should().Be(ownerId);
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_ReturnsConflictWhenPendingReviewAndNotConfirmed()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Pending(ownerId);
        SetupLink(ownerId, word, isAuthor: true);

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PersonalVocabularyWord.DeleteConfirmationRequired");
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_TransfersToSystemAndInvalidatesCacheWhenSharedConfirmed()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(ownerId, visibleToChildren: true);
        UserVocabularyWord link = SetupLink(ownerId, word, isAuthor: true);

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId, Confirm: true));

        result.IsSuccess.Should().BeTrue();
        word.Source.Should().Be(VocabularySource.System);
        word.OwnerUserId.Should().Be(SystemOwner.UserId);
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        _repository.Verify(r => r.UnlinkAsync(link, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteIfOrphanedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.All, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_ReturnsSuccessWhenCacheRemovalFailsAfterTransfer()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(ownerId, visibleToChildren: true);
        UserVocabularyWord link = SetupLink(ownerId, word, isAuthor: true);
        _cache
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis unavailable"));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId, Confirm: true));

        result.IsSuccess.Should().BeTrue();
        word.OwnerUserId.Should().Be(SystemOwner.UserId);
        _repository.Verify(r => r.UnlinkAsync(link, It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_CancelsShareRequestAndDeletesWhenPendingReviewConfirmed()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Pending(ownerId);
        UserVocabularyWord link = SetupLink(ownerId, word, isAuthor: true);
        _repository.Setup(r => r.DeleteIfOrphanedAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId, Confirm: true));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Private);
        word.OwnerUserId.Should().Be(ownerId);
        _repository.Verify(r => r.UnlinkAsync(link, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.DeleteIfOrphanedAsync(word.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalVocabularyWordCommandHandler_HandleAsync_AdopterUnlinksWithoutConfirmation()
    {
        Guid adopterId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        VocabularyWord word = TestWords.Shared(ownerId, visibleToChildren: true);
        SetupLink(adopterId, word, isAuthor: false);

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, adopterId));

        result.IsSuccess.Should().BeTrue();
        word.OwnerUserId.Should().Be(ownerId);
        word.Source.Should().Be(VocabularySource.Learner);
        _repository.Verify(r => r.UnlinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
