using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;

public sealed class AdminRejectUnlinkCommandHandler : ICommandHandler<AdminRejectUnlinkCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<AdminRejectUnlinkCommand> _validator;
    private readonly ILogger<AdminRejectUnlinkCommandHandler> _logger;

    public AdminRejectUnlinkCommandHandler(
        ISupportLinkRepository links,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<AdminRejectUnlinkCommand> validator,
        ILogger<AdminRejectUnlinkCommandHandler> logger)
    {
        _links = links;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(AdminRejectUnlinkCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AdminRejectUnlinkCommand started: AdminId={AdminId}, UnlinkRequestId={UnlinkRequestId}",
            command.AdminId, command.UnlinkRequestId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AdminRejectUnlinkCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("AdminRejectUnlink.Validation", validation.ToString()));
        }

        Result<UnlinkRequest> found = await _links.GetUnlinkRequestTrackedAsync(command.UnlinkRequestId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        UnlinkRequest request = found.Value;
        DateTime now = _time.GetUtcNow().UtcDateTime;

        Result reject = request.RejectByAdmin(command.AdminId, now);
        if (reject.IsFailure)
        {
            _logger.LogWarning("AdminRejectUnlinkCommand rejected: ErrorCode={ErrorCode}", reject.Error.Code);
            return reject;
        }

        await _links.AddAuditEntryAsync(
            new SupportLinkAuditEntry(Guid.NewGuid(), request.LinkId, command.AdminId, SupportLinkAuditAction.AdminUnlinkRejected, now, command.Reason.Trim()),
            ct);

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        IReadOnlyList<SupportLink> links = await _links.GetLinksByIdsAsync([request.LinkId], ct);
        SupportLink? link = links.FirstOrDefault();
        if (link is not null)
        {
            Guid? primaryId = await _links.GetActivePrimarySupporterIdAsync(link.LearnerId, ct);
            await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, primaryId);
        }

        _logger.LogInformation("AdminRejectUnlinkCommand succeeded: LinkId={LinkId}", request.LinkId);
        return Result.Success();
    }
}
