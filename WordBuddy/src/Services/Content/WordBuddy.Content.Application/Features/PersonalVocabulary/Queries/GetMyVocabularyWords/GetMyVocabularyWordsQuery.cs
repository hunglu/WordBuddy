using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;

/// <summary><paramref name="OwnerUserId"/> and <paramref name="RequestingAgeGroup"/> come from the
/// authenticated caller's JWT. A Child gets unapproved auto-filled words as "awaiting approval", without content.</summary>
public sealed record GetMyVocabularyWordsQuery(Guid OwnerUserId, AgeGroup RequestingAgeGroup) : IQuery<IReadOnlyList<PersonalVocabularyWordDto>>;
