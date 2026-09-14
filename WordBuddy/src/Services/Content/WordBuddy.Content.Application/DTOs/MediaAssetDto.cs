using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.DTOs;

public sealed record MediaAssetDto(Guid Id, MediaAssetType Type, string Url);
