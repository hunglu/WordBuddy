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
/// <param name="HasActiveSupporter">Whether the user has at least one active supporter. Child learning features need this.</param>
public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    AgeGroup AgeGroup,
    bool IsAdmin,
    string? Alias,
    string? AvatarId,
    bool HasActiveSupporter)
{
    /// <summary>Maps a user and its supporter state to the DTO.</summary>
    public static UserDto From(User user, bool hasActiveSupporter) =>
        new(user.Id, user.Email, user.DisplayName, user.AgeGroup, user.IsAdmin, user.Alias, user.AvatarId, hasActiveSupporter);
}
