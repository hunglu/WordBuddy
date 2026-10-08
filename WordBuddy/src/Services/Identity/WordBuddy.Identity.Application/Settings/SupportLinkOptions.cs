namespace WordBuddy.Identity.Application.Settings;

/// <summary>Support-link settings, bound from the <c>SupportLinks</c> configuration section.</summary>
public sealed class SupportLinkOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "SupportLinks";

    /// <summary>Days without action before the requester may ask an admin to override an unlink.</summary>
    public int UnlinkOverrideWaitDays { get; init; } = 7;

    /// <summary>Days an invitation stays valid.</summary>
    public int InvitationExpiryDays { get; init; } = 7;

    /// <summary>Absolute expiry of the cached link list, in minutes.</summary>
    public int CacheMinutes { get; init; } = 5;
}
