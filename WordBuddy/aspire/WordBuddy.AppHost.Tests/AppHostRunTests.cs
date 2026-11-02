using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    /// Proves traces leave each API for the endpoint the AppHost injected (not <c>:4317</c>). A fake OTLP/gRPC
    /// receiver (Kestrel, HTTP/2 without TLS, free port) is the dashboard OTLP URL. Protobuf stores strings as
    /// raw UTF-8, so each request body is searched for <c>WordBuddy.&lt;Service&gt;</c> without decoding it.
    /// The gRPC protocol is the real <c>aspire run</c> default; the testing builder cannot select HTTP.
    /// </summary>
    [Fact]
    public async Task AppHost_Run_TracesReachOtlpEndpoint()
    {
        using CancellationTokenSource cts = new(TimeSpan.FromMinutes(12));
        string[] services = ["Identity", "Content", "Quiz", "Progress", "Notification"];
        HashSet<string> seen = [];
        object gate = new();

        int port = GetFreePort();
        await using WebApplication receiver = await StartFakeOtlpReceiverAsync(port, body =>
        {
            string text = Encoding.UTF8.GetString(body);
            lock (gate)
            {
                foreach (string service in services.Where(sv => text.Contains($"WordBuddy.{sv}", StringComparison.Ordinal)))
                {
                    seen.Add(service);
                }
            }
        });

        IDistributedApplicationTestingBuilder builder = await AppHostFixture.CreateAsync(
            $"ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:{port}");
        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        UseThrowawayContainers(builder);
        await using DistributedApplication app = await builder.BuildAsync(cts.Token);
        await app.StartAsync(cts.Token);

        foreach (string api in new[] { "identity", "content", "quiz", "progress", "notification" })
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(api, cts.Token);
            using HttpClient client = app.CreateHttpClient(api);
            (await client.GetAsync("/health/ready", cts.Token)).IsSuccessStatusCode.Should().BeTrue("{0} must be ready", api);
        }

        // The batch exporter flushes about every 5 s; allow a few cycles.
        DateTime deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            lock (gate)
            {
                if (seen.Count == services.Length)
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
        }

        lock (gate)
        {
            seen.Should().BeEquivalentTo(services, "every API must export traces to the injected OTLP endpoint");
        }
    }

    private static int GetFreePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Minimal OTLP/gRPC trace receiver: replies with an empty gRPC success to any POST.</summary>
    private static async Task<WebApplication> StartFakeOtlpReceiverAsync(int port, Action<byte[]> onRequest)
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(port, o => o.Protocols = HttpProtocols.Http2));
        WebApplication web = builder.Build();
        web.MapPost("/{**path}", async (HttpContext context) =>
        {
            using MemoryStream buffer = new();
            await context.Request.Body.CopyToAsync(buffer);
            onRequest(buffer.ToArray());
            context.Response.ContentType = "application/grpc";
            await context.Response.Body.WriteAsync(new byte[5]); // empty message: flag 0 + length 0
            context.Response.AppendTrailer("grpc-status", "0");
        });
        await web.StartAsync();
        return web;
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
