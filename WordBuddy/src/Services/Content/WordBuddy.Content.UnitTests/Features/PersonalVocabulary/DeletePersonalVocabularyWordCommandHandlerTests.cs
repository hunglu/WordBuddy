using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class DeletePersonalVocabularyWordCommandHandlerTests
{
    private readonly Mock<IPersonalVocabularyWordRepository> _repository = new();
    private readonly DeletePersonalVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<DeletePersonalVocabularyWordCommandHandler>> _logger = new();

    private DeletePersonalVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task HandleAsync_OwnedWord_DeletesAndReturnsSuccess()
    {
        Guid ownerId = Guid.NewGuid();
        PersonalVocabularyWord word = new(Guid.NewGuid(), ownerId, AgeGroup.Adult, "apple", "a fruit", null);

        _repository.Setup(r => r.GetOwnedByIdAsync(word.Id, ownerId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(word));
        _repository.Setup(r => r.DeleteAsync(word, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(word.Id, ownerId));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.DeleteAsync(word, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WordNotOwnedOrNotFound_ReturnsNotFoundWithoutDeleting()
    {
        Guid requesterId = Guid.NewGuid();
        Guid wordId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetOwnedByIdAsync(wordId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PersonalVocabularyWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result result = await CreateHandler().HandleAsync(new DeletePersonalVocabularyWordCommand(wordId, requesterId));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.DeleteAsync(It.IsAny<PersonalVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
