using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Features.LexemesAdmin.Commands.RemoveLexemeAudio;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.LexemesAdmin;

public class RemoveLexemeAudioCommandHandlerTests
{
    private const string FileUrl = "audio/lexeme-x-en-GB.mp3";

    private readonly Mock<ILexemeAdminRepository> _repository = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly CapturingLogger<RemoveLexemeAudioCommandHandler> _logger = new();

    private RemoveLexemeAudioCommandHandler Handler() =>
        new(_repository.Object, _storage.Object, _cache.Object, new RemoveLexemeAudioCommandValidator(), _logger);

    private Lexeme SetupLexeme(bool withAudio = true)
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple", PartOfSpeech.Noun).Value;
        if (withAudio)
        {
            lexeme.AttachAudio(Lexeme.UkLocale, Guid.NewGuid());
        }

        _repository.Setup(r => r.GetTrackedAsync(lexeme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(lexeme));
        return lexeme;
    }

    private void SetupUnlink(Lexeme lexeme, string? deletedUrl) =>
        _repository
            .Setup(r => r.UnlinkAndDeleteAssetIfUnusedAsync(lexeme, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(deletedUrl));

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_UnusedAsset_DeletesFile()
    {
        Lexeme lexeme = SetupLexeme();
        SetupUnlink(lexeme, FileUrl);
        _repository.Setup(r => r.IsUrlUsedAsync(FileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(false));
        _storage.Setup(s => s.DeleteAsync(FileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(lexeme.Id, "en-GB"));

        result.IsSuccess.Should().BeTrue();
        lexeme.UkAudioAssetId.Should().BeNull();
        _storage.Verify(s => s.DeleteAsync(FileUrl, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.All, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_SharedAsset_OnlyUnlinks()
    {
        Lexeme lexeme = SetupLexeme();
        SetupUnlink(lexeme, deletedUrl: null);

        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(lexeme.Id, "en-GB"));

        result.IsSuccess.Should().BeTrue();
        lexeme.UkAudioAssetId.Should().BeNull();
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_UrlStillUsedByOtherRow_KeepsFile()
    {
        Lexeme lexeme = SetupLexeme();
        SetupUnlink(lexeme, FileUrl);
        _repository.Setup(r => r.IsUrlUsedAsync(FileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(true));

        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(lexeme.Id, "en-GB"));

        result.IsSuccess.Should().BeTrue();
        _storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_FileDeleteThrows_LogsWarningAndSucceeds()
    {
        Lexeme lexeme = SetupLexeme();
        SetupUnlink(lexeme, FileUrl);
        _repository.Setup(r => r.IsUrlUsedAsync(FileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(false));
        _storage.Setup(s => s.DeleteAsync(FileUrl, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk"));

        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(lexeme.Id, "en-GB"));

        result.IsSuccess.Should().BeTrue();
        _logger.Entries.Should().Contain(e => e.Contains("delete failed"));
        _logger.Entries.Should().NotContain(e => e.Contains(FileUrl), "the file path must not be logged");
    }

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_NothingLinked_Succeeds()
    {
        Lexeme lexeme = SetupLexeme(withAudio: false);

        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(lexeme.Id, "en-US"));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.UnlinkAndDeleteAssetIfUnusedAsync(It.IsAny<Lexeme>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_NotFound_Returns404Error()
    {
        Guid id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetTrackedAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Lexeme>(Error.NotFound("Lexeme.NotFound", "missing")));

        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(id, "en-GB"));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RemoveLexemeAudioCommandHandler_HandleAsync_BadLocale_ReturnsValidation()
    {
        Result result = await Handler().HandleAsync(new RemoveLexemeAudioCommand(Guid.NewGuid(), "fr-FR"));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
