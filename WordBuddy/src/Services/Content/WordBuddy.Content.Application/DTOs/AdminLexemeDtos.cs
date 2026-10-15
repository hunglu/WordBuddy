using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>One row of the admin lexeme list. Counts only: private learner text never leaves the
/// server. <paramref name="Lemma"/> is taken from a catalog sense (ADR 0004), or the lower-case
/// normalized lemma when there is none.</summary>
public sealed record AdminLexemeListItemDto(
    Guid Id,
    string Lemma,
    PartOfSpeech? PartOfSpeech,
    string? IpaUk,
    string? IpaUs,
    CefrLevel? CefrLevel,
    int? FrequencyRank,
    bool HasUkAudio,
    bool HasUsAudio,
    int CatalogSenseCount,
    int PrivateLearnerSenseCount);

/// <summary>A catalog sense (system word or shared-pool word) of a lexeme, read-only. Private,
/// pending and rejected learner senses are never listed.</summary>
public sealed record AdminLexemeSenseDto(
    Guid Id,
    string Word,
    string Definition,
    string? Example,
    VocabularySource Source,
    VocabularyShareStatus ShareStatus,
    SenseOrigin Origin,
    bool VisibleToChildren,
    DateTime CreatedAtUtc);

/// <summary>One lexeme for the admin editor. <paramref name="AudioUkUrl"/>/<paramref name="AudioUsUrl"/>
/// are the stored paths; the asset ids let the UI stream a file through <c>/api/media/{id}</c>.</summary>
public sealed record AdminLexemeDetailDto(
    Guid Id,
    string Lemma,
    PartOfSpeech? PartOfSpeech,
    string? IpaUk,
    string? IpaUs,
    CefrLevel? CefrLevel,
    int? FrequencyRank,
    bool HasUkAudio,
    bool HasUsAudio,
    int CatalogSenseCount,
    int PrivateLearnerSenseCount,
    string? Syllables,
    IReadOnlyList<string> WordForms,
    string? AudioUkUrl,
    string? AudioUsUrl,
    Guid? AudioUkAssetId,
    Guid? AudioUsAssetId,
    DateTime CreatedAtUtc,
    IReadOnlyList<AdminLexemeSenseDto> Senses);
