using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>Test doubles for the three external auto-fill clients. Words starting with <c>unknown</c>
/// are not in the dictionary; words starting with <c>broken</c> make the generator fail. Every other
/// word gets one noun sense with a Vietnamese translation and UK/US audio.</summary>
public sealed class FakeAutofillClients : IDictionaryClient, ISenseGenerator, IAudioDownloader
{
    private int _dictionaryCalls;
    private int _generatorCalls;

    /// <summary>Dictionary calls so far.</summary>
    public int DictionaryCalls => _dictionaryCalls;

    /// <summary>Generator calls so far.</summary>
    public int GeneratorCalls => _generatorCalls;

    public Task<Result<DictionaryEntry>> LookupAsync(string word, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _dictionaryCalls);
        if (word.StartsWith("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result.Failure<DictionaryEntry>(Error.NotFound("Dictionary.WordNotFound", "Not found.")));
        }

        return Task.FromResult(Result.Success(new DictionaryEntry(
            word,
            "/test-uk/",
            "/test-us/",
            "https://audio.test/word-uk.mp3",
            "https://audio.test/word-us.mp3",
            [PartOfSpeech.Noun])));
    }

    public Task<Result<GeneratedWord>> GenerateAsync(SenseGenerationRequest request, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _generatorCalls);
        if (request.Word.StartsWith("broken", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result.Failure<GeneratedWord>(Error.Failure("SenseGenerator.Unavailable", "Down.")));
        }

        GeneratedSense sense = new(
            $"A test meaning of {request.Word}.",
            [$"I see a {request.Word}.", $"The {request.Word} is here."],
            new Dictionary<string, string> { ["vi"] = $"nghia {request.Word}" },
            ["big " + request.Word],
            ["thing"],
            [],
            ["test"],
            null,
            true);

        return Task.FromResult(Result.Success(new GeneratedWord(
        [
            new GeneratedLexeme(
                PartOfSpeech.Noun,
                request.Word,
                [request.Word + "s"],
                CefrLevel.A1,
                [sense],
                request.ExistingCatalogSenses.Select(s => s.SenseId).ToList()),
        ])));
    }

    public Task<Result<byte[]>> DownloadAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(new byte[] { 0x49, 0x44, 0x33 }));
}
