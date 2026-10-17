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
    public static readonly Error UnknownTimeZone = Error.Validation("User.UnknownTimeZone", "The time zone id is not known.");
    public static readonly Error InvalidLanguage = Error.Validation("User.InvalidLanguage", "The language code is not valid.");
    public static readonly Error GuardianMustBeAdult = Error.Validation("User.GuardianMustBeAdult", "Only an adult account can hold the Guardian role.");
    public static readonly Error ModeratorMustBeAdult = Error.Validation("User.ModeratorMustBeAdult", "Only an adult account can hold the Moderator role.");
    public static readonly Error ModeratorNotForAdmin = Error.Validation("User.ModeratorNotForAdmin", "An admin already has every right and cannot be a moderator.");
    public static readonly Error AdminCannotBeDeleted = Error.Conflict("User.AdminCannotBeDeleted", "An admin account cannot be deleted.");
    public static readonly Error DateOfBirthInvalid = Error.Validation("User.DateOfBirthInvalid", "The date of birth is not valid.");
    public static readonly Error DateOfBirthAgeGroupMismatch = Error.Validation("User.DateOfBirthAgeGroupMismatch", "The date of birth does not match the account age group.");
    public static readonly Error DisplayNameLength = Error.Validation("User.DisplayNameLength", "The display name must be 2 to 50 characters.");
    public static readonly Error WrongCurrentPassword = Error.Validation("User.WrongCurrentPassword", "The current password is incorrect.");
    public static readonly Error PasswordTooWeak = Error.Validation("User.PasswordTooWeak", "The new password is not valid.");
}
