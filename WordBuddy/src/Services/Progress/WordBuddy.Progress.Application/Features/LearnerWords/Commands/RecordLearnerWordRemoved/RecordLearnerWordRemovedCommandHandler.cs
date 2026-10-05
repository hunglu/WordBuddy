using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;

/// <summary>Marks the learner's membership inactive (row kept). Removing an unknown membership is a
/// success. Idempotent; an event older than the stored state is ignored. Logs ids only.</summary>
public sealed class RecordLearnerWordRemovedCommandHandler : ICommandHandler<RecordLearnerWordRemovedCommand>
{
    private readonly ILearnerWordMembershipRepository _repository;
    private readonly IValidator<RecordLearnerWordRemovedCommand> _validator;
    private readonly ILogger<RecordLearnerWordRemovedCommandHandler> _logger;

    public RecordLearnerWordRemovedCommandHandler(
        ILearnerWordMembershipRepository repository,
        IValidator<RecordLearnerWordRemovedCommand> validator,
        ILogger<RecordLearnerWordRemovedCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RecordLearnerWordRemovedCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RecordLearnerWordRemovedCommand started: UserId={UserId}, SenseId={SenseId}",
            command.UserId, command.SenseId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RecordLearnerWordRemovedCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RecordLearnerWordRemoved.Validation", validation.ToString()));
        }

        Result<bool> removeResult = await _repository.MarkRemovedAsync(
            command.UserId, command.SenseId, command.RemovedAtUtc, ct);
        if (removeResult.IsFailure)
        {
            _logger.LogWarning(
                "RecordLearnerWordRemovedCommand failed to persist membership: {ErrorCode} — {ErrorDescription}",
                removeResult.Error.Code, removeResult.Error.Description);
            return Result.Failure(removeResult.Error);
        }

        _logger.LogInformation(
            "RecordLearnerWordRemovedCommand succeeded: UserId={UserId}, SenseId={SenseId}, Applied={Applied}",
            command.UserId, command.SenseId, removeResult.Value);
        return Result.Success();
    }
}
