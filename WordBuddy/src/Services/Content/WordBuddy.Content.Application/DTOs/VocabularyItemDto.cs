namespace WordBuddy.Content.Application.DTOs;

public sealed record VocabularyItemDto(Guid Id, string Word, string Definition, string Example, MediaAssetDto? Audio);
