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
    private readonly Mock<IPersonalVocabularyWordRepository> _repository = new();
    private readonly AddSharedVocabularyWordToMyListCommandValidator _validator = new();
    private readonly Mock<ILogger<AddSharedVocabularyWordToMyListCommandHandler>> _logger = new();

    private AddSharedVocabularyWordToMyListCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    [Fact]
    public async Task HandleAsync_SharedWord_CopiesAsNewPrivateWordForRequester()
    {
        PersonalVocabularyWord source = new(Guid.NewGuid(), Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", "eg");
        source.RequestShare();
        source.Approve(true, Guid.NewGuid());

        Guid requesterId = Guid.NewGuid();

        _repository.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(source));
        _repository
            .Setup(r => r.AddAsync(It.IsAny<PersonalVocabularyWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, requesterId, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.AddAsync(
                It.Is<PersonalVocabularyWord>(w =>
                    w.OwnerUserId == requesterId && w.ShareStatus == VocabularyShareStatus.Private && w.Word == "apple"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_SourceWordNotShared_ReturnsNotFoundWithoutCopying()
    {
        PersonalVocabularyWord source = new(Guid.NewGuid(), Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null);

        _repository.Setup(r => r.GetByIdAsync(source.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(source));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(source.Id, Guid.NewGuid(), AgeGroup.Adult));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.AddAsync(It.IsAny<PersonalVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SourceWordNotFound_ReturnsFailure()
    {
        Guid sourceId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<PersonalVocabularyWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddSharedVocabularyWordToMyListCommand(sourceId, Guid.NewGuid(), AgeGroup.Adult));

        result.IsFailure.Should().BeTrue();
    }
}
