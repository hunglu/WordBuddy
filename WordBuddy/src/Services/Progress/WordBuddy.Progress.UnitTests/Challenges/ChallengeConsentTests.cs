using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Features.Challenges;
using WordBuddy.Progress.Application.Features.Challenges.Commands.SetChildChallengeConsent;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class ChallengeConsentTests
{
    private readonly FakeChallengeRepository _repo = new();
    private readonly FakeChallengeProfileRepository _profiles = new();
    private readonly FakeDistributedCache _cache = new();
    private readonly Mock<ISupportLinkProjectionRepository> _links = new();
    private readonly FixedTimeProvider _time = new(Monday);
    private readonly Guid _guardian = Guid.NewGuid();
    private readonly Guid _child = Guid.NewGuid();

    private ChallengeConsentCleaner Cleaner() => new(_repo, _cache, Mock.Of<ILogger<ChallengeConsentCleaner>>());

    private SetChildChallengeConsentCommandHandler Handler() =>
        new(_repo, _profiles, _links.Object, Cleaner(), _time, new SetChildChallengeConsentCommandValidator(),
            Mock.Of<ILogger<SetChildChallengeConsentCommandHandler>>());

    private void Arrange(bool callerIsGuardian, bool linkActive, AgeGroup childAge = AgeGroup.Child)
    {
        _profiles.Profiles[_guardian] = Profile(_guardian, AgeGroup.Adult, guardian: callerIsGuardian);
        _profiles.Profiles[_child] = Profile(_child, childAge);
        _links.Setup(l => l.HasActiveLinkAsync(_guardian, _child, It.IsAny<CancellationToken>())).ReturnsAsync(linkActive);
    }

    [Fact]
    public async Task SetChildChallengeConsentCommandHandler_HandleAsync_LinkWithoutGuardianRoleFails()
    {
        Arrange(callerIsGuardian: false, linkActive: true);

        Result result = await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: true));

        result.Error.Should().Be(ChallengeErrors.NotGuardian);
        _repo.Consents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetChildChallengeConsentCommandHandler_HandleAsync_GuardianRoleWithoutLinkFails()
    {
        Arrange(callerIsGuardian: true, linkActive: false);

        Result result = await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: true));

        result.Error.Should().Be(ChallengeErrors.NoActiveLink);
        _repo.Consents.Should().BeEmpty();
    }

    [Fact]
    public async Task SetChildChallengeConsentCommandHandler_HandleAsync_CallerWithoutProfileIsNotGuardian()
    {
        _links.Setup(l => l.HasActiveLinkAsync(_guardian, _child, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Result result = await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: true));

        result.Error.Should().Be(ChallengeErrors.NotGuardian);
    }

    [Fact]
    public async Task SetChildChallengeConsentCommandHandler_HandleAsync_GuardianWithLinkGivesConsentOnce()
    {
        Arrange(callerIsGuardian: true, linkActive: true);

        (await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: true))).IsSuccess.Should().BeTrue();
        (await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: true))).IsSuccess.Should().BeTrue();

        _repo.Consents.Should().ContainSingle(c => c.ChildId == _child && c.EnabledBy == _guardian);
    }

    [Fact]
    public async Task SetChildChallengeConsentCommandHandler_HandleAsync_AdultTargetIsRejected()
    {
        Arrange(callerIsGuardian: true, linkActive: true, childAge: AgeGroup.Adult);

        Result result = await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: true));

        result.Error.Should().Be(ChallengeErrors.NotChild);
    }

    [Fact]
    public async Task SetChildChallengeConsentCommandHandler_HandleAsync_SwitchingOffRemovesConsentAndUnfinishedParticipation()
    {
        Arrange(callerIsGuardian: true, linkActive: true);
        Challenge running = NewChallenge(ChallengeAudience.Child, status: ChallengeStatus.Running);
        Challenge finished = NewChallenge(ChallengeAudience.Child, status: ChallengeStatus.Finished);
        _repo.Challenges.AddRange([running, finished]);
        _repo.Consents.Add(ChallengeConsent.Create(_child, _guardian, Monday));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), running.Id, _child, Monday));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), finished.Id, _child, Monday));
        _cache.Items[ChallengeCache.BoardKey(running.Id)] = [1];

        Result result = await Handler().HandleAsync(new SetChildChallengeConsentCommand(_guardian, _child, Enabled: false));

        result.IsSuccess.Should().BeTrue();
        _repo.Consents.Should().BeEmpty();
        _repo.Participants.Should().ContainSingle(p => p.ChallengeId == finished.Id, "a finished result is kept");
        _cache.Items.Should().NotContainKey(ChallengeCache.BoardKey(running.Id));
    }

    [Fact]
    public async Task ChallengeConsentCleaner_RemoveForLink_DeletesOnlyThatSupportersConsent()
    {
        Guid otherGuardian = Guid.NewGuid();
        Challenge running = NewChallenge(ChallengeAudience.Child, status: ChallengeStatus.Running);
        _repo.Challenges.Add(running);
        _repo.Consents.Add(ChallengeConsent.Create(_child, _guardian, Monday));
        _repo.Consents.Add(ChallengeConsent.Create(_child, otherGuardian, Monday));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), running.Id, _child, Monday));

        Result result = await Cleaner().RemoveForLinkAsync(_child, _guardian);

        result.IsSuccess.Should().BeTrue();
        _repo.Consents.Should().ContainSingle(c => c.EnabledBy == otherGuardian);
        _repo.Participants.Should().ContainSingle("the child still has the other guardian's consent");
    }

    [Fact]
    public async Task ChallengeConsentCleaner_RemoveForLink_LastConsentGoneTakesTheChildOutOfUnfinishedChallenges()
    {
        Challenge running = NewChallenge(ChallengeAudience.Child, status: ChallengeStatus.Running);
        _repo.Challenges.Add(running);
        _repo.Consents.Add(ChallengeConsent.Create(_child, _guardian, Monday));
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), running.Id, _child, Monday));

        await Cleaner().RemoveForLinkAsync(_child, _guardian);

        _repo.Consents.Should().BeEmpty();
        _repo.Participants.Should().BeEmpty();
        _repo.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task ChallengeConsentCleaner_RemoveByGuardian_DeletesEveryConsentThatGuardianGave()
    {
        Guid secondChild = Guid.NewGuid();
        _repo.Consents.Add(ChallengeConsent.Create(_child, _guardian, Monday));
        _repo.Consents.Add(ChallengeConsent.Create(secondChild, _guardian, Monday));
        _repo.Consents.Add(ChallengeConsent.Create(secondChild, Guid.NewGuid(), Monday));

        await Cleaner().RemoveByGuardianAsync(_guardian);

        _repo.Consents.Should().ContainSingle(c => c.ChildId == secondChild && c.EnabledBy != _guardian);
    }

    [Fact]
    public async Task ChallengeConsentCleaner_NothingToRemove_DoesNotSave()
    {
        Result result = await Cleaner().RemoveByGuardianAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        _repo.SaveCount.Should().Be(0);
    }
}
