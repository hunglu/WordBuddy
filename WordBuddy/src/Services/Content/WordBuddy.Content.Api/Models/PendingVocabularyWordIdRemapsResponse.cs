using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Api.Models;

/// <summary>Body of <c>GET /internal/vocabulary-remaps</c> — <c>{ items: [{ oldId, newId }] }</c>.</summary>
public sealed record PendingVocabularyWordIdRemapsResponse(IReadOnlyList<VocabularyWordIdRemapDto> Items);
