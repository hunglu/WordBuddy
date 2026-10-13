using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;

/// <summary>Open and Running challenges of the caller audience (from the <c>age_group</c> claim).</summary>
public sealed record ListChallengesQuery(Guid UserId, AgeGroup AgeGroup) : IQuery<IReadOnlyList<ChallengeDto>>;
