namespace WordBuddy.Shared.Contracts.Profiles;

/// <summary>
/// Published by Identity after a profile change: register, alias or avatar, time zone, Guardian role.
/// A full snapshot, so consumers can upsert it and ignore older events by <paramref name="OccurredAtUtc"/>.
/// Payload rule: no email, no password. <paramref name="DisplayName"/> is null for child accounts.
/// </summary>
/// <param name="UserId">The account.</param>
/// <param name="AgeGroup">"Child" or "Adult".</param>
/// <param name="Alias">Optional public alias.</param>
/// <param name="AvatarId">Optional avatar id from the fixed catalog.</param>
/// <param name="DisplayName">Display name. Always null for a child.</param>
/// <param name="TimeZoneId">IANA time zone id, or null when unset.</param>
/// <param name="IsGuardian">Whether the account holds the Guardian role.</param>
/// <param name="OccurredAtUtc">When the change happened, UTC.</param>
public sealed record LearnerProfileUpdated(
    Guid UserId,
    string AgeGroup,
    string? Alias,
    string? AvatarId,
    string? DisplayName,
    string? TimeZoneId,
    bool IsGuardian,
    DateTime OccurredAtUtc);
