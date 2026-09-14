using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;
using WordBuddy.Content.Application.Features.Media.Queries.GetMediaAsset;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Media asset upload and retrieval — audio pronunciations, images, and video clips.</summary>
[ApiController]
[Route("api/media")]
public sealed class MediaController : ControllerBase
{
    private const long MaxUploadBytes = 50 * 1024 * 1024;

    private readonly ICommandHandler<UploadMediaCommand, MediaAssetDto> _uploadMedia;
    private readonly IQueryHandler<GetMediaAssetQuery, MediaAssetDto> _getMediaAsset;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(
        ICommandHandler<UploadMediaCommand, MediaAssetDto> uploadMedia,
        IQueryHandler<GetMediaAssetQuery, MediaAssetDto> getMediaAsset,
        IFileStorageService fileStorageService,
        ILogger<MediaController> logger)
    {
        _uploadMedia = uploadMedia;
        _getMediaAsset = getMediaAsset;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>Uploads a media file (max 50MB; image/jpeg, image/png, audio/mpeg, audio/wav, video/mp4).</summary>
    [Authorize]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return Problem(title: "UploadMedia.Empty", detail: "The uploaded file is empty.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (file.Length > MaxUploadBytes)
        {
            return Problem(title: "UploadMedia.TooLarge", detail: "The uploaded file exceeds the 50MB limit.", statusCode: StatusCodes.Status400BadRequest);
        }

        await using Stream stream = file.OpenReadStream();
        Result<MediaAssetDto> result = await _uploadMedia.HandleAsync(
            new UploadMediaCommand(stream, file.FileName, file.ContentType), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("Upload succeeded: MediaAssetId={MediaAssetId}", result.Value.Id);
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>Streams a previously uploaded media file back.</summary>
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        Result<MediaAssetDto> assetResult = await _getMediaAsset.HandleAsync(new GetMediaAssetQuery(id), ct);
        if (assetResult.IsFailure)
        {
            return assetResult.ToProblemResult(this);
        }

        MediaAssetDto asset = assetResult.Value;
        Stream stream = await _fileStorageService.GetAsync(asset.Url, ct);
        return File(stream, ToContentType(asset.Type));
    }

    private static string ToContentType(MediaAssetType type) => type switch
    {
        MediaAssetType.Image => "image/jpeg",
        MediaAssetType.Audio => "audio/mpeg",
        MediaAssetType.Video => "video/mp4",
        _ => "application/octet-stream",
    };
}
