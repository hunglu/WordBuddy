using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Runs only when <c>E2E_PROGRESS_DB</c> holds a connection string to the Progress database.
/// Progress has no public reader for memberships yet, so the test reads the table directly.
/// </summary>
public sealed class ProgressDbFactAttribute : FactAttribute
{
    public ProgressDbFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LearnerWordMessagingTests.ProgressDbVariable)))
        {
            Skip = $"Set {LearnerWordMessagingTests.ProgressDbVariable} to the Progress DB connection string to run.";
        }
    }
}

/// <summary>
/// WB-21: a word added or deleted in Content reaches Progress through RabbitMQ
/// (Content outbox → broker → Progress inbox → <c>LearnerWordMemberships</c>).
/// Needs the full stack with RabbitMQ (docker compose or kind) and both migrations applied.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class LearnerWordMessagingTests
{
    public const string ProgressDbVariable = "E2E_PROGRESS_DB";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ApiRequestContextFixture _fixture;

    public LearnerWordMessagingTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [ProgressDbFact]
    public async Task AddWord_AdultLearner_CreatesActiveMembershipInProgress()
    {
        await AddWord_CreatesActiveMembership("Adult");
    }

    [ProgressDbFact]
    public async Task AddWord_ChildLearner_CreatesActiveMembershipInProgress()
    {
        await AddWord_CreatesActiveMembership("Child");
    }

    [ProgressDbFact]
    public async Task DeleteWord_AdultLearner_MarksMembershipInactive()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string token = await RegisterLearnerAsync(identity, "Adult");
            Guid userId = UserIdFrom(token);
            Guid wordId = await AddWordAsync(content, token);
            (await WaitForMembershipAsync(userId, expectActive: true)).Should().BeTrue("the add event should arrive first");

            IAPIResponse delete = await content.DeleteAsync($"/api/vocabulary/{wordId}", new APIRequestContextOptions { Headers = AuthHeader(token) });
            delete.Ok.Should().BeTrue($"delete should succeed, got {delete.Status}: {await delete.TextAsync()}");

            (await WaitForMembershipAsync(userId, expectActive: false))
                .Should().BeTrue("LearnerWordRemoved should mark the row inactive and keep it");
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private async Task AddWord_CreatesActiveMembership(string ageGroup)
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string token = await RegisterLearnerAsync(identity, ageGroup);
            Guid userId = UserIdFrom(token);
            await AddWordAsync(content, token);

            (await WaitForMembershipAsync(userId, expectActive: true))
                .Should().BeTrue($"LearnerWordAdded for a {ageGroup} learner should reach Progress within {Timeout.TotalSeconds}s");
            (await CountRowsAsync(userId)).Should().Be(1, "one link change gives exactly one membership row");
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task<bool> WaitForMembershipAsync(Guid userId, bool expectActive)
    {
        DateTime deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            await using SqlConnection connection = new(Environment.GetEnvironmentVariable(ProgressDbVariable));
            await connection.OpenAsync();
            await using SqlCommand command = new(
                "SELECT COUNT(*) FROM LearnerWordMemberships WHERE UserId = @u AND IsActive = @a", connection);
            command.Parameters.AddWithValue("@u", userId);
            command.Parameters.AddWithValue("@a", expectActive);
            if ((int)(await command.ExecuteScalarAsync())! == 1)
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static async Task<int> CountRowsAsync(Guid userId)
    {
        await using SqlConnection connection = new(Environment.GetEnvironmentVariable(ProgressDbVariable));
        await connection.OpenAsync();
        await using SqlCommand command = new("SELECT COUNT(*) FROM LearnerWordMemberships WHERE UserId = @u", connection);
        command.Parameters.AddWithValue("@u", userId);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<Guid> AddWordAsync(IAPIRequestContext content, string token)
    {
        IAPIResponse response = await content.PostAsync("/api/vocabulary", new APIRequestContextOptions
        {
            Headers = AuthHeader(token),
            DataObject = new { word = $"messaging-{Guid.NewGuid():N}", definition = "an e2e definition", example = (string?)null },
        });
        response.Ok.Should().BeTrue($"adding a word should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetGuid();
    }

    private static async Task<string> RegisterLearnerAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"msg-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E Messaging Learner",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue($"self-registration should succeed, got {response.Status}: {await response.TextAsync()}");
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    /// <summary>Reads the <c>sub</c> claim (no signature check; test use only).</summary>
    private static Guid UserIdFrom(string token)
    {
        string payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using JsonDocument doc = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
        return Guid.Parse(doc.RootElement.GetProperty("sub").GetString()!);
    }

    private static Dictionary<string, string> AuthHeader(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
