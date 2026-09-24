using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using WordBuddy.Progress.Api.Models;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// End-to-end coverage of <c>VocabularyRecallController</c> against a real SQL Server database
/// (migrated fresh per test-class run) — never mocks EF Core, per repo convention.
///
/// Requires a reachable SQL Server instance (local SQL Server Express/LocalDB, or set
/// <c>PROGRESS_TEST_CONNECTION_STRING</c>). Not run as part of a normal `dotnet build`/unit-test
/// pass — see the coder's final report for how to run this against a live database.
/// </summary>
public sealed class VocabularyRecallEndpointsTests : IClassFixture<ProgressApiFactory>
{
    private readonly ProgressApiFactory _factory;

    public VocabularyRecallEndpointsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(Guid userId)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId));
        return client;
    }

    [Fact]
    public async Task SubmitCheck_ThenGetProgress_ReflectsUpdatedCountsAndNewSession()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = CreateClient(userId);

        SubmitVocabularyRecallCheckRequest request = new(
        [
            new VocabularyRecallResultItemRequest(Guid.NewGuid(), "apple", true),
            new VocabularyRecallResultItemRequest(Guid.NewGuid(), "banana", false),
        ]);

        HttpResponseMessage submitResponse = await client.PostAsJsonAsync("/api/progress/vocabulary-recall", request);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage progressResponse = await client.GetAsync("/api/progress/vocabulary-recall");
        progressResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        VocabularyRecallProgressDto? progress = await progressResponse.Content.ReadFromJsonAsync<VocabularyRecallProgressDto>();
        progress.Should().NotBeNull();
        progress!.TotalWordsTracked.Should().Be(2);
        progress.KnownCount.Should().Be(1);
        progress.LearningCount.Should().Be(1);
        progress.RecentSessions.Should().ContainSingle(s => s.WordsChecked == 2 && s.WordsKnown == 1);
    }

    [Fact]
    public async Task SubmitCheck_SameWordCheckedTwice_LastCheckWinsAndCountsAccumulate()
    {
        Guid userId = Guid.NewGuid();
        Guid wordId = Guid.NewGuid();
        HttpClient client = CreateClient(userId);

        await client.PostAsJsonAsync(
            "/api/progress/vocabulary-recall",
            new SubmitVocabularyRecallCheckRequest([new VocabularyRecallResultItemRequest(wordId, "apple", true)]));
        await client.PostAsJsonAsync(
            "/api/progress/vocabulary-recall",
            new SubmitVocabularyRecallCheckRequest([new VocabularyRecallResultItemRequest(wordId, "apple", false)]));

        HttpResponseMessage progressResponse = await client.GetAsync("/api/progress/vocabulary-recall");
        VocabularyRecallProgressDto? progress = await progressResponse.Content.ReadFromJsonAsync<VocabularyRecallProgressDto>();

        progress.Should().NotBeNull();
        progress!.TotalWordsTracked.Should().Be(1);
        progress.LearningCount.Should().Be(1);
        progress.KnownCount.Should().Be(0);
        progress.RecentSessions.Should().HaveCount(2);
    }
}
