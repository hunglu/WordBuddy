using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>Enrichment of one sense (lexeme data, examples, translations, lists), shared by the
/// word DTOs. <see cref="Empty"/> is used while a child waits for approval.</summary>
public sealed record SenseDetails(
    PartOfSpeech? PartOfSpeech,
    string? IpaUk,
    string? IpaUs,
    string? AudioUkUrl,
    string? AudioUsUrl,
    IReadOnlyList<string> Examples,
    IReadOnlyList<SenseTranslationDto> Translations,
    IReadOnlyList<string> Collocations,
    IReadOnlyList<string> Synonyms,
    IReadOnlyList<string> Antonyms,
    IReadOnlyList<string> TopicTags,
    string? RegisterNote,
    SenseOrigin Origin)
{
    /// <summary>No content, origin kept.</summary>
    public static SenseDetails Empty(SenseOrigin origin) => new(null, null, null, null, null, [], [], [], [], [], [], null, origin);

    /// <summary>Reads the details of <paramref name="sense"/>. Lexeme fields need the lexeme loaded;
    /// manual senses without <see cref="Sense.Examples"/> fall back to <see cref="Sense.Example"/>.</summary>
    public static SenseDetails From(Sense sense)
    {
        Lexeme? lexeme = sense.Lexeme;
        IReadOnlyList<string> examples = sense.Examples.Count > 0
            ? sense.Examples
            : string.IsNullOrWhiteSpace(sense.Example) ? [] : [sense.Example];

        return new SenseDetails(
            lexeme?.PartOfSpeech,
            lexeme?.IpaUk,
            lexeme?.IpaUs,
            lexeme?.UkAudio?.Url,
            lexeme?.UsAudio?.Url,
            examples,
            sense.Translations.Select(t => new SenseTranslationDto(t.Locale, t.Text)).ToList(),
            sense.Collocations,
            sense.Synonyms,
            sense.Antonyms,
            sense.TopicTags,
            sense.RegisterNote,
            sense.Origin);
    }
}
