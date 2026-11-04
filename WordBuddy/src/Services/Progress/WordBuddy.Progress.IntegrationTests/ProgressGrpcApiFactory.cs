using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WordBuddy.Progress.Api;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Protos.Content.V1;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// WB-68: the Progress host with its <b>default</b> transport (gRPC): no <c>Services:Content:Transport</c>
/// is set. Content is a fake <see cref="ContentSenses.ContentSensesClient"/>; the real client class,
/// session cache and readiness checks still run.
///
/// It shares the database of <see cref="ProgressApiFactory"/> (same collection, so tests run one at a
/// time) and never drops it. The process-wide <c>Services__Content__Transport=Rest</c> variable set by
/// <see cref="ProgressApiFactory"/> is lifted while this host is built, because <c>Program.Main</c> reads
/// it at that moment (see the note in <see cref="ProgressApiFactory"/>).
/// </summary>
public sealed class ProgressGrpcApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TransportVariable = "Services__Content__Transport";

    static ProgressGrpcApiFactory()
    {
        // Connection string, JWT and messaging variables are set by ProgressApiFactory's static constructor.
        RuntimeHelpers.RunClassConstructor(typeof(ProgressApiFactory).TypeHandle);
    }

    /// <summary>The fake Content gRPC server: tests register the senses it returns and read its call count.</summary>
    public FakeContentSensesClient ContentGrpc { get; } = new();

    /// <summary>Answers Content's REST <c>/health</c>, which the "content" readiness check still calls.</summary>
    public StubContentHandler ContentRest { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ContentSenses.ContentSensesClient>(ContentGrpc);
            services.AddSingleton<Health.HealthClient>(new FakeContentHealthClient());
            services.AddHttpClient(ContentHealthCheck.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => ContentRest);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        string? previous = Environment.GetEnvironmentVariable(TransportVariable);
        Environment.SetEnvironmentVariable(TransportVariable, null);
        try
        {
            return base.CreateHost(builder);
        }
        finally
        {
            Environment.SetEnvironmentVariable(TransportVariable, previous);
        }
    }

    public async Task InitializeAsync()
    {
        // Builds the host while the transport variable is lifted (see CreateHost), then makes sure the schema exists.
        using IServiceScope scope = Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // The database belongs to ProgressApiFactory, which drops it.
        await base.DisposeAsync();
    }
}

/// <summary>Stands in for Content's <c>ContentSenses</c> gRPC service. Omits unknown ids like the real one.</summary>
public sealed class FakeContentSensesClient : ContentSenses.ContentSensesClient
{
    private readonly object _lock = new();
    private readonly Dictionary<string, SenseReview> _senses = [];
    private int _calls;

    /// <summary>How many <c>GetSensesByIds</c> calls arrived.</summary>
    public int Calls => Volatile.Read(ref _calls);

    /// <summary>Registers a sense Content would return.</summary>
    public void Register(Guid senseId, string word, string? imageUrl = null, string? audioUrl = null)
    {
        SenseReview sense = new() { SenseId = senseId.ToString(), Word = word, Definition = $"definition {senseId:N}" };
        if (imageUrl is not null)
        {
            sense.ImageUrl = imageUrl;
        }

        if (audioUrl is not null)
        {
            sense.AudioUrl = audioUrl;
        }

        lock (_lock)
        {
            _senses[sense.SenseId] = sense;
        }
    }

    public override AsyncUnaryCall<GetSensesByIdsReply> GetSensesByIdsAsync(GetSensesByIdsRequest request, CallOptions options)
    {
        Interlocked.Increment(ref _calls);
        GetSensesByIdsReply reply = new();
        lock (_lock)
        {
            foreach (string id in request.SenseIds)
            {
                if (_senses.TryGetValue(id, out SenseReview? sense))
                {
                    reply.Senses.Add(sense);
                }
            }
        }

        return new AsyncUnaryCall<GetSensesByIdsReply>(
            Task.FromResult(reply),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }
}

/// <summary>Content's <c>grpc.health.v1</c> service, always <c>SERVING</c>.</summary>
public sealed class FakeContentHealthClient : Health.HealthClient
{
    public override AsyncUnaryCall<HealthCheckResponse> CheckAsync(HealthCheckRequest request, CallOptions options) =>
        new(
            Task.FromResult(new HealthCheckResponse { Status = HealthCheckResponse.Types.ServingStatus.Serving }),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
}
