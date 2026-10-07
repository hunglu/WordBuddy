using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;

/// <summary>Batch read of senses for a review session. <paramref name="RequestingUserId"/> and
/// <paramref name="RequestingAgeGroup"/> come from the caller's JWT. The handler keeps only senses
/// that pass <see cref="Sense.IsVisibleTo"/>; hidden and unknown ids are both omitted.
/// <paramref name="Ids"/> is validated to 1–100 non-empty ids.</summary>
public sealed record GetSensesByIdsQuery(IReadOnlyList<Guid> Ids, Guid RequestingUserId, AgeGroup RequestingAgeGroup)
    : IQuery<IReadOnlyList<SenseReviewDto>>;
