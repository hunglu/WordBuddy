using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace WordBuddy.AppHost.Tests;

/// <summary>Starts the real stack. Needs Docker Desktop, packed Shared packages and a few minutes.</summary>
[Trait("Category", "Integration")]
public class AppHostRunTests
{
    [Fact]
    public async Task AppHost_Run_AllApisReachHealthy()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(5));
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        await using DistributedApplication app = await builder.BuildAsync(cts.Token);
        await app.StartAsync(cts.Token);

        string[] apis = ["identity", "content", "quiz", "progress", "notification"];
        foreach (string api in apis)
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(api, cts.Token);
            using HttpClient client = app.CreateHttpClient(api);
            HttpResponseMessage response = await client.GetAsync("/health/ready", cts.Token);
            response.IsSuccessStatusCode.Should().BeTrue("{0} /health/ready must return 200", api);
        }
    }
}
