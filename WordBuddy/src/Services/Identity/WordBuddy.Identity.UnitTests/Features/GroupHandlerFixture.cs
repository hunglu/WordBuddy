using Microsoft.Extensions.Caching.Distributed;
using Moq;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

/// <summary>Mocks shared by the group handler tests. The group repository is backed by two lists.
/// Records the staged events.</summary>
internal sealed class GroupHandlerFixture
{
    public Mock<IUserRepository> Users { get; } = new();
    public Mock<ISupportLinkRepository> Links { get; } = new();
    public Mock<ILearnerGroupRepository> Groups { get; } = new();
    public Mock<ILearnerGroupEventPublisher> Events { get; } = new();
    public Mock<IDistributedCache> Cache { get; } = new();
    public LearnerGroupOptions Options { get; } = new();
    public FixedTimeProvider Time { get; } = new(TestData.Now);

    public List<LearnerGroup> GroupRows { get; } = [];
    public List<LearnerGroupMember> MemberRows { get; } = [];
    public List<(Guid GroupId, Guid LearnerId)> Activated { get; } = [];
    public List<(Guid GroupId, Guid LearnerId, GroupMemberRemovedReason Reason)> Removed { get; } = [];
    public List<Guid> Deleted { get; } = [];

    /// <summary>Active Primary supporter per learner (for the approval flow).</summary>
    public Dictionary<Guid, Guid> Primaries { get; } = [];

    public GroupHandlerFixture()
    {
        Cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);

        Groups.Setup(g => g.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        Groups.Setup(g => g.GetGroupTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => FindGroup(id));
        Groups.Setup(g => g.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => FindGroup(id));
        Groups.Setup(g => g.GetGroupsByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                GroupRows.Where(g => !g.IsDeleted && ids.Contains(g.Id)).ToList());
        Groups.Setup(g => g.CountGroupsByOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid owner, CancellationToken _) => GroupRows.Count(g => !g.IsDeleted && g.OwnerId == owner));
        Groups.Setup(g => g.GetGroupsByOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid owner, CancellationToken _) => GroupRows.Where(g => !g.IsDeleted && g.OwnerId == owner).ToList());
        Groups.Setup(g => g.GetOpenMembersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                MemberRows.Where(m => ids.Contains(m.GroupId) && m.Status != GroupMemberStatus.Removed).ToList());
        Groups.Setup(g => g.GetOpenMembersTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                MemberRows.Where(m => m.GroupId == id && m.Status != GroupMemberStatus.Removed).ToList());
        Groups.Setup(g => g.GetActiveMembershipsOfLearnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid learner, CancellationToken _) =>
                MemberRows.Where(m => m.LearnerId == learner && m.IsActive).ToList());
        Groups.Setup(g => g.GetMemberTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                MemberRows.FirstOrDefault(m => m.Id == id) is { } member
                    ? Result.Success(member)
                    : Result.Failure<LearnerGroupMember>(LearnerGroupErrors.MemberNotFound));
        Groups.Setup(g => g.GetOpenMemberTrackedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid group, Guid learner, CancellationToken _) =>
                MemberRows.FirstOrDefault(m => m.GroupId == group && m.LearnerId == learner && m.Status != GroupMemberStatus.Removed) is { } member
                    ? Result.Success(member)
                    : Result.Failure<LearnerGroupMember>(LearnerGroupErrors.MemberNotFound));
        Groups.Setup(g => g.GetOpenMembersOfOwnerAndLearnerTrackedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid owner, Guid learner, CancellationToken _) =>
            {
                HashSet<Guid> ownerGroups = GroupRows.Where(g => !g.IsDeleted && g.OwnerId == owner).Select(g => g.Id).ToHashSet();
                return MemberRows
                    .Where(m => m.LearnerId == learner && m.Status != GroupMemberStatus.Removed && ownerGroups.Contains(m.GroupId))
                    .ToList();
            });
        Groups.Setup(g => g.GetPendingForPrimaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid primary, CancellationToken _) =>
                MemberRows
                    .Where(m => m.Status == GroupMemberStatus.PendingPrimaryApproval
                        && Primaries.TryGetValue(m.LearnerId, out Guid p) && p == primary)
                    .ToList());
        Groups.Setup(g => g.AddGroupAsync(It.IsAny<LearnerGroup>(), It.IsAny<CancellationToken>()))
            .Callback<LearnerGroup, CancellationToken>((group, _) => GroupRows.Add(group))
            .Returns(Task.CompletedTask);
        Groups.Setup(g => g.AddMemberAsync(It.IsAny<LearnerGroupMember>(), It.IsAny<CancellationToken>()))
            .Callback<LearnerGroupMember, CancellationToken>((member, _) => MemberRows.Add(member))
            .Returns(Task.CompletedTask);

        Links.Setup(l => l.GetActivePrimarySupporterIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid learner, CancellationToken _) => Primaries.TryGetValue(learner, out Guid p) ? p : null);

        Events.Setup(e => e.PublishMemberActivatedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid, DateTime, CancellationToken>((group, _, learner, _, _) => Activated.Add((group, learner)))
            .Returns(Task.CompletedTask);
        Events.Setup(e => e.PublishMemberRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<GroupMemberRemovedReason>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid, GroupMemberRemovedReason, DateTime, CancellationToken>((group, _, learner, reason, _, _) => Removed.Add((group, learner, reason)))
            .Returns(Task.CompletedTask);
        Events.Setup(e => e.PublishGroupDeletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, DateTime, CancellationToken>((group, _, _, _) => Deleted.Add(group))
            .Returns(Task.CompletedTask);
    }

    private Result<LearnerGroup> FindGroup(Guid id) =>
        GroupRows.FirstOrDefault(g => g.Id == id && !g.IsDeleted) is { } group
            ? Result.Success(group)
            : Result.Failure<LearnerGroup>(LearnerGroupErrors.NotFound);

    public void GivenUsers(params User[] users)
    {
        foreach (User user in users)
        {
            Users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        }

        Users.Setup(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => users.Where(u => ids.Contains(u.Id)).ToList());
    }

    /// <summary>Gives <paramref name="supporter"/> an active link to each learner (read through the owner link list).</summary>
    public void GivenSupporterLinks(User supporter, params User[] learners)
    {
        List<SupportLink> links = [];
        foreach (User learner in learners)
        {
            links.Add(TestData.Link(learner, supporter, links.Where(l => l.LearnerId == learner.Id).ToList()));
        }

        Links.Setup(l => l.GetLinksForUserAsync(supporter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(links);
        foreach (SupportLink link in links.Where(l => l.IsPrimary))
        {
            Primaries[link.LearnerId] = supporter.Id;
        }
    }

    public LearnerGroup GivenGroup(User owner, string name = "Class 5A")
    {
        LearnerGroup group = LearnerGroup.Create(Guid.NewGuid(), owner.Id, name, TestData.Now).Value;
        GroupRows.Add(group);
        return group;
    }

    public LearnerGroupMember GivenMember(LearnerGroup group, User owner, User learner, bool ownerIsPrimary = true)
    {
        LearnerGroupMember member = LearnerGroupPolicy.AddMember(
            Guid.NewGuid(), group, TestData.Party(owner), TestData.Party(learner), true, ownerIsPrimary, false, MemberRows.Count, 40, TestData.Now).Value;
        MemberRows.Add(member);
        return member;
    }
}
