namespace WordBuddy.Application.Features.Lessons.Queries.GetLessonDetail;

/// <summary>Query to retrieve a full lesson with all associated vocabulary, grammar rules, and daily phrases.</summary>
public sealed record GetLessonDetailQuery(Guid LessonId);
