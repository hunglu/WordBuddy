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

/// <summary>Body of <c>PUT /api/auth/profile/time-zone</c>.</summary>
/// <param name="TimeZoneId">IANA id, for example <c>Asia/Ho_Chi_Minh</c>; null clears.</param>
public sealed record UpdateTimeZoneRequest(string? TimeZoneId);

/// <summary>Body of <c>PUT api/auth/profile/language</c>.</summary>
/// <param name="Language">Language code, for example <c>vi</c>; null clears.</param>
public sealed record UpdateLanguageRequest(string? Language);

/// <summary>Body of <c>PUT /api/auth/admin/users/{id}/guardian</c>.</summary>
/// <param name="IsGuardian">True grants the Guardian role (adults only); false removes it.</param>
public sealed record SetGuardianRequest(bool IsGuardian);

/// <summary>Body of <c>PUT /api/auth/admin/users/{id}/moderator</c>.</summary>
/// <param name="IsModerator">True grants the Moderator role (adult, non-admin accounts only); false removes it.</param>
public sealed record SetModeratorRequest(bool IsModerator);

/// <summary>Body of <c>PUT /api/auth/profile/date-of-birth</c>.</summary>
/// <param name="DateOfBirth">ISO date (<c>yyyy-MM-dd</c>); null clears.</param>
public sealed record UpdateDateOfBirthRequest(DateOnly? DateOfBirth);

/// <summary>Body of <c>PUT /api/auth/profile/display-name</c>.</summary>
public sealed record UpdateDisplayNameRequest(string DisplayName);

/// <summary>Body of <c>POST /api/auth/profile/password</c>.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
