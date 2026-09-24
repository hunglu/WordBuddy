using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>A personal vocabulary word as returned to callers — owner list, community pool, and the
/// moderation queue all project onto this same shape.</summary>
public sealed record PersonalVocabularyWordDto(
    Guid Id,
    Guid OwnerUserId,
    string Word,
    string Definition,
    string? Example,
    VocabularyShareStatus ShareStatus,
    bool VisibleToChildren,
    DateTime CreatedAtUtc);
