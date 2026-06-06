using WordBuddy.Domain.Enums;

namespace WordBuddy.API.Models.Requests;

/// <summary>Request body for creating a new lesson.</summary>
public sealed record CreateLessonRequest(
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    TargetAgeGroup TargetAgeGroup,
    int OrderIndex);
