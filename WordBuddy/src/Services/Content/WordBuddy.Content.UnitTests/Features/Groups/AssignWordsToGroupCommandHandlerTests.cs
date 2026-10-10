using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Groups;
using WordBuddy.Content.Application.Features.Groups.Commands.AssignWordsToGroup;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Groups;

public class AssignWordsToGroupCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerGroupProjectionRepository> _projection = new();
    private readonly Mock<ISupportLinkProjectionRepository> _supportLinks = new();
    private readonly Mock<IGroupWordRepository> _words = new();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly List<LearnerWord> _addedLinks = [];
    private readonly List<GroupWordAssignment> _addedAssignments = [];
    private readonly HashSet<(Guid UserId, Guid SenseId)> _existing = [];
    private readonly List<Sense> _senses = [];
    private List<LearnerGroupMemberProjection> _rows = [];
    private HashSet<Guid> _linked = [];

    public AssignWordsToGroupCommandHandlerTests()
    {
        _projection.Setup(p => p.GetGroupAsync(_groupId, It.IsAny<CancellationToken>())).ReturnsAsync(() => _rows);
        _supportLinks.Setup(l => l.GetLearnersWithActiveLinkAsync(_owner, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, IReadOnlyCollection<Guid> ids, CancellationToken _) => (IReadOnlySet<Guid>)ids.Where(_linked.Contains).ToHashSet());
        _words.Setup(w => w.GetSensesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => _senses.Where(s => ids.Contains(s.Id)).ToList());
        _words.Setup(w => w.GetExistingLinksAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (IReadOnlySet<(Guid UserId, Guid SenseId)>)_existing);
        _words.Setup(w => w.AddLinksAsync(It.IsAny<IReadOnlyCollection<LearnerWord>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<LearnerWord>, CancellationToken>((links, _) => _addedLinks.AddRange(links))
            .Returns(Task.CompletedTask);
        _words.Setup(w => w.AddAssignmentsAsync(It.IsAny<IReadOnlyCollection<GroupWordAssignment>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<GroupWordAssignment>, CancellationToken>((items, _) => _addedAssignments.AddRange(items))
            .Returns(Task.CompletedTask);
        _words.Setup(w => w.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private AssignWordsToGroupCommandHandler CreateHandler() =>
        new(_projection.Object, _supportLinks.Object, _words.Object, new FixedTime(Now), new AssignWordsToGroupCommandValidator(),
            Mock.Of<ILogger<AssignWordsToGroupCommandHandler>>());

    private Guid GivenMember(bool active = true, bool linked = true, Guid? owner = null)
    {
        Guid learner = Guid.NewGuid();
        _rows.Add(LearnerGroupMemberProjection.Create(_groupId, owner ?? _owner, learner, active, Now));
        if (linked)
        {
            _linked.Add(learner);
        }

        return learner;
    }

    private Sense GivenSense(Sense sense)
    {
        _senses.Add(sense);
        return sense;
    }

    private static Sense ChildSafeShared() => TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);

    private static Sense AdultOnlyShared() => TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);

    private static Sense AutoFillAwaitingApproval() =>
        Sense.CreateAutoFill(Guid.NewGuid(), Guid.NewGuid(), "apple", "a fruit", ["I eat an apple."]).Value;

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_ChildSafeWordAddedForEveryActiveMemberOnce()
    {
        Guid a = GivenMember();
        Guid b = GivenMember();
        Sense sense = GivenSense(ChildSafeShared());

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [sense.Id]));

        result.Value.Should().Be(new GroupWordAssignmentResultDto(2, 0, 0));
        _addedLinks.Select(l => l.UserId).Should().BeEquivalentTo([a, b]);
        _addedLinks.Should().OnlyContain(l => l.AddedBy == LearnerWordAddedBy.Supporter && l.AddedByUserId == _owner && !l.IsAuthor);
        _addedAssignments.Should().ContainSingle(x => x.GroupId == _groupId && x.SenseId == sense.Id && x.AssignedBy == _owner);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_AutoFillWordApprovedByAssignerForEachMember()
    {
        GivenMember();
        Sense sense = GivenSense(AutoFillAwaitingApproval());

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [sense.Id]));

        result.Value.Added.Should().Be(1);
        _addedLinks.Should().ContainSingle(l => l.ChildApprovedByUserId == _owner && l.ChildApprovedAtUtc != null);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_AdultOnlyWordSkippedAndCounted()
    {
        GivenMember();
        GivenMember();
        Sense sense = GivenSense(AdultOnlyShared());

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [sense.Id]));

        result.Value.Should().Be(new GroupWordAssignmentResultDto(0, 0, 2));
        _addedLinks.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_ExistingLinkCountedNotDuplicated()
    {
        Guid a = GivenMember();
        Guid b = GivenMember();
        Sense sense = GivenSense(ChildSafeShared());
        _existing.Add((a, sense.Id));

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [sense.Id]));

        result.Value.Should().Be(new GroupWordAssignmentResultDto(1, 1, 0));
        _addedLinks.Should().ContainSingle(l => l.UserId == b);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_InactiveMemberAndUnlinkedMemberLeftOut()
    {
        GivenMember(active: false);
        GivenMember(linked: false);
        Guid kept = GivenMember();
        Sense sense = GivenSense(ChildSafeShared());

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [sense.Id]));

        result.Value.Added.Should().Be(1);
        _addedLinks.Should().ContainSingle(l => l.UserId == kept);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_NonOwnerForbidden()
    {
        GivenMember();
        Sense sense = GivenSense(ChildSafeShared());

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, Guid.NewGuid(), [sense.Id]));

        result.Error.Should().Be(GroupWordErrors.Forbidden);
        _addedLinks.Should().BeEmpty();
        _words.Verify(w => w.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_PrivateOrUnknownWordRejected()
    {
        GivenMember();
        Sense privateWord = GivenSense(TestWords.Learner(_owner));

        Result<GroupWordAssignmentResultDto> privateResult =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [privateWord.Id]));
        Result<GroupWordAssignmentResultDto> unknownResult =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [Guid.NewGuid()]));

        privateResult.Error.Should().Be(GroupWordErrors.SenseNotFound);
        unknownResult.Error.Should().Be(GroupWordErrors.SenseNotFound);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_NoActiveMembersRejected()
    {
        GivenMember(active: false);
        Sense sense = GivenSense(ChildSafeShared());

        Result<GroupWordAssignmentResultDto> result =
            await CreateHandler().HandleAsync(new AssignWordsToGroupCommand(_groupId, _owner, [sense.Id]));

        result.Error.Should().Be(GroupWordErrors.NoActiveMembers);
    }

    [Fact]
    public async Task AssignWordsToGroupCommandHandler_HandleAsync_MoreThanFiftyWordsFailsValidation()
    {
        GivenMember();

        Result<GroupWordAssignmentResultDto> result = await CreateHandler().HandleAsync(
            new AssignWordsToGroupCommand(_groupId, _owner, Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToList()));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    private sealed class FixedTime : TimeProvider
    {
        private readonly DateTime _utcNow;

        public FixedTime(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
