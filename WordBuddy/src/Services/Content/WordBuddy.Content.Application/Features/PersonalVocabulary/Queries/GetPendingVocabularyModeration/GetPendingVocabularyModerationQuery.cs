using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;

/// <summary>The admin-only moderation queue — no parameters, callers are gated by the
/// <c>AdminOnly</c> authorization policy at the controller.</summary>
public sealed record GetPendingVocabularyModerationQuery : IQuery<IReadOnlyList<PersonalVocabularyWordDto>>;
