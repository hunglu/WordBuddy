using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Smoke tests proving this project is wired end to end against the real, running services.
/// Requires the backend to be up (e.g. <c>docker compose up</c> from <c>WordBuddy/</c>).
/// This is the pattern to follow when adding a new API E2E test class: one class per
/// service/feature, a fresh <see cref="IAPIRequestContext"/> per test via the shared
/// <see cref="ApiRequestContextFixture"/>.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public class HealthCheckTests
{
    private readonly ApiRequestContextFixture _fixture;

    public HealthCheckTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(nameof(ServiceUrls.Identity))]
    [InlineData(nameof(ServiceUrls.Content))]
    [InlineData(nameof(ServiceUrls.Quiz))]
    [InlineData(nameof(ServiceUrls.Progress))]
    public async Task GetHealth_ServiceIsRunning_ReturnsOk(string serviceName)
    {
        string baseUrl = serviceName switch
        {
            nameof(ServiceUrls.Identity) => ServiceUrls.Identity,
            nameof(ServiceUrls.Content) => ServiceUrls.Content,
            nameof(ServiceUrls.Quiz) => ServiceUrls.Quiz,
            nameof(ServiceUrls.Progress) => ServiceUrls.Progress,
            _ => throw new ArgumentOutOfRangeException(nameof(serviceName)),
        };

        IAPIRequestContext context = await _fixture.NewContextAsync(baseUrl);
        try
        {
            IAPIResponse response = await context.GetAsync("/health");

            response.Ok.Should().BeTrue($"{serviceName}'s liveness check should report healthy");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }
}
