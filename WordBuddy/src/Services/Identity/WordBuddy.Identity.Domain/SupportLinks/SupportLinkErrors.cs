using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain.SupportLinks;

/// <summary>
/// Typed errors of the support-link feature. Codes ending in <c>.Forbidden</c> map to HTTP 403.
/// Descriptions hold no names, emails or child data.
/// </summary>
public static class SupportLinkErrors
{
    public static readonly Error LinkNotFound = Error.NotFound("SupportLink.NotFound", "The support link was not found.");
    public static readonly Error InvitationNotFound = Error.NotFound("SupportLink.InvitationNotFound", "The invitation was not found.");
    public static readonly Error UnlinkRequestNotFound = Error.NotFound("SupportLink.UnlinkRequestNotFound", "No open unlink request was found.");
    public static readonly Error InvitationExpired = Error.Validation("SupportLink.InvitationExpired", "The invitation has expired.");
    public static readonly Error InvitationAlreadyUsed = Error.Conflict("SupportLink.InvitationAlreadyUsed", "The invitation is no longer open.");
    public static readonly Error SelfLink = Error.Validation("SupportLink.SelfLink", "You cannot support yourself.");
    public static readonly Error SupporterMustBeAdult = Error.Validation("SupportLink.SupporterMustBeAdult", "A supporter must be an adult account.");
    public static readonly Error AlreadyLinked = Error.Conflict("SupportLink.AlreadyLinked", "This supporter is already linked to this learner.");
    public static readonly Error InvalidStatus = Error.Conflict("SupportLink.InvalidStatus", "The link is not in a state that allows this action.");
    public static readonly Error PrimaryCannotBeUnlinked = Error.Validation("SupportLink.PrimaryCannotBeUnlinked", "The Primary supporter link cannot be unlinked. Ask an admin for a Primary handover.");
    public static readonly Error UnlinkAlreadyOpen = Error.Conflict("SupportLink.UnlinkAlreadyOpen", "An unlink request is already open for this link.");
    public static readonly Error UnlinkInvalidStatus = Error.Conflict("SupportLink.UnlinkInvalidStatus", "The unlink request is not in a state that allows this action.");
    public static readonly Error EscalationTooEarly = Error.Validation("SupportLink.EscalationTooEarly", "The waiting time before asking an admin has not passed yet.");
    public static readonly Error PrimaryChildOnly = Error.Validation("SupportLink.PrimaryChildOnly", "Only child learners have a Primary supporter.");
    public static readonly Error Forbidden = Error.Failure("SupportLink.Forbidden", "You are not allowed to perform this action on this link.");
    public static readonly Error ChildForbidden = Error.Failure("SupportLink.ChildForbidden", "A child account cannot perform this action. The Primary supporter acts for the child.");
}
