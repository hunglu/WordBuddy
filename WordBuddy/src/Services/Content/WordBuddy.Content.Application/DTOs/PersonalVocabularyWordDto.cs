using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>A personal vocabulary word as returned to callers — owner list, community pool, and the
/// moderation queue all project onto this same shape.</summary>
/// <param name="IsAuthor">On caller-scoped lists ("mine", recall check): whether the caller wrote the
/// word, as opposed to adopting a shared word or matching a system word. Only authors may request
/// sharing. Always <see langword="false"/> on the pool and moderation lists, which aren't
/// caller-scoped.</param>
public sealed record PersonalVocabularyWordDto(
    Guid Id,
    Guid OwnerUserId,
    string Word,
    string Definition,
    string? Example,
    VocabularyShareStatus ShareStatus,
    bool VisibleToChildren,
    DateTime CreatedAtUtc,
    bool IsAuthor);
