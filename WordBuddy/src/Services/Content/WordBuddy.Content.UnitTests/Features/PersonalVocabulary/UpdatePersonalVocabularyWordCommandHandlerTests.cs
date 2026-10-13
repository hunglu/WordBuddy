using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class UpdatePersonalVocabularyWordCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly UpdatePersonalVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<UpdatePersonalVocabularyWordCommandHandler>> _logger = new();

    public UpdatePersonalVocabularyWordCommandHandlerTests()
    {
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Sense>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _repository.Setup(r => r.FindByContentHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([]));
        _repository.Setup(r => r.DeleteLexemeIfOrphanedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));
    }

    private UpdatePersonalVocabularyWordCommandHandler CreateHandler() => new(_repository.Object, _validator, _logger.Object);

    private void SetupLink(Guid userId, Sense sense, bool isAuthor = true) =>
        _repository.Setup(r => r.GetLinkAsync(userId, sense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LearnerWord(Guid.NewGuid(), userId, sense, isAuthor)));

    [Theory]
    [InlineData(AgeGroup.Adult)]
    [InlineData(AgeGroup.Child)]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_OwnPrivate_UpdatesInPlace(AgeGroup ageGroup)
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Learner(userId, ageGroup: ageGroup);
        Guid id = sense.Id;
        SetupLink(userId, sense);

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(id, userId, "apple", "a red fruit", null));

        result.IsSuccess.Should().BeTrue();
        sense.Id.Should().Be(id);
        sense.Definition.Should().Be("a red fruit");
        _repository.Verify(r => r.UpdateAsync(sense, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.GetOrCreateLexemeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.DeleteLexemeIfOrphanedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_NoChange_SucceedsWithoutSaving()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Learner(userId);
        SetupLink(userId, sense);

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Sense>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_NotLinked_ReturnsNotFound()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        _repository.Setup(r => r.GetLinkAsync(userId, senseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "x")));

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(senseId, userId, "pear", "a fruit", null));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_SystemSense_ReturnsForbidden()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.System();
        SetupLink(userId, sense, isAuthor: false);

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "pear", "a fruit", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith("Forbidden");
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_OthersSenseLinked_ReturnsForbidden()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupLink(userId, sense, isAuthor: false);

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "pear", "a fruit", null));

        result.Error.Code.Should().EndWith("Forbidden");
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_PendingReview_ReturnsConflict()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Pending(userId);
        SetupLink(userId, sense);

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "pear", "a fruit", null));

        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Sense.NotEditable");
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_OwnDuplicate_ReturnsConflict()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Learner(userId);
        Sense other = TestWords.Learner(userId, "apple", "a red fruit");
        SetupLink(userId, sense);
        _repository.Setup(r => r.FindByContentHashAsync(other.ContentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([other]));

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "apple", "a red fruit", null));

        result.Error.Code.Should().Be("PersonalVocabularyWord.Duplicate");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        sense.Definition.Should().Be("a fruit");
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Sense>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_MatchingSharedSense_StillEditsOwnSense()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Learner(userId);
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, definition: "a red fruit");
        SetupLink(userId, sense);
        _repository.Setup(r => r.FindByContentHashAsync(shared.ContentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([shared]));

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "apple", "a red fruit", null));

        result.IsSuccess.Should().BeTrue();
        sense.Definition.Should().Be("a red fruit");
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_WordChanged_MovesLexemeAndCleansOldOne()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Learner(userId);
        Guid oldLexemeId = sense.LexemeId;
        Lexeme newLexeme = Lexeme.Create(Guid.NewGuid(), "pear").Value;
        SetupLink(userId, sense);
        _repository.Setup(r => r.GetOrCreateLexemeAsync("pear", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(newLexeme));

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "pear", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        sense.LexemeId.Should().Be(newLexeme.Id);
        _repository.Verify(r => r.DeleteLexemeIfOrphanedAsync(oldLexemeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_SaveConflict_ReturnsConflictAndKeepsLexeme()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = TestWords.Learner(userId);
        Lexeme newLexeme = Lexeme.Create(Guid.NewGuid(), "pear").Value;
        SetupLink(userId, sense);
        _repository.Setup(r => r.GetOrCreateLexemeAsync("pear", It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(newLexeme));
        _repository.Setup(r => r.UpdateAsync(sense, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("PersonalVocabularyWord.Duplicate", "dup")));

        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(sense.Id, userId, "pear", "a fruit", null));

        result.Error.Type.Should().Be(ErrorType.Conflict);
        _repository.Verify(r => r.DeleteLexemeIfOrphanedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "a fruit")]
    [InlineData("pear", "")]
    public async Task UpdatePersonalVocabularyWordCommandHandler_HandleAsync_InvalidInput_ReturnsValidationError(string word, string definition)
    {
        Result result = await CreateHandler().HandleAsync(new UpdatePersonalVocabularyWordCommand(Guid.NewGuid(), Guid.NewGuid(), word, definition, null));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.GetLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
