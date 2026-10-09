using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-25 vocabulary auto-fill, blackbox over HTTP (Identity + Content).
/// Runs against the Development-only fake auto-fill clients (`Autofill__UseFakeClients=true` in the
/// e2e Content container): "serendipity" is known, any other word returns <c>autofillUnavailable = true</c>.
/// The failure-path test uses a nonsense word.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class VocabularyAutofillTests
{
    private const string KnownWord = "serendipity";

    private static readonly string AdminEmail =
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@wordbuddy.com";

    private static readonly string AdminPassword =
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? "Admin@123";

    private readonly ApiRequestContextFixture _fixture;

    public VocabularyAutofillTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdultAddsWordViaAutofill_SenseInMine()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string token = (await RegisterAsync(identity, "Adult")).Token;

            JsonElement result = await LookupAsync(content, token, KnownWord);
            Guid senseId = FirstSenseId(result);

            IAPIResponse add = await content.PostAsync($"/api/vocabulary/autofill/{senseId}/add-to-mine", new() { Headers = Auth(token) });
            add.Ok.Should().BeTrue(await add.TextAsync());

            JsonElement mine = await GetJsonAsync(content, "/api/vocabulary/mine", token);
            mine.EnumerateArray().Should().Contain(w =>
                w.GetProperty("id").GetGuid() == senseId
                && w.GetProperty("origin").GetString() == "AutoFill"
                && !string.IsNullOrEmpty(w.GetProperty("definition").GetString()));
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task ChildAddsWordViaAutofill_HiddenUntilSupporterApproves()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            (string childToken, Guid childId) = await RegisterAsync(identity, "Child");
            string supporterToken = await LinkSupporterAsync(identity, childToken);
            await WaitUntilChildCanLearnAsync(content, childToken);

            JsonElement result = await LookupAsync(content, childToken, KnownWord);
            Guid senseId = FirstSenseId(result);

            (await content.PostAsync($"/api/vocabulary/autofill/{senseId}/add-to-mine", new() { Headers = Auth(childToken) }))
                .Ok.Should().BeTrue();

            JsonElement before = await GetJsonAsync(content, "/api/vocabulary/mine", childToken);
            JsonElement hidden = before.EnumerateArray().Single(w => w.GetProperty("id").GetGuid() == senseId);
            hidden.GetProperty("awaitingApproval").GetBoolean().Should().BeTrue();
            hidden.GetProperty("definition").GetString().Should().BeNullOrEmpty();

            IAPIResponse approve = await content.PostAsync(
                $"/api/vocabulary/learners/{childId}/words/{senseId}/approve", new() { Headers = Auth(supporterToken) });
            approve.Ok.Should().BeTrue(await approve.TextAsync());

            JsonElement after = await GetJsonAsync(content, "/api/vocabulary/mine", childToken);
            JsonElement visible = after.EnumerateArray().Single(w => w.GetProperty("id").GetGuid() == senseId);
            visible.GetProperty("awaitingApproval").GetBoolean().Should().BeFalse();
            visible.GetProperty("definition").GetString().Should().NotBeNullOrEmpty();
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task Lookup_UnknownWord_ReturnsUnavailableAndManualAddStillWorks()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string token = (await RegisterAsync(identity, "Adult")).Token;
            string word = "zzqxautofill" + new string(Guid.NewGuid().ToString("N")[..12].Select(c => (char)('a' + (c % 26))).ToArray());

            IAPIResponse lookup = await content.GetAsync($"/api/vocabulary/autofill?word={word}", new() { Headers = Auth(token) });
            lookup.Status.Should().Be(200, await lookup.TextAsync());
            JsonElement body = (await lookup.JsonAsync())!.Value;
            body.GetProperty("autofillUnavailable").GetBoolean().Should().BeTrue();
            body.GetProperty("senses").GetArrayLength().Should().Be(0);

            IAPIResponse add = await content.PostAsync("/api/vocabulary", new()
            {
                Headers = Auth(token),
                DataObject = new { word, definition = "added by hand after auto-fill failed", example = (string?)null },
            });
            add.Ok.Should().BeTrue(await add.TextAsync());
            Guid id = (await add.JsonAsync())!.Value.GetGuid();

            JsonElement mine = await GetJsonAsync(content, "/api/vocabulary/mine", token);
            mine.EnumerateArray().Should().Contain(w => w.GetProperty("id").GetGuid() == id);
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task Lookup_ChildWithoutSupporter_ReturnsForbidden()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string childToken = (await RegisterAsync(identity, "Child")).Token;

            IAPIResponse lookup = await content.GetAsync($"/api/vocabulary/autofill?word={KnownWord}", new() { Headers = Auth(childToken) });

            lookup.Status.Should().Be(403, await lookup.TextAsync());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task PendingApprovals_NonSupporterAdult_ReturnsForbidden()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            Guid childId = (await RegisterAsync(identity, "Child")).UserId;
            string strangerToken = (await RegisterAsync(identity, "Adult")).Token;

            IAPIResponse pending = await content.GetAsync(
                $"/api/vocabulary/learners/{childId}/pending-approvals", new() { Headers = Auth(strangerToken) });

            pending.Status.Should().Be(403, await pending.TextAsync());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AdminAutofillQueue_NonAdminForbidden_AdminOk()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string adultToken = (await RegisterAsync(identity, "Adult")).Token;
            string adminToken = await LoginAsync(identity, AdminEmail, AdminPassword);

            IAPIResponse asAdult = await content.GetAsync("/api/vocabulary/moderation/autofill-pending", new() { Headers = Auth(adultToken) });
            IAPIResponse asAdmin = await content.GetAsync("/api/vocabulary/moderation/autofill-pending", new() { Headers = Auth(adminToken) });

            asAdult.Status.Should().Be(403);
            asAdmin.Status.Should().Be(200, await asAdmin.TextAsync());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    [Fact]
    public async Task AddToMine_UnknownSense_ReturnsNotFound()
    {
        IAPIRequestContext identity = await _fixture.NewContextAsync(ServiceUrls.Identity);
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            string token = (await RegisterAsync(identity, "Adult")).Token;

            IAPIResponse add = await content.PostAsync($"/api/vocabulary/autofill/{Guid.NewGuid()}/add-to-mine", new() { Headers = Auth(token) });

            add.Status.Should().Be(404, await add.TextAsync());
        }
        finally
        {
            await identity.DisposeAsync();
            await content.DisposeAsync();
        }
    }

    private static async Task<JsonElement> LookupAsync(IAPIRequestContext content, string token, string word)
    {
        IAPIResponse lookup = await content.GetAsync($"/api/vocabulary/autofill?word={word}", new() { Headers = Auth(token) });
        lookup.Status.Should().Be(200, await lookup.TextAsync());
        JsonElement body = (await lookup.JsonAsync())!.Value;
        body.GetProperty("autofillUnavailable").GetBoolean().Should().BeFalse(
            "auto-fill must return senses; the e2e Content container needs Autofill__UseFakeClients=true");
        return body;
    }

    private static Guid FirstSenseId(JsonElement result) =>
        result.GetProperty("senses").EnumerateArray().First().GetProperty("senseId").GetGuid();

    /// <summary>Same flow as <see cref="ChildSupport.LinkSupporterAsync"/>, but returns the supporter token.</summary>
    private static async Task<string> LinkSupporterAsync(IAPIRequestContext identity, string childToken)
    {
        string supporterToken = (await RegisterAsync(identity, "Adult")).Token;

        IAPIResponse invitation = await identity.PostAsync("/api/auth/support-links/invitations", new()
        {
            Headers = Auth(childToken),
            DataObject = new { inviteAs = "Learner", relationship = (string?)null },
        });
        invitation.Status.Should().Be(201, await invitation.TextAsync());
        string code = (await invitation.JsonAsync())!.Value.GetProperty("code").GetString()!;

        IAPIResponse accept = await identity.PostAsync("/api/auth/support-links/accept", new()
        {
            Headers = Auth(supporterToken),
            DataObject = new { code, token = (string?)null },
        });
        accept.Status.Should().Be(200, await accept.TextAsync());
        return supporterToken;
    }

    private static async Task WaitUntilChildCanLearnAsync(IAPIRequestContext content, string childToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            if ((await content.GetAsync("/api/lessons", new() { Headers = Auth(childToken) })).Status == 200)
            {
                return;
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException("Child supporter link did not reach Content within 30s.");
    }

    private static async Task<(string Token, Guid UserId)> RegisterAsync(IAPIRequestContext identity, string ageGroup)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/register", new()
        {
            DataObject = new
            {
                email = $"autofill-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E Autofill Learner",
                ageGroup,
            },
        });
        response.Ok.Should().BeTrue(await response.TextAsync());
        string token = (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
        return (token, ReadSubject(token));
    }

    /// <summary>Reads the user id from the JWT payload (display only, no verification).</summary>
    private static Guid ReadSubject(string jwt)
    {
        string payload = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');
        using JsonDocument doc = JsonDocument.Parse(Convert.FromBase64String(payload));
        JsonElement root = doc.RootElement;
        string sub = root.TryGetProperty("sub", out JsonElement s)
            ? s.GetString()!
            : root.GetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").GetString()!;
        return Guid.Parse(sub);
    }

    private static async Task<string> LoginAsync(IAPIRequestContext identity, string email, string password)
    {
        IAPIResponse response = await identity.PostAsync("/api/auth/login", new() { DataObject = new { email, password } });
        response.Ok.Should().BeTrue(await response.TextAsync());
        return (await response.JsonAsync())!.Value.GetProperty("token").GetString()!;
    }

    private static async Task<JsonElement> GetJsonAsync(IAPIRequestContext content, string path, string token)
    {
        IAPIResponse response = await content.GetAsync(path, new() { Headers = Auth(token) });
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value;
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
