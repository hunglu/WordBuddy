using System.IdentityModel.Tokens.Jwt;
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

/// <summary>WB-56 on real SQL Server: Moderator role endpoints, the <c>moderator</c> claim, the admin user list,
/// and the support-link split between admin and moderator.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class ModeratorEndpointsTests
{
    private const string ModeratorEmail = "moderator@wordbuddy.com";
    private const string ModeratorPassword = "Moderator@123";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IdentityApiFactory _factory;

    public ModeratorEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Account(HttpClient Client, AuthTokenDto Auth, string Email, string Password);

    private async Task<Account> RegisterAsync(string ageGroup, string name)
    {
        string email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        string displayName = $"{name} {Guid.NewGuid():N}"[..20];
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { email, password = "Passw0rd!", displayName, ageGroup }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return new Account(Authorized(auth.Token), auth, email, "Passw0rd!");
    }

    private async Task<AuthTokenDto> LoginAsync(string email, string password)
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
    }

    private async Task<HttpClient> LoginAdminAsync() =>
        Authorized((await LoginAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword)).Token);

    private async Task<HttpClient> LoginModeratorAsync() =>
        Authorized((await LoginAsync(ModeratorEmail, ModeratorPassword)).Token);

    private HttpClient Authorized(string token)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static bool HasModeratorClaim(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Any(c => c.Type == "moderator" && c.Value == "true");

    private static Task<HttpResponseMessage> SetModeratorAsync(HttpClient client, Guid userId, bool isModerator) =>
        client.PutAsJsonAsync($"/api/auth/admin/users/{userId}/moderator", new { isModerator });

    [Fact]
    public async Task ModeratorEndpoint_Put_AdminGrantsAndNextLoginTokenHasClaim()
    {
        Account adult = await RegisterAsync("Adult", "Mod");
        HasModeratorClaim(adult.Auth.Token).Should().BeFalse();
        HttpClient admin = await LoginAdminAsync();

        (await SetModeratorAsync(admin, adult.Auth.User.Id, true)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        AuthTokenDto relogin = await LoginAsync(adult.Email, adult.Password);
        HasModeratorClaim(relogin.Token).Should().BeTrue();
        relogin.User.IsModerator.Should().BeTrue();

        (await SetModeratorAsync(admin, adult.Auth.User.Id, false)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        HasModeratorClaim((await LoginAsync(adult.Email, adult.Password)).Token).Should().BeFalse();
    }

    [Fact]
    public async Task ModeratorEndpoint_Put_NonAdminGets403()
    {
        Account learner = await RegisterAsync("Adult", "Plain");
        Account target = await RegisterAsync("Adult", "Target");
        HttpClient moderator = await LoginModeratorAsync();

        (await SetModeratorAsync(moderator, target.Auth.User.Id, true)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SetModeratorAsync(learner.Client, target.Auth.User.Id, true)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ModeratorEndpoint_Put_ChildOrAdminTargetGets400AndUnknownGets404()
    {
        Account child = await RegisterAsync("Child", "Kid");
        HttpClient admin = await LoginAdminAsync();
        Guid adminId = (await LoginAsync(IdentityApiFactory.AdminEmail, IdentityApiFactory.AdminPassword)).User.Id;

        (await SetModeratorAsync(admin, child.Auth.User.Id, true)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetModeratorAsync(admin, adminId, true)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await SetModeratorAsync(admin, Guid.NewGuid(), true)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdminUsersEndpoint_Get_AdminSeesAdultsOnlyAndSearchWorks()
    {
        Account adult = await RegisterAsync("Adult", "Findable");
        Account child = await RegisterAsync("Child", "Hidden");
        HttpClient admin = await LoginAdminAsync();

        List<AdminUserDto> all = await PagedHttp.GetAllAsync<AdminUserDto>(admin, "/api/auth/admin/users", Json);
        all.Should().Contain(u => u.Id == adult.Auth.User.Id);
        all.Should().NotContain(u => u.Id == child.Auth.User.Id);

        List<AdminUserDto> found = await PagedHttp.GetAllAsync<AdminUserDto>(
            admin, $"/api/auth/admin/users?search={Uri.EscapeDataString(adult.Email)}", Json);
        found.Should().ContainSingle().Which.Id.Should().Be(adult.Auth.User.Id);
    }

    [Fact]
    public async Task AdminUsersEndpoint_Get_ModeratorAndLearnerGet403AndBadPagingGets400()
    {
        Account learner = await RegisterAsync("Adult", "Plain");
        HttpClient moderator = await LoginModeratorAsync();
        HttpClient admin = await LoginAdminAsync();

        (await moderator.GetAsync("/api/auth/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await learner.Client.GetAsync("/api/auth/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.GetAsync("/api/auth/admin/users?page=0")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.GetAsync("/api/auth/admin/users?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_SeededModerator_TokenHasClaimAndLearnerTokenDoesNot()
    {
        HasModeratorClaim((await LoginAsync(ModeratorEmail, ModeratorPassword)).Token).Should().BeTrue();
        HasModeratorClaim((await RegisterAsync("Adult", "Plain")).Auth.Token).Should().BeFalse();
    }

    [Fact]
    public async Task UnlinkAdmin_Moderator_SeesAdultRequestsOnlyAndCannotActOnChildRequestsOrHandover()
    {
        // Adult link, escalated.
        Account adultLearner = await RegisterAsync("Adult", "Learner");
        Account partner = await RegisterAsync("Adult", "Partner");
        SupportLinkDto adultLink = await AcceptAsync(adultLearner.Client, (await InviteAsync(partner.Client, "Supporter")).Token);
        await RequestAndEscalateAsync(adultLearner.Client, adultLink.Id);

        // Child link (parent is Primary), unlink of an extra supporter escalated by the Primary.
        Account child = await RegisterAsync("Child", "Kid");
        Account parent = await RegisterAsync("Adult", "Parent");
        Account teacher = await RegisterAsync("Adult", "Teacher");
        await AcceptCodeAsync(parent.Client, (await InviteAsync(child.Client, "Learner")).Code);
        SupportLinkDto extra = await AcceptAsync(teacher.Client, (await InviteAsync(child.Client, "Learner")).Token);
        (await parent.Client.PostAsync($"/api/auth/support-links/{extra.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await RequestAndEscalateAsync(parent.Client, extra.Id);

        HttpClient moderator = await LoginModeratorAsync();
        HttpClient admin = await LoginAdminAsync();

        List<AdminUnlinkRequestDto> moderatorView = await PagedHttp.GetAllAsync<AdminUnlinkRequestDto>(
            moderator, "/api/auth/admin/unlink-requests", Json);
        moderatorView.Should().Contain(r => r.LinkId == adultLink.Id);
        moderatorView.Should().NotContain(r => r.LinkId == extra.Id);

        List<AdminUnlinkRequestDto> adminView = await PagedHttp.GetAllAsync<AdminUnlinkRequestDto>(
            admin, "/api/auth/admin/unlink-requests", Json);
        AdminUnlinkRequestDto childRequest = adminView.Should().ContainSingle(r => r.LinkId == extra.Id).Subject;

        (await moderator.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{childRequest.Id}/complete", new { reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await moderator.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{childRequest.Id}/reject", new { reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);

        (await moderator.GetAsync($"/api/auth/admin/support-links?learnerId={child.Auth.User.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await moderator.PostAsJsonAsync(
            "/api/auth/admin/support-links/handover",
            new { learnerId = child.Auth.User.Id, newPrimaryLinkId = extra.Id, reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The moderator completes the adult request; the child request stays open and admin can still act.
        AdminUnlinkRequestDto adultRequest = moderatorView.Single(r => r.LinkId == adultLink.Id);
        (await moderator.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{adultRequest.Id}/complete", new { reason = "No answer" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LinkStatusAsync(adultLink.Id)).Should().Be(SupportLinkStatus.Revoked);
        (await LinkStatusAsync(extra.Id)).Should().Be(SupportLinkStatus.Active);

        (await admin.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{childRequest.Id}/complete", new { reason = "Escalated" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LinkStatusAsync(extra.Id)).Should().Be(SupportLinkStatus.Revoked);
    }

    [Fact]
    public async Task UnlinkAdmin_Learner_Gets403()
    {
        Account learner = await RegisterAsync("Adult", "Plain");

        (await learner.Client.GetAsync("/api/auth/admin/unlink-requests")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<CreatedInvitationDto> InviteAsync(HttpClient client, string inviteAs)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/support-links/invitations", new { inviteAs, relationship = "Parent" }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedInvitationDto>(Json))!;
    }

    private static async Task<SupportLinkDto> AcceptAsync(HttpClient client, string token)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/support-links/accept", new { token }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SupportLinkDto>(Json))!;
    }

    private static async Task AcceptCodeAsync(HttpClient client, string code)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/support-links/accept", new { code }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task RequestAndEscalateAsync(HttpClient requester, Guid linkId)
    {
        (await requester.PostAsync($"/api/auth/support-links/{linkId}/unlink-request", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await requester.PostAsync($"/api/auth/support-links/{linkId}/unlink-request/escalate", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<SupportLinkStatus> LinkStatusAsync(Guid linkId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return (await dbContext.SupportLinks.AsNoTracking().SingleAsync(l => l.Id == linkId)).Status;
    }
}
