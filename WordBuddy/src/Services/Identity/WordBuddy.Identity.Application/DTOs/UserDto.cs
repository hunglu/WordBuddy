using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.DTOs;

/// <summary>A lightweight, non-sensitive projection of a <see cref="User"/>.</summary>
public sealed record UserDto(Guid Id, string Email, string DisplayName, AgeGroup AgeGroup, bool IsAdmin);
