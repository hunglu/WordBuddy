using FluentAssertions;
using Moq;
using WordBuddy.Content.Application.Features.SupportLinks;
using WordBuddy.Content.Application.Interfaces;

namespace WordBuddy.Content.UnitTests.Features.SupportLinks;

public class SupportAccessTests
{
    private readonly Mock<ISupportLinkProjectionRepository> _links = new();
    private readonly Guid _supporter = Guid.NewGuid();
    private readonly Guid _learner = Guid.NewGuid();

    private SupportAccess Create() => new(_links.Object);

    [Fact]
    public async Task SupportAccess_CanSupportLearnerAsync_ActiveLinkAllowed()
    {
        _links.Setup(l => l.HasActiveLinkAsync(_supporter, _learner, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        (await Create().CanSupportLearnerAsync(_supporter, _learner)).Should().BeTrue();
    }

    [Fact]
    public async Task SupportAccess_CanSupportLearnerAsync_RevokedOrMissingLinkDenied()
    {
        _links.Setup(l => l.HasActiveLinkAsync(_supporter, _learner, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        (await Create().CanSupportLearnerAsync(_supporter, _learner)).Should().BeFalse();
        (await Create().CanSupportLearnerAsync(_supporter, null)).Should().BeFalse();
        (await Create().CanSupportLearnerAsync(null, _learner)).Should().BeFalse();
    }

    [Fact]
    public async Task SupportAccess_ChildHasSupporterAsync_ChildWithoutLinkDenied()
    {
        _links.Setup(l => l.HasActiveSupporterAsync(_learner, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        (await Create().ChildHasSupporterAsync(_learner, isChild: true, isAdmin: false)).Should().BeFalse();
    }

    [Fact]
    public async Task SupportAccess_ChildHasSupporterAsync_ChildWithLinkAllowed()
    {
        _links.Setup(l => l.HasActiveSupporterAsync(_learner, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        (await Create().ChildHasSupporterAsync(_learner, isChild: true, isAdmin: false)).Should().BeTrue();
    }

    [Fact]
    public async Task SupportAccess_ChildHasSupporterAsync_AdultAndAdminPassWithoutLookup()
    {
        (await Create().ChildHasSupporterAsync(_learner, isChild: false, isAdmin: false)).Should().BeTrue();
        (await Create().ChildHasSupporterAsync(_learner, isChild: true, isAdmin: true)).Should().BeTrue();

        _links.Verify(l => l.HasActiveSupporterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
