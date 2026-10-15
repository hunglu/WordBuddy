using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Groups.Commands.AddGroupMembers;
using WordBuddy.Identity.Application.Features.Groups.Commands.CreateGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.DeleteGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.LeaveGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.RemoveGroupMember;
using WordBuddy.Identity.Application.Features.Groups.Commands.RenameGroup;
using WordBuddy.Identity.Application.Features.Groups.Commands.RespondToGroupMembership;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetGroup;
using WordBuddy.Identity.Application.Features.Groups;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetGroupMembers;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetJoinedGroups;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetOwnedGroups;
using WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

public class GroupHandlerTests
{
    private readonly GroupHandlerFixture _f = new();

    private CreateGroupCommandHandler CreateHandler() =>
        new(_f.Users.Object, _f.Groups.Object, _f.Options, _f.Time, new CreateGroupCommandValidator(), Mock.Of<ILogger<CreateGroupCommandHandler>>());

    private RenameGroupCommandHandler RenameHandler() =>
        new(_f.Groups.Object, _f.Cache.Object, _f.Time, new RenameGroupCommandValidator(), Mock.Of<ILogger<RenameGroupCommandHandler>>());

    private DeleteGroupCommandHandler DeleteHandler() =>
        new(_f.Groups.Object, _f.Events.Object, _f.Cache.Object, _f.Time, new DeleteGroupCommandValidator(), Mock.Of<ILogger<DeleteGroupCommandHandler>>());

    private AddGroupMembersCommandHandler AddHandler() =>
        new(_f.Users.Object, _f.Links.Object, _f.Groups.Object, _f.Events.Object, _f.Cache.Object, _f.Options, _f.Time,
            new AddGroupMembersCommandValidator(), Mock.Of<ILogger<AddGroupMembersCommandHandler>>());

    private RemoveGroupMemberCommandHandler RemoveHandler() =>
        new(_f.Groups.Object, _f.Events.Object, _f.Cache.Object, _f.Time, new RemoveGroupMemberCommandValidator(),
            Mock.Of<ILogger<RemoveGroupMemberCommandHandler>>());

    private LeaveGroupCommandHandler LeaveHandler() =>
        new(_f.Users.Object, _f.Groups.Object, _f.Events.Object, _f.Cache.Object, _f.Time, new LeaveGroupCommandValidator(),
            Mock.Of<ILogger<LeaveGroupCommandHandler>>());

    private RespondToGroupMembershipCommandHandler RespondHandler() =>
        new(_f.Users.Object, _f.Links.Object, _f.Groups.Object, _f.Events.Object, _f.Cache.Object, _f.Time,
            new RespondToGroupMembershipCommandValidator(), Mock.Of<ILogger<RespondToGroupMembershipCommandHandler>>());

    // ── Create / rename / delete ────────────────────────────────────────────

    [Fact]
    public async Task CreateGroupCommandHandler_HandleAsync_AdultCreatesGroupWithTrimmedName()
    {
        User teacher = TestData.Adult("Teacher");
        _f.GivenUsers(teacher);

        Result<Guid> result = await CreateHandler().HandleAsync(new CreateGroupCommand(teacher.Id, "  Class 5A "));

        result.IsSuccess.Should().BeTrue();
        _f.GroupRows.Should().ContainSingle(g => g.Id == result.Value && g.OwnerId == teacher.Id && g.Name == "Class 5A");
    }

    [Fact]
    public async Task CreateGroupCommandHandler_HandleAsync_ChildForbidden()
    {
        User child = TestData.Child();
        _f.GivenUsers(child);

        Result<Guid> result = await CreateHandler().HandleAsync(new CreateGroupCommand(child.Id, "Class 5A"));

        result.Error.Should().Be(LearnerGroupErrors.ChildForbidden);
        _f.GroupRows.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateGroupCommandHandler_HandleAsync_LimitReachedRejected()
    {
        User teacher = TestData.Adult("Teacher");
        _f.GivenUsers(teacher);
        for (int i = 0; i < _f.Options.MaxGroupsPerOwner; i++)
        {
            _f.GivenGroup(teacher, $"Group {i:00}");
        }

        Result<Guid> result = await CreateHandler().HandleAsync(new CreateGroupCommand(teacher.Id, "One more"));

        result.Error.Should().Be(LearnerGroupErrors.TooManyGroups);
    }

    [Fact]
    public async Task CreateGroupCommandHandler_HandleAsync_ShortNameFailsValidation()
    {
        User teacher = TestData.Adult("Teacher");
        _f.GivenUsers(teacher);

        Result<Guid> result = await CreateHandler().HandleAsync(new CreateGroupCommand(teacher.Id, "ab"));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task RenameGroupCommandHandler_HandleAsync_OwnerRenamesAndCacheKeyDeleted()
    {
        User teacher = TestData.Adult("Teacher");
        LearnerGroup group = _f.GivenGroup(teacher);

        Result result = await RenameHandler().HandleAsync(new RenameGroupCommand(group.Id, teacher.Id, "Class 5B"));

        result.IsSuccess.Should().BeTrue();
        group.Name.Should().Be("Class 5B");
        _f.Cache.Verify(c => c.RemoveAsync(LearnerGroupCache.Key(group.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenameGroupCommandHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        LearnerGroup group = _f.GivenGroup(teacher);

        Result result = await RenameHandler().HandleAsync(new RenameGroupCommand(group.Id, Guid.NewGuid(), "Class 5B"));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
        group.Name.Should().Be("Class 5A");
    }

    [Fact]
    public async Task DeleteGroupCommandHandler_HandleAsync_RemovesAllMembersAndPublishesGroupDeleted()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        User child = TestData.Child();
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember adultMember = _f.GivenMember(group, teacher, adult);
        LearnerGroupMember childMember = _f.GivenMember(group, teacher, child, ownerIsPrimary: false);

        Result result = await DeleteHandler().HandleAsync(new DeleteGroupCommand(group.Id, teacher.Id));

        result.IsSuccess.Should().BeTrue();
        group.IsDeleted.Should().BeTrue();
        adultMember.RemovedReason.Should().Be(GroupMemberRemovedReason.GroupDeleted);
        childMember.RemovedReason.Should().Be(GroupMemberRemovedReason.GroupDeleted);
        _f.Deleted.Should().ContainSingle(id => id == group.Id);
        _f.Cache.Verify(c => c.RemoveAsync(LearnerGroupCache.Key(group.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGroupCommandHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        LearnerGroup group = _f.GivenGroup(teacher);

        Result result = await DeleteHandler().HandleAsync(new DeleteGroupCommand(group.Id, Guid.NewGuid()));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
        group.IsDeleted.Should().BeFalse();
        _f.Deleted.Should().BeEmpty();
    }

    // ── Members ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddGroupMembersCommandHandler_HandleAsync_AdultActiveAndPublished()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, adult);
        _f.GivenSupporterLinks(teacher, adult);
        LearnerGroup group = _f.GivenGroup(teacher);

        Result<IReadOnlyList<AddMemberResultDto>> result =
            await AddHandler().HandleAsync(new AddGroupMembersCommand(group.Id, teacher.Id, [adult.Id]));

        result.Value.Should().ContainSingle(r => r.LearnerId == adult.Id && r.Outcome == AddMemberOutcome.Added);
        _f.Activated.Should().ContainSingle(a => a.GroupId == group.Id && a.LearnerId == adult.Id);
    }

    [Fact]
    public async Task AddGroupMembersCommandHandler_HandleAsync_ChildOfPrimaryOwnerActive()
    {
        User parent = TestData.Adult("Parent");
        User child = TestData.Child();
        _f.GivenUsers(parent, child);
        _f.GivenSupporterLinks(parent, child);
        LearnerGroup group = _f.GivenGroup(parent);

        Result<IReadOnlyList<AddMemberResultDto>> result =
            await AddHandler().HandleAsync(new AddGroupMembersCommand(group.Id, parent.Id, [child.Id]));

        result.Value.Should().ContainSingle(r => r.Outcome == AddMemberOutcome.Added);
        _f.Activated.Should().ContainSingle();
    }

    [Fact]
    public async Task AddGroupMembersCommandHandler_HandleAsync_ChildOfOtherSupporterPendingNotPublished()
    {
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        _f.GivenUsers(parent, teacher, child);
        SupportLink primary = TestData.Link(child, parent);
        SupportLink extra = TestData.Link(child, teacher, [primary]);
        extra.ApproveByPrimary(TestData.Now);
        _f.Links.Setup(l => l.GetLinksForUserAsync(teacher.Id, It.IsAny<CancellationToken>())).ReturnsAsync([extra]);
        LearnerGroup group = _f.GivenGroup(teacher);

        Result<IReadOnlyList<AddMemberResultDto>> result =
            await AddHandler().HandleAsync(new AddGroupMembersCommand(group.Id, teacher.Id, [child.Id]));

        result.Value.Should().ContainSingle(r => r.Outcome == AddMemberOutcome.PendingApproval);
        _f.MemberRows.Should().ContainSingle(m => m.Status == GroupMemberStatus.PendingPrimaryApproval);
        _f.Activated.Should().BeEmpty();
    }

    [Fact]
    public async Task AddGroupMembersCommandHandler_HandleAsync_NoActiveLinkRejectedOthersStillAdded()
    {
        User teacher = TestData.Adult("Teacher");
        User supported = TestData.Adult("Supported");
        User stranger = TestData.Adult("Stranger");
        _f.GivenUsers(teacher, supported, stranger);
        _f.GivenSupporterLinks(teacher, supported);
        LearnerGroup group = _f.GivenGroup(teacher);

        Result<IReadOnlyList<AddMemberResultDto>> result =
            await AddHandler().HandleAsync(new AddGroupMembersCommand(group.Id, teacher.Id, [stranger.Id, supported.Id]));

        result.Value.Should().Contain(r => r.LearnerId == stranger.Id && r.Outcome == AddMemberOutcome.Rejected && r.ErrorCode == LearnerGroupErrors.NoActiveLink.Code);
        result.Value.Should().Contain(r => r.LearnerId == supported.Id && r.Outcome == AddMemberOutcome.Added);
    }

    [Fact]
    public async Task AddGroupMembersCommandHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        User other = TestData.Adult("Other");
        _f.GivenUsers(teacher, other);
        LearnerGroup group = _f.GivenGroup(teacher);

        Result<IReadOnlyList<AddMemberResultDto>> result =
            await AddHandler().HandleAsync(new AddGroupMembersCommand(group.Id, other.Id, [Guid.NewGuid()]));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
    }

    [Fact]
    public async Task RemoveGroupMemberCommandHandler_HandleAsync_ActiveMemberRemovedAndPublished()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, adult);

        Result result = await RemoveHandler().HandleAsync(new RemoveGroupMemberCommand(group.Id, teacher.Id, adult.Id));

        result.IsSuccess.Should().BeTrue();
        member.RemovedReason.Should().Be(GroupMemberRemovedReason.ByOwner);
        _f.Removed.Should().ContainSingle(r => r.LearnerId == adult.Id && r.Reason == GroupMemberRemovedReason.ByOwner);
    }

    [Fact]
    public async Task RemoveGroupMemberCommandHandler_HandleAsync_PendingMemberRemovedWithoutEvent()
    {
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, child, ownerIsPrimary: false);

        Result result = await RemoveHandler().HandleAsync(new RemoveGroupMemberCommand(group.Id, teacher.Id, child.Id));

        result.IsSuccess.Should().BeTrue();
        _f.Removed.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveGroupMemberCommandHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);

        Result result = await RemoveHandler().HandleAsync(new RemoveGroupMemberCommand(group.Id, adult.Id, adult.Id));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
    }

    [Fact]
    public async Task LeaveGroupCommandHandler_HandleAsync_AdultLeavesAndPublished()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, adult);

        Result result = await LeaveHandler().HandleAsync(new LeaveGroupCommand(group.Id, adult.Id));

        result.IsSuccess.Should().BeTrue();
        member.RemovedReason.Should().Be(GroupMemberRemovedReason.Left);
        _f.Removed.Should().ContainSingle(r => r.Reason == GroupMemberRemovedReason.Left);
    }

    [Fact]
    public async Task LeaveGroupCommandHandler_HandleAsync_ChildCannotLeave()
    {
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        _f.GivenUsers(teacher, child);
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, child);

        Result result = await LeaveHandler().HandleAsync(new LeaveGroupCommand(group.Id, child.Id));

        result.Error.Should().Be(LearnerGroupErrors.ChildCannotLeave);
        member.IsActive.Should().BeTrue();
        _f.Removed.Should().BeEmpty();
    }

    // ── Primary approval ────────────────────────────────────────────────────

    [Fact]
    public async Task RespondToGroupMembershipCommandHandler_HandleAsync_PrimaryApprovesAndPublished()
    {
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        _f.GivenUsers(parent, teacher, child);
        _f.Primaries[child.Id] = parent.Id;
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, child, ownerIsPrimary: false);

        Result result = await RespondHandler().HandleAsync(new RespondToGroupMembershipCommand(member.Id, parent.Id, Approve: true));

        result.IsSuccess.Should().BeTrue();
        member.Status.Should().Be(GroupMemberStatus.Active);
        _f.Activated.Should().ContainSingle(a => a.LearnerId == child.Id);
    }

    [Fact]
    public async Task RespondToGroupMembershipCommandHandler_HandleAsync_PrimaryRejectsWithoutEvent()
    {
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        _f.GivenUsers(parent, teacher, child);
        _f.Primaries[child.Id] = parent.Id;
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, child, ownerIsPrimary: false);

        Result result = await RespondHandler().HandleAsync(new RespondToGroupMembershipCommand(member.Id, parent.Id, Approve: false));

        result.IsSuccess.Should().BeTrue();
        member.RemovedReason.Should().Be(GroupMemberRemovedReason.RejectedByPrimary);
        _f.Activated.Should().BeEmpty();
    }

    [Fact]
    public async Task RespondToGroupMembershipCommandHandler_HandleAsync_NonPrimaryForbidden()
    {
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        _f.GivenUsers(parent, teacher, child);
        _f.Primaries[child.Id] = parent.Id;
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, child, ownerIsPrimary: false);

        Result result = await RespondHandler().HandleAsync(new RespondToGroupMembershipCommand(member.Id, teacher.Id, Approve: true));

        result.Error.Should().Be(LearnerGroupErrors.NotPrimary);
        member.Status.Should().Be(GroupMemberStatus.PendingPrimaryApproval);
    }

    [Fact]
    public async Task GetPendingGroupApprovalsQueryHandler_HandleAsync_PrimarySeesPendingChildOnly()
    {
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child();
        _f.GivenUsers(parent, teacher, child);
        _f.Primaries[child.Id] = parent.Id;
        LearnerGroup group = _f.GivenGroup(teacher);
        LearnerGroupMember member = _f.GivenMember(group, teacher, child, ownerIsPrimary: false);
        GetPendingGroupApprovalsQueryHandler handler = new(_f.Users.Object, _f.Groups.Object, new GetPendingGroupApprovalsQueryValidator(), Mock.Of<ILogger<GetPendingGroupApprovalsQueryHandler>>());

        Result<PagedResult<PendingGroupApprovalDto>> forPrimary = await handler.HandleAsync(new GetPendingGroupApprovalsQuery(parent.Id));
        Result<PagedResult<PendingGroupApprovalDto>> forTeacher = await handler.HandleAsync(new GetPendingGroupApprovalsQuery(teacher.Id));

        forPrimary.Value.Items.Should().ContainSingle(a => a.MemberId == member.Id && a.LearnerName == "Child learner");
        forPrimary.Value.TotalCount.Should().Be(1);
        forTeacher.Value.Items.Should().BeEmpty();
        forTeacher.Value.TotalCount.Should().Be(0);
    }

    // ── Queries ─────────────────────────────────────────────────────────────

    private LearnerGroupSnapshotLoader SnapshotLoader() =>
        new(_f.Users.Object, _f.Groups.Object, _f.Cache.Object, _f.Options, Mock.Of<ILogger<LearnerGroupSnapshotLoader>>());

    private GetGroupQueryHandler GetGroupHandler() => new(SnapshotLoader(), Mock.Of<ILogger<GetGroupQueryHandler>>());

    private GetGroupMembersQueryHandler MembersHandler() =>
        new(SnapshotLoader(), new GetGroupMembersQueryValidator(), Mock.Of<ILogger<GetGroupMembersQueryHandler>>());

    [Fact]
    public async Task GetGroupQueryHandler_HandleAsync_OwnerGetsCountsAndLearnerIdsNoMembers()
    {
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child("Secret");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, child, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, child);
        _f.GivenMember(group, teacher, adult);

        Result<LearnerGroupDetailDto> result = await GetGroupHandler().HandleAsync(new GetGroupQuery(group.Id, teacher.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveMemberCount.Should().Be(2);
        result.Value.PendingMemberCount.Should().Be(0);
        result.Value.MemberLearnerIds.Should().BeEquivalentTo([child.Id, adult.Id]);
        typeof(LearnerGroupDetailDto).GetProperties().Select(p => p.Name).Should().NotContain("Members");
    }

    [Fact]
    public async Task GetGroupQueryHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);

        Result<LearnerGroupDetailDto> result = await GetGroupHandler().HandleAsync(new GetGroupQuery(group.Id, adult.Id));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
    }

    [Fact]
    public async Task GetGroupMembersQueryHandler_HandleAsync_OwnerSeesPublicNamesOnlyInPages()
    {
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child("Secret");
        User adult = TestData.Adult("Learner");
        User third = TestData.Adult("Third");
        _f.GivenUsers(teacher, child, adult, third);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, child);
        _f.GivenMember(group, teacher, adult);
        _f.GivenMember(group, teacher, third);

        Result<PagedResult<GroupMemberDto>> first = await MembersHandler().HandleAsync(new GetGroupMembersQuery(group.Id, teacher.Id, 1, 2));
        Result<PagedResult<GroupMemberDto>> second = await MembersHandler().HandleAsync(new GetGroupMembersQuery(group.Id, teacher.Id, 2, 2));

        first.IsSuccess.Should().BeTrue();
        first.Value.Items.Should().HaveCount(2);
        first.Value.TotalCount.Should().Be(3);
        second.Value.Items.Should().HaveCount(1);
        first.Value.Items.Concat(second.Value.Items).Should().Contain(m => m.LearnerId == child.Id && m.PublicName == "Child learner");
        first.Value.Items.Concat(second.Value.Items).Should().NotContain(m => m.PublicName.Contains("Secret"));
    }

    [Fact]
    public async Task GetGroupMembersQueryHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);

        Result<PagedResult<GroupMemberDto>> result = await MembersHandler().HandleAsync(new GetGroupMembersQuery(group.Id, adult.Id));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetGroupMembersQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<GroupMemberDto>> result = await MembersHandler().HandleAsync(new GetGroupMembersQuery(Guid.NewGuid(), Guid.NewGuid(), page, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task LearnerGroupSnapshotLoader_LoadAsync_CacheMiss_StoresSnapshotUnderSnapshotKey()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);

        Result<LearnerGroupSnapshot> result = await SnapshotLoader().LoadAsync(group.Id);

        result.IsSuccess.Should().BeTrue();
        LearnerGroupCache.Key(group.Id).Should().Be($"identity:group-snapshot:{group.Id}");
        _f.Cache.Verify(c => c.SetAsync(
            $"identity:group-snapshot:{group.Id}", It.IsAny<byte[]>(), It.IsAny<Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LearnerGroupSnapshotLoader_LoadAsync_CacheHit_SkipsRepository()
    {
        Guid groupId = Guid.NewGuid();
        LearnerGroupSnapshot cached = new(groupId, Guid.NewGuid(), "Cached", TestData.Now, []);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(cached, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        _f.Cache.Setup(c => c.GetAsync($"identity:group-snapshot:{groupId}", It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        Result<LearnerGroupSnapshot> result = await SnapshotLoader().LoadAsync(groupId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Cached");
        _f.Groups.Verify(g => g.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetJoinedAndOwnedGroups_HandleAsync_LearnerSeesNameAndOwnerNoMembers()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        User other = TestData.Adult("Other");
        _f.GivenUsers(teacher, adult, other);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);
        _f.GivenMember(group, teacher, other);
        GetJoinedGroupsQueryHandler joined = new(_f.Users.Object, _f.Groups.Object, new GetJoinedGroupsQueryValidator(), Mock.Of<ILogger<GetJoinedGroupsQueryHandler>>());
        GetOwnedGroupsQueryHandler owned = new(_f.Groups.Object, new GetOwnedGroupsQueryValidator(), Mock.Of<ILogger<GetOwnedGroupsQueryHandler>>());

        Result<PagedResult<JoinedGroupDto>> learnerJoined = await joined.HandleAsync(new GetJoinedGroupsQuery(adult.Id));
        Result<PagedResult<OwnedGroupDto>> learnerOwned = await owned.HandleAsync(new GetOwnedGroupsQuery(adult.Id));
        Result<PagedResult<OwnedGroupDto>> ownerOwned = await owned.HandleAsync(new GetOwnedGroupsQuery(teacher.Id));

        learnerOwned.Value.Items.Should().BeEmpty();
        learnerOwned.Value.TotalCount.Should().Be(0);
        learnerJoined.Value.Items.Should().ContainSingle(j => j.Id == group.Id && j.OwnerName == "Teacher");
        learnerJoined.Value.TotalCount.Should().Be(1);
        typeof(JoinedGroupDto).GetProperties().Select(p => p.Name).Should().NotContain("Members");
        ownerOwned.Value.Items.Should().ContainSingle(o => o.Id == group.Id && o.ActiveMemberCount == 2);
    }

    [Fact]
    public async Task GetOwnedGroupsQueryHandler_HandleAsync_PagesGroups()
    {
        User teacher = TestData.Adult("Teacher");
        _f.GivenUsers(teacher);
        for (int i = 0; i < 3; i++)
        {
            _f.GivenGroup(teacher, $"Group {i}");
        }

        GetOwnedGroupsQueryHandler owned = new(_f.Groups.Object, new GetOwnedGroupsQueryValidator(), Mock.Of<ILogger<GetOwnedGroupsQueryHandler>>());

        Result<PagedResult<OwnedGroupDto>> second = await owned.HandleAsync(new GetOwnedGroupsQuery(teacher.Id, 2, 2));

        second.Value.Items.Should().HaveCount(1);
        second.Value.TotalCount.Should().Be(3);
        second.Value.Page.Should().Be(2);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 101)]
    public async Task GetOwnedGroupsQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        GetOwnedGroupsQueryHandler owned = new(_f.Groups.Object, new GetOwnedGroupsQueryValidator(), Mock.Of<ILogger<GetOwnedGroupsQueryHandler>>());

        Result<PagedResult<OwnedGroupDto>> result = await owned.HandleAsync(new GetOwnedGroupsQuery(Guid.NewGuid(), page, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
