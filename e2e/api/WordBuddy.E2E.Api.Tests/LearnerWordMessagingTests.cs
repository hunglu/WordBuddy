using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Runs only when the Progress database is reachable: through <c>E2E_PROGRESS_DB</c> (kind/CI, SQL Server),
/// or else through <c>sqlcmd</c> (SQL Server stack) or <c>psql</c> (PostgreSQL stack, WB-71) inside the
/// docker compose database container.
/// Progress has no public reader for memberships yet, so the test reads the table directly.
/// </summary>
public sealed class ProgressDbFactAttribute : FactAttribute
{
    public ProgressDbFactAttribute()
    {
        if (!ProgressDb.IsAvailable)
        {
            Skip = $"Set {ProgressDb.ConnectionVariable} or run the docker compose stack (sqlserver or postgres) to run.";
        }
    }
}

/// <summary>
/// Membership counts from the Progress database. The provider is detected once: SQL Server first, then PostgreSQL.
/// Compose path: the database password stays inside the container, never on the host or in logs.
/// </summary>
internal static class ProgressDb
{
    public const string ConnectionVariable = "E2E_PROGRESS_DB";
    private const string DatabaseName = "WordBuddyProgress";

    private enum Kind
    {
        None,
        ConnectionString,
        ComposeSqlServer,
        ComposePostgres,
    }

    private static readonly Lazy<Kind> Detected = new(Detect);

    private static string? ConnectionString => Environment.GetEnvironmentVariable(ConnectionVariable);

    public static bool IsAvailable => Detected.Value != Kind.None;

    /// <summary>Counts the membership rows of a user, optionally filtered by <c>IsActive</c>.</summary>
    public static async Task<int> CountMembershipsAsync(Guid userId, bool? active = null)
    {
        bool postgres = Detected.Value == Kind.ComposePostgres;
        string filter = active is null
            ? string.Empty
            : postgres ? $" AND \\\"IsActive\\\" = {(active.Value ? "true" : "false")}" : $" AND IsActive = {(active.Value ? 1 : 0)}";
        string sql = postgres
            ? $"SELECT COUNT(*) FROM \\\"LearnerWordMemberships\\\" WHERE \\\"UserId\\\" = '{userId}'{filter}"
            : $"SELECT COUNT(*) FROM LearnerWordMemberships WHERE UserId = '{userId}'{filter}";
        return await ScalarIntAsync(sql);
    }

    private static async Task<int> ScalarIntAsync(string sql)
    {
        if (Detected.Value == Kind.ConnectionString)
        {
            await using SqlConnection connection = new(ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand command = new(sql, connection);
            return (int)(await command.ExecuteScalarAsync())!;
        }

        (int exitCode, string output) = Detected.Value == Kind.ComposePostgres
            ? await RunPsqlAsync(sql)
            : await RunSqlcmdAsync($"SET NOCOUNT ON; {sql}");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"The database query in the compose container failed ({exitCode}).");
        }

        return int.Parse(output.Trim());
    }

    private static Kind Detect()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
        {
            return Kind.ConnectionString;
        }

        if (Probe(() => RunSqlcmdAsync("SET NOCOUNT ON; SELECT 1"), "1"))
        {
            return Kind.ComposeSqlServer;
        }

        return Probe(() => RunPsqlAsync("SELECT 1"), "1") ? Kind.ComposePostgres : Kind.None;
    }

    private static bool Probe(Func<Task<(int ExitCode, string Output)>> run, string expected)
    {
        try
        {
            (int exitCode, string output) = run().GetAwaiter().GetResult();
            return exitCode == 0 && output.Trim() == expected;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static Task<(int ExitCode, string Output)> RunSqlcmdAsync(string sql)
    {
        string script = "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$SA_PASSWORD\" -C -d "
            + DatabaseName + " -h -1 -W -Q \"" + sql + "\"";
        return RunComposeExecAsync(["docker-compose.yml"], "sqlserver", script);
    }

    private static Task<(int ExitCode, string Output)> RunPsqlAsync(string sql)
    {
        // Local socket inside the postgres container: trusted, so no password is needed on the command line.
        string script = "psql -U postgres -d " + DatabaseName + " -tA -c \"" + sql + "\"";
        return RunComposeExecAsync(["docker-compose.yml", "docker-compose.postgres.yml"], "postgres", script);
    }

    private static async Task<(int ExitCode, string Output)> RunComposeExecAsync(string[] files, string service, string script)
    {
        string? composeDir = FindComposeDirectory();
        if (composeDir is null)
        {
            return (-1, string.Empty);
        }

        ProcessStartInfo info = new("docker")
        {
            WorkingDirectory = composeDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        info.ArgumentList.Add("compose");
        foreach (string file in files)
        {
            info.ArgumentList.Add("-f");
            info.ArgumentList.Add(file);
        }

        foreach (string arg in new[] { "exec", "-T", service, "sh", "-c", script })
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
        while (DateTime.UtcNow < deadline)
        {
            if (await ProgressDb.CountMembershipsAsync(userId, expectActive) == 1)
            {
                return true;
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static Task<int> CountRowsAsync(Guid userId) =>
        ProgressDb.CountMembershipsAsync(userId);

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
