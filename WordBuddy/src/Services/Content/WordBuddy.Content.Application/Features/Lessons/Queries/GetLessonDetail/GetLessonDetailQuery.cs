using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;

public sealed record GetLessonDetailQuery(Guid LessonId) : IQuery<LessonDetailDto>;
