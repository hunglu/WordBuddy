using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Autofill;

/// <summary><see cref="IDictionaryClient"/> on the Free Dictionary API (<c>GET {base}/{word}</c>).
/// Only the word is sent. Maps IPA and audio by the <c>-uk</c> / <c>-us</c> audio file suffix.</summary>
internal sealed class FreeDictionaryClient : IDictionaryClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly ILogger<FreeDictionaryClient> _logger;

    public FreeDictionaryClient(HttpClient httpClient, ILogger<FreeDictionaryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<DictionaryEntry>> LookupAsync(string word, CancellationToken ct = default)
    {
        string trimmed = (word ?? string.Empty).Trim();
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(Uri.EscapeDataString(trimmed), ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<DictionaryEntry>(NotFound);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Dictionary lookup failed: StatusCode={StatusCode}", (int)response.StatusCode);
                return Result.Failure<DictionaryEntry>(Unavailable);
            }

            string json = await response.Content.ReadAsStringAsync(ct);
            return Parse(trimmed, json);
        }
        catch (Exception ex) when (IsTransportFailure(ex, ct))
        {
            _logger.LogWarning(ex, "Dictionary lookup failed with a transport error");
            return Result.Failure<DictionaryEntry>(Unavailable);
        }
    }

    private static Error NotFound => Error.NotFound("Dictionary.WordNotFound", "The dictionary does not know this word.");

    private static Error Unavailable => Error.Failure("Dictionary.Unavailable", "The dictionary is not available.");

    /// <summary>HTTP errors and timeouts (the resilience handler's timeout surfaces as a
    /// cancellation) — but not a cancellation the caller asked for.</summary>
    internal static bool IsTransportFailure(Exception ex, CancellationToken ct) =>
        ex is HttpRequestException ||
        (ex is OperationCanceledException && !ct.IsCancellationRequested) ||
        ex is Polly.Timeout.TimeoutRejectedException;

    /// <summary>Parses a Free Dictionary API body. Bad or empty JSON → failure.</summary>
    internal static Result<DictionaryEntry> Parse(string word, string json)
    {
        List<EntryJson>? entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<EntryJson>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return Result.Failure<DictionaryEntry>(Error.Failure("Dictionary.BadResponse", "The dictionary sent an invalid response."));
        }

        if (entries is null || entries.Count == 0)
        {
            return Result.Failure<DictionaryEntry>(NotFound);
        }

        List<PhoneticJson> phonetics = entries.SelectMany(e => e.Phonetics ?? []).ToList();
        string? fallbackIpa = entries.Select(e => e.Phonetic).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));

        PhoneticJson? uk = phonetics.FirstOrDefault(p => HasAudioSuffix(p, "-uk.mp3"));
        PhoneticJson? us = phonetics.FirstOrDefault(p => HasAudioSuffix(p, "-us.mp3"));
        string? anyIpa = phonetics.Select(p => p.Text).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)) ?? fallbackIpa;

        List<PartOfSpeech> partsOfSpeech = entries
            .SelectMany(e => e.Meanings ?? [])
            .Select(m => MapPartOfSpeech(m.PartOfSpeech))
            .OfType<PartOfSpeech>()
            .Distinct()
            .ToList();

        return Result.Success(new DictionaryEntry(
            entries[0].Word ?? word,
            NullIfEmpty(uk?.Text) ?? NullIfEmpty(anyIpa),
            NullIfEmpty(us?.Text) ?? NullIfEmpty(anyIpa),
            HttpsOrNull(uk?.Audio),
            HttpsOrNull(us?.Audio),
            partsOfSpeech));
    }

    private static bool HasAudioSuffix(PhoneticJson phonetic, string suffix) =>
        phonetic.Audio is { } audio && audio.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? HttpsOrNull(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.ToString() : null;

    private static PartOfSpeech? MapPartOfSpeech(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "noun" => PartOfSpeech.Noun,
            "verb" => PartOfSpeech.Verb,
            "adjective" => PartOfSpeech.Adjective,
            "adverb" => PartOfSpeech.Adverb,
            "pronoun" => PartOfSpeech.Pronoun,
            "preposition" => PartOfSpeech.Preposition,
            "conjunction" => PartOfSpeech.Conjunction,
            "determiner" or "article" => PartOfSpeech.Determiner,
            "interjection" or "exclamation" => PartOfSpeech.Interjection,
            "phrase" or "idiom" => PartOfSpeech.Phrase,
            _ => null,
        };

    private sealed record EntryJson(
        [property: JsonPropertyName("word")] string? Word,
        [property: JsonPropertyName("phonetic")] string? Phonetic,
        [property: JsonPropertyName("phonetics")] List<PhoneticJson>? Phonetics,
        [property: JsonPropertyName("meanings")] List<MeaningJson>? Meanings);

    private sealed record PhoneticJson(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("audio")] string? Audio);

    private sealed record MeaningJson([property: JsonPropertyName("partOfSpeech")] string? PartOfSpeech);
}
