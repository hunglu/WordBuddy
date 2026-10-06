using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WordBuddy.Shared.Infrastructure.Health;
using WordBuddy.Shared.Infrastructure.Messaging;

namespace WordBuddy.Content.IntegrationTests.Messaging;

/// <summary><c>/health/ready</c> includes the message broker check.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class BrokerHealthCheckTests
{
    private readonly ContentApiFactory _factory;

    public BrokerHealthCheckTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void HealthChecks_Registration_IncludesBrokerCheckTaggedReady()
    {
        HealthCheckServiceOptions options = _factory.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.Should().Contain(r =>
            r.Name == MessagingExtensions.HealthCheckName && r.Tags.Contains(HealthCheckExtensions.ReadyTag));
    }

    [Fact]
    public async Task HealthReady_WithBrokerRunning_ReturnsOk()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync(HealthCheckExtensions.ReadinessPath);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
