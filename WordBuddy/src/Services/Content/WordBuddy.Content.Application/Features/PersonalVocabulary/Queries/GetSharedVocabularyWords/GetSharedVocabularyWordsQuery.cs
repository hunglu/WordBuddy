using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;

/// <summary><paramref name="RequestingAgeGroup"/> comes from the authenticated caller's JWT. The
/// handler — not just the controller's <c>[Authorize]</c> policy — filters to
/// <see cref="VocabularyWord.VisibleToChildren"/> items when the requester is a Child, so a
/// Child-authenticated call can never receive a pool item a moderator didn't explicitly clear.</summary>
public sealed record GetSharedVocabularyWordsQuery(AgeGroup RequestingAgeGroup) : IQuery<IReadOnlyList<PersonalVocabularyWordDto>>;
