using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class ModerateSharedVocabularyWordCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly ModerateSharedVocabularyWordCommandValidator _validator = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<ILogger<ModerateSharedVocabularyWordCommandHandler>> _logger = new();

    public ModerateSharedVocabularyWordCommandHandlerTests()
    {
        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private ModerateSharedVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _cache.Object, _logger.Object);

    private static Sense CreatePendingWord() => TestWords.Pending(Guid.NewGuid());

    [Fact]
    public async Task HandleAsync_ApproveWithVisibleToChildrenTrue_SetsSharedAndVisible()
    {
        Sense word = CreatePendingWord();
        Guid moderatorId = Guid.NewGuid();

        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
        _repository.Setup(r => r.UpdateAsync(word, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new ModerateSharedVocabularyWordCommand(word.Id, true, true, moderatorId, true));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        word.VisibleToChildren.Should().BeTrue();
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_ApproveWithVisibleToChildrenFalse_SetsSharedNotVisible()
    {
        Sense word = CreatePendingWord();

        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
        _repository.Setup(r => r.UpdateAsync(word, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new ModerateSharedVocabularyWordCommand(word.Id, true, false, Guid.NewGuid(), true));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
        word.VisibleToChildren.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_Reject_SetsRejected()
    {
        Sense word = CreatePendingWord();

        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
        _repository.Setup(r => r.UpdateAsync(word, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new ModerateSharedVocabularyWordCommand(word.Id, false, false, Guid.NewGuid(), true));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Rejected);
    }

    [Fact]
    public async Task HandleAsync_ModeratorOwnsWord_ReturnsForbiddenAndNoChange()
    {
        Guid moderatorId = Guid.NewGuid();
        Sense word = TestWords.Pending(moderatorId);
        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));

        Result result = await CreateHandler().HandleAsync(new ModerateSharedVocabularyWordCommand(word.Id, true, true, moderatorId, false));

        result.Error.Code.Should().Be("PersonalVocabularyWord.ModerateOwnWordForbidden");
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Sense>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AdminOwnsWord_Succeeds()
    {
        Guid adminId = Guid.NewGuid();
        Sense word = TestWords.Pending(adminId);
        _repository.Setup(r => r.GetByIdAsync(word.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
        _repository.Setup(r => r.UpdateAsync(word, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new ModerateSharedVocabularyWordCommand(word.Id, true, true, adminId, true));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.Shared);
    }

    [Fact]
    public async Task HandleAsync_WordNotFound_ReturnsFailure()
    {
        Guid wordId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(wordId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Sense>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result result = await CreateHandler().HandleAsync(new ModerateSharedVocabularyWordCommand(wordId, true, true, Guid.NewGuid(), true));

        result.IsFailure.Should().BeTrue();
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
