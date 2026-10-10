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
using WordBuddy.Identity.Application.Features.Groups.Queries.GetMyGroups;
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
        GetPendingGroupApprovalsQueryHandler handler = new(_f.Users.Object, _f.Groups.Object, Mock.Of<ILogger<GetPendingGroupApprovalsQueryHandler>>());

        Result<IReadOnlyList<PendingGroupApprovalDto>> forPrimary = await handler.HandleAsync(new GetPendingGroupApprovalsQuery(parent.Id));
        Result<IReadOnlyList<PendingGroupApprovalDto>> forTeacher = await handler.HandleAsync(new GetPendingGroupApprovalsQuery(teacher.Id));

        forPrimary.Value.Should().ContainSingle(a => a.MemberId == member.Id && a.LearnerName == "Child learner");
        forTeacher.Value.Should().BeEmpty();
    }

    // ── Queries ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetGroupQueryHandler_HandleAsync_OwnerSeesPublicNamesOnly()
    {
        User teacher = TestData.Adult("Teacher");
        User child = TestData.Child("Secret");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, child, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, child);
        _f.GivenMember(group, teacher, adult);
        GetGroupQueryHandler handler = new(_f.Users.Object, _f.Groups.Object, _f.Cache.Object, _f.Options, Mock.Of<ILogger<GetGroupQueryHandler>>());

        Result<LearnerGroupDetailDto> result = await handler.HandleAsync(new GetGroupQuery(group.Id, teacher.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().Contain(m => m.LearnerId == child.Id && m.PublicName == "Child learner");
        result.Value.Members.Should().NotContain(m => m.PublicName.Contains("Secret"));
        result.Value.Members.Should().Contain(m => m.LearnerId == adult.Id && m.PublicName == "Learner");
    }

    [Fact]
    public async Task GetGroupQueryHandler_HandleAsync_NonOwnerForbidden()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        _f.GivenUsers(teacher, adult);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);
        GetGroupQueryHandler handler = new(_f.Users.Object, _f.Groups.Object, _f.Cache.Object, _f.Options, Mock.Of<ILogger<GetGroupQueryHandler>>());

        Result<LearnerGroupDetailDto> result = await handler.HandleAsync(new GetGroupQuery(group.Id, adult.Id));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
    }

    [Fact]
    public async Task GetMyGroupsQueryHandler_HandleAsync_LearnerSeesNameAndOwnerNoMembers()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        User other = TestData.Adult("Other");
        _f.GivenUsers(teacher, adult, other);
        LearnerGroup group = _f.GivenGroup(teacher);
        _f.GivenMember(group, teacher, adult);
        _f.GivenMember(group, teacher, other);
        GetMyGroupsQueryHandler handler = new(_f.Users.Object, _f.Groups.Object, Mock.Of<ILogger<GetMyGroupsQueryHandler>>());

        Result<MyGroupsDto> learnerView = await handler.HandleAsync(new GetMyGroupsQuery(adult.Id));
        Result<MyGroupsDto> ownerView = await handler.HandleAsync(new GetMyGroupsQuery(teacher.Id));

        learnerView.Value.Owned.Should().BeEmpty();
        learnerView.Value.Joined.Should().ContainSingle(j => j.Id == group.Id && j.OwnerName == "Teacher");
        typeof(JoinedGroupDto).GetProperties().Select(p => p.Name).Should().NotContain("Members");
        ownerView.Value.Owned.Should().ContainSingle(o => o.Id == group.Id && o.ActiveMemberCount == 2);
    }
}
