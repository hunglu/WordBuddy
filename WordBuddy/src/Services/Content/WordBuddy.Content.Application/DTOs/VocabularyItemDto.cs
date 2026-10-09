using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>A lesson word. Fields after <paramref name="Audio"/> were added by WB-25 (auto-fill);
/// old JSON stays intact.</summary>
/// <param name="AwaitingApproval">Child callers only: an auto-filled word not approved yet, sent without content.</param>
public sealed record VocabularyItemDto(
    Guid Id,
    string Word,
    string Definition,
    string Example,
    MediaAssetDto? Audio,
    PartOfSpeech? PartOfSpeech = null,
    string? IpaUk = null,
    string? IpaUs = null,
    string? AudioUkUrl = null,
    string? AudioUsUrl = null,
    IReadOnlyList<string>? Examples = null,
    IReadOnlyList<SenseTranslationDto>? Translations = null,
    IReadOnlyList<string>? Collocations = null,
    IReadOnlyList<string>? Synonyms = null,
    IReadOnlyList<string>? Antonyms = null,
    IReadOnlyList<string>? TopicTags = null,
    string? RegisterNote = null,
    SenseOrigin Origin = SenseOrigin.Manual,
    bool AwaitingApproval = false)
{
    /// <summary>Copies <paramref name="details"/> onto <paramref name="item"/>.</summary>
    public static VocabularyItemDto WithDetails(VocabularyItemDto item, SenseDetails details) => item with
    {
        PartOfSpeech = details.PartOfSpeech,
        IpaUk = details.IpaUk,
        IpaUs = details.IpaUs,
        AudioUkUrl = details.AudioUkUrl,
        AudioUsUrl = details.AudioUsUrl,
        Examples = details.Examples,
        Translations = details.Translations,
        Collocations = details.Collocations,
        Synonyms = details.Synonyms,
        Antonyms = details.Antonyms,
        TopicTags = details.TopicTags,
        RegisterNote = details.RegisterNote,
        Origin = details.Origin,
    };
}
