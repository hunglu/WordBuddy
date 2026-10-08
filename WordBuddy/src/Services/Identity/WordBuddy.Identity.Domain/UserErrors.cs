using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain;

/// <summary>Typed errors of the <see cref="User"/> profile. Descriptions hold no personal data.</summary>
public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user was not found.");
    public static readonly Error AliasLength = Error.Validation("User.AliasLength", "The alias must be 3 to 20 characters.");
    public static readonly Error AliasRevealsIdentity = Error.Validation("User.AliasRevealsIdentity", "A child alias must not be the display name or the email name.");
    public static readonly Error AliasTaken = Error.Conflict("User.AliasTaken", "This alias is already taken.");
    public static readonly Error UnknownAvatar = Error.Validation("User.UnknownAvatar", "The avatar is not in the catalog.");
}
