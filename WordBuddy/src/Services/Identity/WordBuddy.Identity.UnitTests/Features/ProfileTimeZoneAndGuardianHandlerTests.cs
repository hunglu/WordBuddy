using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;
using WordBuddy.Identity.Application.Features.Profile.Commands.RepublishLearnerProfiles;
using WordBuddy.Identity.Application.Features.Profile.Commands.SetGuardianRole;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileTimeZone;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

/// <summary>WB-29: the time zone, Guardian role, register and backfill handlers stage exactly one LearnerProfileUpdated per success.</summary>
public class ProfileTimeZoneAndGuardianHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISupportLinkRepository> _links = new();
    private readonly Mock<ILearnerProfileEventPublisher> _events = new();
    private readonly FixedTimeProvider _time = new(TestData.Now);
    private readonly List<(User User, DateTime At)> _published = [];

    public ProfileTimeZoneAndGuardianHandlerTests()
    {
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _links.Setup(l => l.HasActiveSupporterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _events.Setup(e => e.PublishAsync(It.IsAny<User>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<User, DateTime, CancellationToken>((u, at, _) => _published.Add((u, at)))
            .Returns(Task.CompletedTask);
    }

    private User Given(User user)
    {
        _users.Setup(u => u.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        return user;
    }

    private UpdateProfileTimeZoneCommandHandler TimeZoneHandler() =>
        new(_users.Object, _links.Object, _events.Object, _time, new UpdateProfileTimeZoneCommandValidator(),
            Mock.Of<ILogger<UpdateProfileTimeZoneCommandHandler>>());

    private SetGuardianRoleCommandHandler GuardianHandler() =>
        new(_users.Object, _events.Object, _time, new SetGuardianRoleCommandValidator(),
            Mock.Of<ILogger<SetGuardianRoleCommandHandler>>());

    [Fact]
    public async Task UpdateProfileTimeZoneCommandHandler_HandleAsync_SavesZoneAndPublishesOnce()
    {
        User user = Given(TestData.Adult("Sam"));

        Result<UserDto> result = await TimeZoneHandler().HandleAsync(new UpdateProfileTimeZoneCommand(user.Id, "Asia/Ho_Chi_Minh"));

        result.IsSuccess.Should().BeTrue();
        result.Value.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
        _published.Should().ContainSingle().Which.At.Should().Be(TestData.Now);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileTimeZoneCommandHandler_HandleAsync_UnknownZoneFailsWithoutEvent()
    {
        User user = Given(TestData.Adult("Sam"));

        Result<UserDto> result = await TimeZoneHandler().HandleAsync(new UpdateProfileTimeZoneCommand(user.Id, "Mars/Olympus_Mons"));

        result.IsFailure.Should().BeTrue();
        _published.Should().BeEmpty();
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetGuardianRoleCommandHandler_HandleAsync_AdultBecomesGuardianAndEventCarriesFlag()
    {
        User adult = Given(TestData.Adult("Pat"));

        Result result = await GuardianHandler().HandleAsync(new SetGuardianRoleCommand(Guid.NewGuid(), adult.Id, IsGuardian: true));

        result.IsSuccess.Should().BeTrue();
        adult.IsGuardian.Should().BeTrue();
        _published.Should().ContainSingle().Which.User.IsGuardian.Should().BeTrue();
    }

    [Fact]
    public async Task SetGuardianRoleCommandHandler_HandleAsync_RemovingRolePublishesFalseFlag()
    {
        User adult = Given(TestData.Adult("Pat"));
        adult.SetGuardian(true);

        Result result = await GuardianHandler().HandleAsync(new SetGuardianRoleCommand(Guid.NewGuid(), adult.Id, IsGuardian: false));

        result.IsSuccess.Should().BeTrue();
        _published.Should().ContainSingle().Which.User.IsGuardian.Should().BeFalse();
    }

    [Fact]
    public async Task SetGuardianRoleCommandHandler_HandleAsync_UnknownUserIsNotFound()
    {
        Guid missing = Guid.NewGuid();
        _users.Setup(u => u.GetTrackedByIdAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<User>(UserErrors.NotFound));

        Result result = await GuardianHandler().HandleAsync(new SetGuardianRoleCommand(Guid.NewGuid(), missing, IsGuardian: true));

        result.Error.Should().Be(UserErrors.NotFound);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task SetGuardianRoleCommandHandler_HandleAsync_ChildUserIsRejected()
    {
        User child = Given(TestData.Child("Kid"));

        Result result = await GuardianHandler().HandleAsync(new SetGuardianRoleCommand(Guid.NewGuid(), child.Id, IsGuardian: true));

        result.Error.Should().Be(UserErrors.GuardianMustBeAdult);
        child.IsGuardian.Should().BeFalse();
        _published.Should().BeEmpty();
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserCommandHandler_HandleAsync_StoresZoneAndPublishesOnce()
    {
        Mock<IPasswordHasher> hasher = new();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        Mock<IJwtTokenGenerator> jwt = new();
        jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(("token", TestData.Now.AddHours(1)));
        _users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        RegisterUserCommandHandler handler = new(
            _users.Object, hasher.Object, jwt.Object, _events.Object, _time, new RegisterUserCommandValidator(),
            Mock.Of<ILogger<RegisterUserCommandHandler>>());

        Result<AuthTokenDto> result = await handler.HandleAsync(
            new RegisterUserCommand("kid@example.com", "Passw0rd!", "Kid", AgeGroup.Child, "Asia/Ho_Chi_Minh"));

        result.IsSuccess.Should().BeTrue();
        _published.Should().ContainSingle().Which.User.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterUserCommandHandler_HandleAsync_UnknownZoneFailsValidationWithoutEvent()
    {
        RegisterUserCommandHandler handler = new(
            _users.Object, Mock.Of<IPasswordHasher>(), Mock.Of<IJwtTokenGenerator>(), _events.Object, _time,
            new RegisterUserCommandValidator(), Mock.Of<ILogger<RegisterUserCommandHandler>>());

        Result<AuthTokenDto> result = await handler.HandleAsync(
            new RegisterUserCommand("kid@example.com", "Passw0rd!", "Kid", AgeGroup.Child, "Mars/Olympus_Mons"));

        result.IsFailure.Should().BeTrue();
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task RepublishLearnerProfilesCommandHandler_HandleAsync_PublishesOnePerUserAndCanRunTwice()
    {
        User[] all = [TestData.Adult("A"), TestData.Child("B"), TestData.Adult("C")];
        _users.Setup(u => u.GetPageAsync(0, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(all);
        _users.Setup(u => u.GetPageAsync(all.Length, It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<User>());
        RepublishLearnerProfilesCommandHandler handler = new(
            _users.Object, _events.Object, _time, Mock.Of<ILogger<RepublishLearnerProfilesCommandHandler>>());

        Result<int> first = await handler.HandleAsync(new RepublishLearnerProfilesCommand());
        Result<int> second = await handler.HandleAsync(new RepublishLearnerProfilesCommand());

        first.Value.Should().Be(3);
        second.Value.Should().Be(3);
        _published.Should().HaveCount(6);
    }
}
