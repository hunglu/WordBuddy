using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdatePreferredLanguage;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

public class UpdatePreferredLanguageCommandValidatorTests
{
    private readonly UpdatePreferredLanguageCommandValidator _validator = new();

    [Fact]
    public void UpdatePreferredLanguageCommandValidator_Validate_EmptyUserIdFails() =>
        _validator.Validate(new UpdatePreferredLanguageCommand(Guid.Empty, "vi")).IsValid.Should().BeFalse();

    [Theory]
    [InlineData("english")]
    [InlineData("v")]
    [InlineData("en_US")]
    public void UpdatePreferredLanguageCommandValidator_Validate_MalformedCodeFails(string code) =>
        _validator.Validate(new UpdatePreferredLanguageCommand(Guid.NewGuid(), code)).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("vi")]
    [InlineData("en")]
    public void UpdatePreferredLanguageCommandValidator_Validate_NullOrValidPasses(string? code) =>
        _validator.Validate(new UpdatePreferredLanguageCommand(Guid.NewGuid(), code)).IsValid.Should().BeTrue();
}

public class UpdatePreferredLanguageCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISupportLinkRepository> _links = new();
    private readonly Mock<ILearnerProfileEventPublisher> _events = new();

    public UpdatePreferredLanguageCommandHandlerTests()
    {
        _users.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _links.Setup(l => l.HasActiveSupporterAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
    }

    private UpdatePreferredLanguageCommandHandler Handler() =>
        new(_users.Object, _links.Object, new UpdatePreferredLanguageCommandValidator(),
            Mock.Of<ILogger<UpdatePreferredLanguageCommandHandler>>());

    private User Given(User user)
    {
        _users.Setup(u => u.GetTrackedByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(user));
        return user;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdatePreferredLanguageCommandHandler_HandleAsync_SavesLanguageForChildAndAdult(bool child)
    {
        User user = Given(child ? TestData.Child("Kid") : TestData.Adult("Sam"));

        Result<UserDto> result = await Handler().HandleAsync(new UpdatePreferredLanguageCommand(user.Id, "VI"));

        result.IsSuccess.Should().BeTrue();
        result.Value.PreferredLanguage.Should().Be("vi");
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<User>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePreferredLanguageCommandHandler_HandleAsync_UnknownUserIsNotFound()
    {
        Guid missing = Guid.NewGuid();
        _users.Setup(u => u.GetTrackedByIdAsync(missing, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<User>(UserErrors.NotFound));

        Result<UserDto> result = await Handler().HandleAsync(new UpdatePreferredLanguageCommand(missing, "vi"));

        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task UpdatePreferredLanguageCommandHandler_HandleAsync_MalformedCodeFailsValidationWithoutSave()
    {
        User user = Given(TestData.Adult("Sam"));

        Result<UserDto> result = await Handler().HandleAsync(new UpdatePreferredLanguageCommand(user.Id, "english"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdatePreferredLanguage.Validation");
        _users.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private RegisterUserCommandHandler RegisterHandler()
    {
        Mock<IPasswordHasher> hasher = new();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        Mock<IJwtTokenGenerator> jwt = new();
        jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(("token", TestData.Now.AddHours(1)));
        _users.Setup(u => u.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        return new(_users.Object, hasher.Object, jwt.Object, _events.Object, new FixedTimeProvider(TestData.Now),
            new RegisterUserCommandValidator(), Mock.Of<ILogger<RegisterUserCommandHandler>>());
    }

    [Fact]
    public async Task RegisterUserCommandHandler_HandleAsync_StoresValidPreferredLanguage()
    {
        Result<AuthTokenDto> result = await RegisterHandler().HandleAsync(
            new RegisterUserCommand("a@example.com", "Passw0rd!", "Ann", AgeGroup.Adult, null, "vi"));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.PreferredLanguage.Should().Be("vi");
    }

    [Fact]
    public async Task RegisterUserCommandHandler_HandleAsync_NullLanguageLeavesItNull()
    {
        Result<AuthTokenDto> result = await RegisterHandler().HandleAsync(
            new RegisterUserCommand("a@example.com", "Passw0rd!", "Ann", AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        result.Value.User.PreferredLanguage.Should().BeNull();
    }

    [Fact]
    public async Task RegisterUserCommandHandler_HandleAsync_MalformedLanguageFailsValidation()
    {
        Result<AuthTokenDto> result = await RegisterHandler().HandleAsync(
            new RegisterUserCommand("a@example.com", "Passw0rd!", "Ann", AgeGroup.Adult, null, "english"));

        result.IsFailure.Should().BeTrue();
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
