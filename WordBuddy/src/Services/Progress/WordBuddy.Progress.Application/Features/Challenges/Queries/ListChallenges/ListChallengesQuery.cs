using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;

/// <summary>One page of the Open and Running challenges of the caller audience (from the <c>age_group</c> claim).</summary>
public sealed record ListChallengesQuery(Guid UserId, AgeGroup AgeGroup, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<ChallengeDto>>;
