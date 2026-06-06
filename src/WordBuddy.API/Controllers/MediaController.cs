using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Application.DTOs;
using WordBuddy.Application.Features.Media.Commands.UploadMedia;
using WordBuddy.Application.Features.Media.Queries.GetMediaAsset;
using WordBuddy.Application.Interfaces;
using WordBuddy.Domain.Common;

namespace WordBuddy.API.Controllers;

/// <summary>Provides endpoints for uploading and streaming media assets.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class MediaController : ControllerBase
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "audio/mpeg",
        "audio/wav",
        "video/mp4",
    };

    private const long MaxFileSizeBytes = 50L * 1024 * 1024; // 52 428 800 bytes

    private readonly UploadMediaCommandHandler _uploadHandler;
    private readonly GetMediaAssetQueryHandler _getAssetHandler;
    private readonly IFileStorageService _fileStorageService;

    /// <summary>Initializes a new <see cref="MediaController"/>.</summary>
    public MediaController(
        UploadMediaCommandHandler uploadHandler,
        GetMediaAssetQueryHandler getAssetHandler,
        IFileStorageService fileStorageService)
    {
        _uploadHandler = uploadHandler;
        _getAssetHandler = getAssetHandler;
        _fileStorageService = fileStorageService;
    }

    /// <summary>
    /// Uploads a media file and creates a MediaAsset record.
    /// Allowed types: image/jpeg, image/png, audio/mpeg, audio/wav, video/mp4. Maximum size: 50 MB.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    [ProducesResponseType<MediaAssetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(Error.Validation("Upload.Empty", "A non-empty file is required."));

        string contentType = file.ContentType ?? string.Empty;
        if (!AllowedMimeTypes.Contains(contentType))
            return BadRequest(Error.Validation(
                "Upload.UnsupportedMediaType",
                $"Content type '{contentType}' is not allowed. Allowed: image/jpeg, image/png, audio/mpeg, audio/wav, video/mp4."));

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(Error.Validation("Upload.FileTooLarge", "File must not exceed 50 MB."));

        await using Stream fileStream = file.OpenReadStream();

        Result<MediaAssetDto> result = await _uploadHandler.HandleAsync(
            new UploadMediaCommand(fileStream, file.FileName, contentType, file.Length),
            ct);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Streams the raw media file identified by <paramref name="id"/> with the correct Content-Type header.
    /// </summary>
    /// <param name="id">The MediaAsset identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedia(Guid id, CancellationToken ct)
    {
        Result<MediaAssetDto> assetResult = await _getAssetHandler.HandleAsync(
            new GetMediaAssetQuery(id), ct);

        if (assetResult.IsFailure)
            return NotFound(assetResult.Error);

        MediaAssetDto asset = assetResult.Value;

        try
        {
            Stream stream = await _fileStorageService.GetAsync(asset.StorageUrl, ct);
            return new FileStreamResult(stream, asset.MimeType);
        }
        catch (FileNotFoundException)
        {
            return NotFound(Error.NotFound(
                "MediaAsset.FileNotFound",
                $"The file for asset {id} could not be found."));
        }
    }
}
