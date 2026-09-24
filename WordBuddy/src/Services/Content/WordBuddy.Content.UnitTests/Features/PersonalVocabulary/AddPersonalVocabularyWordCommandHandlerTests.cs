using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class AddPersonalVocabularyWordCommandHandlerTests
{
    private readonly Mock<IPersonalVocabularyWordRepository> _repository = new();
    private readonly AddPersonalVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<AddPersonalVocabularyWordCommandHandler>> _logger = new();

    private AddPersonalVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsWordAsPrivateAndReturnsSuccess()
    {
        _repository
            .Setup(r => r.AddAsync(It.IsAny<PersonalVocabularyWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        AddPersonalVocabularyWordCommand command = new(Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", "I ate an apple.");

        Result<Guid> result = await CreateHandler().HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.AddAsync(
                It.Is<PersonalVocabularyWord>(w => w.Word == "apple" && w.ShareStatus == VocabularyShareStatus.Private),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_EmptyWord_ReturnsValidationFailureWithoutCallingRepository()
    {
        AddPersonalVocabularyWordCommand command = new(Guid.NewGuid(), AgeGroup.Adult, string.Empty, "a fruit", null);

        Result<Guid> result = await CreateHandler().HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.AddAsync(It.IsAny<PersonalVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
