using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;

/// <summary><paramref name="RequestingAgeGroup"/> comes from the caller's JWT. A Child gets unapproved
/// auto-filled lesson words as "awaiting approval", without content.</summary>
public sealed record GetLessonDetailQuery(Guid LessonId, AgeGroup RequestingAgeGroup) : IQuery<LessonDetailDto>;
