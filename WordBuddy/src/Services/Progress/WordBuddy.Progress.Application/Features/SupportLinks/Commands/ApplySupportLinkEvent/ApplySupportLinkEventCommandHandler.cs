using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;

/// <summary>
/// Idempotent upsert by <c>LinkId</c>. A replay or an event older than the stored state changes nothing (still a success).
/// On an applied revoke, the supporter cap set by the revoked supporter is cleared in the same save.
/// </summary>
public sealed class ApplySupportLinkEventCommandHandler : ICommandHandler<ApplySupportLinkEventCommand>
{
    private readonly ISupportLinkProjectionRepository _repository;
    private readonly IVocabularyLearnerSettingsRepository _settings;
    private readonly IValidator<ApplySupportLinkEventCommand> _validator;
    private readonly ILogger<ApplySupportLinkEventCommandHandler> _logger;

    public ApplySupportLinkEventCommandHandler(
        ISupportLinkProjectionRepository repository,
        IVocabularyLearnerSettingsRepository settings,
        IValidator<ApplySupportLinkEventCommand> validator,
        ILogger<ApplySupportLinkEventCommandHandler> logger)
    {
        _repository = repository;
        _settings = settings;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ApplySupportLinkEventCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ApplySupportLinkEventCommand started: LinkId={LinkId}, IsActive={IsActive}", command.LinkId, command.IsActive);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ApplySupportLinkEventCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("ApplySupportLinkEvent.Validation", validation.ToString()));
        }

        bool applied;
        Result<SupportLinkProjection> existing = await _repository.GetTrackedAsync(command.LinkId, ct);
        if (existing.IsSuccess)
        {
            applied = existing.Value.Apply(command.IsActive, command.OccurredAtUtc);
        }
        else if (existing.Error.Type == ErrorType.NotFound)
        {
            await _repository.AddAsync(
                SupportLinkProjection.Create(command.LinkId, command.LearnerId, command.SupporterId, command.IsActive, command.OccurredAtUtc),
                ct);
            applied = true;
        }
        else
        {
            return Result.Failure(existing.Error);
        }

        if (applied && !command.IsActive)
        {
            Result<VocabularyLearnerSettings> settings = await _settings.GetTrackedAsync(command.LearnerId, ct);
            if (settings.IsSuccess && settings.Value.ClearSupporterCapSetBy(command.SupporterId))
            {
                _logger.LogInformation(
                    "ApplySupportLinkEventCommand cleared supporter cap: LearnerId={LearnerId}, LinkId={LinkId}",
                    command.LearnerId, command.LinkId);
            }
        }

        if (applied)
        {
            Result save = await _repository.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning("ApplySupportLinkEventCommand failed to persist: {ErrorCode}", save.Error.Code);
                return save;
            }
        }

        _logger.LogInformation("ApplySupportLinkEventCommand succeeded: LinkId={LinkId}, Applied={Applied}", command.LinkId, applied);
        return Result.Success();
    }
}
