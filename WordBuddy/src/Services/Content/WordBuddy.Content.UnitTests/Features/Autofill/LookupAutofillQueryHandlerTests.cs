using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Autofill;
using WordBuddy.Content.Application.Features.Autofill.Queries.LookupAutofill;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Autofill;

public class LookupAutofillQueryHandlerTests
{
    private readonly Mock<IAutofillRepository> _autofillRepository = new();
    private readonly Mock<IVocabularyWordRepository> _vocabularyRepository = new();
    private readonly Mock<IDictionaryClient> _dictionary = new();
    private readonly Mock<ISenseGenerator> _generator = new();
    private readonly Mock<IAudioDownloader> _audio = new();
    private readonly Mock<IFileStorageService> _storage = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly AutofillSettings _settings = new();

    public LookupAutofillQueryHandlerTests()
    {
        _cache
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        _autofillRepository
            .Setup(r => r.GetCatalogAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([]));
        _autofillRepository
            .Setup(r => r.GetMovableCatalogSensesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([]));
        _autofillRepository
            .Setup(r => r.SaveAsync(It.IsAny<AutofillSave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _audio
            .Setup(a => a.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new byte[] { 1 }));
        _storage
            .Setup(s => s.SaveNamedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string name, string _, CancellationToken _) => "audio/" + name);
    }

    private LookupAutofillQueryHandler CreateHandler() => new(
        _autofillRepository.Object,
        _vocabularyRepository.Object,
        _dictionary.Object,
        _generator.Object,
        new AutofillCatalogWriter(_autofillRepository.Object, _audio.Object, _storage.Object, _settings, Mock.Of<ILogger<AutofillCatalogWriter>>()),
        _settings,
        _cache.Object,
        new LookupAutofillQueryValidator(),
        Mock.Of<ILogger<LookupAutofillQueryHandler>>());

    private static LookupAutofillQuery Adult(string word = "apple") => new(word, Guid.NewGuid(), AgeGroup.Adult);

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_CatalogHit_MakesNoExternalCall()
    {
        Sense stored = AutofillTestData.CatalogSense();
        _autofillRepository
            .Setup(r => r.GetCatalogAsync("APPLE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([stored]));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.Value.AutofillUnavailable.Should().BeFalse();
        result.Value.Senses.Should().ContainSingle(s => s.SenseId == stored.Id && s.Definition == "a round fruit");
        _dictionary.Verify(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _generator.Verify(g => g.GenerateAsync(It.IsAny<SenseGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_DictionaryFailure_ReturnsUnavailableAndSavesNothing()
    {
        _dictionary
            .Setup(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DictionaryEntry>(Error.Failure("Dictionary.Unavailable", "down")));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.IsSuccess.Should().BeTrue();
        result.Value.AutofillUnavailable.Should().BeTrue();
        _generator.Verify(g => g.GenerateAsync(It.IsAny<SenseGenerationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _autofillRepository.Verify(r => r.SaveAsync(It.IsAny<AutofillSave>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_GeneratorFailure_ReturnsUnavailableAndSavesNothing()
    {
        _dictionary
            .Setup(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(AutofillTestData.Entry()));
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<SenseGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<GeneratedWord>(Error.Failure("SenseGenerator.Unavailable", "down")));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.Value.AutofillUnavailable.Should().BeTrue();
        _autofillRepository.Verify(r => r.SaveAsync(It.IsAny<AutofillSave>(), It.IsAny<CancellationToken>()), Times.Never);
        _storage.Verify(s => s.SaveNamedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_DictionaryNotFound_SetsNegativeCache()
    {
        _dictionary
            .Setup(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<DictionaryEntry>(Error.NotFound("Dictionary.WordNotFound", "no")));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.Value.AutofillUnavailable.Should().BeTrue();
        _cache.Verify(c => c.SetAsync("content:autofill-miss:APPLE", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_NegativeCacheHit_MakesNoExternalCall()
    {
        _cache
            .Setup(c => c.GetAsync("content:autofill-miss:APPLE", It.IsAny<CancellationToken>()))
            .ReturnsAsync("1"u8.ToArray());

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.Value.AutofillUnavailable.Should().BeTrue();
        _dictionary.Verify(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_NewWord_SavesSendsOnlyWordAndInvalidatesCache()
    {
        Sense saved = AutofillTestData.CatalogSense();
        _dictionary
            .Setup(d => d.LookupAsync("apple", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(AutofillTestData.Entry()));
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<SenseGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(AutofillTestData.Generated()));
        _autofillRepository
            .SetupSequence(r => r.GetCatalogAsync("APPLE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([]))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([saved]));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.Value.Senses.Should().ContainSingle(s => s.SenseId == saved.Id);
        _generator.Verify(g => g.GenerateAsync(
            It.Is<SenseGenerationRequest>(r => r.Word == "apple" && r.TranslationLocales.SequenceEqual(new[] { "vi" })),
            It.IsAny<CancellationToken>()), Times.Once);
        _autofillRepository.Verify(r => r.SaveAsync(It.IsAny<AutofillSave>(), It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("content:autofill:APPLE", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_ConcurrentSave_ReturnsCatalog()
    {
        Sense winner = AutofillTestData.CatalogSense();
        _dictionary
            .Setup(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(AutofillTestData.Entry()));
        _generator
            .Setup(g => g.GenerateAsync(It.IsAny<SenseGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(AutofillTestData.Generated()));
        _autofillRepository
            .Setup(r => r.SaveAsync(It.IsAny<AutofillSave>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("Autofill.ConcurrentSave", "race")));
        _autofillRepository
            .SetupSequence(r => r.GetCatalogAsync("APPLE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([]))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([winner]));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult());

        result.Value.AutofillUnavailable.Should().BeFalse();
        result.Value.Senses.Should().ContainSingle(s => s.SenseId == winner.Id);
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_ChildUnapproved_GetsMaskedSense()
    {
        Guid childId = Guid.NewGuid();
        Sense stored = AutofillTestData.CatalogSense();
        _autofillRepository
            .Setup(r => r.GetCatalogAsync("APPLE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([stored]));
        _vocabularyRepository
            .Setup(r => r.GetForReviewAsync(childId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SenseReviewCandidate>>([new SenseReviewCandidate(stored, null)]));

        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(new LookupAutofillQuery("apple", childId, AgeGroup.Child));

        AutofillSenseDto card = result.Value.Senses.Should().ContainSingle().Subject;
        card.AwaitingApproval.Should().BeTrue();
        card.Definition.Should().BeEmpty();
        card.Examples.Should().BeEmpty();
    }

    [Fact]
    public async Task LookupAutofillQueryHandler_HandleAsync_InvalidWord_ReturnsValidationFailure()
    {
        Result<AutofillResultDto> result = await CreateHandler().HandleAsync(Adult("ab1"));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _dictionary.Verify(d => d.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
