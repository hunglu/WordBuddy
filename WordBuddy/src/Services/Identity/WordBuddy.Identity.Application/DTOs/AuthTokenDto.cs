namespace WordBuddy.Identity.Application.DTOs;

/// <summary>The JWT issued after a successful registration or login.</summary>
public sealed record AuthTokenDto(string Token, DateTime ExpiresAtUtc, UserDto User);
