using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;

/// <summary>One page of the board of one challenge, filtered for the caller age group. A child always gets the
/// single page of the top 5 and the own row: <paramref name="Page"/> and <paramref name="PageSize"/> are ignored.</summary>
public sealed record GetChallengeBoardQuery(
    Guid UserId,
    Guid ChallengeId,
    AgeGroup AgeGroup,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IQuery<ChallengeBoardDto>;
