using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Caching;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.ApproveChildWord;

/// <summary>Supporter → <see cref="LearnerWord.ApproveForChild"/> (that child only). Admin →
/// <see cref="Sense.ApproveForChildren"/> (every child). Deletes the sense, shared-pool and
/// catalog cache keys on success.</summary>
public sealed class ApproveChildWordCommandHandler : ICommandHandler<ApproveChildWordCommand>
{
    private readonly IAutofillRepository _repository;
    private readonly IDistributedCache _cache;
    private readonly IValidator<ApproveChildWordCommand> _validator;
    private readonly ILogger<ApproveChildWordCommandHandler> _logger;

    public ApproveChildWordCommandHandler(
        IAutofillRepository repository,
        IDistributedCache cache,
        IValidator<ApproveChildWordCommand> validator,
        ILogger<ApproveChildWordCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ApproveChildWordCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ApproveChildWordCommand started: SenseId={SenseId}, SupporterApproval={SupporterApproval}",
            command.SenseId, command.LearnerId is not null);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("ApproveChildWordCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("ApproveChildWord.Validation", validation.ToString()));
        }

        Result<Sense> approved = command.LearnerId is { } learnerId
            ? await ApproveForLearnerAsync(command.SenseId, learnerId, command.ApproverId, ct)
            : await ApproveGloballyAsync(command.SenseId, command.ApproverId, ct);
        if (approved.IsFailure)
        {
            _logger.LogWarning("ApproveChildWordCommand failed: {ErrorCode}", approved.Error.Code);
            return Result.Failure(approved.Error);
        }

        Result saved = await _repository.SaveChangesAsync(ct);
        if (saved.IsFailure)
        {
            _logger.LogWarning("ApproveChildWordCommand failed to persist: {ErrorCode}", saved.Error.Code);
            return saved;
        }

        await _cache.RemoveAsync(AutofillCacheKeys.Sense(command.SenseId), ct);
        await _cache.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, ct);
        await _cache.RemoveAsync(SharedVocabularyCacheKeys.All, ct);
        if (approved.Value.Lexeme is { } lexeme)
        {
            await _cache.RemoveAsync(AutofillCacheKeys.Catalog(lexeme.NormalizedLemma), ct);
        }

        _logger.LogInformation("ApproveChildWordCommand succeeded: SenseId={SenseId}", command.SenseId);
        return Result.Success();
    }

    private async Task<Result<Sense>> ApproveForLearnerAsync(Guid senseId, Guid learnerId, Guid supporterId, CancellationToken ct)
    {
        Result<LearnerWord> link = await _repository.GetTrackedLinkAsync(learnerId, senseId, ct);
        if (link.IsFailure || link.Value.Sense is not { Origin: SenseOrigin.AutoFill } sense || !link.Value.RequiresChildApproval)
        {
            return Result.Failure<Sense>(Error.NotFound(
                "ApproveChildWord.NotFound", $"Word {senseId} is not waiting for approval for this learner."));
        }

        Result result = link.Value.ApproveForChild(supporterId);
        return result.IsSuccess ? Result.Success(sense) : Result.Failure<Sense>(result.Error);
    }

    private async Task<Result<Sense>> ApproveGloballyAsync(Guid senseId, Guid adminId, CancellationToken ct)
    {
        Result<Sense> sense = await _repository.GetTrackedSenseAsync(senseId, ct);
        if (sense.IsFailure)
        {
            return sense;
        }

        Result result = sense.Value.ApproveForChildren(adminId);
        return result.IsSuccess ? sense : Result.Failure<Sense>(result.Error);
    }
}
