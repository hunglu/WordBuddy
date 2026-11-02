using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using FluentAssertions;

namespace WordBuddy.AppHost.Tests;

/// <summary>
/// Starts the real stack. Needs Docker Desktop and packed Shared packages. A cold start (SQL Server first
/// boot, 5 API builds, migrations) can take over 5 minutes on a busy machine, so the timeout is 12 minutes.
/// </summary>
[Trait("Category", "Integration")]
public class AppHostRunTests
{
    [Fact]
    public async Task AppHost_Run_AllApisReachHealthy()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(12));
        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync();
        // The testing builder randomizes ports by default. The services connect to RabbitMQ on 5672 (they have
        // no port setting), so pinned ports must stay pinned or the APIs never become healthy.
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        UseThrowawayContainers(builder);
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

    /// <summary>
    /// The real run keeps containers and data volumes (persistent). A persistent SQL volume keeps the SA
    /// password of the first run, so the test's throwaway password is rejected and nothing becomes healthy.
    /// Tests therefore use session containers with no volume.
    /// </summary>
    private static void UseThrowawayContainers(IDistributedApplicationTestingBuilder builder)
    {
        foreach (IResource resource in builder.Resources.Where(r => r is ContainerResource))
        {
            foreach (ContainerLifetimeAnnotation annotation in resource.Annotations.OfType<ContainerLifetimeAnnotation>().ToList())
            {
                resource.Annotations.Remove(annotation);
            }

            foreach (ContainerMountAnnotation mount in resource.Annotations.OfType<ContainerMountAnnotation>()
                         .Where(m => m.Type == ContainerMountType.Volume).ToList())
            {
                resource.Annotations.Remove(mount);
            }
        }
    }
}
