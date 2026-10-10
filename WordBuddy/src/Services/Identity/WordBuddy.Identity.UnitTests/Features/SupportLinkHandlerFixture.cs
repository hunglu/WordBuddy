using Microsoft.Extensions.Caching.Distributed;
using Moq;
using WordBuddy.Identity.Application.Features.Groups;
using WordBuddy.Identity.Application.Features.SupportLinks;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

/// <summary>Mocks shared by the support-link handler tests. Records staged audit rows and events.</summary>
internal sealed class SupportLinkHandlerFixture
{
    public Mock<IUserRepository> Users { get; } = new();
    public Mock<ISupportLinkRepository> Links { get; } = new();
    public Mock<ISupportLinkEventPublisher> Events { get; } = new();
    public Mock<ILearnerGroupMembershipCleaner> GroupCleaner { get; } = new();
    public Mock<IDistributedCache> Cache { get; } = new();
    public SupportLinkOptions Options { get; } = new();
    public FixedTimeProvider Time { get; } = new(TestData.Now);

    public List<SupportLinkAuditEntry> Audit { get; } = [];
    public List<SupportLink> Activated { get; } = [];
    public List<SupportLink> Revoked { get; } = [];
    public List<SupportLink> AddedLinks { get; } = [];
    public List<UnlinkRequest> AddedRequests { get; } = [];

    public SupportLinkHandlerFixture()
    {
        GroupCleaner.Setup(c => c.RemoveForLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Links.Setup(l => l.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        Links.Setup(l => l.AddAuditEntryAsync(It.IsAny<SupportLinkAuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<SupportLinkAuditEntry, CancellationToken>((entry, _) => Audit.Add(entry))
            .Returns(Task.CompletedTask);
        Links.Setup(l => l.AddLinkAsync(It.IsAny<SupportLink>(), It.IsAny<CancellationToken>()))
            .Callback<SupportLink, CancellationToken>((link, _) => AddedLinks.Add(link))
            .Returns(Task.CompletedTask);
        Links.Setup(l => l.AddUnlinkRequestAsync(It.IsAny<UnlinkRequest>(), It.IsAny<CancellationToken>()))
            .Callback<UnlinkRequest, CancellationToken>((request, _) => AddedRequests.Add(request))
            .Returns(Task.CompletedTask);
        Links.Setup(l => l.GetOpenUnlinkRequestTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UnlinkRequest>(SupportLinkErrors.UnlinkRequestNotFound));
        Links.Setup(l => l.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<Result>>, CancellationToken>((work, ct) => work(ct));
        Events.Setup(e => e.PublishActivatedAsync(It.IsAny<SupportLink>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<SupportLink, DateTime, CancellationToken>((link, _, _) => Activated.Add(link))
            .Returns(Task.CompletedTask);
        Events.Setup(e => e.PublishRevokedAsync(It.IsAny<SupportLink>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<SupportLink, DateTime, CancellationToken>((link, _, _) => Revoked.Add(link))
            .Returns(Task.CompletedTask);
    }

    public LinkActorResolver Resolver => new(Users.Object, Links.Object);

    public void GivenUsers(params User[] users)
    {
        foreach (User user in users)
        {
            Users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
            Users.Setup(u => u.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        }

        Users.Setup(u => u.GetByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => users.Where(u => ids.Contains(u.Id)).ToList());
    }

    public void GivenLink(SupportLink link) =>
        Links.Setup(l => l.GetLinkTrackedAsync(link.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(link));

    public void GivenLearnerLinks(Guid learnerId, params SupportLink[] links)
    {
        Links.Setup(l => l.GetLearnerLinksTrackedAsync(learnerId, It.IsAny<CancellationToken>())).ReturnsAsync(links.ToList());
        Guid? primary = links.FirstOrDefault(l => l.IsPrimary && l.IsActive)?.SupporterId;
        Links.Setup(l => l.GetActivePrimarySupporterIdAsync(learnerId, It.IsAny<CancellationToken>())).ReturnsAsync(primary);
    }

    public void GivenOpenRequest(UnlinkRequest request) =>
        Links.Setup(l => l.GetOpenUnlinkRequestTrackedAsync(request.LinkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(request));
}
