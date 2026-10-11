using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Features.Challenges;
using WordBuddy.Progress.Application.Features.Challenges.Commands.JoinChallenge;
using WordBuddy.Progress.Application.Features.Challenges.Commands.LeaveChallenge;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class JoinChallengeCommandHandlerTests
{
    private readonly FakeChallengeRepository _repo = new();
    private readonly FakeDistributedCache _cache = new();
    private readonly FixedTimeProvider _time = new(Monday.AddDays(1));
    private readonly Guid _user = Guid.NewGuid();

    private JoinChallengeCommandHandler Join() =>
        new(_repo, _cache, _time, new JoinChallengeCommandValidator(), Mock.Of<ILogger<JoinChallengeCommandHandler>>());

    private LeaveChallengeCommandHandler Leave() =>
        new(_repo, _cache, new LeaveChallengeCommandValidator(), Mock.Of<ILogger<LeaveChallengeCommandHandler>>());

    private Challenge Given(Challenge challenge)
    {
        _repo.Challenges.Add(challenge);
        return challenge;
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_AdultJoinsOpenAdultChallengeAndBoardCacheIsDeleted()
    {
        Challenge challenge = Given(NewChallenge());
        _cache.Items[ChallengeCache.BoardKey(challenge.Id)] = [1];

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        _repo.Participants.Should().ContainSingle(p => p.UserId == _user && p.JoinedAtUtc == _time.UtcNow);
        _cache.Items.Should().NotContainKey(ChallengeCache.BoardKey(challenge.Id));
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_JoiningTwiceKeepsOneRow()
    {
        Challenge challenge = Given(NewChallenge());

        await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult));
        Result second = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult));

        second.IsSuccess.Should().BeTrue();
        _repo.Participants.Should().ContainSingle();
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_ChildCannotJoinAdultChallenge()
    {
        Challenge challenge = Given(NewChallenge(ChallengeAudience.Adult));
        _repo.Consents.Add(ChallengeConsent.Create(_user, Guid.NewGuid(), Monday));

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Child));

        result.Error.Should().Be(ChallengeErrors.AudienceMismatch);
        _repo.Participants.Should().BeEmpty();
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_AdultCannotJoinChildChallenge()
    {
        Challenge challenge = Given(NewChallenge(ChallengeAudience.Child));

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult));

        result.Error.Should().Be(ChallengeErrors.AudienceMismatch);
    }

    [Theory]
    [InlineData(ChallengeStatus.Scheduled)]
    [InlineData(ChallengeStatus.Finished)]
    public async Task JoinChallengeCommandHandler_HandleAsync_NotOpenChallengeIsRejected(ChallengeStatus status)
    {
        Challenge challenge = Given(NewChallenge(status: status));

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult));

        result.Error.Should().Be(ChallengeErrors.NotOpen);
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_RunningChallengeCanStillBeJoined()
    {
        Challenge challenge = Given(NewChallenge(status: ChallengeStatus.Running));

        (await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult))).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_FullChallengeReturnsFullAndAddsNoOverflow()
    {
        Challenge challenge = Given(NewChallenge(capacity: 2));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, Guid.NewGuid(), Monday));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, Guid.NewGuid(), Monday));

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Adult));

        result.Error.Should().Be(ChallengeErrors.Full);
        _repo.Participants.Should().HaveCount(2);
        _repo.Challenges.Should().ContainSingle("no overflow instance is made");
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_ChildWithoutConsentIsRejected()
    {
        Challenge challenge = Given(NewChallenge(ChallengeAudience.Child));

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Child));

        result.Error.Should().Be(ChallengeErrors.NoConsent);
        _repo.Participants.Should().BeEmpty();
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_ChildWithConsentJoinsChildChallenge()
    {
        Challenge challenge = Given(NewChallenge(ChallengeAudience.Child));
        _repo.Consents.Add(ChallengeConsent.Create(_user, Guid.NewGuid(), Monday));

        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, challenge.Id, AgeGroup.Child));

        result.IsSuccess.Should().BeTrue();
        _repo.Participants.Should().ContainSingle(p => p.UserId == _user);
    }

    [Fact]
    public async Task JoinChallengeCommandHandler_HandleAsync_UnknownChallengeIsNotFound()
    {
        Result result = await Join().HandleAsync(new JoinChallengeCommand(_user, Guid.NewGuid(), AgeGroup.Adult));

        result.Error.Should().Be(ChallengeErrors.NotFound);
    }

    [Fact]
    public async Task LeaveChallengeCommandHandler_HandleAsync_RemovesRowAndIsIdempotent()
    {
        Challenge challenge = Given(NewChallenge());
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, _user, Monday));

        (await Leave().HandleAsync(new LeaveChallengeCommand(_user, challenge.Id))).IsSuccess.Should().BeTrue();
        (await Leave().HandleAsync(new LeaveChallengeCommand(_user, challenge.Id))).IsSuccess.Should().BeTrue();

        _repo.Participants.Should().BeEmpty();
    }

    [Fact]
    public async Task LeaveChallengeCommandHandler_HandleAsync_FinishedChallengeKeepsItsResult()
    {
        Challenge challenge = Given(NewChallenge(status: ChallengeStatus.Finished));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, _user, Monday));

        Result result = await Leave().HandleAsync(new LeaveChallengeCommand(_user, challenge.Id));

        result.Error.Should().Be(ChallengeErrors.NotOpen);
        _repo.Participants.Should().ContainSingle();
    }
}
