using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;

/// <summary><paramref name="OwnerUserId"/> comes from the authenticated caller's JWT.</summary>
public sealed record GetMyVocabularyWordsQuery(Guid OwnerUserId) : IQuery<IReadOnlyList<PersonalVocabularyWordDto>>;
