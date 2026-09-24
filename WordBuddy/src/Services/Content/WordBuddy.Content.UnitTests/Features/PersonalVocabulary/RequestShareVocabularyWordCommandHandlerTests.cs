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
    private readonly Mock<IPersonalVocabularyWordRepository> _repository = new();
    private readonly RequestShareVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<RequestShareVocabularyWordCommandHandler>> _logger = new();

    private RequestShareVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task HandleAsync_OwnedPrivateWord_MovesToPendingReviewAndPersists()
    {
        Guid ownerId = Guid.NewGuid();
        PersonalVocabularyWord word = new(Guid.NewGuid(), ownerId, AgeGroup.Adult, "apple", "a fruit", null);

        _repository
            .Setup(r => r.GetOwnedByIdAsync(word.Id, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(word));
        _repository
            .Setup(r => r.UpdateAsync(word, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new RequestShareVocabularyWordCommand(word.Id, ownerId));

        result.IsSuccess.Should().BeTrue();
        word.ShareStatus.Should().Be(VocabularyShareStatus.PendingReview);
        _repository.Verify(r => r.UpdateAsync(word, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WordNotOwnedOrNotFound_ReturnsNotFoundWithoutUpdating()
    {
        Guid requesterId = Guid.NewGuid();
        Guid wordId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetOwnedByIdAsync(wordId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PersonalVocabularyWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result result = await CreateHandler().HandleAsync(new RequestShareVocabularyWordCommand(wordId, requesterId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<PersonalVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
