using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.DTOs;

/// <summary>A lightweight, non-sensitive projection of a <see cref="User"/>.</summary>
/// <param name="Id">User id.</param>
/// <param name="Email">Sign-in email (own account only).</param>
/// <param name="DisplayName">Display name (own account only).</param>
/// <param name="AgeGroup">Child or Adult.</param>
/// <param name="IsAdmin">Admin flag.</param>
/// <param name="Alias">Optional public alias.</param>
/// <param name="AvatarId">Optional avatar id from the fixed catalog.</param>
/// <param name="TimeZoneId">IANA time zone id, or null when unset.</param>
/// <param name="IsGuardian">Whether the account holds the Guardian role.</param>
/// <param name="DateOfBirth">Own date of birth, or null when unset. Shown to the owner only.</param>
/// <param name="AgeBand">Age band derived from the date of birth, or null.</param>
/// <param name="HasActiveSupporter">Whether the user has at least one active supporter. Child learning features need this.</param>
public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    AgeGroup AgeGroup,
    bool IsAdmin,
    string? Alias,
    string? AvatarId,
    bool HasActiveSupporter,
    string? TimeZoneId = null,
    bool IsGuardian = false,
    DateOnly? DateOfBirth = null,
    AgeBand? AgeBand = null)
{
    /// <summary>Maps a user and its supporter state to the DTO.</summary>
    public static UserDto From(User user, bool hasActiveSupporter, DateOnly? today = null) =>
        new(user.Id, user.Email, user.DisplayName, user.AgeGroup, user.IsAdmin, user.Alias, user.AvatarId, hasActiveSupporter, user.TimeZoneId, user.IsGuardian,
            user.DateOfBirth, user.GetAgeBand(today ?? DateOnly.FromDateTime(DateTime.UtcNow)));
}
