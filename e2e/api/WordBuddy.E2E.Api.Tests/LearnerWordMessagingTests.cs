using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Runs only when the Progress database is reachable: through <c>E2E_PROGRESS_DB</c> (kind/CI),
/// or else through <c>sqlcmd</c> inside the docker compose <c>sqlserver</c> container.
/// Progress has no public reader for memberships yet, so the test reads the table directly.
/// </summary>
public sealed class ProgressDbFactAttribute : FactAttribute
{
    public ProgressDbFactAttribute()
    {
        if (!ProgressDb.IsAvailable)
        {
            Skip = $"Set {ProgressDb.ConnectionVariable} or run the docker compose stack (sqlserver) to run.";
        }
    }
}

/// <summary>
/// Scalar queries against the Progress database.
/// Compose path: the SA password expands inside the container, never on the host or in logs.
/// </summary>
internal static class ProgressDb
{
    public const string ConnectionVariable = "E2E_PROGRESS_DB";
    private const string DatabaseName = "WordBuddyProgress";

    private static readonly Lazy<bool> ComposeAvailable = new(ProbeCompose);

    private static string? ConnectionString => Environment.GetEnvironmentVariable(ConnectionVariable);

    public static bool IsAvailable => !string.IsNullOrWhiteSpace(ConnectionString) || ComposeAvailable.Value;

    /// <summary>Runs a query that returns one integer. Callers must only embed trusted values (Guids, bits).</summary>
    public static async Task<int> ScalarIntAsync(string sql)
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            await using SqlConnection connection = new(ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand command = new(sql, connection);
            return (int)(await command.ExecuteScalarAsync())!;
        }

        (int exitCode, string output) = await RunSqlcmdAsync($"SET NOCOUNT ON; {sql}");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"sqlcmd in the sqlserver container failed ({exitCode}).");
        }

        return int.Parse(output.Trim());
    }

    private static bool ProbeCompose()
    {
        try
        {
            (int exitCode, string output) = RunSqlcmdAsync("SET NOCOUNT ON; SELECT 1").GetAwaiter().GetResult();
            return exitCode == 0 && output.Trim() == "1";
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<(int ExitCode, string Output)> RunSqlcmdAsync(string sql)
    {
        string? composeDir = FindComposeDirectory();
        if (composeDir is null)
        {
            return (-1, string.Empty);
        }

        string script = "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$SA_PASSWORD\" -C -d "
            + DatabaseName + " -h -1 -W -Q \"" + sql + "\"";
        ProcessStartInfo info = new("docker")
        {
            WorkingDirectory = composeDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string arg in new[] { "compose", "exec", "-T", "sqlserver", "sh", "-c", script })
        {
            info.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(info)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
        await process.WaitForExitAsync(cts.Token);
        await stderr;
        return (process.ExitCode, await stdout);
    }

    /// <summary>Walks up from the test output folder to <c>WordBuddy/docker-compose.yml</c>.</summary>
    private static string? FindComposeDirectory()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "WordBuddy");
            if (File.Exists(Path.Combine(candidate, "docker-compose.yml")))
            {
                return candidate;
            }
        }

        return null;
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
        string sql = $"SELECT COUNT(*) FROM LearnerWordMemberships WHERE UserId = '{userId}' AND IsActive = {(expectActive ? 1 : 0)}";
        while (DateTime.UtcNow < deadline)
        {
            if (await ProgressDb.ScalarIntAsync(sql) == 1)
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static Task<int> CountRowsAsync(Guid userId) =>
        ProgressDb.ScalarIntAsync($"SELECT COUNT(*) FROM LearnerWordMemberships WHERE UserId = '{userId}'");

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
        string token = (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
        if (ageGroup == "Child")
        {
            // WB-24: a child needs an active supporter before learning.
            await ChildSupport.LinkSupporterAsync(identity, token);
        }

        return token;
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
