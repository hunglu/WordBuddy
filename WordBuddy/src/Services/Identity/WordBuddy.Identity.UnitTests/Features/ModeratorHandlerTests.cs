using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Profile.Commands.SetModeratorRole;
using WordBuddy.Identity.Application.Features.Users.Queries.GetUsersForAdmin;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

/// <summary>WB-56: Moderator role command and the admin user list query.</summary>
public class ModeratorHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    public ModeratorHandlerTests()
    {
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private SetModeratorRoleCommandHandler SetHandler() =>
        new(_users.Object, new SetModeratorRoleCommandValidator(), NullLogger<SetModeratorRoleCommandHandler>.Instance);

    private GetUsersForAdminQueryHandler ListHandler() =>
        new(_users.Object, new GetUsersForAdminQueryValidator(), NullLogger<GetUsersForAdminQueryHandler>.Instance);

    private User Given(User user)
    {
        _users.Setup(u => u.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        return user;
    }

    [Fact]
    public async Task SetModeratorRoleCommandHandler_HandleAsync_EmptyIds_ReturnsValidationError()
    {
        Result result = await SetHandler().HandleAsync(new SetModeratorRoleCommand(Guid.Empty, Guid.Empty, true));

        result.Error.Type.Should().Be(ErrorType.Validation);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetModeratorRoleCommandHandler_HandleAsync_UnknownUser_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        _users.Setup(u => u.GetTrackedByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<User>(UserErrors.NotFound));

        Result result = await SetHandler().HandleAsync(new SetModeratorRoleCommand(Guid.NewGuid(), id, true));

        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task SetModeratorRoleCommandHandler_HandleAsync_ChildTarget_FailsWithoutSave()
    {
        User child = Given(TestData.Child());

        Result result = await SetHandler().HandleAsync(new SetModeratorRoleCommand(Guid.NewGuid(), child.Id, true));

        result.Error.Should().Be(UserErrors.ModeratorMustBeAdult);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetModeratorRoleCommandHandler_HandleAsync_AdultTarget_SavesOnce()
    {
        User adult = Given(TestData.Adult());

        Result result = await SetHandler().HandleAsync(new SetModeratorRoleCommand(Guid.NewGuid(), adult.Id, true));

        result.IsSuccess.Should().BeTrue();
        adult.IsModerator.Should().BeTrue();
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetUsersForAdminQueryHandler_HandleAsync_BadPaging_ReturnsValidationError(int page, int pageSize)
    {
        Result<PagedResult<AdminUserDto>> result = await ListHandler().HandleAsync(new GetUsersForAdminQuery(null, page, pageSize));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetUsersForAdminQueryHandler_HandleAsync_SearchTooLong_ReturnsValidationError()
    {
        Result<PagedResult<AdminUserDto>> result = await ListHandler().HandleAsync(new GetUsersForAdminQuery(new string('a', 101), 1, 25));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task GetUsersForAdminQueryHandler_HandleAsync_AsksRepositoryForAdultsWithTrimmedSearch()
    {
        User adult = TestData.Adult("Sam");
        adult.SetModerator(true);
        _users.Setup(u => u.GetAdultsPagedAsync("sam", 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<User>([adult], 2, 10, 11));

        Result<PagedResult<AdminUserDto>> result = await ListHandler().HandleAsync(new GetUsersForAdminQuery("  sam ", 2, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(11);
        result.Value.Items.Should().ContainSingle().Which.IsModerator.Should().BeTrue();
    }
}
