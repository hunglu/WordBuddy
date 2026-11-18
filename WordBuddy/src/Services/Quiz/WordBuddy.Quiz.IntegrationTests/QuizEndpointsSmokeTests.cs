using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace WordBuddy.Quiz.IntegrationTests;

/// <summary>
/// Smoke suite for the Quiz API on the selected database provider (SQL Server by default,
/// PostgreSQL with <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>): the host starts and migrates, the
/// seeded quiz is readable, and the JSON options column round-trips.
/// </summary>
[Collection(QuizApiCollection.Name)]
public sealed class QuizEndpointsSmokeTests
{
    private const string SeededTitle = "Present Simple Check";

    private readonly QuizApiFactory _factory;

    public QuizEndpointsSmokeTests(QuizApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(string ageGroup)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", QuizApiFactory.CreateToken(Guid.NewGuid(), ageGroup));
        return client;
    }

    private static async Task<Guid> SeededQuizIdAsync(HttpClient client)
    {
        JsonElement list = await client.GetFromJsonAsync<JsonElement>("/api/quiz");
        return list.EnumerateArray().First(q => q.GetProperty("title").GetString() == SeededTitle).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Health_Ready_ReturnsHealthyAfterMigration()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Quiz_Anonymous_Returns401()
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync("/api/quiz");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Adult")]
    [InlineData("Child")]
    public async Task Quiz_List_AdultAndChildCanReadSeededQuiz(string ageGroup)
    {
        HttpClient client = Client(ageGroup);

        HttpResponseMessage response = await client.GetAsync("/api/quiz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid id = await SeededQuizIdAsync(client);
        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Quiz_GetById_OptionsJsonTextRoundTripsAndAnswersAreHidden()
    {
        HttpClient client = Client("Adult");
        Guid id = await SeededQuizIdAsync(client);

        JsonElement quiz = await client.GetFromJsonAsync<JsonElement>($"/api/quiz/{id}");

        JsonElement first = quiz.GetProperty("questions").EnumerateArray()
            .First(q => q.GetProperty("text").GetString() == "She ___ to school every day.");
        first.GetProperty("options").EnumerateArray().Select(o => o.GetString())
            .Should().Equal("go", "goes", "going");
        first.TryGetProperty("correctOptionIndex", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Quiz_Submit_CorrectOptionIsMarkedCorrect()
    {
        HttpClient client = Client("Adult");
        Guid id = await SeededQuizIdAsync(client);
        JsonElement quiz = await client.GetFromJsonAsync<JsonElement>($"/api/quiz/{id}");
        Guid questionId = quiz.GetProperty("questions").EnumerateArray()
            .First(q => q.GetProperty("text").GetString() == "She ___ to school every day.").GetProperty("id").GetGuid();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/quiz/{id}/submit", new { questionId, selectedOptionIndex = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("isCorrect").GetBoolean().Should().BeTrue();
    }
}
