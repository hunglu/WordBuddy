using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.LearnerWords;

public class RecordLearnerWordCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ILearnerWordMembershipRepository> _repository = new();

    private RecordLearnerWordAddedCommandHandler CreateAddedHandler() =>
        new(_repository.Object, new RecordLearnerWordAddedCommandValidator(), Mock.Of<ILogger<RecordLearnerWordAddedCommandHandler>>());

    private RecordLearnerWordRemovedCommandHandler CreateRemovedHandler() =>
        new(_repository.Object, new RecordLearnerWordRemovedCommandValidator(), Mock.Of<ILogger<RecordLearnerWordRemovedCommandHandler>>());

    [Fact]
    public async Task RecordLearnerWordAddedCommandHandler_HandleAsync_UpsertsMembership()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        _repository
            .Setup(r => r.UpsertAddedAsync(userId, senseId, userId, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        Result result = await CreateAddedHandler().HandleAsync(new RecordLearnerWordAddedCommand(userId, senseId, userId, Now));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.UpsertAddedAsync(userId, senseId, userId, Now, It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(r => r.UpsertAddedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<bool>(error));

        Result result = await CreateAddedHandler().HandleAsync(
            new RecordLearnerWordAddedCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now));

        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task RecordLearnerWordRemovedCommandHandler_HandleAsync_MarksRemoved()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        _repository
            .Setup(r => r.MarkRemovedAsync(userId, senseId, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(true));

        Result result = await CreateRemovedHandler().HandleAsync(new RecordLearnerWordRemovedCommand(userId, senseId, Now));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.MarkRemovedAsync(userId, senseId, Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordLearnerWordRemovedCommandHandler_HandleAsync_UnknownMembershipIsSuccess()
    {
        _repository
            .Setup(r => r.MarkRemovedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(false));

        Result result = await CreateRemovedHandler().HandleAsync(
            new RecordLearnerWordRemovedCommand(Guid.NewGuid(), Guid.NewGuid(), Now));

        result.IsSuccess.Should().BeTrue();
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
