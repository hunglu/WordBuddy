using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.Autofill;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Autofill;

public class AutofillCatalogWriterTests
{
    private readonly Mock<IAutofillRepository> _repository = new();
    private readonly Mock<IAudioDownloader> _audio = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private AutofillSave? _saved;

    public AutofillCatalogWriterTests()
    {
        _repository
            .Setup(r => r.SaveAsync(It.IsAny<AutofillSave>(), It.IsAny<CancellationToken>()))
            .Callback((AutofillSave save, CancellationToken _) => _saved = save)
            .ReturnsAsync(Result.Success());
        _audio
            .Setup(a => a.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new byte[] { 1, 2 }));
        _storage
            .Setup(s => s.SaveNamedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string name, string _, CancellationToken _) => "audio/" + name);
    }

    private AutofillCatalogWriter CreateWriter() =>
        new(_repository.Object, _audio.Object, _storage.Object, new AutofillSettings(), Mock.Of<ILogger<AutofillCatalogWriter>>());

    [Fact]
    public async Task AutofillCatalogWriter_SaveAsync_MatchedCatalogSense_MovedToPosLexeme()
    {
        Sense system = TestWords.System();
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);

        Result result = await CreateWriter().SaveAsync("apple", AutofillTestData.Entry(), AutofillTestData.Generated(system.Id), [system, shared]);

        result.IsSuccess.Should().BeTrue();
        Lexeme noun = _saved!.Lexemes.Should().ContainSingle().Subject;
        noun.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        system.LexemeId.Should().Be(noun.Id);
        _saved.MovedSenses.Should().ContainSingle().Which.Should().BeSameAs(system);
        _saved.NormalizedLemma.Should().Be("APPLE");
    }

    [Fact]
    public async Task AutofillCatalogWriter_SaveAsync_PrivateLearnerSense_NeverMoved()
    {
        Sense learner = TestWords.Learner(Guid.NewGuid());
        Guid originalLexeme = learner.LexemeId;

        await CreateWriter().SaveAsync("apple", AutofillTestData.Entry(), AutofillTestData.Generated(learner.Id), [learner]);

        learner.LexemeId.Should().Be(originalLexeme);
        _saved!.MovedSenses.Should().BeEmpty();
    }

    [Fact]
    public async Task AutofillCatalogWriter_SaveAsync_BuildsAutoFillSenseWithViTranslationAndNamedAudio()
    {
        await CreateWriter().SaveAsync("apple", AutofillTestData.Entry(), AutofillTestData.Generated(), []);

        Sense sense = _saved!.Senses.Should().ContainSingle().Subject;
        Lexeme lexeme = _saved.Lexemes.Single();
        sense.Origin.Should().Be(SenseOrigin.AutoFill);
        sense.VisibleToChildren.Should().BeFalse();
        sense.ChildSuitableHint.Should().BeTrue();
        sense.Translations.Should().ContainSingle(t => t.Locale == "vi");
        lexeme.IpaUk.Should().Be("/uk/");
        lexeme.UkAudioAssetId.Should().NotBeNull();
        lexeme.UsAudioAssetId.Should().NotBeNull();
        _saved.AudioAssets.Should().HaveCount(2);
        _storage.Verify(s => s.SaveNamedAsync(It.IsAny<Stream>(), $"lexeme-{lexeme.Id}-en-GB.mp3", "audio/mpeg", It.IsAny<CancellationToken>()), Times.Once);
        _storage.Verify(s => s.SaveNamedAsync(It.IsAny<Stream>(), $"lexeme-{lexeme.Id}-en-US.mp3", "audio/mpeg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AutofillCatalogWriter_SaveAsync_AudioDownloadFails_StillSavesWithoutAudio()
    {
        _audio
            .Setup(a => a.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<byte[]>(Error.Failure("Audio.Unavailable", "down")));

        Result result = await CreateWriter().SaveAsync("apple", AutofillTestData.Entry(), AutofillTestData.Generated(), []);

        result.IsSuccess.Should().BeTrue();
        _saved!.AudioAssets.Should().BeEmpty();
        _saved.Lexemes.Single().UkAudioAssetId.Should().BeNull();
    }
}
