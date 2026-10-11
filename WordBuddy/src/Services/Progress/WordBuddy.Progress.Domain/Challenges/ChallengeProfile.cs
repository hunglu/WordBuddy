namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>
/// Local copy of the Identity profile fields the challenges need, kept by <c>LearnerProfileUpdated</c>.
/// No email. A child never has a display name here, even if one is sent.
/// </summary>
public sealed class ChallengeProfile
{
    /// <summary>Neutral name shown for a child without an alias.</summary>
    public const string ChildFallbackName = "Child learner";

    /// <summary>Neutral name shown for an adult without alias or display name.</summary>
    public const string AdultFallbackName = "Learner";

    /// <summary>The account (primary key, plain field).</summary>
    public Guid UserId { get; private set; }

    /// <summary>Child or Adult.</summary>
    public AgeGroup AgeGroup { get; private set; }

    /// <summary>Optional public alias.</summary>
    public string? Alias { get; private set; }

    /// <summary>Optional avatar id from the fixed catalog.</summary>
    public string? AvatarId { get; private set; }

    /// <summary>Display name, adults only.</summary>
    public string? DisplayName { get; private set; }

    /// <summary>IANA time zone id, or null (UTC is used).</summary>
    public string? TimeZoneId { get; private set; }

    /// <summary>Whether the account holds the Guardian role.</summary>
    public bool IsGuardian { get; private set; }

    /// <summary>Time of the last applied event, UTC.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private ChallengeProfile()
    {
    }

    /// <summary>Name shown on a board: alias, else display name (adult), else a neutral label.</summary>
    public string PublicName =>
        !string.IsNullOrWhiteSpace(Alias) ? Alias
        : AgeGroup == AgeGroup.Child ? ChildFallbackName
        : DisplayName ?? AdultFallbackName;

    /// <summary>Creates the profile from the first event seen.</summary>
    public static ChallengeProfile Create(
        Guid userId, AgeGroup ageGroup, string? alias, string? avatarId, string? displayName, string? timeZoneId, bool isGuardian, DateTime occurredAtUtc)
    {
        ChallengeProfile profile = new() { UserId = userId, UpdatedAtUtc = DateTime.MinValue };
        profile.Apply(ageGroup, alias, avatarId, displayName, timeZoneId, isGuardian, occurredAtUtc);
        return profile;
    }

    /// <summary>
    /// Applies a snapshot. Returns <see langword="false"/> (no change) for an event older than the last applied one.
    /// A replay of the same time applies again with the same values (idempotent).
    /// </summary>
    public bool Apply(
        AgeGroup ageGroup, string? alias, string? avatarId, string? displayName, string? timeZoneId, bool isGuardian, DateTime occurredAtUtc)
    {
        if (occurredAtUtc < UpdatedAtUtc)
        {
            return false;
        }

        AgeGroup = ageGroup;
        Alias = alias;
        AvatarId = avatarId;
        DisplayName = ageGroup == AgeGroup.Child ? null : displayName;
        TimeZoneId = timeZoneId;
        IsGuardian = isGuardian;
        UpdatedAtUtc = occurredAtUtc;
        return true;
    }
}
