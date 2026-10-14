using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UpdateLexeme;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.LexemesAdmin;

public class UpdateLexemeCommandHandlerTests
{
    private readonly Mock<ILexemeAdminRepository> _repository = new();
    private readonly Mock<IDistributedCache> _cache = new();

    private UpdateLexemeCommandHandler Handler() =>
        new(_repository.Object, _cache.Object, new UpdateLexemeCommandValidator(), NullLogger<UpdateLexemeCommandHandler>.Instance);

    private Lexeme SetupLexeme(bool clash = false)
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple", PartOfSpeech.Noun).Value;
        _repository.Setup(r => r.GetTrackedAsync(lexeme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(lexeme));
        _repository
            .Setup(r => r.ExistsWithPartOfSpeechAsync(lexeme.NormalizedLemma, It.IsAny<PartOfSpeech>(), lexeme.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(clash));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        return lexeme;
    }

    private static UpdateLexemeCommand Command(Guid id, int? rank = 10, IReadOnlyList<string>? forms = null) =>
        new(id, PartOfSpeech.Verb, "/uk/", "/us/", "ap·ple", forms ?? ["apples"], CefrLevel.B1, rank);

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_ValidCommand_SavesAndInvalidatesThreeCacheKeys()
    {
        Lexeme lexeme = SetupLexeme();

        Result result = await Handler().HandleAsync(Command(lexeme.Id));

        result.IsSuccess.Should().BeTrue();
        lexeme.PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        lexeme.IpaUk.Should().Be("/uk/");
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(AutofillCacheKeys.Catalog("APPLE"), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.All, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_SiblingHasPartOfSpeech_ReturnsConflict()
    {
        Lexeme lexeme = SetupLexeme(clash: true);

        Result result = await Handler().HandleAsync(Command(lexeme.Id));

        result.Error.Type.Should().Be(ErrorType.Conflict);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_UniqueViolationOnSave_ReturnsConflictAndKeepsCache()
    {
        Lexeme lexeme = SetupLexeme();
        _repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("Lexeme.DuplicatePartOfSpeech", "race")));

        Result result = await Handler().HandleAsync(Command(lexeme.Id));

        result.Error.Type.Should().Be(ErrorType.Conflict);
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_UnknownLexeme_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetTrackedAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Lexeme>(Error.NotFound("Lexeme.NotFound", "missing")));

        Result result = await Handler().HandleAsync(Command(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_RankZero_ReturnsValidation()
    {
        Lexeme lexeme = SetupLexeme();

        Result result = await Handler().HandleAsync(Command(lexeme.Id, rank: 0));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.GetTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_TwentyOneWordForms_ReturnsValidation()
    {
        Lexeme lexeme = SetupLexeme();
        string[] forms = Enumerable.Range(1, Lexeme.MaxWordForms + 1).Select(i => $"form{i}").ToArray();

        Result result = await Handler().HandleAsync(Command(lexeme.Id, forms: forms));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UpdateLexemeCommandHandler_HandleAsync_UnknownPartOfSpeech_ReturnsValidation()
    {
        Lexeme lexeme = SetupLexeme();

        Result result = await Handler().HandleAsync(Command(lexeme.Id) with { PartOfSpeech = (PartOfSpeech)99 });

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
