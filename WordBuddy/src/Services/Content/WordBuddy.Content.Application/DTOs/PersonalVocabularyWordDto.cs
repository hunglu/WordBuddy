using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>A personal vocabulary word as returned to callers — owner list, community pool, and the
/// moderation queue all project onto this same shape.</summary>
/// <param name="IsAuthor">On caller-scoped lists ("mine", recall check): whether the caller wrote the
/// word, as opposed to adopting a shared word or matching a system word. Only authors may request
/// sharing. Always <see langword="false"/> on the pool and moderation lists, which aren't
/// caller-scoped.</param>
/// <param name="IsMine">On the community pool only: whether the caller is the current owner of the
/// word. <see langword="false"/> everywhere else, and for words handed over to the system owner.</param>
/// <param name="AwaitingApproval">Child callers only: an auto-filled word not approved yet. Then the
/// definition is empty and no example, media or enrichment is sent.</param>
/// <remarks>Fields after <paramref name="IsMine"/> were added by WB-25 (auto-fill); old JSON stays intact.</remarks>
public sealed record PersonalVocabularyWordDto(
    Guid Id,
    Guid OwnerUserId,
    string Word,
    string Definition,
    string? Example,
    VocabularyShareStatus ShareStatus,
    bool VisibleToChildren,
    DateTime CreatedAtUtc,
    bool IsAuthor,
    bool IsMine = false,
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
    bool AwaitingApproval = false);
