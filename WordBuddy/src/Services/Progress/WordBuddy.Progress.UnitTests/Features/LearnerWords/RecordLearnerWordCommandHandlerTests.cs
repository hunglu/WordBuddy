using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.LearnerWords;

public class RecordLearnerWordCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerWordMembershipRepository> _repository = new();
    private readonly Mock<ILearnerWordStateRepository> _states = new();
    private readonly List<LearnerWordState> _addedStates = [];

    public RecordLearnerWordCommandHandlerTests()
    {
        _states
            .Setup(s => s.AddAsync(It.IsAny<LearnerWordState>(), It.IsAny<CancellationToken>()))
            .Callback<LearnerWordState, CancellationToken>((state, _) => _addedStates.Add(state))
            .ReturnsAsync(Result.Success());
    }

    private RecordLearnerWordAddedCommandHandler CreateAddedHandler() =>
        new(_repository.Object, _states.Object, new RecordLearnerWordAddedCommandValidator(), Mock.Of<ILogger<RecordLearnerWordAddedCommandHandler>>());

    private RecordLearnerWordRemovedCommandHandler CreateRemovedHandler() =>
        new(_repository.Object, _states.Object, new RecordLearnerWordRemovedCommandValidator(), Mock.Of<ILogger<RecordLearnerWordRemovedCommandHandler>>());

    /// <summary>Simulates the repository: applies the event and runs the before-save hook.</summary>
    private void GivenAddApplied(Guid userId, Guid senseId)
    {
        _repository
            .Setup(r => r.UpsertAddedAsync(userId, senseId, userId, Now,
                It.IsAny<Func<LearnerWordMembership, CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid u, Guid s, Guid by, DateTime at, Func<LearnerWordMembership, CancellationToken, Task<Result>> hook, CancellationToken ct) =>
            {
                Result hookResult = await hook(LearnerWordMembership.CreateAdded(Guid.NewGuid(), u, s, by, at), ct);
                return hookResult.IsSuccess ? Result.Success(true) : Result.Failure<bool>(hookResult.Error);
            });
    }

    private void GivenRemoveApplied(Guid userId, Guid senseId)
    {
        _repository
            .Setup(r => r.MarkRemovedAsync(userId, senseId, Now,
                It.IsAny<Func<LearnerWordMembership, CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Guid u, Guid s, DateTime at, Func<LearnerWordMembership, CancellationToken, Task<Result>> hook, CancellationToken ct) =>
            {
                Result hookResult = await hook(LearnerWordMembership.CreateRemoved(Guid.NewGuid(), u, s, at), ct);
                return hookResult.IsSuccess ? Result.Success(true) : Result.Failure<bool>(hookResult.Error);
            });
    }

    private void GivenNoState() =>
        _states
            .Setup(s => s.GetTrackedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWordState>(Error.NotFound("LearnerWordState.NotFound", "none")));

    private void GivenState(LearnerWordState state) =>
        _states
            .Setup(s => s.GetTrackedAsync(state.UserId, state.SenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(state));

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_CreatesNewStateDueAtAddTime()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        GivenAddApplied(userId, senseId);
        GivenNoState();

        Result result = await CreateAddedHandler().HandleAsync(new RecordLearnerWordAddedCommand(userId, senseId, userId, Now));

        result.IsSuccess.Should().BeTrue();
        LearnerWordState state = _addedStates.Should().ContainSingle().Subject;
        state.UserId.Should().Be(userId);
        state.SenseId.Should().Be(senseId);
        state.Status.Should().Be(WordStatus.New);
        state.DueAtUtc.Should().Be(Now);
        state.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_DuplicateAddCreatesNoExtraState()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        LearnerWordState existing = LearnerWordState.CreateNew(Guid.NewGuid(), userId, senseId, Now);
        GivenAddApplied(userId, senseId);
        GivenState(existing);

        Result result = await CreateAddedHandler().HandleAsync(new RecordLearnerWordAddedCommand(userId, senseId, userId, Now));

        result.IsSuccess.Should().BeTrue();
        _addedStates.Should().BeEmpty();
        existing.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_ReAddActivatesAndKeepsFsrsData()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        VocabularySchedulingOptions options = new();
        LearnerWordState existing = LearnerWordState.CreateNew(Guid.NewGuid(), userId, senseId, Now.AddDays(-5));
        existing.ApplyReview(FsrsRating.Easy, Now.AddDays(-5), new FsrsScheduler(options), options);
        existing.Deactivate();
        double stability = existing.Stability;
        GivenAddApplied(userId, senseId);
        GivenState(existing);

        await CreateAddedHandler().HandleAsync(new RecordLearnerWordAddedCommand(userId, senseId, userId, Now));

        existing.IsActive.Should().BeTrue();
        existing.Stability.Should().Be(stability);
        existing.Reps.Should().Be(1);
        _addedStates.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_IgnoredEventTouchesNoState()
    {
        _repository
            .Setup(r => r.UpsertAddedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
                It.IsAny<Func<LearnerWordMembership, CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        Result result = await CreateAddedHandler().HandleAsync(
            new RecordLearnerWordAddedCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now));

        result.IsSuccess.Should().BeTrue();
        _states.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_EmptyIdsFailValidation()
    {
        Result result = await CreateAddedHandler().HandleAsync(new RecordLearnerWordAddedCommand(Guid.Empty, Guid.Empty, Guid.Empty, default));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RecordLearnerWordAdded.Validation");
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_RepositoryFailurePassesThrough()
    {
        Error error = Error.Conflict("LearnerWordMembership.ConcurrentUpdate", "conflict");
        _repository
            .Setup(r => r.UpsertAddedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
                It.IsAny<Func<LearnerWordMembership, CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(error));

        Result result = await CreateAddedHandler().HandleAsync(
            new RecordLearnerWordAddedCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now));

        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task RecordLearnerWordRemovedCommandHandler_HandleAsync_DeactivatesStateAndKeepsData()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        LearnerWordState existing = LearnerWordState.CreateNew(Guid.NewGuid(), userId, senseId, Now.AddDays(-1));
        GivenRemoveApplied(userId, senseId);
        GivenState(existing);

        Result result = await CreateRemovedHandler().HandleAsync(new RecordLearnerWordRemovedCommand(userId, senseId, Now));

        result.IsSuccess.Should().BeTrue();
        existing.IsActive.Should().BeFalse();
        existing.DueAtUtc.Should().Be(Now.AddDays(-1));
    }

    [Fact]
    public async Task RecordLearnerWordRemovedCommandHandler_HandleAsync_UnknownStateIsSuccess()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        GivenRemoveApplied(userId, senseId);
        GivenNoState();

        Result result = await CreateRemovedHandler().HandleAsync(new RecordLearnerWordRemovedCommand(userId, senseId, Now));

        result.IsSuccess.Should().BeTrue();
        _addedStates.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordLearnerWordRemovedCommandHandler_HandleAsync_EmptyIdsFailValidation()
    {
        Result result = await CreateRemovedHandler().HandleAsync(new RecordLearnerWordRemovedCommand(Guid.Empty, Guid.Empty, default));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RecordLearnerWordRemoved.Validation");
        _repository.VerifyNoOtherCalls();
    }
}
