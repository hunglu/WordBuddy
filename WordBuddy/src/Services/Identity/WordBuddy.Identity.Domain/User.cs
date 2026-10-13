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
    public string DisplayName { get; private set; }

    /// <summary>Gets the salted password hash. Never exposed outside <c>Identity.Infrastructure</c>.</summary>
    public string PasswordHash { get; private set; }

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

    /// <summary>Gets the optional date of birth. Never logged, never put in events or tokens (only the <see cref="AgeBand"/> is shared).</summary>
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>Minimum display name length.</summary>
    public const int DisplayNameMinLength = 2;

    /// <summary>Maximum display name length.</summary>
    public const int DisplayNameMaxLength = 50;

    /// <summary>Youngest accepted age in years.</summary>
    public const int MinAgeYears = 3;

    /// <summary>Oldest accepted age in years.</summary>
    public const int MaxAgeYears = 120;

    /// <summary>Age at which a person is an adult.</summary>
    public const int AdultAgeYears = 18;

    /// <summary>Youngest age an Adult account may have.</summary>
    public const int MinAdultAccountAgeYears = 13;

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

    /// <summary>
    /// Sets or clears the date of birth. Rules: not in the future; age 3 to 120; a Child account must be
    /// under 18 and an Adult account 13 or older. Null clears.
    /// </summary>
    public Result SetDateOfBirth(DateOnly? dateOfBirth, DateOnly today)
    {
        if (dateOfBirth is null)
        {
            DateOfBirth = null;
            return Result.Success();
        }

        if (dateOfBirth.Value > today)
        {
            return Result.Failure(UserErrors.DateOfBirthInvalid);
        }

        int age = AgeOn(dateOfBirth.Value, today);
        if (age < MinAgeYears || age > MaxAgeYears)
        {
            return Result.Failure(UserErrors.DateOfBirthInvalid);
        }

        if ((AgeGroup == AgeGroup.Child && age >= AdultAgeYears)
            || (AgeGroup == AgeGroup.Adult && age < MinAdultAccountAgeYears))
        {
            return Result.Failure(UserErrors.DateOfBirthAgeGroupMismatch);
        }

        DateOfBirth = dateOfBirth;
        return Result.Success();
    }

    /// <summary>Sets the display name (trimmed, 2 to 50 characters).</summary>
    public Result SetDisplayName(string name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length < DisplayNameMinLength || trimmed.Length > DisplayNameMaxLength)
        {
            return Result.Failure(UserErrors.DisplayNameLength);
        }

        DisplayName = trimmed;
        return Result.Success();
    }

    /// <summary>Replaces the password hash. The caller verifies the current password and hashes the new one.</summary>
    public Result ChangePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
        {
            return Result.Failure(UserErrors.PasswordTooWeak);
        }

        PasswordHash = newHash;
        return Result.Success();
    }

    /// <summary>The age band on <paramref name="today"/>, or null when no date of birth is set.</summary>
    public AgeBand? GetAgeBand(DateOnly today)
    {
        if (DateOfBirth is null)
        {
            return null;
        }

        int age = AgeOn(DateOfBirth.Value, today);
        return age switch
        {
            < 7 => AgeBand.Under7,
            < 10 => AgeBand.Age7To9,
            < 13 => AgeBand.Age10To12,
            < AdultAgeYears => AgeBand.Age13To17,
            _ => AgeBand.Adult,
        };
    }

    private static int AgeOn(DateOnly dateOfBirth, DateOnly today)
    {
        int age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.AddYears(age) > today)
        {
            age--;
        }

        return age;
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
