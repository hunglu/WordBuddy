using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Identity.Infrastructure.Persistence;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>
/// WB-71: behaviour that must be the same on SQL Server and PostgreSQL — case-insensitive lookups
/// (<c>citext</c> on Postgres) and the filtered unique indexes. Runs on whichever provider the factory selects.
/// </summary>
[Collection(IdentityApiCollection.Name)]
public sealed class ProviderParityTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IdentityApiFactory _factory;

    public ProviderParityTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Account(HttpClient Client, UserDto User, string Email);

    private async Task<Account> RegisterAsync(string email, string ageGroup = "Adult")
    {
        string displayName = $"Par {Guid.NewGuid():N}"[..20];
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { email, password = "Passw0rd!", displayName, ageGroup }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return new Account(Authorized(auth.Token), auth.User, email);
    }

    private HttpClient Authorized(string token)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<HttpClient> LoginAdminAsync()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return Authorized(auth.Token);
    }

    [Fact]
    public async Task Login_EmailInOtherCasing_Succeeds()
    {
        string unique = Guid.NewGuid().ToString("N");
        await RegisterAsync($"User-{unique}@Example.com");

        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = $"user-{unique}@example.com", password = "Passw0rd!" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_EmailInOtherCasing_IsRejectedAsDuplicate()
    {
        string unique = Guid.NewGuid().ToString("N");
        await RegisterAsync($"Dup-{unique}@Example.com");

        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register",
            new { email = $"dup-{unique}@example.com", password = "Passw0rd!", displayName = "Other Name", ageGroup = "Adult" },
            Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ProfileAlias_OtherCasingOfTakenAlias_IsRejected()
    {
        string alias = "Al" + Guid.NewGuid().ToString("N")[..8];
        Account first = await RegisterAsync($"alias1-{Guid.NewGuid():N}@example.com");
        Account second = await RegisterAsync($"alias2-{Guid.NewGuid():N}@example.com");

        (await first.Client.PutAsJsonAsync("/api/auth/profile", new { alias, avatarId = (string?)null }, Json))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        HttpResponseMessage response = await second.Client.PutAsJsonAsync(
            "/api/auth/profile", new { alias = alias.ToUpperInvariant(), avatarId = (string?)null }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AdminUserSearch_LowerCasePartOfUpperCaseEmail_FindsUser()
    {
        string unique = Guid.NewGuid().ToString("N");
        Account adult = await RegisterAsync($"SEARCH-{unique}@EXAMPLE.COM");
        HttpClient admin = await LoginAdminAsync();

        HttpResponseMessage response = await admin.GetAsync($"/api/auth/admin/users?search=search-{unique}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement page = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        page.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("id").GetGuid())
            .Should().Contain(adult.User.Id);
    }

    [Fact]
    public async Task UnlinkRequests_TwoOpenForOneLink_SecondIsRejectedButClosedOneIsAllowed()
    {
        Account learner = await RegisterAsync($"unl1-{Guid.NewGuid():N}@example.com");
        Account partner = await RegisterAsync($"unl2-{Guid.NewGuid():N}@example.com");
        HttpResponseMessage invite = await learner.Client.PostAsJsonAsync(
            "/api/auth/support-links/invitations", new { inviteAs = "Learner", relationship = "Parent" }, Json);
        invite.StatusCode.Should().Be(HttpStatusCode.Created);
        string code = (await invite.Content.ReadFromJsonAsync<CreatedInvitationDto>(Json))!.Code;
        HttpResponseMessage accept = await partner.Client.PostAsJsonAsync("/api/auth/support-links/accept", new { code }, Json);
        accept.StatusCode.Should().Be(HttpStatusCode.OK);
        Guid linkId = (await accept.Content.ReadFromJsonAsync<SupportLinkDto>(Json))!.Id;
        DateTime now = DateTime.UtcNow;

        using IServiceScope scope = _factory.Services.CreateScope();
        IdentityDbContext db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.UnlinkRequests.Add(UnlinkRequest.Create(Guid.NewGuid(), linkId, learner.User.Id, LinkSide.Learner, now));
        await db.SaveChangesAsync();

        // Second open request for the same link violates the filtered unique index.
        db.UnlinkRequests.Add(UnlinkRequest.Create(Guid.NewGuid(), linkId, learner.User.Id, LinkSide.Learner, now));
        Func<Task> second = () => db.SaveChangesAsync();
        (await second.Should().ThrowAsync<DbUpdateException>())
            .Which.ToString().Should().Contain("IX_UnlinkRequests_LinkId_Open");

        // A closed request does not count as open.
        db.ChangeTracker.Clear();
        UnlinkRequest closed = UnlinkRequest.Create(Guid.NewGuid(), linkId, learner.User.Id, LinkSide.Learner, now);
        closed.Cancel(learner.User.Id, now).IsSuccess.Should().BeTrue();
        db.UnlinkRequests.Add(closed);
        await db.SaveChangesAsync();
    }
}
