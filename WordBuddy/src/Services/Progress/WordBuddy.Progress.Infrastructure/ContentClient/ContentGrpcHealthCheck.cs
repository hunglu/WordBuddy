using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>Readiness check for the gRPC channel: Content answers the standard <c>grpc.health.v1</c>
/// <c>Check</c> with <c>SERVING</c>. Registered only when <see cref="ContentTransport.Grpc"/> is selected.</summary>
public sealed class ContentGrpcHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(3);

    private readonly Health.HealthClient _client;

    public ContentGrpcHealthCheck(Health.HealthClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            HealthCheckResponse response = await _client.CheckAsync(
                new HealthCheckRequest(),
                deadline: DateTime.UtcNow.Add(Deadline),
                cancellationToken: cancellationToken);

            return response.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Content gRPC reported {response.Status}.");
        }
        catch (Exception ex) when (ex is RpcException or HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Content gRPC is not reachable.", ex);
        }
    }
}
