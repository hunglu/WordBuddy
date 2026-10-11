using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Contracts.SupportLinks;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>WB-24 policies on real SQL Server: <c>ChildHasSupporter</c> on the SRS session and
/// <c>CanSupportLearner</c> on the supporter settings endpoints, incl. a revoke through the consumer.</summary>
[Collection(ProgressApiCollection.Name)]
public sealed class SupportLinkEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ProgressApiFactory _factory;

    public SupportLinkEndpointsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup));
        return client;
    }

    private async Task<SupportLinkProjection?> FindProjectionAsync(Guid linkId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await dbContext.SupportLinkProjections.AsNoTracking().FirstOrDefaultAsync(p => p.LinkId == linkId);
    }

    private async Task WaitForProjectionAsync(Guid linkId, bool isActive)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            SupportLinkProjection? projection = await FindProjectionAsync(linkId);
            if (projection is not null && projection.IsActive == isActive)
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Projection {linkId} did not reach IsActive={isActive}.");
    }

    [Fact]
    public async Task VocabularySrsController_GetSession_ChildWithoutLinkReturns403SupporterRequired()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Child").GetAsync("/api/progress/vocabulary/session");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        problem.GetProperty("title").GetString().Should().Be("Learner.SupporterRequired");
    }

    [Fact]
    public async Task VocabularySrsController_PostReviews_ChildWithoutLinkReturns403()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Child").PostAsJsonAsync(
            "/api/progress/vocabulary/reviews",
            new { exerciseId = Guid.NewGuid(), answer = new { optionKey = "abc" }, clientResponseMs = 3000, hintUsed = false });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VocabularySrsController_GetSession_ChildWithLinkReturns200()
    {
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId);

        HttpResponseMessage response = await Client(childId, "Child").GetAsync("/api/progress/vocabulary/session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VocabularySrsController_GetSession_AdultWithoutLinkReturns200()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Adult").GetAsync("/api/progress/vocabulary/session");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VocabularySrsController_LearnerSettings_ActiveSupporterSetsCapAndLearnerSeesIt()
    {
        Guid learnerId = Guid.NewGuid();
        Guid supporterId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, learnerId, supporterId);
        HttpClient supporter = Client(supporterId, "Adult");

        HttpResponseMessage put = await supporter.PutAsJsonAsync(
            $"/api/progress/vocabulary/learners/{learnerId}/settings", new { supporterNewWordCap = 3 });
        HttpResponseMessage get = await supporter.GetAsync($"/api/progress/vocabulary/learners/{learnerId}/settings");

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        LearnerVocabularySettingsDto? dto = await get.Content.ReadFromJsonAsync<LearnerVocabularySettingsDto>(Json);
        dto!.SupporterNewWordCap.Should().Be(3);
        dto.SupporterCapSetBy.Should().Be(supporterId);

        VocabularySettingsDto? own = await (await Client(learnerId, "Child").GetAsync("/api/progress/vocabulary/settings"))
            .Content.ReadFromJsonAsync<VocabularySettingsDto>(Json);
        own!.SupporterNewWordCap.Should().Be(3);
    }

    [Fact]
    public async Task VocabularySrsController_LearnerSettings_NoLinkReturns403()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Adult")
            .GetAsync($"/api/progress/vocabulary/learners/{Guid.NewGuid()}/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VocabularySrsController_LearnerSettings_RevokedLinkReturns403AfterConsumeAndCapCleared()
    {
        Guid linkId = Guid.NewGuid();
        Guid learnerId = Guid.NewGuid();
        Guid supporterId = Guid.NewGuid();
        ITestHarness harness = _factory.Services.GetRequiredService<ITestHarness>();
        HttpClient supporter = Client(supporterId, "Adult");

        await harness.Bus.Publish(new SupportLinkActivated(linkId, learnerId, supporterId, DateTime.UtcNow.AddMinutes(-1)));
        await WaitForProjectionAsync(linkId, isActive: true);
        (await supporter.PutAsJsonAsync($"/api/progress/vocabulary/learners/{learnerId}/settings", new { supporterNewWordCap = 2 }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await harness.Bus.Publish(new SupportLinkRevoked(linkId, learnerId, supporterId, DateTime.UtcNow));
        await WaitForProjectionAsync(linkId, isActive: false);

        (await supporter.GetAsync($"/api/progress/vocabulary/learners/{learnerId}/settings"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        VocabularyLearnerSettings settings = await dbContext.VocabularyLearnerSettings.AsNoTracking().SingleAsync(s => s.UserId == learnerId);
        settings.SupporterNewWordCap.Should().BeNull();
    }
}
