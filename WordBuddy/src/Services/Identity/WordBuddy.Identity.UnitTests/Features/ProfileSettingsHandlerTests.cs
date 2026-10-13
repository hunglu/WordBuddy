using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Profile.Commands.ChangePassword;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDateOfBirth;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDisplayName;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

/// <summary>WB-20: date of birth, display name and password handlers.</summary>
public class ProfileSettingsHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISupportLinkRepository> _links = new();
    private readonly Mock<ILearnerProfileEventPublisher> _events = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly FixedTimeProvider _time = new(TestData.Now);
    private readonly List<User> _published = [];

    public ProfileSettingsHandlerTests()
    {
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _links.Setup(l => l.HasActiveSupporterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _links.Setup(l => l.GetLinksForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SupportLink>());
        _jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(("token", TestData.Now.AddHours(1)));
        _events.Setup(e => e.PublishAsync(It.IsAny<User>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<User, DateTime, CancellationToken>((u, _, _) => _published.Add(u))
            .Returns(Task.CompletedTask);
    }

    private User Given(User user)
    {
        _users.Setup(u => u.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        return user;
    }

    private UpdateDateOfBirthCommandHandler DobHandler() =>
        new(_users.Object, _links.Object, _cache.Object, _jwt.Object, _time, new UpdateDateOfBirthCommandValidator(),
            Mock.Of<ILogger<UpdateDateOfBirthCommandHandler>>());

    private UpdateDisplayNameCommandHandler NameHandler() =>
        new(_users.Object, _links.Object, _events.Object, _time, new UpdateDisplayNameCommandValidator(),
            Mock.Of<ILogger<UpdateDisplayNameCommandHandler>>());

    private ChangePasswordCommandHandler PasswordHandler() =>
        new(_users.Object, _links.Object, _hasher.Object, _jwt.Object, _time, new ChangePasswordCommandValidator(),
            Mock.Of<ILogger<ChangePasswordCommandHandler>>());

    [Fact]
    public async Task UpdateDateOfBirthCommandHandler_HandleAsync_AdultSavesAndReturnsTokenWithBand()
    {
        User adult = Given(TestData.Adult());

        Result<AuthTokenDto> result = await DobHandler().HandleAsync(new UpdateDateOfBirthCommand(adult.Id, new DateOnly(1990, 5, 1)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("token");
        result.Value.User.AgeBand.Should().Be(AgeBand.Adult);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateDateOfBirthCommandHandler_HandleAsync_ChildSavesBand()
    {
        User child = Given(TestData.Child());

        Result<AuthTokenDto> result = await DobHandler().HandleAsync(new UpdateDateOfBirthCommand(child.Id, new DateOnly(2018, 3, 1)));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.AgeBand.Should().Be(AgeBand.Age7To9);
    }

    [Fact]
    public async Task UpdateDateOfBirthCommandHandler_HandleAsync_ConflictWithAgeGroupFailsWithoutSave()
    {
        User child = Given(TestData.Child());

        Result<AuthTokenDto> result = await DobHandler().HandleAsync(new UpdateDateOfBirthCommand(child.Id, new DateOnly(1980, 1, 1)));

        result.Error.Should().Be(UserErrors.DateOfBirthAgeGroupMismatch);
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDateOfBirthCommandHandler_HandleAsync_UnknownUserIsNotFound()
    {
        Guid missing = Guid.NewGuid();
        _users.Setup(u => u.GetTrackedByIdAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<User>(UserErrors.NotFound));

        Result<AuthTokenDto> result = await DobHandler().HandleAsync(new UpdateDateOfBirthCommand(missing, new DateOnly(1990, 5, 1)));

        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task UpdateDisplayNameCommandHandler_HandleAsync_AdultSavesAndPublishesOnce()
    {
        User adult = Given(TestData.Adult());

        Result<UserDto> result = await NameHandler().HandleAsync(new UpdateDisplayNameCommand(adult.Id, "Sam Lu"));

        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Sam Lu");
        _published.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateDisplayNameCommandHandler_HandleAsync_ChildMayEditName()
    {
        User child = Given(TestData.Child());

        Result<UserDto> result = await NameHandler().HandleAsync(new UpdateDisplayNameCommand(child.Id, "Mila R"));

        result.IsSuccess.Should().BeTrue();
        child.DisplayName.Should().Be("Mila R");
    }

    [Fact]
    public async Task UpdateDisplayNameCommandHandler_HandleAsync_TooShortFailsWithoutEvent()
    {
        User adult = Given(TestData.Adult());

        Result<UserDto> result = await NameHandler().HandleAsync(new UpdateDisplayNameCommand(adult.Id, "A"));

        result.IsFailure.Should().BeTrue();
        _published.Should().BeEmpty();
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateDisplayNameCommandHandler_HandleAsync_UnknownUserIsNotFound()
    {
        Guid missing = Guid.NewGuid();
        _users.Setup(u => u.GetTrackedByIdAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<User>(UserErrors.NotFound));

        Result<UserDto> result = await NameHandler().HandleAsync(new UpdateDisplayNameCommand(missing, "Sam Lu"));

        result.Error.Should().Be(UserErrors.NotFound);
        _published.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_HandleAsync_CorrectCurrentPasswordStoresNewHash()
    {
        User adult = Given(TestData.Adult());
        _hasher.Setup(h => h.Verify("OldPassw0rd!", "hash")).Returns(true);
        _hasher.Setup(h => h.Hash("NewPassw0rd!")).Returns("new-hash");

        Result<AuthTokenDto> result = await PasswordHandler().HandleAsync(new ChangePasswordCommand(adult.Id, "OldPassw0rd!", "NewPassw0rd!"));

        result.IsSuccess.Should().BeTrue();
        adult.PasswordHash.Should().Be("new-hash");
        result.Value.Token.Should().Be("token");
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_HandleAsync_ChildCanChangePassword()
    {
        User child = Given(TestData.Child());
        _hasher.Setup(h => h.Verify("OldPassw0rd!", "hash")).Returns(true);
        _hasher.Setup(h => h.Hash("NewPassw0rd!")).Returns("new-hash");

        Result<AuthTokenDto> result = await PasswordHandler().HandleAsync(new ChangePasswordCommand(child.Id, "OldPassw0rd!", "NewPassw0rd!"));

        result.IsSuccess.Should().BeTrue();
        child.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_HandleAsync_WrongCurrentPasswordRejectedWithoutSave()
    {
        User adult = Given(TestData.Adult());
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "hash")).Returns(false);

        Result<AuthTokenDto> result = await PasswordHandler().HandleAsync(new ChangePasswordCommand(adult.Id, "Wrong", "NewPassw0rd!"));

        result.Error.Should().Be(UserErrors.WrongCurrentPassword);
        adult.PasswordHash.Should().Be("hash");
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("OldPassw0rd!")]
    public async Task ChangePasswordCommandHandler_HandleAsync_WeakOrSameNewPasswordFailsValidation(string newPassword)
    {
        User adult = Given(TestData.Adult());

        Result<AuthTokenDto> result = await PasswordHandler().HandleAsync(new ChangePasswordCommand(adult.Id, "OldPassw0rd!", newPassword));

        result.IsFailure.Should().BeTrue();
        _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_HandleAsync_UnknownUserIsNotFound()
    {
        Guid missing = Guid.NewGuid();
        _users.Setup(u => u.GetTrackedByIdAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<User>(UserErrors.NotFound));

        Result<AuthTokenDto> result = await PasswordHandler().HandleAsync(new ChangePasswordCommand(missing, "OldPassw0rd!", "NewPassw0rd!"));

        result.Error.Should().Be(UserErrors.NotFound);
    }
}
