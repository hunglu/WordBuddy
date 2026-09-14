using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

public sealed record LessonDto(Guid Id, string Title, string Description, LessonType Type, Level Level, AgeGroup TargetAgeGroup);
