using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Autofill;

/// <summary><see cref="IAudioDownloader"/> over a typed <see cref="HttpClient"/>. Accepts only
/// <c>https</c> URLs and bodies up to <c>Autofill:Audio:MaxBytes</c>.</summary>
internal sealed class HttpAudioDownloader : IAudioDownloader
{
    private readonly HttpClient _httpClient;
    private readonly AutofillClientSettings _settings;
    private readonly ILogger<HttpAudioDownloader> _logger;

    public HttpAudioDownloader(HttpClient httpClient, IOptions<AutofillClientSettings> settings, ILogger<HttpAudioDownloader> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<byte[]>> DownloadAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result.Failure<byte[]>(Error.Validation("Audio.InvalidUrl", "Audio URL must be an https URL."));
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Audio download failed: StatusCode={StatusCode}", (int)response.StatusCode);
                return Result.Failure<byte[]>(Error.Failure("Audio.Unavailable", "The audio file is not available."));
            }

            if (response.Content.Headers.ContentLength > _settings.Audio.MaxBytes)
            {
                return Result.Failure<byte[]>(Error.Failure("Audio.TooLarge", "The audio file is too large."));
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
            return bytes.Length == 0 || bytes.Length > _settings.Audio.MaxBytes
                ? Result.Failure<byte[]>(Error.Failure("Audio.InvalidSize", "The audio file is empty or too large."))
                : Result.Success(bytes);
        }
        catch (Exception ex) when (FreeDictionaryClient.IsTransportFailure(ex, ct))
        {
            _logger.LogWarning(ex, "Audio download failed with a transport error");
            return Result.Failure<byte[]>(Error.Failure("Audio.Unavailable", "The audio file is not available."));
        }
    }
}
