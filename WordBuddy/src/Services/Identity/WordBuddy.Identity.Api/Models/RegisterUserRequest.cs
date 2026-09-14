using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Api.Models;

/// <summary>Request body for registering a new user account.</summary>
public sealed record RegisterUserRequest(string Email, string Password, string DisplayName, AgeGroup AgeGroup);
