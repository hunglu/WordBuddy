using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>WB-27 group endpoints on real SQL Server: create, add a child (pending), Primary approval,
/// owner-only access, child restrictions, and removal when the support link ends.</summary>
[Collection(IdentityApiCollection.Name)]
public sealed class GroupEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IdentityApiFactory _factory;

    public GroupEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Account(HttpClient Client, UserDto User);

    private async Task<Account> RegisterAsync(string ageGroup, string name)
    {
        string email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";
        string displayName = $"{name} {Guid.NewGuid():N}"[..20];
        HttpResponseMessage response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new { email, password = "Passw0rd!", displayName, ageGroup }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        AuthTokenDto auth = (await response.Content.ReadFromJsonAsync<AuthTokenDto>(Json))!;
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new Account(client, auth.User);
    }

    private static async Task<CreatedInvitationDto> InviteAsLearnerAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/support-links/invitations", new { inviteAs = "Learner", relationship = "Parent" }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreatedInvitationDto>(Json))!;
    }

    private static async Task<SupportLinkDto> AcceptAsync(HttpClient client, string code)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/support-links/accept", new { code }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<SupportLinkDto>(Json))!;
    }

    private static async Task<Guid> CreateGroupAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/groups", new { name }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return body.GetProperty("id").GetGuid();
    }

    private static async Task<List<AddMemberResultDto>> AddMembersAsync(HttpClient client, Guid groupId, params Guid[] learnerIds)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/auth/groups/{groupId}/members", new { learnerIds }, Json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<AddMemberResultDto>>(Json))!;
    }

    [Fact]
    public async Task Groups_ChildMember_PendingUntilPrimaryApprovesThenRemovedWhenLinkEnds()
    {
        Account parent = await RegisterAsync("Adult", "Parent");
        Account teacher = await RegisterAsync("Adult", "Teacher");
        Account child = await RegisterAsync("Child", "Kid");

        // Parent is Primary; the teacher is an approved extra supporter.
        await AcceptAsync(parent.Client, (await InviteAsLearnerAsync(child.Client)).Code);
        SupportLinkDto extra = await AcceptAsync(teacher.Client, (await InviteAsLearnerAsync(child.Client)).Code);
        (await parent.Client.PostAsync($"/api/auth/support-links/{extra.Id}/approve", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The teacher adds the child: pending.
        Guid groupId = await CreateGroupAsync(teacher.Client, "Class 5A");
        List<AddMemberResultDto> added = await AddMembersAsync(teacher.Client, groupId, child.User.Id);
        added.Should().ContainSingle(r => r.Outcome == AddMemberOutcome.PendingApproval);

        // Only the Primary sees and decides the request.
        List<PendingGroupApprovalDto> approvals = await PagedHttp.GetAllAsync<PendingGroupApprovalDto>(
            parent.Client, "/api/auth/groups/approvals", Json);
        PendingGroupApprovalDto request = approvals.Should().ContainSingle(a => a.LearnerId == child.User.Id).Subject;
        (await teacher.Client.PostAsync($"/api/auth/groups/approvals/{request.MemberId}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await parent.Client.PostAsync($"/api/auth/groups/approvals/{request.MemberId}/approve", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The owner sees the child by public name only.
        LearnerGroupDetailDto detail = (await teacher.Client.GetFromJsonAsync<LearnerGroupDetailDto>($"/api/auth/groups/{groupId}", Json))!;
        detail.ActiveMemberCount.Should().Be(1);
        detail.MemberLearnerIds.Should().ContainSingle(id => id == child.User.Id);
        List<GroupMemberDto> members = await PagedHttp.GetAllAsync<GroupMemberDto>(teacher.Client, $"/api/auth/groups/{groupId}/members", Json);
        members.Should().ContainSingle(m => m.LearnerId == child.User.Id && m.Status == GroupMemberStatus.Active && m.PublicName == "Child learner");

        // A child cannot leave and cannot own a group; a non-owner cannot read the group.
        (await child.Client.PostAsync($"/api/auth/groups/{groupId}/leave", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await child.Client.PostAsJsonAsync("/api/auth/groups", new { name = "Mine" }, Json)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await parent.Client.GetAsync($"/api/auth/groups/{groupId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The child sees the group name and owner, not other members.
        List<JoinedGroupDto> childJoined = await PagedHttp.GetAllAsync<JoinedGroupDto>(child.Client, "/api/auth/groups/joined", Json);
        childJoined.Should().ContainSingle(g => g.Id == groupId);
        (await PagedHttp.GetAllAsync<OwnedGroupDto>(child.Client, "/api/auth/groups/owned", Json)).Should().BeEmpty();

        // The support link ends: the child leaves the teacher's group.
        (await parent.Client.PostAsync($"/api/auth/support-links/{extra.Id}/unlink-request", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await teacher.Client.PostAsync($"/api/auth/support-links/{extra.Id}/unlink-request/confirm", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        LearnerGroupDetailDto after = (await teacher.Client.GetFromJsonAsync<LearnerGroupDetailDto>($"/api/auth/groups/{groupId}", Json))!;
        after.MemberLearnerIds.Should().BeEmpty();
        (await PagedHttp.GetAllAsync<GroupMemberDto>(teacher.Client, $"/api/auth/groups/{groupId}/members", Json)).Should().BeEmpty();
    }

    [Fact]
    public async Task Groups_AdultMember_ActiveAtOnceAndCanLeave()
    {
        Account teacher = await RegisterAsync("Adult", "Teacher");
        Account learner = await RegisterAsync("Adult", "Learner");
        await AcceptAsync(teacher.Client, (await InviteAsLearnerAsync(learner.Client)).Code);

        Guid groupId = await CreateGroupAsync(teacher.Client, "Evening class");
        List<AddMemberResultDto> added = await AddMembersAsync(teacher.Client, groupId, learner.User.Id);
        added.Should().ContainSingle(r => r.Outcome == AddMemberOutcome.Added);

        // A learner without a support link to the owner is rejected, others are still added.
        Account stranger = await RegisterAsync("Adult", "Stranger");
        List<AddMemberResultDto> rejected = await AddMembersAsync(teacher.Client, groupId, stranger.User.Id);
        rejected.Should().ContainSingle(r => r.Outcome == AddMemberOutcome.Rejected && r.ErrorCode == LearnerGroupErrors.NoActiveLink.Code);

        (await learner.Client.PostAsync($"/api/auth/groups/{groupId}/leave", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        LearnerGroupDetailDto detail = (await teacher.Client.GetFromJsonAsync<LearnerGroupDetailDto>($"/api/auth/groups/{groupId}", Json))!;
        detail.MemberLearnerIds.Should().BeEmpty();
    }

    [Fact]
    public async Task Groups_Lists_ArePagedAndOldRouteIsGone()
    {
        Account teacher = await RegisterAsync("Adult", "Teacher");
        Account learner = await RegisterAsync("Adult", "Learner");
        await AcceptAsync(teacher.Client, (await InviteAsLearnerAsync(learner.Client)).Code);
        Guid first = await CreateGroupAsync(teacher.Client, "Group one");
        Guid second = await CreateGroupAsync(teacher.Client, "Group two");
        Guid third = await CreateGroupAsync(teacher.Client, "Group three");
        foreach (Guid id in new[] { first, second, third })
        {
            await AddMembersAsync(teacher.Client, id, learner.User.Id);
        }

        JsonElement ownedFirst = await PagedHttp.GetPageAsync(teacher.Client, "/api/auth/groups/owned?page=1&pageSize=2");
        JsonElement ownedSecond = await PagedHttp.GetPageAsync(teacher.Client, "/api/auth/groups/owned?page=2&pageSize=2");
        JsonElement joined = await PagedHttp.GetPageAsync(learner.Client, "/api/auth/groups/joined?page=1&pageSize=2");
        JsonElement learnerOwned = await PagedHttp.GetPageAsync(learner.Client, "/api/auth/groups/owned");

        ownedFirst.GetProperty("totalCount").GetInt32().Should().Be(3);
        ownedFirst.GetProperty("items").GetArrayLength().Should().Be(2);
        ownedSecond.GetProperty("items").GetArrayLength().Should().Be(1);
        joined.GetProperty("totalCount").GetInt32().Should().Be(3);
        joined.GetProperty("items").GetArrayLength().Should().Be(2);
        learnerOwned.GetProperty("totalCount").GetInt32().Should().Be(0);
        (await teacher.Client.GetAsync("/api/auth/groups")).StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        (await teacher.Client.GetAsync("/api/auth/groups/owned?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await teacher.Client.GetAsync("/api/auth/groups/joined?page=0")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GroupMembers_OwnerGetsPageNonOwnerForbiddenAndCommandRefreshesSnapshot()
    {
        Account teacher = await RegisterAsync("Adult", "Teacher");
        Account one = await RegisterAsync("Adult", "LearnerOne");
        Account two = await RegisterAsync("Adult", "LearnerTwo");
        await AcceptAsync(teacher.Client, (await InviteAsLearnerAsync(one.Client)).Code);
        await AcceptAsync(teacher.Client, (await InviteAsLearnerAsync(two.Client)).Code);
        Guid groupId = await CreateGroupAsync(teacher.Client, "Members page");
        await AddMembersAsync(teacher.Client, groupId, one.User.Id);

        JsonElement before = await PagedHttp.GetPageAsync(teacher.Client, $"/api/auth/groups/{groupId}/members?pageSize=1");
        (await one.Client.GetAsync($"/api/auth/groups/{groupId}/members")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AddMembersAsync(teacher.Client, groupId, two.User.Id);
        JsonElement after = await PagedHttp.GetPageAsync(teacher.Client, $"/api/auth/groups/{groupId}/members?pageSize=1");
        JsonElement past = await PagedHttp.GetPageAsync(teacher.Client, $"/api/auth/groups/{groupId}/members?page=9&pageSize=1");

        before.GetProperty("totalCount").GetInt32().Should().Be(1);
        after.GetProperty("totalCount").GetInt32().Should().Be(2);
        after.GetProperty("items").GetArrayLength().Should().Be(1);
        past.GetProperty("items").GetArrayLength().Should().Be(0);
        past.GetProperty("totalCount").GetInt32().Should().Be(2);
        (await teacher.Client.GetAsync($"/api/auth/groups/{groupId}/members?pageSize=101")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
