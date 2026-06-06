using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.DTOs;

/// <summary>Read model for a <see cref="WordBuddy.Domain.Entities.MediaAsset"/>.</summary>
public sealed record MediaAssetDto(
    Guid Id,
    string FileName,
    string StorageUrl,
    MediaAssetType Type,
    long FileSizeBytes,
    string MimeType,
    DateTime UploadedAt);
