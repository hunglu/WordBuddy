namespace WordBuddy.Content.Application.DTOs;

public sealed record DailyPhraseDto(Guid Id, string Phrase, string Translation, MediaAssetDto? Audio, MediaAssetDto? Video);
