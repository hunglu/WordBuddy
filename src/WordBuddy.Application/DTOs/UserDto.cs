using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.DTOs;

/// <summary>A lightweight projection of a user account.</summary>
public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    AgeGroup AgeGroup,
    Level Level);
