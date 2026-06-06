using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.DTOs;

/// <summary>Lightweight lesson representation for list views.</summary>
public sealed record LessonDto(
    Guid Id,
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    TargetAgeGroup TargetAgeGroup,
    bool IsPublished,
    int OrderIndex,
    DateTime CreatedAt);
