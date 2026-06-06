namespace WordBuddy.Application.Features.Media.Commands.UploadMedia;

/// <summary>Command to upload a media file and persist a corresponding <see cref="WordBuddy.Domain.Entities.MediaAsset"/> record.</summary>
public sealed record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes);
