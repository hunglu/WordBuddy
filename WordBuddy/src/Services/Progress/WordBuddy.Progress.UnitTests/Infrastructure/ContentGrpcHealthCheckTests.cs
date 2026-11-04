using FluentAssertions;
using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using WordBuddy.Progress.Infrastructure.ContentClient;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class ContentGrpcHealthCheckTests
{
    private readonly Mock<Health.HealthClient> _client = new();

    private static AsyncUnaryCall<HealthCheckResponse> Reply(HealthCheckResponse.Types.ServingStatus status) => new(
        Task.FromResult(new HealthCheckResponse { Status = status }),
        Task.FromResult(new Metadata()),
        () => Status.DefaultSuccess,
        () => [],
        () => { });


    [Fact]
    public async Task ContentGrpcHealthCheck_CheckHealthAsync_Serving_ReturnsHealthy()
    {
        _client.Setup(c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Reply(HealthCheckResponse.Types.ServingStatus.Serving));

        HealthCheckResult result = await new ContentGrpcHealthCheck(_client.Object).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Theory]
    [InlineData(HealthCheckResponse.Types.ServingStatus.NotServing)]
    [InlineData(HealthCheckResponse.Types.ServingStatus.Unknown)]
    public async Task ContentGrpcHealthCheck_CheckHealthAsync_NotServing_ReturnsUnhealthy(HealthCheckResponse.Types.ServingStatus status)
    {
        _client.Setup(c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Reply(status));

        HealthCheckResult result = await new ContentGrpcHealthCheck(_client.Object).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task ContentGrpcHealthCheck_CheckHealthAsync_RpcException_ReturnsUnhealthy()
    {
        _client.Setup(c => c.CheckAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<Metadata?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));

        HealthCheckResult result = await new ContentGrpcHealthCheck(_client.Object).CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
