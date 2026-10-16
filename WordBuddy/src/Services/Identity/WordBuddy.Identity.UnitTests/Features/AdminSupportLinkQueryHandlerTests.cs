using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

public class AdminSupportLinkQueryHandlerTests
{
    private readonly Mock<ISupportLinkRepository> _links = new();

    private GetUnlinkRequestsForAdminQueryHandler UnlinkHandler() =>
        new(_links.Object, new GetUnlinkRequestsForAdminQueryValidator(), NullLogger<GetUnlinkRequestsForAdminQueryHandler>.Instance);

    private GetLearnerSupportLinksForAdminQueryHandler LinksHandler() =>
        new(_links.Object, new GetLearnerSupportLinksForAdminQueryValidator(), NullLogger<GetLearnerSupportLinksForAdminQueryHandler>.Instance);

    [Fact]
    public async Task GetUnlinkRequestsForAdminQueryHandler_HandleAsync_EmptyPage_KeepsRealTotal()
    {
        _links
            .Setup(l => l.GetEscalatedUnlinkRequestsAsync(3, 10, true, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<UnlinkRequest>([], 3, 10, 21));
        _links
            .Setup(l => l.GetLinksByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result<PagedResult<AdminUnlinkRequestDto>> result = await UnlinkHandler().HandleAsync(new GetUnlinkRequestsForAdminQuery(3, 10, true, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.Page.Should().Be(3);
        result.Value.TotalCount.Should().Be(21);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetUnlinkRequestsForAdminQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<AdminUnlinkRequestDto>> result = await UnlinkHandler().HandleAsync(new GetUnlinkRequestsForAdminQuery(page, pageSize, true, null));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _links.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLearnerSupportLinksForAdminQueryHandler_HandleAsync_ReturnsPageWithTotal()
    {
        User learner = TestData.Adult("Learner");
        User supporter = TestData.Adult("Supporter");
        SupportLink link = TestData.Link(learner, supporter);
        _links
            .Setup(l => l.GetLearnerLinksPagedAsync(learner.Id, 2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SupportLink>([link], 2, 5, 6));

        Result<PagedResult<AdminSupportLinkDto>> result = await LinksHandler().HandleAsync(new GetLearnerSupportLinksForAdminQuery(learner.Id, 2, 5));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.Id == link.Id);
        result.Value.TotalCount.Should().Be(6);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 101)]
    public async Task GetLearnerSupportLinksForAdminQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<AdminSupportLinkDto>> result = await LinksHandler().HandleAsync(new GetLearnerSupportLinksForAdminQuery(Guid.NewGuid(), page, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetLearnerSupportLinksForAdminQueryHandler_HandleAsync_EmptyLearnerId_ReturnsValidationError()
    {
        Result<PagedResult<AdminSupportLinkDto>> result = await LinksHandler().HandleAsync(new GetLearnerSupportLinksForAdminQuery(Guid.Empty));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
