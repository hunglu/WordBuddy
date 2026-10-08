using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Api.Models;

/// <summary>Body of <c>POST /api/auth/support-links/invitations</c>.</summary>
/// <param name="InviteAs">Learner (invite a supporter) or Supporter (offer to support a learner).</param>
/// <param name="Relationship">Optional label. No effect on permissions.</param>
public sealed record CreateInvitationRequest(InvitationSide InviteAs, SupportRelationship? Relationship);

/// <summary>Body of <c>POST /api/auth/support-links/accept</c>: exactly one of code or token.</summary>
public sealed record AcceptInvitationRequest(string? Code, string? Token);

/// <summary>Body of admin unlink actions.</summary>
/// <param name="Reason">Required, max 500 characters. Stored in the audit log.</param>
public sealed record AdminReasonRequest(string Reason);

/// <summary>Body of <c>POST /api/auth/admin/support-links/handover</c>.</summary>
public sealed record HandoverPrimaryRequest(Guid LearnerId, Guid NewPrimaryLinkId, string Reason);

/// <summary>Body of <c>PUT /api/auth/profile</c>. Null clears the value.</summary>
public sealed record UpdateProfileRequest(string? Alias, string? AvatarId);
