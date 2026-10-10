namespace WordBuddy.Identity.Application.Settings;

/// <summary>Learning-group settings, bound from the <c>LearnerGroups</c> configuration section.</summary>
public sealed class LearnerGroupOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LearnerGroups";

    /// <summary>Most groups one owner may have.</summary>
    public int MaxGroupsPerOwner { get; init; } = 20;

    /// <summary>Most pending plus active members in one group.</summary>
    public int MaxMembersPerGroup { get; init; } = 40;

    /// <summary>Absolute expiry of the cached group detail, in minutes.</summary>
    public int CacheMinutes { get; init; } = 5;
}
