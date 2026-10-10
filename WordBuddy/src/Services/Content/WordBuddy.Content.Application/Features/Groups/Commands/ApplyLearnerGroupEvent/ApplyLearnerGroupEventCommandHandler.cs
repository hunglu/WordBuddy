using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;

/// <summary>
/// Idempotent upsert by (<c>GroupId</c>, <c>LearnerId</c>). A replay or an event older than the stored
/// state changes nothing (still a success). A whole-group event deactivates every known member.
/// </summary>
public sealed class ApplyLearnerGroupEventCommandHandler : ICommandHandler<ApplyLearnerGroupEventCommand>
{
    private readonly ILearnerGroupProjectionRepository _repository;
    private readonly IValidator<ApplyLearnerGroupEventCommand> _validator;
    private readonly ILogger<ApplyLearnerGroupEventCommandHandler> _logger;

    public ApplyLearnerGroupEventCommandHandler(
        ILearnerGroupProjectionRepository repository,
        IValidator<ApplyLearnerGroupEventCommand> validator,
        ILogger<ApplyLearnerGroupEventCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ApplyLearnerGroupEventCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ApplyLearnerGroupEventCommand started: GroupId={GroupId}, IsActive={IsActive}, WholeGroup={WholeGroup}",
            command.GroupId, command.IsActive, command.LearnerId is null);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ApplyLearnerGroupEventCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("ApplyLearnerGroupEvent.Validation", validation.ToString()));
        }

        bool applied = command.LearnerId is { } learnerId
            ? await ApplyToMemberAsync(command, learnerId, ct)
            : await ApplyToGroupAsync(command, ct);

        if (applied)
        {
            Result save = await _repository.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning("ApplyLearnerGroupEventCommand failed to persist: {ErrorCode}", save.Error.Code);
                return save;
            }
        }

        _logger.LogInformation("ApplyLearnerGroupEventCommand succeeded: GroupId={GroupId}, Applied={Applied}", command.GroupId, applied);
        return Result.Success();
    }

    private async Task<bool> ApplyToMemberAsync(ApplyLearnerGroupEventCommand command, Guid learnerId, CancellationToken ct)
    {
        Result<LearnerGroupMemberProjection> existing = await _repository.GetTrackedAsync(command.GroupId, learnerId, ct);
        if (existing.IsSuccess)
        {
            return existing.Value.Apply(command.IsActive, command.OccurredAtUtc);
        }

        await _repository.AddAsync(
            LearnerGroupMemberProjection.Create(command.GroupId, command.OwnerId, learnerId, command.IsActive, command.OccurredAtUtc),
            ct);
        return true;
    }

    private async Task<bool> ApplyToGroupAsync(ApplyLearnerGroupEventCommand command, CancellationToken ct)
    {
        IReadOnlyList<LearnerGroupMemberProjection> rows = await _repository.GetGroupTrackedAsync(command.GroupId, ct);
        bool changed = false;
        foreach (LearnerGroupMemberProjection row in rows)
        {
            changed |= row.Apply(isActive: false, command.OccurredAtUtc);
        }

        return changed;
    }
}
