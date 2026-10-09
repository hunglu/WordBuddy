using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Autofill;

/// <summary>Deterministic, keyless auto-fill clients for local E2E runs. Enabled only by
/// <c>Autofill:UseFakeClients = true</c> in the Development environment; never calls the network.
/// <para>Known test words (case-insensitive): <c>serendipity</c> (noun), <c>apple</c> (noun),
/// <c>happy</c> (adjective), <c>run</c> (verb + noun). Any other word → dictionary "not found" →
/// <c>autofillUnavailable = true</c>.</para></summary>
internal sealed class DevelopmentFakeAutofillClients : IDictionaryClient, ISenseGenerator, IAudioDownloader
{
    /// <summary>The known test words and their senses.</summary>
    internal static readonly IReadOnlyDictionary<string, IReadOnlyList<(PartOfSpeech Pos, string Definition, string Example, string Vi)>> Words =
        new Dictionary<string, IReadOnlyList<(PartOfSpeech, string, string, string)>>(StringComparer.OrdinalIgnoreCase)
        {
            ["serendipity"] = [(PartOfSpeech.Noun, "Finding something good by chance.", "Meeting her was pure serendipity.", "sự tình cờ may mắn")],
            ["apple"] = [(PartOfSpeech.Noun, "A round fruit with red or green skin.", "I eat an apple every day.", "quả táo")],
            ["happy"] = [(PartOfSpeech.Adjective, "Feeling pleased and glad.", "The children are happy today.", "vui vẻ")],
            ["run"] =
            [
                (PartOfSpeech.Verb, "To move quickly on your feet.", "I run to school.", "chạy"),
                (PartOfSpeech.Noun, "A time when you run.", "We went for a run.", "cuộc chạy"),
            ],
        };

    /// <summary>A tiny valid-looking MP3 header (ID3), enough for the audio file to be stored.</summary>
    private static readonly byte[] FakeMp3 = [0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    public Task<Result<DictionaryEntry>> LookupAsync(string word, CancellationToken ct = default)
    {
        string key = (word ?? string.Empty).Trim();
        if (!Words.TryGetValue(key, out IReadOnlyList<(PartOfSpeech Pos, string Definition, string Example, string Vi)>? senses))
        {
            return Task.FromResult(Result.Failure<DictionaryEntry>(Error.NotFound("Dictionary.WordNotFound", "The dictionary does not know this word.")));
        }

        string lower = key.ToLowerInvariant();
        return Task.FromResult(Result.Success(new DictionaryEntry(
            lower,
            $"/{lower}-uk/",
            $"/{lower}-us/",
            $"https://fake-audio.invalid/{lower}-uk.mp3",
            $"https://fake-audio.invalid/{lower}-us.mp3",
            senses.Select(s => s.Pos).Distinct().ToList())));
    }

    public Task<Result<GeneratedWord>> GenerateAsync(SenseGenerationRequest request, CancellationToken ct = default)
    {
        if (!Words.TryGetValue(request.Word.Trim(), out IReadOnlyList<(PartOfSpeech Pos, string Definition, string Example, string Vi)>? senses))
        {
            return Task.FromResult(Result.Failure<GeneratedWord>(Error.Failure("SenseGenerator.Unavailable", "Unknown fake word.")));
        }

        List<GeneratedLexeme> lexemes = senses
            .GroupBy(s => s.Pos)
            .Select((group, index) => new GeneratedLexeme(
                group.Key,
                null,
                [],
                CefrLevel.B1,
                group.Select(s => new GeneratedSense(
                    s.Definition,
                    [s.Example],
                    new Dictionary<string, string> { ["vi"] = s.Vi },
                    [],
                    [],
                    [],
                    ["test"],
                    null,
                    true)).ToList(),
                // Existing null-POS catalog senses move to the first part of speech.
                index == 0 ? request.ExistingCatalogSenses.Select(e => e.SenseId).ToList() : []))
            .ToList();

        return Task.FromResult(Result.Success(new GeneratedWord(lexemes)));
    }

    public Task<Result<byte[]>> DownloadAsync(string url, CancellationToken ct = default) =>
        Task.FromResult(Result.Success(FakeMp3));
}
