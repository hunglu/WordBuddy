namespace WordBuddy.Identity.Infrastructure.Settings;

/// <summary>JWT token generation settings, bound from the <c>Jwt</c> configuration section.</summary>
public sealed class JwtSettings
{
    /// <summary>The signing secret. Must be at least 32 characters (HS256).</summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>The token issuer claim value.</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>How long an issued token remains valid, in minutes.</summary>
    public int ExpiryMinutes { get; init; } = 60;
}
