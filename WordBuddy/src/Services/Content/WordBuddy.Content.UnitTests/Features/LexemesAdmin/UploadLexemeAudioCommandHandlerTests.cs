using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UploadLexemeAudio;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.LexemesAdmin;

public class UploadLexemeAudioCommandHandlerTests
{
    private readonly Mock<ILexemeAdminRepository> _repository = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IDistributedCache> _cache = new();

    private UploadLexemeAudioCommandHandler Handler() =>
        new(_repository.Object, _storage.Object, _cache.Object, new UploadLexemeAudioCommandValidator(),
            NullLogger<UploadLexemeAudioCommandHandler>.Instance);

    private Lexeme SetupLexeme()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple", PartOfSpeech.Noun).Value;
        _repository.Setup(r => r.GetTrackedAsync(lexeme.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(lexeme));
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _storage
            .Setup(s => s.SaveNamedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string name, string _, CancellationToken _) => $"audio/{name}");
        return lexeme;
    }

    private static UploadLexemeAudioCommand Command(Guid id, string locale = "en-GB", string contentType = "audio/mpeg", long length = 100) =>
        new(id, locale, new MemoryStream([1, 2, 3]), contentType, length);

    private static void Link(Lexeme lexeme, string locale, MediaAsset asset)
    {
        lexeme.AttachAudio(locale, asset.Id);
        string nav = locale == Lexeme.UkLocale ? nameof(Lexeme.UkAudio) : nameof(Lexeme.UsAudio);
        typeof(Lexeme).GetProperty(nav)!.SetValue(lexeme, asset);
    }

    [Fact]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_NoAudioYet_CreatesAndAttachesAsset()
    {
        Lexeme lexeme = SetupLexeme();

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(lexeme.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be($"audio/lexeme-{lexeme.Id}-en-GB.mp3");
        result.Value.Type.Should().Be(MediaAssetType.Audio);
        lexeme.UkAudioAssetId.Should().Be(result.Value.Id);
        _repository.Verify(r => r.AddMediaAsset(It.Is<MediaAsset>(a => a.Id == result.Value.Id)), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(AutofillCacheKeys.Catalog("APPLE"), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.All, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_ExistingAssetWithSameUrl_ReusesAssetWithoutCleanup()
    {
        Lexeme lexeme = SetupLexeme();
        MediaAsset existing = new(Guid.NewGuid(), MediaAssetType.Audio, $"audio/lexeme-{lexeme.Id}-en-GB.mp3");
        Link(lexeme, Lexeme.UkLocale, existing);

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(lexeme.Id));

        result.Value.Id.Should().Be(existing.Id);
        _repository.Verify(r => r.AddMediaAsset(It.IsAny<MediaAsset>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.UnlinkAndDeleteAssetIfUnusedAsync(It.IsAny<Lexeme>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(c => c.RemoveAsync(SharedVocabularyCacheKeys.All, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_ExistingAssetWithOtherUrl_ReplacesAndCleansOldAsset()
    {
        Lexeme lexeme = SetupLexeme();
        MediaAsset old = new(Guid.NewGuid(), MediaAssetType.Audio, "audio/legacy-apple.mp3");
        Link(lexeme, Lexeme.UkLocale, old);
        _repository
            .Setup(r => r.UnlinkAndDeleteAssetIfUnusedAsync(lexeme, old.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<string?>(old.Url));
        _repository.Setup(r => r.IsUrlUsedAsync(old.Url, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(false));

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(lexeme.Id));

        result.Value.Id.Should().NotBe(old.Id);
        lexeme.UkAudioAssetId.Should().Be(result.Value.Id);
        _repository.Verify(r => r.UnlinkAndDeleteAssetIfUnusedAsync(lexeme, old.Id, It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _storage.Verify(s => s.DeleteAsync(old.Url, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_WrongContentType_ReturnsValidation()
    {
        Lexeme lexeme = SetupLexeme();

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(lexeme.Id, contentType: "audio/wav"));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _storage.Verify(s => s.SaveNamedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(5L * 1024 * 1024 + 1)]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_BadSize_ReturnsValidation(long length)
    {
        Lexeme lexeme = SetupLexeme();

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(lexeme.Id, length: length));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_BadLocale_ReturnsValidation()
    {
        Lexeme lexeme = SetupLexeme();

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(lexeme.Id, locale: "en-AU"));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadLexemeAudioCommandHandler_HandleAsync_UnknownLexeme_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetTrackedAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Lexeme>(Error.NotFound("Lexeme.NotFound", "missing")));

        Result<MediaAssetDto> result = await Handler().HandleAsync(Command(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
        _storage.VerifyNoOtherCalls();
    }
}
