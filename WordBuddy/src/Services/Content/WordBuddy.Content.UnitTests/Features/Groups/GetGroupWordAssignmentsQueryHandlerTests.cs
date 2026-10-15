using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Groups;

public class GetGroupWordAssignmentsQueryHandlerTests
{
    private readonly Mock<ILearnerGroupProjectionRepository> _projection = new();
    private readonly Mock<IGroupWordRepository> _words = new();

    private GetGroupWordAssignmentsQueryHandler CreateHandler() =>
        new(_projection.Object, _words.Object, new GetGroupWordAssignmentsQueryValidator(), NullLogger<GetGroupWordAssignmentsQueryHandler>.Instance);

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetGroupWordAssignmentsQueryHandler_HandleAsync_InvalidPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<GroupWordAssignmentDto>> result = await CreateHandler()
            .HandleAsync(new GetGroupWordAssignmentsQuery(Guid.NewGuid(), Guid.NewGuid(), page, pageSize));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _words.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGroupWordAssignmentsQueryHandler_HandleAsync_UnknownGroup_ReturnsEmptyPage()
    {
        _projection
            .Setup(p => p.GetGroupAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Result<PagedResult<GroupWordAssignmentDto>> result = await CreateHandler()
            .HandleAsync(new GetGroupWordAssignmentsQuery(Guid.NewGuid(), Guid.NewGuid(), 2, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Page.Should().Be(2);
    }
}
