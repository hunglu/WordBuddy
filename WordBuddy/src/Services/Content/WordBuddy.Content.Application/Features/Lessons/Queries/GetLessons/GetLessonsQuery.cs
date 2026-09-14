using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;

public sealed record GetLessonsQuery(LessonType? Type, Level? Level) : IQuery<IReadOnlyList<LessonDto>>;
