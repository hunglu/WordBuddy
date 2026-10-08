using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Shared.Contracts.SupportLinks;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>WB-24 support-link endpoints on real SQL Server: child and adult lifecycles, child and
/// Primary restrictions, admin override, rate limit, and the outbox events.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class SupportLinkEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IdentityApiFactory _factory;

    public SupportLinkEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Account(HttpClient Client, UserDto User, string Email, string DisplayName);

    private async Task<Account> RegisterAsync(string ageGroup, string name)
    {
        string email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        string displayName = $"{name} {Guid.NewGuid():N}"[..20];
        HttpClient anonymous = _factory.CreateClient();
        HttpResponseMessage response = await anonymous.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "Passw0rd!", displayName, ageGroup }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return new Account(Authorized(auth.Token), auth.User, email, displayName);
    }

    private async Task<HttpClient> LoginAdminAsync()
    {
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/login", new { email = IdentityApiFactory.AdminEmail, password = IdentityApiFactory.AdminPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        return Authorized(auth.Token);
    }

    private HttpClient Authorized(string token)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<CreatedInvitationDto> InviteAsync(HttpClient client, string inviteAs)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/support-links/invitations", new { inviteAs, relationship = "Parent" }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedInvitationDto>(Json))!;
    }

    private static async Task<SupportLinkDto> AcceptAsync(HttpClient client, object body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/support-links/accept", body, Json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SupportLinkDto>(Json))!;
    }

    private static async Task<string?> ProblemTitleAsync(HttpResponseMessage response)
    {
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return problem.GetProperty("title").GetString();
    }

    private async Task<SupportLinkStatus> LinkStatusAsync(Guid linkId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return (await dbContext.SupportLinks.AsNoTracking().SingleAsync(l => l.Id == linkId)).Status;
    }

    [Fact]
    public async Task SupportLinks_ChildLifecycle_PrimaryApprovesUnlinksExtraAndChildIsRestricted()
    {
        Account child = await RegisterAsync("Child", "Kid");
        Account parent = await RegisterAsync("Adult", "Parent");
        Account teacher = await RegisterAsync("Adult", "Teacher");

        // Child without supporter.
        UserDto me = (await child.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!;
        me.HasActiveSupporter.Should().BeFalse();

        // First supporter becomes Primary.
        CreatedInvitationDto first = await InviteAsync(child.Client, "Learner");
        SupportLinkDto primary = await AcceptAsync(parent.Client, new { code = first.Code });
        primary.IsPrimary.Should().BeTrue();
        primary.Status.Should().Be(SupportLinkStatus.Active);
        (await child.Client.GetFromJsonAsync<UserDto>("/api/auth/me", Json))!.HasActiveSupporter.Should().BeTrue();

        // Extra supporter waits for the Primary.
        CreatedInvitationDto second = await InviteAsync(child.Client, "Learner");
        SupportLinkDto extra = await AcceptAsync(teacher.Client, new { token = second.Token });
        extra.Status.Should().Be(SupportLinkStatus.PendingPrimaryApproval);

        List<SupportLinkDto> pending = (await parent.Client.GetFromJsonAsync<List<SupportLinkDto>>(
            "/api/auth/support-links/pending-approvals", Json))!;
        pending.Should().ContainSingle(l => l.Id == extra.Id);
        (await teacher.Client.PostAsync($"/api/auth/support-links/{extra.Id}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await parent.Client.PostAsync($"/api/auth/support-links/{extra.Id}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LinkStatusAsync(extra.Id)).Should().Be(SupportLinkStatus.Active);

        // A child cannot unlink; the Primary link cannot be unlinked.
        HttpResponseMessage childUnlink = await child.Client.PostAsync($"/api/auth/support-links/{extra.Id}/unlink-request", null);
        childUnlink.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        HttpResponseMessage primaryUnlink = await parent.Client.PostAsync($"/api/auth/support-links/{primary.Id}/unlink-request", null);
        primaryUnlink.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ProblemTitleAsync(primaryUnlink)).Should().Be("SupportLink.PrimaryCannotBeUnlinked");

        // The Primary acts for the child; the teacher confirms.
        (await parent.Client.PostAsync($"/api/auth/support-links/{extra.Id}/unlink-request", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await teacher.Client.PostAsync($"/api/auth/support-links/{extra.Id}/unlink-request/confirm", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await LinkStatusAsync(extra.Id)).Should().Be(SupportLinkStatus.Revoked);

        // Child view: only the Primary remains.
        MySupportLinksDto mine = (await child.Client.GetFromJsonAsync<MySupportLinksDto>("/api/auth/support-links", Json))!;
        mine.AsLearner.Should().ContainSingle(l => l.Id == primary.Id);
        mine.HasActiveSupporter.Should().BeTrue();
    }

    [Fact]
    public async Task SupportLinks_AdultLifecycle_EscalatedUnlinkCompletedByAdminOnly()
    {
        Account learner = await RegisterAsync("Adult", "Learner");
        Account partner = await RegisterAsync("Adult", "Partner");

        CreatedInvitationDto offer = await InviteAsync(partner.Client, "Supporter");
        SupportLinkDto link = await AcceptAsync(learner.Client, new { token = offer.Token });
        link.Status.Should().Be(SupportLinkStatus.Active);
        link.IsPrimary.Should().BeFalse();

        (await learner.Client.PostAsync($"/api/auth/support-links/{link.Id}/unlink-request", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await partner.Client.PostAsync($"/api/auth/support-links/{link.Id}/unlink-request/escalate", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden, "only the requester may escalate");
        (await learner.Client.PostAsync($"/api/auth/support-links/{link.Id}/unlink-request/escalate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await learner.Client.GetAsync("/api/auth/admin/unlink-requests")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        HttpClient admin = await LoginAdminAsync();
        List<AdminUnlinkRequestDto> escalated = (await admin.GetFromJsonAsync<List<AdminUnlinkRequestDto>>(
            "/api/auth/admin/unlink-requests", Json))!;
        AdminUnlinkRequestDto request = escalated.Should().ContainSingle(r => r.LinkId == link.Id).Subject;

        (await learner.Client.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{request.Id}/complete", new { reason = "x" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await admin.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{request.Id}/complete", new { reason = "" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.PostAsJsonAsync($"/api/auth/admin/unlink-requests/{request.Id}/complete", new { reason = "No answer" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await LinkStatusAsync(link.Id)).Should().Be(SupportLinkStatus.Revoked);
        using IServiceScope scope = _factory.Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await dbContext.SupportLinkAuditEntries.AsNoTracking().Where(a => a.LinkId == link.Id).Select(a => a.Action).ToListAsync())
            .Should().Contain([SupportLinkAuditAction.LinkActivated, SupportLinkAuditAction.UnlinkRequested,
                SupportLinkAuditAction.UnlinkEscalated, SupportLinkAuditAction.AdminUnlinkCompleted]);
    }

    [Fact]
    public async Task SupportLinks_Accept_RateLimitedWith429AndRetryAfter()
    {
        Account caller = await RegisterAsync("Adult", "Guesser");
        HttpResponseMessage? last = null;

        for (int i = 0; i < 11; i++)
        {
            last = await caller.Client.PostAsJsonAsync("/api/auth/support-links/accept", new { code = "ZZZZ9999" });
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        last.Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Fact]
    public async Task SupportLinks_Outbox_ActivateAndRevokePublishIdsOnlyWithoutPii()
    {
        // The outbox delivers a serialized copy, so a real subscriber on the in-memory bus is the faithful check.
        ConcurrentBag<SupportLinkActivated> activated = [];
        ConcurrentBag<SupportLinkRevoked> revoked = [];
        IBus bus = _factory.Services.GetRequiredService<IBus>();
        HostReceiveEndpointHandle endpoint = bus.ConnectReceiveEndpoint($"identity-tests-{Guid.NewGuid():N}", e =>
        {
            e.Handler<SupportLinkActivated>(context =>
            {
                activated.Add(context.Message);
                return Task.CompletedTask;
            });
            e.Handler<SupportLinkRevoked>(context =>
            {
                revoked.Add(context.Message);
                return Task.CompletedTask;
            });
        });
        await endpoint.Ready;

        try
        {
            Account learner = await RegisterAsync("Adult", "Outbox");
            Account partner = await RegisterAsync("Adult", "Helper");

            SupportLinkDto link = await AcceptAsync(partner.Client, new { code = (await InviteAsync(learner.Client, "Learner")).Code });
            (await learner.Client.PostAsync($"/api/auth/support-links/{link.Id}/unlink-request", null)).EnsureSuccessStatusCode();
            (await partner.Client.PostAsync($"/api/auth/support-links/{link.Id}/unlink-request/confirm", null)).EnsureSuccessStatusCode();

            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (DateTime.UtcNow < deadline && !(activated.Any(m => m.LinkId == link.Id) && revoked.Any(m => m.LinkId == link.Id)))
            {
                await Task.Delay(100);
            }

            SupportLinkActivated message = activated.Should().ContainSingle(m => m.LinkId == link.Id).Subject;
            revoked.Should().ContainSingle(m => m.LinkId == link.Id);
            message.LearnerId.Should().Be(learner.User.Id);
            message.SupporterId.Should().Be(partner.User.Id);

            string payload = JsonSerializer.Serialize(message);
            payload.Should().NotContain(learner.Email).And.NotContain(learner.DisplayName).And.NotContain(partner.Email);
            typeof(SupportLinkActivated).GetProperties().Select(p => p.Name)
                .Should().BeEquivalentTo(["LinkId", "LearnerId", "SupporterId", "OccurredAtUtc"]);
        }
        finally
        {
            await endpoint.StopAsync();
        }
    }
}
