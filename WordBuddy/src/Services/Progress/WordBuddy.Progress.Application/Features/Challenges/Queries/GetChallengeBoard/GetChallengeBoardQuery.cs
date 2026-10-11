using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;

/// <summary>The board of one challenge, filtered for the caller age group.</summary>
public sealed record GetChallengeBoardQuery(Guid UserId, Guid ChallengeId, AgeGroup AgeGroup) : IQuery<ChallengeBoardDto>;
