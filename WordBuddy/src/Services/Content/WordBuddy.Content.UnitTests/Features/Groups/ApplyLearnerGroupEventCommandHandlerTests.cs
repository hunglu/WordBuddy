using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Groups;

public class ApplyLearnerGroupEventCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 10, 9, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerGroupProjectionRepository> _repository = new();
    private readonly List<LearnerGroupMemberProjection> _rows = [];
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _learnerId = Guid.NewGuid();

    public ApplyLearnerGroupEventCommandHandlerTests()
    {
        _repository.Setup(r => r.GetTrackedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid group, Guid learner, CancellationToken _) =>
                _rows.FirstOrDefault(p => p.GroupId == group && p.LearnerId == learner) is { } row
                    ? Result.Success(row)
                    : Result.Failure<LearnerGroupMemberProjection>(Error.NotFound("LearnerGroupMemberProjection.NotFound", "none")));
        _repository.Setup(r => r.GetGroupTrackedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid group, CancellationToken _) => _rows.Where(p => p.GroupId == group).ToList());
        _repository.Setup(r => r.AddAsync(It.IsAny<LearnerGroupMemberProjection>(), It.IsAny<CancellationToken>()))
            .Callback<LearnerGroupMemberProjection, CancellationToken>((p, _) => _rows.Add(p))
            .Returns(Task.CompletedTask);
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private ApplyLearnerGroupEventCommandHandler CreateHandler() =>
        new(_repository.Object, new ApplyLearnerGroupEventCommandValidator(), Mock.Of<ILogger<ApplyLearnerGroupEventCommandHandler>>());

    private ApplyLearnerGroupEventCommand Member(bool isActive, DateTime at) => new(_groupId, _ownerId, _learnerId, isActive, at);

    [Fact]
    public async Task ApplyLearnerGroupEventCommandHandler_HandleAsync_FirstEventInsertsRow()
    {
        (await CreateHandler().HandleAsync(Member(isActive: true, T0))).IsSuccess.Should().BeTrue();

        _rows.Should().ContainSingle(p => p.GroupId == _groupId && p.LearnerId == _learnerId && p.OwnerId == _ownerId && p.IsActive);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyLearnerGroupEventCommandHandler_HandleAsync_ReplayChangesNothing()
    {
        ApplyLearnerGroupEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Member(isActive: true, T0));

        (await handler.HandleAsync(Member(isActive: true, T0))).IsSuccess.Should().BeTrue();

        _rows.Should().ContainSingle();
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyLearnerGroupEventCommandHandler_HandleAsync_OlderEventIgnored()
    {
        ApplyLearnerGroupEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Member(isActive: false, T0.AddMinutes(5)));

        (await handler.HandleAsync(Member(isActive: true, T0))).IsSuccess.Should().BeTrue();

        _rows[0].IsActive.Should().BeFalse();
        _rows[0].UpdatedAtUtc.Should().Be(T0.AddMinutes(5));
    }

    [Fact]
    public async Task ApplyLearnerGroupEventCommandHandler_HandleAsync_RemovedAfterActivatedDeactivates()
    {
        ApplyLearnerGroupEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Member(isActive: true, T0));

        await handler.HandleAsync(Member(isActive: false, T0.AddDays(1)));

        _rows[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyLearnerGroupEventCommandHandler_HandleAsync_GroupDeletedDeactivatesEveryMember()
    {
        ApplyLearnerGroupEventCommandHandler handler = CreateHandler();
        await handler.HandleAsync(Member(isActive: true, T0));
        await handler.HandleAsync(new ApplyLearnerGroupEventCommand(_groupId, _ownerId, Guid.NewGuid(), true, T0));

        (await handler.HandleAsync(new ApplyLearnerGroupEventCommand(_groupId, _ownerId, null, false, T0.AddHours(1)))).IsSuccess.Should().BeTrue();

        _rows.Should().HaveCount(2).And.OnlyContain(p => !p.IsActive);
    }

    [Fact]
    public async Task ApplyLearnerGroupEventCommandHandler_HandleAsync_WholeGroupCannotActivate()
    {
        Result result = await CreateHandler().HandleAsync(new ApplyLearnerGroupEventCommand(_groupId, _ownerId, null, true, T0));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }
}
