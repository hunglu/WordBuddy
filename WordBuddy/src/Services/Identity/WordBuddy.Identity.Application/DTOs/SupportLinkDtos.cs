using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.DTOs;

/// <summary>A support link as seen by one of its parties. Names are public names (alias first; never a child display name).</summary>
public sealed record SupportLinkDto(
    Guid Id,
    Guid LearnerId,
    string LearnerName,
    string? LearnerAvatarId,
    Guid SupporterId,
    string SupporterName,
    string? SupporterAvatarId,
    bool IsPrimary,
    SupportRelationship? Relationship,
    SupportLinkStatus Status,
    DateTime CreatedAtUtc,
    UnlinkRequestDto? UnlinkRequest);

/// <summary>The open unlink request of a link, as seen by the caller.</summary>
/// <param name="Id">Request id.</param>
/// <param name="Status">Pending or OverrideRequested.</param>
/// <param name="RequestedByMe">Whether the caller made the request (only the requester may cancel or escalate).</param>
/// <param name="RequestedAtUtc">When it was requested.</param>
/// <param name="EscalationAvailableAtUtc">First moment the requester may ask an admin.</param>
public sealed record UnlinkRequestDto(
    Guid Id,
    UnlinkRequestStatus Status,
    bool RequestedByMe,
    DateTime RequestedAtUtc,
    DateTime EscalationAvailableAtUtc);

/// <summary>An open invitation created by the caller. The code is shown only once, at creation.</summary>
public sealed record InvitationDto(Guid Id, InvitationSide CreatorSide, SupportRelationship? Relationship, DateTime ExpiresAtUtc);

/// <summary>A new invitation: the plain code and token are returned once and never stored.</summary>
public sealed record CreatedInvitationDto(Guid Id, string Code, string Token, DateTime ExpiresAtUtc);

/// <summary>All links of the caller.</summary>
/// <param name="AsLearner">Links where the caller is the learner.</param>
/// <param name="AsSupporter">Links where the caller is the supporter.</param>
/// <param name="ManagedForChildren">Other links of children for whom the caller is the Primary supporter.</param>
/// <param name="OpenInvitations">Pending invitations the caller created.</param>
/// <param name="HasActiveSupporter">Whether the caller has at least one active supporter.</param>
public sealed record MySupportLinksDto(
    IReadOnlyList<SupportLinkDto> AsLearner,
    IReadOnlyList<SupportLinkDto> AsSupporter,
    IReadOnlyList<SupportLinkDto> ManagedForChildren,
    IReadOnlyList<InvitationDto> OpenInvitations,
    bool HasActiveSupporter);

/// <summary>Admin view of an unlink request. Ids only.</summary>
public sealed record AdminUnlinkRequestDto(
    Guid Id,
    Guid LinkId,
    Guid LearnerId,
    Guid SupporterId,
    Guid RequestedById,
    LinkSide RequestedBySide,
    UnlinkRequestStatus Status,
    DateTime RequestedAtUtc,
    DateTime? EscalatedAtUtc);

/// <summary>Admin view of a link. Ids only.</summary>
public sealed record AdminSupportLinkDto(
    Guid Id,
    Guid LearnerId,
    Guid SupporterId,
    bool IsPrimary,
    SupportRelationship? Relationship,
    SupportLinkStatus Status,
    DateTime CreatedAtUtc);
