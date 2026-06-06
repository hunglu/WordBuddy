using WordBuddy.Domain.Enums;

namespace WordBuddy.API.Models.Requests;

/// <summary>Request body for registering a new user account.</summary>
public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string DisplayName,
    AgeGroup AgeGroup);
