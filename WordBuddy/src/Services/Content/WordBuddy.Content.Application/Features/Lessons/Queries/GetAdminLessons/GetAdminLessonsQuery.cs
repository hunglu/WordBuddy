using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;

/// <summary>Pages drafts and published lessons for admins. Every filter is optional.</summary>
public sealed record GetAdminLessonsQuery(
    LessonType? Type,
    Level? Level,
    bool? IsPublished,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IQuery<PagedResult<AdminLessonDto>>;
