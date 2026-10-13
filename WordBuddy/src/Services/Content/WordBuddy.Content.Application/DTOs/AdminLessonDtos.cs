using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

/// <summary>One row of the admin lesson list. Drafts are included.</summary>
public sealed record AdminLessonDto(
    Guid Id,
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    AgeGroup TargetAgeGroup,
    bool IsPublished,
    int SenseCount);

/// <summary>A lesson as the editor sees it: draft-capable, with every attached word.</summary>
public sealed record AdminLessonDetailDto(
    Guid Id,
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    AgeGroup TargetAgeGroup,
    bool IsPublished,
    IReadOnlyList<AdminLessonSenseDto> Senses);

/// <summary>A word attached to a lesson. <paramref name="VisibleToChildren"/> is <see langword="false"/>
/// when a Child would not see it in this lesson.</summary>
public sealed record AdminLessonSenseDto(
    Guid SenseId,
    string Word,
    string Definition,
    string? Example,
    int SortOrder,
    bool VisibleToChildren,
    VocabularyShareStatus ShareStatus,
    VocabularySource Source);

/// <summary>A word an admin may attach to a lesson (system or shared pool).</summary>
public sealed record AdminSenseSearchResultDto(
    Guid SenseId,
    string Word,
    string Definition,
    string? Example,
    bool VisibleToChildren,
    VocabularyShareStatus ShareStatus,
    VocabularySource Source);
