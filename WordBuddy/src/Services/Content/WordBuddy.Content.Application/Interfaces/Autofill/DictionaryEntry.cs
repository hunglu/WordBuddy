using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>What the dictionary knows about a word. Audio URLs are absolute <c>https</c> links to MP3 files.</summary>
public sealed record DictionaryEntry(
    string Word,
    string? IpaUk,
    string? IpaUs,
    string? AudioUkUrl,
    string? AudioUsUrl,
    IReadOnlyList<PartOfSpeech> PartsOfSpeech);
