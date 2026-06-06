namespace WordBuddy.Application.DTOs;

/// <summary>Carries the credentials used to authenticate a user.</summary>
public sealed record LoginCredentialsDto(string Email, string Password);
