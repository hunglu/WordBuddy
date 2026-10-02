using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Api;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SyncVocabularyWordIdRemaps;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Services;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// <see cref="SyncVocabularyWordIdRemapsCommand"/> through the host's real DI pipeline (typed client,
/// service-token handler, resilience handler) against a real SQL Server, with only Content's transport
/// replaced by an in-memory fake: stats are rewritten or merged, the acknowledgement carries exactly
/// the applied old ids, and a second run is a no-op. The background sync service is disabled in
/// <see cref="ProgressApiFactory"/>.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class SyncVocabularyWordIdRemapsTests
{
    private readonly ProgressApiFactory _factory;

    public SyncVocabularyWordIdRemapsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> WithFakeContent(FakeContentHandler content) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddHttpClient(ContentVocabularyRemapClient.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => content)));

    private static VocabularyRecallStat Stat(Guid userId, Guid wordId, params bool[] checks)
    {
        VocabularyRecallStat stat = new(Guid.NewGuid(), userId, wordId, "apple");
        foreach (bool known in checks)
        {
            stat.ApplyCheckResult(known);
        }
        return stat;
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemaps_PullsAppliesAndAcknowledges_SecondRunIsNoOp()
    {
        Guid rewriteUser = Guid.NewGuid();
        Guid mergeUser = Guid.NewGuid();
        Guid oldA = Guid.NewGuid();
        Guid oldB = Guid.NewGuid();
        Guid newId = Guid.NewGuid();

        FakeContentHandler content = new([(oldA, newId), (oldB, newId)]);
        using WebApplicationFactory<Program> host = WithFakeContent(content);

        using (IServiceScope scope = host.Services.CreateScope())
        {
            ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            await dbContext.VocabularyRecallStats.AddRangeAsync(
                Stat(rewriteUser, oldA, true, false),
                Stat(mergeUser, newId, true),
                Stat(mergeUser, oldB, false, true));
            await dbContext.SaveChangesAsync();
        }

        Result<int> first = await RunSyncAsync(host);
        Result<int> second = await RunSyncAsync(host);

        first.IsSuccess.Should().BeTrue();
        first.Value.Should().Be(2);
        second.IsSuccess.Should().BeTrue();
        second.Value.Should().Be(0);

        content.Acknowledged.Should().ContainSingle().Which.Should().BeEquivalentTo([oldA, oldB]);
        content.AuthorizationHeaders.Should().OnlyContain(h => h != null && h.StartsWith("Bearer "));

        using IServiceScope verifyScope = host.Services.CreateScope();
        ProgressDbContext verify = verifyScope.ServiceProvider.GetRequiredService<ProgressDbContext>();

        List<VocabularyRecallStat> rewritten = await verify.VocabularyRecallStats.AsNoTracking().Where(s => s.UserId == rewriteUser).ToListAsync();
        rewritten.Should().ContainSingle().Which.Should().Match<VocabularyRecallStat>(s =>
            s.VocabularyWordId == newId && s.TimesChecked == 2 && s.TimesKnown == 1);

        List<VocabularyRecallStat> merged = await verify.VocabularyRecallStats.AsNoTracking().Where(s => s.UserId == mergeUser).ToListAsync();
        merged.Should().ContainSingle().Which.Should().Match<VocabularyRecallStat>(s =>
            s.VocabularyWordId == newId && s.TimesChecked == 3 && s.TimesKnown == 2);
    }

    [Fact]
    public async Task SyncVocabularyWordIdRemaps_ContentNotMigrated_ReturnsUnavailableWithoutChanges()
    {
        FakeContentHandler content = new([]) { NotFound = true };
        using WebApplicationFactory<Program> host = WithFakeContent(content);

        Result<int> result = await RunSyncAsync(host);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContentApi.Unavailable");
        content.Acknowledged.Should().BeEmpty();
    }

    private static async Task<Result<int>> RunSyncAsync(WebApplicationFactory<Program> host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        ICommandHandler<SyncVocabularyWordIdRemapsCommand, int> handler =
            scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncVocabularyWordIdRemapsCommand, int>>();
        return await handler.HandleAsync(new SyncVocabularyWordIdRemapsCommand());
    }

    /// <summary>In-memory stand-in for Content's <c>/internal/vocabulary-remaps</c> endpoints.</summary>
    private sealed class FakeContentHandler : HttpMessageHandler
    {
        private readonly List<(Guid OldId, Guid NewId)> _pending;

        public FakeContentHandler(IEnumerable<(Guid OldId, Guid NewId)> pending)
        {
            _pending = pending.ToList();
        }

        public bool NotFound { get; init; }

        public List<List<Guid>> Acknowledged { get; } = [];

        public List<string?> AuthorizationHeaders { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationHeaders.Add(request.Headers.Authorization?.ToString());

            if (NotFound)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/internal/vocabulary-remaps")
            {
                var items = _pending.Select(p => new { oldId = p.OldId, newId = p.NewId }).ToList();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { items }) };
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/internal/vocabulary-remaps/acknowledge")
            {
                using JsonDocument body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                List<Guid> ids = body.RootElement.GetProperty("oldIds").EnumerateArray().Select(e => e.GetGuid()).ToList();
                Acknowledged.Add(ids);
                _pending.RemoveAll(p => ids.Contains(p.OldId));
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
