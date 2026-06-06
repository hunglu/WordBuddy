using WordBuddy.Domain.Enums;

namespace WordBuddy.Domain.Entities;

/// <summary>A media asset (image, audio, or video) stored externally and referenced by domain entities.</summary>
public sealed class MediaAsset
{
    /// <summary>Initializes a new <see cref="MediaAsset"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="fileName">Original file name of the asset.</param>
    /// <param name="storageUrl">URL at which the asset is accessible in blob or CDN storage.</param>
    /// <param name="type">Media format of the asset.</param>
    /// <param name="fileSizeBytes">File size in bytes.</param>
    /// <param name="mimeType">MIME type (e.g. <c>audio/mpeg</c>, <c>image/png</c>).</param>
    /// <param name="uploadedAt">UTC timestamp when the asset was uploaded.</param>
    public MediaAsset(
        Guid id,
        string fileName,
        string storageUrl,
        MediaAssetType type,
        long fileSizeBytes,
        string mimeType,
        DateTime uploadedAt)
    {
        Id = id;
        FileName = fileName;
        StorageUrl = storageUrl;
        Type = type;
        FileSizeBytes = fileSizeBytes;
        MimeType = mimeType;
        UploadedAt = uploadedAt;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the original file name of the asset.</summary>
    public string FileName { get; }

    /// <summary>Gets the URL at which the asset is accessible in blob or CDN storage.</summary>
    public string StorageUrl { get; }

    /// <summary>Gets the media format of the asset.</summary>
    public MediaAssetType Type { get; }

    /// <summary>Gets the file size in bytes.</summary>
    public long FileSizeBytes { get; }

    /// <summary>Gets the MIME type (e.g. <c>audio/mpeg</c>, <c>image/png</c>).</summary>
    public string MimeType { get; }

    /// <summary>Gets the UTC timestamp when the asset was uploaded.</summary>
    public DateTime UploadedAt { get; }
}
