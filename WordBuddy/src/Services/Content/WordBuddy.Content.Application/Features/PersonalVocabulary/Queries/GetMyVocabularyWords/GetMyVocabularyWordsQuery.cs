using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;

/// <summary><paramref name="OwnerUserId"/> and <paramref name="RequestingAgeGroup"/> come from the
/// authenticated caller's JWT. A Child gets unapproved auto-filled words as "awaiting approval", without content.
/// Returns one page, newest first.</summary>
public sealed record GetMyVocabularyWordsQuery(
    Guid OwnerUserId,
    AgeGroup RequestingAgeGroup,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IQuery<PagedResult<PersonalVocabularyWordDto>>;
