using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;

/// <summary>Lists drafts and published lessons for admins. Every filter is optional.</summary>
public sealed record GetAdminLessonsQuery(LessonType? Type, Level? Level, bool? IsPublished)
    : IQuery<IReadOnlyList<AdminLessonDto>>;
