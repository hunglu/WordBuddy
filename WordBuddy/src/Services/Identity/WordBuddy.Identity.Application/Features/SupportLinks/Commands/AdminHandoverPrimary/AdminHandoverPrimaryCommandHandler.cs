using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;

/// <summary>
/// Moves the Primary flag to another active link of the learner, in one transaction: clear the
/// old Primary first (save), then set the new one (save), so the one-active-Primary index holds.
/// Writes one audit row per changed link. After this, the old Primary link can be unlinked normally.
/// </summary>
public sealed class AdminHandoverPrimaryCommandHandler : ICommandHandler<AdminHandoverPrimaryCommand>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<AdminHandoverPrimaryCommand> _validator;
    private readonly ILogger<AdminHandoverPrimaryCommandHandler> _logger;

    public AdminHandoverPrimaryCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<AdminHandoverPrimaryCommand> validator,
        ILogger<AdminHandoverPrimaryCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(AdminHandoverPrimaryCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AdminHandoverPrimaryCommand started: AdminId={AdminId}, LearnerId={LearnerId}, NewPrimaryLinkId={NewPrimaryLinkId}",
            command.AdminId, command.LearnerId, command.NewPrimaryLinkId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AdminHandoverPrimaryCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("AdminHandoverPrimary.Validation", validation.ToString()));
        }

        Result<User> learner = await _users.GetByIdAsync(command.LearnerId, ct);
        if (learner.IsFailure)
        {
            return learner;
        }

        if (learner.Value.AgeGroup != AgeGroup.Child)
        {
            return Result.Failure(SupportLinkErrors.PrimaryChildOnly);
        }

        IReadOnlyList<SupportLink> learnerLinks = await _links.GetLearnerLinksTrackedAsync(command.LearnerId, ct);
        SupportLink? newPrimary = learnerLinks.FirstOrDefault(l => l.Id == command.NewPrimaryLinkId);
        if (newPrimary is null)
        {
            return Result.Failure(SupportLinkErrors.LinkNotFound);
        }

        if (!newPrimary.IsActive || newPrimary.IsPrimary)
        {
            return Result.Failure(SupportLinkErrors.InvalidStatus);
        }

        SupportLink? oldPrimary = learnerLinks.FirstOrDefault(l => l.IsPrimary && l.IsActive);
        DateTime now = _time.GetUtcNow().UtcDateTime;
        string reason = command.Reason.Trim();

        Result result = await _links.ExecuteInTransactionAsync(async token =>
        {
            if (oldPrimary is not null)
            {
                oldPrimary.ClearPrimary(now);
                await _links.AddAuditEntryAsync(
                    new SupportLinkAuditEntry(Guid.NewGuid(), oldPrimary.Id, command.AdminId, SupportLinkAuditAction.AdminPrimaryHandover, now, reason),
                    token);

                Result first = await _links.SaveChangesAsync(token);
                if (first.IsFailure)
                {
                    return first;
                }
            }

            Result make = newPrimary.MakePrimary(now);
            if (make.IsFailure)
            {
                return make;
            }

            await _links.AddAuditEntryAsync(
                new SupportLinkAuditEntry(Guid.NewGuid(), newPrimary.Id, command.AdminId, SupportLinkAuditAction.AdminPrimaryHandover, now, reason),
                token);
            return await _links.SaveChangesAsync(token);
        }, ct);

        if (result.IsFailure)
        {
            _logger.LogWarning("AdminHandoverPrimaryCommand failed: {ErrorCode}", result.Error.Code);
            return result;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, command.LearnerId, newPrimary.SupporterId, oldPrimary?.SupporterId);
        _logger.LogInformation(
            "AdminHandoverPrimaryCommand succeeded: LearnerId={LearnerId}, NewPrimaryLinkId={NewPrimaryLinkId}",
            command.LearnerId, newPrimary.Id);
        return Result.Success();
    }
}
