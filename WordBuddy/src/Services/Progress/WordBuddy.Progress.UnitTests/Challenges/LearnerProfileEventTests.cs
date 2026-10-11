using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.Challenges;
using WordBuddy.Progress.Application.Features.Challenges.Commands.ApplyLearnerProfileEvent;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Progress.Infrastructure.Messaging;
using WordBuddy.Shared.Contracts.Profiles;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class LearnerProfileEventTests
{
    private readonly FakeChallengeProfileRepository _profiles = new();
    private readonly Mock<IChallengeConsentCleaner> _cleaner = new();
    private readonly Guid _user = Guid.NewGuid();

    public LearnerProfileEventTests()
    {
        _cleaner.Setup(c => c.RemoveByGuardianAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private ApplyLearnerProfileEventCommandHandler Handler() =>
        new(_profiles, _cleaner.Object, new ApplyLearnerProfileEventCommandValidator(), Mock.Of<ILogger<ApplyLearnerProfileEventCommandHandler>>());

    private ApplyLearnerProfileEventCommand Event(DateTime at, string age = "Adult", string? alias = "Sam", bool guardian = false, string? zone = "Asia/Ho_Chi_Minh", string? name = "Sam Real") =>
        new(_user, age, alias, "owl", name, zone, guardian, at);

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_FirstEventInsertsProfile()
    {
        Result result = await Handler().HandleAsync(Event(Monday));

        result.IsSuccess.Should().BeTrue();
        ChallengeProfile profile = _profiles.Profiles[_user];
        profile.AgeGroup.Should().Be(AgeGroup.Adult);
        profile.TimeZoneId.Should().Be("Asia/Ho_Chi_Minh");
        profile.PublicName.Should().Be("Sam");
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_DuplicateMessageLeavesOneRow()
    {
        await Handler().HandleAsync(Event(Monday));
        await Handler().HandleAsync(Event(Monday));

        _profiles.Profiles.Should().ContainSingle();
        _profiles.Profiles[_user].Alias.Should().Be("Sam");
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_OlderEventIsIgnored()
    {
        await Handler().HandleAsync(Event(Monday.AddHours(2), alias: "New"));

        Result result = await Handler().HandleAsync(Event(Monday.AddHours(1), alias: "Old"));

        result.IsSuccess.Should().BeTrue();
        _profiles.Profiles[_user].Alias.Should().Be("New");
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_NewerEventUpdatesFields()
    {
        await Handler().HandleAsync(Event(Monday, zone: "Asia/Ho_Chi_Minh"));

        await Handler().HandleAsync(Event(Monday.AddHours(1), zone: "America/New_York"));

        _profiles.Profiles[_user].TimeZoneId.Should().Be("America/New_York");
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_ChildNeverStoresADisplayName()
    {
        await Handler().HandleAsync(Event(Monday, age: "Child", alias: null, name: "Real Child Name"));

        _profiles.Profiles[_user].DisplayName.Should().BeNull();
        _profiles.Profiles[_user].PublicName.Should().Be(ChallengeProfile.ChildFallbackName);
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_UnknownAgeGroupFailsValidation()
    {
        Result result = await Handler().HandleAsync(Event(Monday, age: "Teen"));

        result.IsFailure.Should().BeTrue();
        _profiles.Profiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_GuardianRoleRemovedDeletesThatGuardiansConsents()
    {
        await Handler().HandleAsync(Event(Monday, guardian: true));

        await Handler().HandleAsync(Event(Monday.AddHours(1), guardian: false));

        _cleaner.Verify(c => c.RemoveByGuardianAsync(_user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_NonGuardianEventsDoNotTouchConsents()
    {
        await Handler().HandleAsync(Event(Monday, guardian: false));
        await Handler().HandleAsync(Event(Monday.AddHours(1), guardian: false, alias: "Other"));
        await Handler().HandleAsync(Event(Monday.AddHours(2), guardian: true));

        _cleaner.Verify(c => c.RemoveByGuardianAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyLearnerProfileEventCommandHandler_HandleAsync_OlderGuardianRemovalIsIgnored()
    {
        await Handler().HandleAsync(Event(Monday.AddHours(2), guardian: true));

        await Handler().HandleAsync(Event(Monday.AddHours(1), guardian: false));

        _profiles.Profiles[_user].IsGuardian.Should().BeTrue();
        _cleaner.Verify(c => c.RemoveByGuardianAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LearnerProfileUpdatedConsumer_Consume_MapsTheMessageToTheCommand()
    {
        Mock<ICommandHandler<ApplyLearnerProfileEventCommand>> handler = new();
        handler.Setup(h => h.HandleAsync(It.IsAny<ApplyLearnerProfileEventCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        LearnerProfileUpdated message = new(_user, "Child", "Fox", "fox", null, "UTC", false, Monday);
        Mock<ConsumeContext<LearnerProfileUpdated>> context = new();
        context.SetupGet(c => c.Message).Returns(message);
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await new LearnerProfileUpdatedConsumer(handler.Object).Consume(context.Object);

        handler.Verify(h => h.HandleAsync(
            It.Is<ApplyLearnerProfileEventCommand>(c => c.UserId == _user && c.AgeGroup == "Child" && c.Alias == "Fox" && c.TimeZoneId == "UTC" && !c.IsGuardian && c.OccurredAtUtc == Monday),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LearnerProfileUpdatedConsumer_Consume_FailedResultThrowsSoTheRetryPolicyRuns()
    {
        Mock<ICommandHandler<ApplyLearnerProfileEventCommand>> handler = new();
        handler.Setup(h => h.HandleAsync(It.IsAny<ApplyLearnerProfileEventCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Conflict("ChallengeProfile.Conflict", "x")));
        Mock<ConsumeContext<LearnerProfileUpdated>> context = new();
        context.SetupGet(c => c.Message).Returns(new LearnerProfileUpdated(_user, "Adult", null, null, "A", null, false, Monday));
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        Func<Task> act = () => new LearnerProfileUpdatedConsumer(handler.Object).Consume(context.Object);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
