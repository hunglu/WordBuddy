namespace WordBuddy.Identity.Application.DTOs;

/// <summary>An adult account row on the admin role screen. Never holds child data.</summary>
/// <param name="Id">User id.</param>
/// <param name="Email">Sign-in email.</param>
/// <param name="DisplayName">Display name.</param>
/// <param name="IsAdmin">Admin flag.</param>
/// <param name="IsModerator">Moderator role flag.</param>
/// <param name="IsGuardian">Guardian role flag.</param>
public sealed record AdminUserDto(Guid Id, string Email, string DisplayName, bool IsAdmin, bool IsModerator, bool IsGuardian);
