using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileAliasAvatar;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

public class UpdateProfileAliasAvatarCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISupportLinkRepository> _links = new();

    public UpdateProfileAliasAvatarCommandHandlerTests()
    {
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _links.Setup(l => l.GetLinksForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<SupportLink>());
    }

    private UpdateProfileAliasAvatarCommandHandler CreateHandler() =>
        new(_users.Object, _links.Object, Mock.Of<IDistributedCache>(), new UpdateProfileAliasAvatarCommandValidator(),
            Mock.Of<ILogger<UpdateProfileAliasAvatarCommandHandler>>());

    private User Given(User user)
    {
        _users.Setup(u => u.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        return user;
    }

    [Fact]
    public async Task UpdateProfileAliasAvatarCommandHandler_HandleAsync_SavesAliasAndAvatar()
    {
        User user = Given(TestData.Adult("Sam"));

        Result<UserDto> result = await CreateHandler().HandleAsync(new UpdateProfileAliasAvatarCommand(user.Id, " Sammy ", "owl"));

        result.Value.Alias.Should().Be("Sammy");
        result.Value.AvatarId.Should().Be("owl");
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAliasAvatarCommandHandler_HandleAsync_TakenAliasConflict()
    {
        User user = Given(TestData.Adult("Sam"));
        _users.Setup(u => u.AliasTakenAsync("Sammy", user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Result<UserDto> result = await CreateHandler().HandleAsync(new UpdateProfileAliasAvatarCommand(user.Id, "Sammy", null));

        result.Error.Should().Be(UserErrors.AliasTaken);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAliasAvatarCommandHandler_HandleAsync_ChildAliasRevealingNameRejected()
    {
        User child = Given(TestData.Child("Kid"));

        Result<UserDto> result = await CreateHandler().HandleAsync(new UpdateProfileAliasAvatarCommand(child.Id, "Kid", null));

        result.Error.Should().Be(UserErrors.AliasRevealsIdentity);
    }

    [Fact]
    public async Task UpdateProfileAliasAvatarCommandHandler_HandleAsync_UnknownAvatarValidationError()
    {
        User user = Given(TestData.Adult());

        Result<UserDto> result = await CreateHandler().HandleAsync(new UpdateProfileAliasAvatarCommand(user.Id, null, "dragon"));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
