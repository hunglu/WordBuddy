using WordBuddy.Identity.Domain;

namespace WordBuddy.Identity.Application.Interfaces;

/// <summary>Issues signed JWT access tokens for authenticated users.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Builds a signed JWT for <paramref name="user"/>. Returns the token and its expiry.</summary>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
