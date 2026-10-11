using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Contracts.Profiles;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>WB-29 on real SQL Server: time zone endpoint, admin Guardian endpoint (403 for non-admin), the
/// <c>guardian</c> JWT claim, the profile backfill and the <c>LearnerProfileUpdated</c> events.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class ProfileChallengeEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IdentityApiFactory _factory;

    public ProfileChallengeEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Account(HttpClient Client, AuthTokenDto Auth, string Email, string Password);

    private async Task<Account> RegisterAsync(string ageGroup, string name, string? timeZoneId = null)
    {
        string email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new { email, password = "Passw0rd!", displayName = $"{name} Real", ageGroup, timeZoneId },
            Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return new Account(Authorized(auth.Token), auth, email, "Passw0rd!");
    }

    private async Task<HttpClient> LoginAdminAsync()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword });
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return Authorized(auth.Token);
    }

    private async Task<string> LoginTokenAsync(Account account)
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = account.Email, password = account.Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!.Token;
    }

    private HttpClient Authorized(string token)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static bool HasGuardianClaim(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Any(c => c.Type == "guardian" && c.Value == "true");

    [Fact]
    public async Task TimeZoneEndpoint_Put_SavesZoneAndMeShowsIt()
    {
        Account adult = await RegisterAsync("Adult", "Zone");

        HttpResponseMessage response = await adult.Client.PutAsJsonAsync("/api/auth/profile/time-zone", new { timeZoneId = "Asia/Ho_Chi_Minh" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
        (await adult.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
    }

    [Fact]
    public async Task TimeZoneEndpoint_Put_UnknownZoneIsBadRequest()
    {
        Account adult = await RegisterAsync("Adult", "BadZone");

        HttpResponseMessage response = await adult.Client.PutAsJsonAsync("/api/auth/profile/time-zone", new { timeZoneId = "Mars/Olympus_Mons" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TimeZoneEndpoint_Put_WithoutTokenIsUnauthorized()
    {
        HttpResponseMessage response = await _factory.CreateClient().PutAsJsonAsync("/api/auth/profile/time-zone", new { timeZoneId = "UTC" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_UnknownTimeZoneIsBadRequest()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"zone-{Guid.NewGuid():N}@example.com", password = "Passw0rd!", displayName = "Zed", ageGroup = "Adult", timeZoneId = "Mars/Olympus_Mons" },
            Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GuardianEndpoint_NonAdminIsForbidden()
    {
        Account adult = await RegisterAsync("Adult", "NotAdmin");

        HttpResponseMessage response = await adult.Client.PutAsJsonAsync(
            $"/api/auth/admin/users/{adult.Auth.User.Id}/guardian", new { isGuardian = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GuardianEndpoint_AdminGrantsAdultAndTokenCarriesClaimOnlyForGuardians()
    {
        HttpClient admin = await LoginAdminAsync();
        Account guardian = await RegisterAsync("Adult", "Guard");
        Account plain = await RegisterAsync("Adult", "Plain");

        HttpResponseMessage grant = await admin.PutAsJsonAsync($"/api/auth/admin/users/{guardian.Auth.User.Id}/guardian", new { isGuardian = true });

        grant.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HasGuardianClaim(await LoginTokenAsync(guardian)).Should().BeTrue();
        HasGuardianClaim(await LoginTokenAsync(plain)).Should().BeFalse();
        (await Authorized(await LoginTokenAsync(guardian)).GetFromJsonAsync<UserDto>("/api/auth/me", Json))!.IsGuardian.Should().BeTrue();

        (await admin.PutAsJsonAsync($"/api/auth/admin/users/{guardian.Auth.User.Id}/guardian", new { isGuardian = false }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        HasGuardianClaim(await LoginTokenAsync(guardian)).Should().BeFalse();
    }

    [Fact]
    public async Task GuardianEndpoint_ChildAccountIsBadRequest()
    {
        HttpClient admin = await LoginAdminAsync();
        Account child = await RegisterAsync("Child", "Kid");

        HttpResponseMessage response = await admin.PutAsJsonAsync($"/api/auth/admin/users/{child.Auth.User.Id}/guardian", new { isGuardian = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        HasGuardianClaim(await LoginTokenAsync(child)).Should().BeFalse();
    }

    [Fact]
    public async Task GuardianEndpoint_UnknownUserIsNotFound()
    {
        HttpClient admin = await LoginAdminAsync();

        HttpResponseMessage response = await admin.PutAsJsonAsync($"/api/auth/admin/users/{Guid.NewGuid()}/guardian", new { isGuardian = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RepublishEndpoint_NonAdminIsForbiddenAndAdminCanRunItTwice()
    {
        Account adult = await RegisterAsync("Adult", "Backfill");
        (await adult.Client.PostAsync("/api/auth/admin/learner-profiles/republish", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        HttpClient admin = await LoginAdminAsync();
        HttpResponseMessage first = await admin.PostAsync("/api/auth/admin/learner-profiles/republish", null);
        HttpResponseMessage second = await admin.PostAsync("/api/auth/admin/learner-profiles/republish", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("published").GetInt32().Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task ProfileEvents_RegisterTimeZoneAndGuardian_PublishOneEventEachWithoutChildName()
    {
        ConcurrentBag<LearnerProfileUpdated> received = [];
        IBus bus = _factory.Services.GetRequiredService<IBus>();
        HostReceiveEndpointHandle endpoint = bus.ConnectReceiveEndpoint($"identity-profile-tests-{Guid.NewGuid():N}", e =>
        {
            e.Handler<LearnerProfileUpdated>(context =>
            {
                received.Add(context.Message);
                return Task.CompletedTask;
            });
        });
        await endpoint.Ready;

        try
        {
            HttpClient admin = await LoginAdminAsync();
            Account adult = await RegisterAsync("Adult", "Events", "Asia/Ho_Chi_Minh");
            Account child = await RegisterAsync("Child", "Small");
            (await adult.Client.PutAsJsonAsync("/api/auth/profile/time-zone", new { timeZoneId = "America/New_York" })).EnsureSuccessStatusCode();
            (await admin.PutAsJsonAsync($"/api/auth/admin/users/{adult.Auth.User.Id}/guardian", new { isGuardian = true })).EnsureSuccessStatusCode();

            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline
                && !(received.Count(m => m.UserId == adult.Auth.User.Id) >= 3 && received.Any(m => m.UserId == child.Auth.User.Id)))
            {
                await Task.Delay(100);
            }

            List<LearnerProfileUpdated> adultEvents = [.. received.Where(m => m.UserId == adult.Auth.User.Id).OrderBy(m => m.OccurredAtUtc)];
            adultEvents.Should().HaveCount(3);
            adultEvents[0].TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
            adultEvents[0].DisplayName.Should().Be("Events Real");
            adultEvents[1].TimeZoneId.Should().Be("America/New_York");
            adultEvents[2].IsGuardian.Should().BeTrue();

            LearnerProfileUpdated childEvent = received.Should().ContainSingle(m => m.UserId == child.Auth.User.Id).Subject;
            childEvent.AgeGroup.Should().Be("Child");
            childEvent.DisplayName.Should().BeNull();
            JsonSerializer.Serialize(childEvent).Should().NotContain(child.Email).And.NotContain("Small Real");
        }
        finally
        {
            await endpoint.StopAsync();
        }
    }
}
