using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessonDetail;

/// <summary>One lesson for the editor: drafts allowed, no child filter.</summary>
public sealed record GetAdminLessonDetailQuery(Guid LessonId) : IQuery<AdminLessonDetailDto>;
