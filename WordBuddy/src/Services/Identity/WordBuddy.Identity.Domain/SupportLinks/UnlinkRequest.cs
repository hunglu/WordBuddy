using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>
/// Two-sided unlink of an active link. One side requests, the other confirms or declines. After
/// the wait time without action, the requester may escalate to an admin. Decline blocks the override.
/// The link stays <see cref="SupportLinkStatus.Active"/> until revoked.
/// </summary>
public sealed class UnlinkRequest : Entity
{
    /// <summary>Gets the link to end.</summary>
    public Guid LinkId { get; private set; }

    /// <summary>Gets the user who requested (the Primary when acting for a child learner).</summary>
    public Guid RequestedById { get; private set; }

    /// <summary>Gets the side the requester acted for.</summary>
    public LinkSide RequestedBySide { get; private set; }

    /// <summary>Gets when the request was made, UTC.</summary>
    public DateTime RequestedAtUtc { get; private set; }

    /// <summary>Gets the current status.</summary>
    public UnlinkRequestStatus Status { get; private set; }

    /// <summary>Gets when the requester escalated to an admin, UTC.</summary>
    public DateTime? EscalatedAtUtc { get; private set; }

    /// <summary>Gets the user who closed the request, if closed.</summary>
    public Guid? ResolvedById { get; private set; }

    /// <summary>Gets when the request was closed, UTC.</summary>
    public DateTime? ResolvedAtUtc { get; private set; }

    private UnlinkRequest(Guid id) : base(id)
    {
    }

    /// <summary>Opens a request.</summary>
    public static UnlinkRequest Create(Guid id, Guid linkId, Guid requestedById, LinkSide side, DateTime nowUtc) =>
        new(id)
        {
            LinkId = linkId,
            RequestedById = requestedById,
            RequestedBySide = side,
            RequestedAtUtc = nowUtc,
            Status = UnlinkRequestStatus.Pending,
        };

    /// <summary>Gets a value indicating whether the request is still open.</summary>
    public bool IsOpen => Status is UnlinkRequestStatus.Pending or UnlinkRequestStatus.OverrideRequested;

    /// <summary>Gets the first moment the requester may escalate.</summary>
    public DateTime EscalationAvailableAtUtc(int waitDays) => RequestedAtUtc.AddDays(waitDays);

    /// <summary>The other side confirms; the caller then revokes the link.</summary>
    public Result Confirm(Guid actorId, LinkSide actorSide, DateTime nowUtc) =>
        RespondAsOtherSide(actorId, actorSide, nowUtc, UnlinkRequestStatus.Confirmed);

    /// <summary>The other side declines; the link stays active and no override is possible.</summary>
    public Result Decline(Guid actorId, LinkSide actorSide, DateTime nowUtc) =>
        RespondAsOtherSide(actorId, actorSide, nowUtc, UnlinkRequestStatus.Declined);

    /// <summary>The requester withdraws the request.</summary>
    public Result Cancel(Guid actorId, DateTime nowUtc)
    {
        if (!IsOpen)
        {
            return Result.Failure(SupportLinkErrors.UnlinkInvalidStatus);
        }

        if (actorId != RequestedById)
        {
            return Result.Failure(SupportLinkErrors.Forbidden);
        }

        Close(UnlinkRequestStatus.Cancelled, actorId, nowUtc);
        return Result.Success();
    }

    /// <summary>The requester asks an admin, only after <paramref name="waitDays"/> without action.</summary>
    public Result Escalate(Guid actorId, DateTime nowUtc, int waitDays)
    {
        if (Status != UnlinkRequestStatus.Pending)
        {
            return Result.Failure(SupportLinkErrors.UnlinkInvalidStatus);
        }

        if (actorId != RequestedById)
        {
            return Result.Failure(SupportLinkErrors.Forbidden);
        }

        if (nowUtc < EscalationAvailableAtUtc(waitDays))
        {
            return Result.Failure(SupportLinkErrors.EscalationTooEarly);
        }

        Status = UnlinkRequestStatus.OverrideRequested;
        EscalatedAtUtc = nowUtc;
        return Result.Success();
    }

    /// <summary>Admin completes an escalated request; the caller then revokes the link.</summary>
    public Result CompleteByAdmin(Guid adminId, DateTime nowUtc) =>
        ResolveByAdmin(adminId, nowUtc, UnlinkRequestStatus.CompletedByAdmin);

    /// <summary>Admin rejects an escalated request; the link stays active.</summary>
    public Result RejectByAdmin(Guid adminId, DateTime nowUtc) =>
        ResolveByAdmin(adminId, nowUtc, UnlinkRequestStatus.RejectedByAdmin);

    private Result RespondAsOtherSide(Guid actorId, LinkSide actorSide, DateTime nowUtc, UnlinkRequestStatus target)
    {
        if (Status != UnlinkRequestStatus.Pending)
        {
            return Result.Failure(SupportLinkErrors.UnlinkInvalidStatus);
        }

        if (actorSide == RequestedBySide || actorId == RequestedById)
        {
            return Result.Failure(SupportLinkErrors.Forbidden);
        }

        Close(target, actorId, nowUtc);
        return Result.Success();
    }

    private Result ResolveByAdmin(Guid adminId, DateTime nowUtc, UnlinkRequestStatus target)
    {
        if (Status != UnlinkRequestStatus.OverrideRequested)
        {
            return Result.Failure(SupportLinkErrors.UnlinkInvalidStatus);
        }

        Close(target, adminId, nowUtc);
        return Result.Success();
    }

    private void Close(UnlinkRequestStatus status, Guid actorId, DateTime nowUtc)
    {
        Status = status;
        ResolvedById = actorId;
        ResolvedAtUtc = nowUtc;
    }
}
