using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;

/// <summary><paramref name="RequestingAgeGroup"/> comes from the authenticated caller's JWT. The
/// handler — not just the controller's <c>[Authorize]</c> policy — filters to
/// <see cref="Sense.VisibleToChildren"/> items when the requester is a Child, so a
/// Child-authenticated call can never receive a pool item a moderator didn't explicitly clear.
/// <paramref name="RequestingUserId"/> (also from the JWT) sets <c>IsMine</c> on each item.
/// <paramref name="Search"/> keeps words that contain the text (case-insensitive). Returns one page.</summary>
public sealed record GetSharedVocabularyWordsQuery(
    AgeGroup RequestingAgeGroup,
    Guid RequestingUserId,
    string? Search = null,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IQuery<PagedResult<PersonalVocabularyWordDto>>;
