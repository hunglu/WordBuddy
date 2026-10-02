namespace WordBuddy.Content.Application.DTOs;

/// <summary>One merged vocabulary word id: <paramref name="OldId"/> no longer exists and every
/// reference to it should point at <paramref name="NewId"/>. Carries word ids only — no user ids,
/// no word text.</summary>
public sealed record VocabularyWordIdRemapDto(Guid OldId, Guid NewId);
