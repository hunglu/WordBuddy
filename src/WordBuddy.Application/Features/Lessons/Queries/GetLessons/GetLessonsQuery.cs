using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.Features.Lessons.Queries.GetLessons;

/// <summary>Query to retrieve published lessons with optional filters.</summary>
/// <param name="Type">Filter by content type; <see langword="null"/> returns all types.</param>
/// <param name="Level">Filter by proficiency level; <see langword="null"/> returns all levels.</param>
/// <param name="AgeGroup">Filter by learner age group; <see langword="null"/> returns all age groups.</param>
public sealed record GetLessonsQuery(
    LessonType? Type,
    Level? Level,
    AgeGroup? AgeGroup);
