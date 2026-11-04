using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Infrastructure.Health;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// WB-68: Progress reaches Content over gRPC by default and over REST when selected. The REST suite
/// (<see cref="VocabularySrsEndpointsTests"/>) runs on <see cref="ProgressApiFactory"/>; the default
/// transport runs here on <see cref="ProgressGrpcApiFactory"/>.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class ContentTransportTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ProgressApiFactory _restFactory;
    private readonly ProgressGrpcApiFactory _grpcFactory;

    public ContentTransportTests(ProgressApiFactory restFactory, ProgressGrpcApiFactory grpcFactory)
    {
        _restFactory = restFactory;
        _grpcFactory = grpcFactory;
    }

    private static string WordOf(Guid senseId) => "w" + senseId.ToString("N")[..10];

    [Fact]
    public void ContentClient_DefaultHost_UsesGrpcClient()
    {
        using IServiceScope scope = _grpcFactory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IContentSenseClient>().GetType().Name.Should().Be("ContentSenseGrpcClient");
    }

    [Fact]
    public void ContentClient_RestPinnedHost_UsesRestClient()
    {
        using IServiceScope scope = _restFactory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IContentSenseClient>().GetType().Name.Should().Be("ContentSenseClient");
    }

    [Fact]
    public async Task ReadyChecks_DefaultGrpcHost_ListContentAndContentGrpc()
    {
        HealthCheckService health = _grpcFactory.Services.GetRequiredService<HealthCheckService>();

        HealthReport report = await health.CheckHealthAsync(r => r.Tags.Contains(HealthCheckExtensions.ReadyTag));

        report.Entries.Keys.Should().Contain(["content", "content-grpc"]);
        report.Entries["content-grpc"].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public void ReadyChecks_RestPinnedHost_DoNotListContentGrpc()
    {
        HealthCheckServiceOptions options = _restFactory.Services
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>().Value;

        options.Registrations.Select(r => r.Name).Should().Contain("content").And.NotContain("content-grpc");
    }

    [Fact]
    public async Task PostExercises_DefaultGrpcHost_BuildsExerciseFromGrpcSenses()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = _grpcFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, "Adult"));

        List<Guid> senseIds = [];
        DateTime start = DateTime.UtcNow.AddDays(-1);
        for (int i = 0; i < 5; i++)
        {
            Guid senseId = Guid.NewGuid();
            senseIds.Add(senseId);
            _grpcFactory.ContentGrpc.Register(senseId, WordOf(senseId), $"https://img.test/{senseId:N}.png", $"https://aud.test/{senseId:N}.mp3");
            await SeedWordAsync(userId, senseId, start.AddMinutes(i));
        }

        int callsBefore = _grpcFactory.ContentGrpc.Calls;
        HttpResponseMessage sessionResponse = await client.GetAsync("/api/progress/vocabulary/session");
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularySessionDto session = (await sessionResponse.Content.ReadFromJsonAsync<VocabularySessionDto>(Json))!;

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/progress/vocabulary/exercises", new { sessionId = session.SessionId, senseId = senseIds[0] });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        VocabularyExerciseDto exercise = (await response.Content.ReadFromJsonAsync<VocabularyExerciseDto>(Json))!;
        exercise.Prompt.ImageUrl.Should().Contain(senseIds[0].ToString("N"));
        exercise.Options.Select(o => o.Text).Should().Contain(WordOf(senseIds[0]));
        _grpcFactory.ContentGrpc.Calls.Should().BeGreaterThan(callsBefore, "the senses came over gRPC");
    }

    private async Task SeedWordAsync(Guid userId, Guid senseId, DateTime addedAtUtc)
    {
        using IServiceScope scope = _grpcFactory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        dbContext.LearnerWordMemberships.Add(LearnerWordMembership.CreateAdded(Guid.NewGuid(), userId, senseId, userId, addedAtUtc));
        dbContext.LearnerWordStates.Add(LearnerWordState.CreateNew(Guid.NewGuid(), userId, senseId, addedAtUtc));
        await dbContext.SaveChangesAsync();
    }
}
