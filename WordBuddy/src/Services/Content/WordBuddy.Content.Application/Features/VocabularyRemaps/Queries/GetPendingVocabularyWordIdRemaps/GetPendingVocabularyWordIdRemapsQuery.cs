using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.VocabularyRemaps.Queries.GetPendingVocabularyWordIdRemaps;

/// <summary>Pending (not yet acknowledged) word-id remaps for Progress, at most
/// <paramref name="Limit"/> (1–500). Service-to-service only — no user scope.</summary>
public sealed record GetPendingVocabularyWordIdRemapsQuery(int Limit) : IQuery<IReadOnlyList<VocabularyWordIdRemapDto>>;
