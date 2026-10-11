using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>Readiness check: Content answers its own <c>/health</c> (liveness) endpoint.</summary>
public sealed class ContentHealthCheck : IHealthCheck
{
    /// <summary>Name of the <see cref="HttpClient"/> used by this check.</summary>
    public const string HttpClientName = "content-health";

    private readonly IHttpClientFactory _httpClientFactory;

    public ContentHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = _httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.GetAsync("health", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Content returned {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Content is not reachable.", ex);
        }
    }
}
