using WordBuddy.Quiz.Domain;

namespace WordBuddy.Quiz.Application.DTOs;

public sealed record QuizSummaryDto(Guid Id, Guid LessonId, string Title, string Description, Level Level, AgeGroup TargetAgeGroup);
