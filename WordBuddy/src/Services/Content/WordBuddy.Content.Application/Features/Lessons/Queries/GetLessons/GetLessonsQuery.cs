using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;

/// <summary>Lists published lessons. A Child (<paramref name="RequestingAgeGroup"/>, from the JWT) only gets
/// lessons targeted at children; an Adult gets all.</summary>
public sealed record GetLessonsQuery(LessonType? Type, Level? Level, AgeGroup RequestingAgeGroup) : IQuery<IReadOnlyList<LessonDto>>;
