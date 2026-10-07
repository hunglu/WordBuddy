using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;

/// <summary>Creates or re-activates the learner's membership and its <see cref="LearnerWordState"/>
/// (new state, or <see cref="LearnerWordState.Activate"/> keeping FSRS history) in the same save.
/// Idempotent; an event older than the stored state is ignored (still a success). Logs ids only.</summary>
public sealed class RecordLearnerWordAddedCommandHandler : ICommandHandler<RecordLearnerWordAddedCommand>
{
    private readonly ILearnerWordMembershipRepository _repository;
    private readonly ILearnerWordStateRepository _states;
    private readonly IValidator<RecordLearnerWordAddedCommand> _validator;
    private readonly ILogger<RecordLearnerWordAddedCommandHandler> _logger;

    public RecordLearnerWordAddedCommandHandler(
        ILearnerWordMembershipRepository repository,
        ILearnerWordStateRepository states,
        IValidator<RecordLearnerWordAddedCommand> validator,
        ILogger<RecordLearnerWordAddedCommandHandler> logger)
    {
        _repository = repository;
        _states = states;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RecordLearnerWordAddedCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RecordLearnerWordAddedCommand started: UserId={UserId}, SenseId={SenseId}",
            command.UserId, command.SenseId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RecordLearnerWordAddedCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RecordLearnerWordAdded.Validation", validation.ToString()));
        }

        Result<bool> upsertResult = await _repository.UpsertAddedAsync(
            command.UserId, command.SenseId, command.AddedBy, command.AddedAtUtc, SyncStateAsync, ct);
        if (upsertResult.IsFailure)
        {
            _logger.LogWarning(
                "RecordLearnerWordAddedCommand failed to persist membership: {ErrorCode} — {ErrorDescription}",
                upsertResult.Error.Code, upsertResult.Error.Description);
            return Result.Failure(upsertResult.Error);
        }

        _logger.LogInformation(
            "RecordLearnerWordAddedCommand succeeded: UserId={UserId}, SenseId={SenseId}, Applied={Applied}",
            command.UserId, command.SenseId, upsertResult.Value);
        return Result.Success();
    }

    /// <summary>Runs before the membership save: creates the state, or re-activates it.</summary>
    private async Task<Result> SyncStateAsync(LearnerWordMembership membership, CancellationToken ct)
    {
        Result<LearnerWordState> existing = await _states.GetTrackedAsync(membership.UserId, membership.SenseId, ct);
        if (existing.IsSuccess)
        {
            existing.Value.Activate();
            return Result.Success();
        }

        if (existing.Error.Type != ErrorType.NotFound)
        {
            return Result.Failure(existing.Error);
        }

        LearnerWordState state = LearnerWordState.CreateNew(Guid.NewGuid(), membership.UserId, membership.SenseId, membership.AddedAtUtc);
        return await _states.AddAsync(state, ct);
    }
}
