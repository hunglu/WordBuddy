using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.DTOs;

/// <summary>Carries the data required to register a new user account.</summary>
public sealed record RegisterUserDto(
    string Email,
    string Password,
    string DisplayName,
    AgeGroup AgeGroup);
