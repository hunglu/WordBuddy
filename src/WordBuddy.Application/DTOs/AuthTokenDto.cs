namespace WordBuddy.Application.DTOs;

/// <summary>The JWT token issued after a successful authentication or registration.</summary>
public sealed record AuthTokenDto(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    string DisplayName,
    string Email);
