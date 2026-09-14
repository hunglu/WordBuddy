using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;

/// <summary>
/// <paramref name="Content"/> is the raw upload stream — the handler saves it via
/// <c>IFileStorageService</c> before this command is otherwise processed further.
/// </summary>
public sealed record UploadMediaCommand(Stream Content, string FileName, string ContentType) : ICommand<MediaAssetDto>;
