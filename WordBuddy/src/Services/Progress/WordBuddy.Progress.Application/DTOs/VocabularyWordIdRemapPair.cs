namespace WordBuddy.Progress.Application.DTOs;

/// <summary>One merged Content word id, as pulled from Content's internal remap endpoint.</summary>
public sealed record VocabularyWordIdRemapPair(Guid OldId, Guid NewId);
