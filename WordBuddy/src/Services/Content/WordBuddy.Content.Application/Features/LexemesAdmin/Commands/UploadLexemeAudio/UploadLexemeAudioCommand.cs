using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UploadLexemeAudio;

/// <summary>Sets the UK or US pronunciation of a lexeme from an MP3 stream.
/// <paramref name="Length"/> is the file size in bytes.</summary>
public sealed record UploadLexemeAudioCommand(
    Guid LexemeId,
    string Locale,
    Stream Content,
    string ContentType,
    long Length) : ICommand<MediaAssetDto>
{
    /// <summary>Largest accepted file (5 MB).</summary>
    public const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>The canonical content type; files are stored and served as this.</summary>
    public const string AllowedContentType = "audio/mpeg";

    /// <summary>Alias some browsers and operating systems report for MP3 files.</summary>
    public const string AliasContentType = "audio/mp3";
}
