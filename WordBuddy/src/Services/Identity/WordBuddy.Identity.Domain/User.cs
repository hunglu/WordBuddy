using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Domain;

/// <summary>A learner or admin account.</summary>
public sealed class User : Entity
{
    /// <summary>Minimum alias length.</summary>
    public const int AliasMinLength = 3;

    /// <summary>Maximum alias length.</summary>
    public const int AliasMaxLength = 20;

    /// <summary>Gets the account's email address, used to sign in.</summary>
    public string Email { get; }

    /// <summary>Gets the display name shown in the UI.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the salted password hash. Never exposed outside <c>Identity.Infrastructure</c>.</summary>
    public string PasswordHash { get; }

    /// <summary>Gets the account's age group, used to enforce content restrictions.</summary>
    public AgeGroup AgeGroup { get; }

    /// <summary>Gets a value indicating whether this account has administrative privileges.</summary>
    public bool IsAdmin { get; }

    /// <summary>Gets the optional public alias (3-20 chars, unique). Shown to linked users instead of the name.</summary>
    public string? Alias { get; private set; }

    /// <summary>Gets the optional avatar id from <see cref="AvatarCatalog"/>.</summary>
    public string? AvatarId { get; private set; }

    /// <summary>Gets the optional IANA time zone id (for example <c>Asia/Ho_Chi_Minh</c>). Defines the learner's local day.</summary>
    public string? TimeZoneId { get; private set; }

    /// <summary>Gets a value indicating whether this adult account holds the Guardian role (can allow challenges for linked children).</summary>
    public bool IsGuardian { get; private set; }

    /// <summary>Maximum time zone id length.</summary>
    public const int TimeZoneIdMaxLength = 64;

    public User(Guid id, string email, string displayName, string passwordHash, AgeGroup ageGroup, bool isAdmin)
        : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        AgeGroup = ageGroup;
        IsAdmin = isAdmin;
    }

    /// <summary>
    /// Sets alias and avatar (null clears). Rules: alias 3-20 chars; avatar from the catalog; a
    /// child alias must not equal the display name or the email local part (case-insensitive).
    /// Uniqueness is checked by the caller.
    /// </summary>
    public Result SetAliasAndAvatar(string? alias, string? avatarId)
    {
        string? trimmed = string.IsNullOrWhiteSpace(alias) ? null : alias.Trim();

        if (trimmed is not null && (trimmed.Length < AliasMinLength || trimmed.Length > AliasMaxLength))
        {
            return Result.Failure(UserErrors.AliasLength);
        }

        if (trimmed is not null && AgeGroup == AgeGroup.Child && RevealsIdentity(trimmed))
        {
            return Result.Failure(UserErrors.AliasRevealsIdentity);
        }

        if (avatarId is not null && !AvatarCatalog.Contains(avatarId))
        {
            return Result.Failure(UserErrors.UnknownAvatar);
        }

        Alias = trimmed;
        AvatarId = avatarId;
        return Result.Success();
    }

    /// <summary>Whether <paramref name="timeZoneId"/> is a known IANA (or system) time zone id of an allowed length.</summary>
    public static bool IsKnownTimeZone(string timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId)
        && timeZoneId.Trim().Length <= TimeZoneIdMaxLength
        && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out _);

    /// <summary>Sets the IANA time zone (null clears). Unknown ids return <see cref="UserErrors.UnknownTimeZone"/>.</summary>
    public Result SetTimeZone(string? timeZoneId)
    {
        string? trimmed = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        if (trimmed is not null && !IsKnownTimeZone(trimmed))
        {
            return Result.Failure(UserErrors.UnknownTimeZone);
        }

        TimeZoneId = trimmed;
        return Result.Success();
    }

    /// <summary>Grants or removes the Guardian role. Only adult accounts can hold it.</summary>
    public Result SetGuardian(bool isGuardian)
    {
        if (isGuardian && AgeGroup != AgeGroup.Adult)
        {
            return Result.Failure(UserErrors.GuardianMustBeAdult);
        }

        IsGuardian = isGuardian;
        return Result.Success();
    }

    /// <summary>Name shown to linked users: alias, else display name for adults, else a neutral label for children.</summary>
    public string PublicName => Alias ?? (AgeGroup == AgeGroup.Child ? "Child learner" : DisplayName);

    private bool RevealsIdentity(string alias)
    {
        int at = Email.IndexOf('@');
        string localPart = at > 0 ? Email[..at] : Email;

        return string.Equals(alias, DisplayName.Trim(), StringComparison.OrdinalIgnoreCase)
            || string.Equals(alias, localPart, StringComparison.OrdinalIgnoreCase);
    }
}
