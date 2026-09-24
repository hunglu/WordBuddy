using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;

/// <summary><paramref name="OwnerUserId"/> comes from the authenticated caller's JWT.
/// <paramref name="Count"/> is validated 1–50.</summary>
public sealed record GetRandomVocabularyWordsForCheckQuery(Guid OwnerUserId, int Count) : IQuery<IReadOnlyList<PersonalVocabularyWordDto>>;
