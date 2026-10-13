using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// WB-20: the <c>age_band</c> claim. The claim reader treats a missing or unknown value as null (fallback to the
/// age group), and a review made with a band claim is accepted and appended without touching earlier rows.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class AgeBandReviewTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ProgressApiFactory _factory;

    public AgeBandReviewTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    [Theory]
    [InlineData("Under7", AgeBand.Under7)]
    [InlineData("Age7To9", AgeBand.Age7To9)]
    [InlineData("Age10To12", AgeBand.Age10To12)]
    [InlineData("Age13To17", AgeBand.Age13To17)]
    [InlineData("Adult", AgeBand.Adult)]
    public void ClaimsPrincipalExtensions_GetAgeBand_KnownClaimParsed(string claim, AgeBand expected)
    {
        Principal(new Claim("age_band", claim)).GetAgeBand().Should().Be(expected);
    }

    [Theory]
    [InlineData("Teenager")]
    [InlineData("42")]
    [InlineData("")]
    public void ClaimsPrincipalExtensions_GetAgeBand_UnknownClaimIsNull(string claim)
    {
        Principal(new Claim("age_band", claim)).GetAgeBand().Should().BeNull();
    }

    [Fact]
    public void ClaimsPrincipalExtensions_GetAgeBand_MissingClaimIsNull()
    {
        Principal(new Claim("age_group", "Child")).GetAgeBand().Should().BeNull();
    }

    [Fact]
    public async Task PostReviews_WithAgeBandClaim_IsAcceptedAndEarlierRowsStayUnchanged()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.Empty;
        DateTime start = DateTime.UtcNow.AddDays(-1);
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();

            // Five words, so the session stays open after the first word is answered.
            for (int i = 0; i < 5; i++)
            {
                Guid id = Guid.NewGuid();
                senseId = i == 0 ? id : senseId;
                _factory.Content.Register(id, WordOf(id), $"https://img.test/{id:N}.png", $"https://aud.test/{id:N}.mp3", hiddenForChild: false);
                DateTime added = start.AddMinutes(i);
                db.LearnerWordMemberships.Add(LearnerWordMembership.CreateAdded(Guid.NewGuid(), userId, id, userId, added));
                db.LearnerWordStates.Add(LearnerWordState.CreateNew(Guid.NewGuid(), userId, id, added));
            }

            await db.SaveChangesAsync();
        }

        string word = WordOf(senseId);

        // First answer without a band, second with one: the first row must not change.
        VocabularyReviewResultDto first = await AnswerAsync(userId, senseId, word, ageBand: null);
        first.IsCorrect.Should().BeTrue();
        List<ReviewLog> afterFirst = await LogsAsync(userId);
        afterFirst.Should().ContainSingle();

        VocabularyReviewResultDto second = await AnswerAsync(userId, senseId, word, ageBand: "Under7");
        second.IsCorrect.Should().BeTrue();

        List<ReviewLog> afterSecond = await LogsAsync(userId);
        afterSecond.Should().HaveCount(2);
        afterSecond[0].Should().BeEquivalentTo(afterFirst[0], o => o.ComparingByMembers<ReviewLog>());
    }

    private static string WordOf(Guid senseId) => "w" + senseId.ToString("N")[..10];

    private async Task<VocabularyReviewResultDto> AnswerAsync(Guid userId, Guid senseId, string word, string? ageBand)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestJwtTokenFactory.CreateToken(userId, "Adult", ageBand: ageBand));

        HttpResponseMessage sessionResponse = await client.GetAsync("/api/progress/vocabulary/session");
        sessionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularySessionDto session = (await sessionResponse.Content.ReadFromJsonAsync<VocabularySessionDto>(Json))!;

        HttpResponseMessage exerciseResponse = await client.PostAsJsonAsync(
            "/api/progress/vocabulary/exercises", new { sessionId = session.SessionId, senseId });
        exerciseResponse.StatusCode.Should().Be(HttpStatusCode.OK, await exerciseResponse.Content.ReadAsStringAsync());
        VocabularyExerciseDto exercise = (await exerciseResponse.Content.ReadFromJsonAsync<VocabularyExerciseDto>(Json))!;

        string? optionKey = exercise.ExerciseType == ExerciseType.Typing ? null : exercise.Options.Single(o => o.Text == word).Key;
        string? text = exercise.ExerciseType == ExerciseType.Typing ? word : null;
        object answer = new { optionKey, text };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/progress/vocabulary/reviews",
            new { exerciseId = exercise.ExerciseId, answer, clientResponseMs = 0, hintUsed = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<VocabularyReviewResultDto>(Json))!;
    }

    private async Task<List<ReviewLog>> LogsAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await db.ReviewLogs.AsNoTracking().Where(l => l.UserId == userId).OrderBy(l => l.AttemptNo).ToListAsync();
    }
}
