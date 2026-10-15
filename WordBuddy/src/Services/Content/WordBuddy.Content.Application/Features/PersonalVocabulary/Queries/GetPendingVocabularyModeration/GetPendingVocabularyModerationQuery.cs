using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;

/// <summary>The admin-only moderation queue, one page at a time. Callers are gated by the
/// <c>AdminOnly</c> authorization policy at the controller.</summary>
public sealed record GetPendingVocabularyModerationQuery(int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<PersonalVocabularyWordDto>>;
